using System;
using System.Linq;
using System.Reflection;
using ExistForAll.SimpleSettings.Conversion;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal class TypeConverter : ITypeConverter
	{
		public object? ConvertValue(object? value, PropertyInfo propertyInfo, SettingsOptions options)
		{
			var propertyType = propertyInfo.PropertyType;

			if (value == null)
			{
				ValidateNullAcceptance(propertyInfo);

				if (!propertyType.IsEnumerable())
					return propertyType.GetTypeInfo().IsValueType ? Activator.CreateInstance(propertyType) : null;

				var genericType = propertyType.GetTypeInfo().GetGenericArguments().First();
				var method = typeof(Enumerable).GetTypeInfo().GetMethod("Empty")!.MakeGenericMethod(genericType);
				var emptyEnumerable = method.Invoke(null, null);
				return emptyEnumerable;
			}

			var strippedType = StripIfNullable(propertyType);

			var settingsTypeConverter = GetConverter(strippedType, propertyInfo, options);

			return settingsTypeConverter.Convert(value, strippedType);
		}

		private ISettingsTypeConverter GetConverter(Type strippedType, PropertyInfo propertyInfo, SettingsOptions options)
		{
			var attribute = propertyInfo
				.GetCustomAttribute<SettingsPropertyAttribute>();

			if (attribute?.ConverterType == null)
				return options.Converters.First(x => x.CanConvert(strippedType));

			var converter = (ISettingsTypeConverter)Activator.CreateInstance(attribute.ConverterType)!;

			return converter;
		}

		private static Type StripIfNullable(Type type)
		{
			return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ?
				type.GetTypeInfo().GetGenericArguments()[0] :
				type;
		}

		private static void ValidateNullAcceptance(PropertyInfo propertyInfo)
		{
			var attribute = propertyInfo.GetCustomAttribute<SettingsPropertyAttribute>();

			if(attribute == null)
				return;

			if(!attribute.AllowEmpty)
				throw new Exception(Resources.PropertyNotAllowNullMessage(propertyInfo.Name));
		}
	}
}
