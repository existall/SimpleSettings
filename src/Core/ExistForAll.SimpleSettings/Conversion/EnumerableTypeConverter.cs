using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExistForAll.SimpleSettings.Core.Reflection;

namespace ExistForAll.SimpleSettings.Conversion
{
	internal class EnumerableTypeConverter : ISettingsTypeConverter
	{
		private readonly SettingsOptions _settingsOptions;
		private readonly TypeConvertersCollections _converters;

		public EnumerableTypeConverter(SettingsOptions settingsOptions, TypeConvertersCollections converters)
		{
			_settingsOptions = settingsOptions;
			_converters = converters;
		}

		public bool CanConvert(Type settingsType)
		{
			return settingsType.IsEnumerable();
		}

		public object Convert(object value, Type settingsType)
		{
			if (value is string stringArray)
			{
				value = stringArray.Split(new[] { _settingsOptions.ArraySplitDelimiter }, StringSplitOptions.RemoveEmptyEntries)
					.ToArray();
			}

			var values = value.GetType().IsArray ? (IEnumerable)value : new[] { value };

			var elementType = settingsType.GetTypeInfo().GetGenericArguments().First();

			var configTypeConverter = _converters.First(x => x.CanConvert(elementType));

			var instance = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
			
			foreach (var item in values)
			{
				var convertedValue = configTypeConverter.Convert(item, elementType);
				instance.Add(convertedValue);
			}

			return instance;
		}
	}
}