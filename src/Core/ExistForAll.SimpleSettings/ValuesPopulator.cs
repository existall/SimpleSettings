using System.Collections.Concurrent;
using System.Reflection;
using ExistForAll.SimpleSettings.Core.Reflection;
using ExistForAll.SimpleSettings.Validations;

namespace ExistForAll.SimpleSettings
{
	internal class ValuesPopulator : IValuesPopulator
	{
		private readonly ITypePropertiesExtractor _typePropertiesExtractor;
		private readonly ITypeConverter _typeConverter;

		// One plan per settings interface, reused across every populate. Instance field (not static): a plan
		// bakes in this builder's SettingsOptions (section formatter, converters, per-property converters), and
		// exactly one ValuesPopulator is created per SettingsBuilder, so Options is fixed for this cache's
		// lifetime. Same reasoning as the TypePropertiesExtractor cache (P2).
		private readonly ConcurrentDictionary<Type, SettingsPlan> _plans = new();

		public ValuesPopulator() :
			this(new TypePropertiesExtractor(), new TypeConverter())
		{
		}

		internal ValuesPopulator(ITypePropertiesExtractor typePropertiesExtractor, ITypeConverter typeConverter)
		{
			_typePropertiesExtractor = typePropertiesExtractor;
			_typeConverter = typeConverter;
		}

		public void PopulateInstanceWithValues(object instance,
			Type settings,
			SettingsOptions options,
			IEnumerable<ISectionBinder> binders)
		{
			var sectionBinders = binders as ISectionBinder[] ?? binders.ToArray();

			var plan = GetOrBuildPlan(settings, options);

			foreach (var propertyPlan in plan.Properties)
			{
				var tempValue = propertyPlan.DefaultValue;

				foreach (var binder in sectionBinders)
				{
					var context = new BindingContext(plan.SectionName,
						propertyPlan.Key,
						settings,
						propertyPlan.Property,
						tempValue);

					try
					{
						binder.BindPropertySettings(context);
						if (context.HasNewValue)
							tempValue = context.NewValue;
					}
					catch (Exception e)
					{
						throw new SettingsBindingException(binder, context, e);
					}
				}

				var propertyValue = ConvertPropertyValue(settings, tempValue, propertyPlan);
				propertyPlan.Property.SetValue(instance, propertyValue);
			}

			// Runs AFTER the property-set loop so cross-property rules see the fully-populated instance.
			RunValidators(instance, plan);
		}

		// Zero-allocation short-circuit for validator-free types: a single field read returns before anything is
		// allocated, keeping the warm populate path byte-identical to the pre-validation build. The error list
		// is allocated lazily only after a validator actually produces an error.
		private static void RunValidators(object instance, SettingsPlan plan)
		{
			if (!plan.HasValidators)
				return;

			List<ValidationError>? errors = null;

			if (plan.ObjectValidatorType is not null)
				errors = InvokeValidator(plan.ObjectValidatorType, instance, errors);

			foreach (var propertyPlan in plan.Properties)
			{
				if (propertyPlan.ValidatorType is null)
					continue;

				var propertyValue = propertyPlan.Property.GetValue(instance);
				errors = InvokeValidator(propertyPlan.ValidatorType, propertyValue, errors);
			}

			// The single aggregate-and-throw entry point shared with the DI-resolved runner: the
			// thrown contract cannot drift between the two paths.
			if (errors is not null)
				SettingsValidationException.ThrowIfAny(errors);
		}

		private static List<ValidationError>? InvokeValidator(Type validatorType, object? target, List<ValidationError>? errors)
		{
			ValidationResult result;
			try
			{
				// The validator type comes from a compile-time attribute; instantiate it and dispatch through the
				// non-generic ISettingsValidator.Validate. ISettingValidation<T>'s default implementation forwards
				// to the author's generic overload, so no reflection over the Validate methods is needed.
				var validator = (ISettingsValidator)Activator.CreateInstance(validatorType)!;
				result = validator.Validate(new ValidationContext(target));
			}
			catch (Exception e)
			{
				// A validator threw instead of returning a result. Surface value-free: the inner may embed a
				// secret the validator read, so it is never chained and the value never enters the message. See S1.
				throw new SettingsValidatorInvocationException(validatorType, e.GetType());
			}

			foreach (var error in result.Errors)
			{
				errors ??= new List<ValidationError>();
				errors.Add(error);
			}

			return errors;
		}

		private SettingsPlan GetOrBuildPlan(Type settings, SettingsOptions options)
		{
			if (_plans.TryGetValue(settings, out var existing))
				return existing;

			var extracted = _typePropertiesExtractor.ExtractTypeProperties(settings);
			var properties = extracted as PropertyInfo[] ?? extracted.ToArray();

			var propertyPlans = new PropertyPlan[properties.Length];
			for (var i = 0; i < properties.Length; i++)
			{
				var property = properties[i];

				// Read [SettingsProperty] once and thread it into key/default/conversion — GetCustomAttribute
				// re-materializes the attribute + its backing array on every call, and this runs per property
				// for every type at cold-scan scale. inherit:true matches the prior key-resolution behavior
				// (a no-op for interface members, which is all settings types are).
				var attribute = property.GetCustomAttribute<SettingsPropertyAttribute>(inherit: true);

				try
				{
					var key = !string.IsNullOrWhiteSpace(attribute?.Name) ? attribute.Name : property.Name;

					propertyPlans[i] = new PropertyPlan(property,
						key,
						attribute?.DefaultValue,
						_typeConverter.CreateConversion(property, attribute, options),
						attribute?.ValidatorType);
				}
				catch (Exception e)
				{
					// Restore the original exception contract: converter-setup failures used to surface inside
					// the per-populate convert try as SettingsPropertyValueException. No bound value exists at
					// plan build, and setup failures describe types/converters, not a value — so nothing sensitive
					// is dropped by the redacting exception (which never carries the value or chains the inner).
					throw new SettingsPropertyValueException(settings, property, e.GetType());
				}
			}

			// Read the object-level [SettingsValidator] once per type at plan build, mirroring the per-property
			// attribute read above — never on the hot populate path.
			var objectValidatorType = settings.GetTypeInfo()
				.GetCustomAttribute<SettingsValidatorAttribute>(inherit: true)?.ValidatorType;

			var plan = new SettingsPlan(settings, options, propertyPlans, objectValidatorType);

			// Concurrent builds would produce equivalent plans, so last-writer-wins is harmless. A build that
			// throws is never cached (we don't reach here), so the next call retries — matching the old
			// re-throw-every-time behavior.
			_plans[settings] = plan;
			return plan;
		}

		private static object? ConvertPropertyValue(Type settingsType, object? value, in PropertyPlan propertyPlan)
		{
			try
			{
				return propertyPlan.Conversion.Convert(value);
			}
			// SettingsPropertyNullException is the value-free "required value missing" signal — the filter skips
			// this catch so it propagates as-is, rather than being redacted into a value-conversion exception.
			catch (Exception e) when (e is not SettingsPropertyNullException)
			{
				// e (and its message) may embed the raw bound value, which could be a secret — never chain it
				// or put the value in the message. Only the failure's type name is surfaced. See S1.
				throw new SettingsPropertyValueException(settingsType, propertyPlan.Property, e.GetType());
			}
		}
	}
}
