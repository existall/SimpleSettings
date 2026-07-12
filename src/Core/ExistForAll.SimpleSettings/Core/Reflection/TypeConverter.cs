using System;
using System.Linq;
using System.Reflection;
using ExistForAll.SimpleSettings.Conversion;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal class TypeConverter : ITypeConverter
	{
		// Resolve everything about a property's conversion up front: which converter handles it, the target
		// type once nullability is stripped, and what a null bound value should yield. Previously all of this
		// (including up to three SettingsPropertyAttribute reads and a linear converter scan) ran on every
		// single populate; now it runs once per type and the result is cached in the settings plan.
		public PropertyConversion CreateConversion(PropertyInfo propertyInfo, SettingsOptions options)
		{
			var propertyType = propertyInfo.PropertyType;
			var attribute = propertyInfo.GetCustomAttribute<SettingsPropertyAttribute>();

			var throwOnNull = attribute != null && !attribute.AllowEmpty;
			var nullResult = CreateNullResult(propertyType);

			var strippedType = StripIfNullable(propertyType);
			var converter = GetConverter(strippedType, attribute, options);

			return new PropertyConversion(converter, strippedType, throwOnNull, nullResult, propertyInfo.Name);
		}

		// The value a null bound value converts to: an empty sequence for IEnumerable<T>, a default instance
		// for a value type, otherwise null. Constant per property, so it is materialized once at plan build.
		private static object? CreateNullResult(Type propertyType)
		{
			if (!propertyType.IsEnumerable())
				return propertyType.GetTypeInfo().IsValueType ? Activator.CreateInstance(propertyType) : null;

			var genericType = propertyType.GetTypeInfo().GetGenericArguments().First();
			var method = typeof(Enumerable).GetTypeInfo().GetMethod("Empty")!.MakeGenericMethod(genericType);
			return method.Invoke(null, null);
		}

		private static ISettingsTypeConverter GetConverter(Type strippedType,
			SettingsPropertyAttribute? attribute,
			SettingsOptions options)
		{
			if (attribute?.ConverterType != null)
				return (ISettingsTypeConverter)Activator.CreateInstance(attribute.ConverterType)!;

			// Manual walk over the concrete LinkedList (not LINQ First) so the struct enumerator isn't boxed
			// onto the heap — this runs once per property at plan build.
			foreach (var converter in options.Converters)
			{
				if (converter.CanConvert(strippedType))
					return converter;
			}

			// Unreachable in practice: DefaultTypeConverter.CanConvert always returns true. Kept so every path
			// returns and to mirror the old First(...) which also threw when nothing matched.
			throw new InvalidOperationException($"No converter found for type '{strippedType}'.");
		}

		private static Type StripIfNullable(Type type)
		{
			return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ?
				type.GetTypeInfo().GetGenericArguments()[0] :
				type;
		}
	}
}
