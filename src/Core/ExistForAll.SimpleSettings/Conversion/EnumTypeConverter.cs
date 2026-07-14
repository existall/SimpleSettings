using System.Reflection;

namespace ExistForAll.SimpleSettings.Conversion
{
	internal class EnumTypeConverter : ISettingsTypeConverter
	{
		public bool CanConvert(Type configType)
		{
			return configType.GetTypeInfo().IsEnum;
		}

		public object Convert(object value, Type configType)
		{
			return value.GetType() == configType ? value : Enum.Parse(configType, (string)value);
		}
	}
}