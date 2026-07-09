using System.Reflection;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal interface ITypeConverter
	{
		object? ConvertValue(object? value, PropertyInfo propertyInfo, SettingsOptions options);
	}
}