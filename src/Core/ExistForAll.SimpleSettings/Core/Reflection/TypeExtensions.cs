using System.Reflection;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal static class TypeExtensions
	{
		public static string GetNormalizeInterfaceName(this Type target)
		{
			return target.Name[0] == 'I' ? target.Name[1..] : target.Name;
		}

		public static string GetSectionName(this Type settingsClass, SettingsOptions options)
		{
			var attribute = settingsClass.GetTypeInfo().GetCustomAttribute<SettingsSectionAttribute>(true);

			return !string.IsNullOrWhiteSpace(attribute?.Name)
				? attribute.Name
				: options.SectionNameFormatter(settingsClass);
		}

		public static bool IsEnumerable(this Type type)
		{
			var info = type.GetTypeInfo();

			return info.IsGenericType && info.GetGenericTypeDefinition() == typeof(IEnumerable<>);
		}

		// Disjoint from IsEnumerable/IsArray: matches only the List<T> family the ListTypeConverter claims.
		public static bool IsListLike(this Type type)
		{
			var info = type.GetTypeInfo();

			if (!info.IsGenericType)
				return false;

			var definition = info.GetGenericTypeDefinition();

			return definition == typeof(List<>)
				|| definition == typeof(IList<>)
				|| definition == typeof(ICollection<>)
				|| definition == typeof(IReadOnlyList<>)
				|| definition == typeof(IReadOnlyCollection<>);
		}

		public static bool IsCollectionShape(this Type type)
		{
			return type.IsArray || type.IsEnumerable() || type.IsListLike();
		}
	}
}