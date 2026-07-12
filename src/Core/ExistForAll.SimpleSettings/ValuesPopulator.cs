using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExistForAll.SimpleSettings.Core.Reflection;

namespace ExistForAll.SimpleSettings
{
	internal class ValuesPopulator : IValuesPopulator
	{
		private readonly ITypePropertiesExtractor _typePropertiesExtractor;
		private readonly ITypeConverter _typeConverter;

		public ValuesPopulator() :
			this(new TypePropertiesExtractor(new ConcurrentDictionary<Type, PropertyInfo[]>()), new TypeConverter())
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
            foreach (var property in _typePropertiesExtractor.ExtractTypeProperties(settings))
            {
	            var tempValue = property.GetDefaultValue();
				foreach (var binder in sectionBinders)
				{
					var context = new BindingContext(settings.GetSectionName(options),
						property.GetPropertyName(),
						settings,
						property,
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

				var propertyValue = ConvertPropertyValue(settings, tempValue, property, options);
				property.SetValue(instance, propertyValue);
			}
        }

		private object? ConvertPropertyValue(Type settingsType,
			object? value,
			PropertyInfo property,
			SettingsOptions options)
		{
			try
			{
				var propertyValue = _typeConverter.ConvertValue(value, property, options);

				return propertyValue;
			}
			catch (Exception e)
			{
				throw new SettingsPropertyValueException(settingsType, value, property, e);
			}
		}
	}
}