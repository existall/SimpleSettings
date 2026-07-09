using System.Reflection;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal static class PropertyInfoExtensions
	{
		public static object? GetDefaultValue(this PropertyInfo property)
		{
			var attribute = property.GetCustomAttribute<SettingsPropertyAttribute>();

			return attribute?.DefaultValue;
		}
	}
}
