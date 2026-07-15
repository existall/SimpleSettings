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
		public PropertyConversion CreateConversion(PropertyInfo propertyInfo, SettingsPropertyAttribute? attribute, SettingsOptions options)
		{
			var propertyType = propertyInfo.PropertyType;

			var throwOnNull = attribute is { AllowEmpty: false };
			var nullResult = CreateNullResult(propertyType);

			var strippedType = StripIfNullable(propertyType);
			var converter = GetConverter(strippedType, attribute, options);

			return new PropertyConversion(converter, strippedType, throwOnNull, nullResult, propertyInfo.Name);
		}

		private static readonly MethodInfo EmptyListFactoryMethod =
			typeof(TypeConverter).GetMethod(nameof(CreateEmptyList), BindingFlags.NonPublic | BindingFlags.Static)!;

		// The value a null bound value converts to per shape: a shared empty array for arrays and IEnumerable<T>,
		// a fresh-per-bind empty List<T> for the List family (a mutable list must not be shared), a
		// default instance for a value type, otherwise null. Resolved once at plan build; the list branch bakes a
		// factory delegate (cold reflection here, plain new List<T>() per call) rather than a shared instance.
		private static object? CreateNullResult(Type propertyType)
		{
			if (propertyType.IsArray)
				return Array.CreateInstance(propertyType.GetElementType()!, 0);

			if (propertyType.IsEnumerable())
				return Array.CreateInstance(propertyType.GetTypeInfo().GetGenericArguments()[0], 0);

			if (propertyType.IsListLike())
			{
				var elementType = propertyType.GetTypeInfo().GetGenericArguments()[0];
				return (Func<object>)EmptyListFactoryMethod.MakeGenericMethod(elementType)
					.CreateDelegate(typeof(Func<object>));
			}

			return propertyType.GetTypeInfo().IsValueType ? Activator.CreateInstance(propertyType) : null;
		}

		private static object CreateEmptyList<TElement>()
		{
			return new List<TElement>();
		}

		private static ISettingsTypeConverter GetConverter(Type strippedType,
			SettingsPropertyAttribute? attribute,
			SettingsOptions options)
		{
			if (attribute?.ConverterType != null)
				return (ISettingsTypeConverter)Activator.CreateInstance(attribute.ConverterType)!;

			// Manual walk over the concrete LinkedList (not LINQ First/Where) so the struct enumerator isn't
			// boxed onto the heap and no predicate closure is allocated — this runs once per property at plan
			// build.
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
