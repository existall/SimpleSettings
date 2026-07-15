using System.Reflection;
using ExistForAll.SimpleSettings.Conversion;
using ExistForAll.SimpleSettings.Core.Reflection;

namespace ExistForAll.SimpleSettings
{
	// Everything needed to populate one settings interface that is constant across instances, computed once
	// and cached per type: the section name (resolved once, not per property/binder) and the per-property
	// plan (resolved key, default value, and precomputed conversion).
	internal sealed class SettingsPlan
	{
		private readonly Type _settingsType;
		private readonly SettingsOptions _options;
		private string? _sectionName;

		public SettingsPlan(Type settingsType, SettingsOptions options, PropertyPlan[] properties, Type? objectValidatorType)
		{
			_settingsType = settingsType;
			_options = options;
			Properties = properties;
			ObjectValidatorType = objectValidatorType;
			HasValidators = objectValidatorType is not null || AnyPropertyHasValidator(properties);
		}

		private static bool AnyPropertyHasValidator(PropertyPlan[] properties)
		{
			for (var i = 0; i < properties.Length; i++)
			{
				if (properties[i].ValidatorType is not null)
					return true;
			}

			return false;
		}

		// Resolved lazily and cached: a builder with no binders (e.g. the cold assembly scan) never reads it,
		// so it never pays the section-name attribute lookup. The value is deterministic, so the racy set is
		// harmless — concurrent readers compute the same string.
		public string SectionName => _sectionName ??= _settingsType.GetSectionName(_options);

		public PropertyPlan[] Properties { get; }

		// The [SettingsValidator] object-level validator declared on the settings interface, resolved once at
		// plan build (null when none is declared).
		public Type? ObjectValidatorType { get; }

		// Computed once at plan build so the post-populate hook can short-circuit with a single field read
		// before allocating anything on the validator-free warm path.
		public bool HasValidators { get; }
	}

	// A readonly struct held inline in SettingsPlan.Properties: the whole per-type plan is then one array
	// allocation plus the section string, rather than an object per property (matters at scan scale).
	internal readonly struct PropertyPlan
	{
		public PropertyPlan(PropertyInfo property, string key, object? defaultValue, PropertyConversion conversion, Type? validatorType)
		{
			Property = property;
			Key = key;
			DefaultValue = defaultValue;
			Conversion = conversion;
			ValidatorType = validatorType;
		}

		public PropertyInfo Property { get; }

		public string Key { get; }

		public object? DefaultValue { get; }

		public PropertyConversion Conversion { get; }

		// The [SettingsProperty(ValidatorType)] validator for this property, resolved once at plan build.
		public Type? ValidatorType { get; }
	}
}
