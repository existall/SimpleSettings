using System.Reflection;
using ExistForAll.SimpleSettings.Conversion;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal interface ITypeConverter
	{
		PropertyConversion CreateConversion(PropertyInfo propertyInfo, SettingsPropertyAttribute? attribute, SettingsOptions options);
	}
}
