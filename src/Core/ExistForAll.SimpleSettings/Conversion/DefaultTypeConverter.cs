using System.Globalization;

namespace ExistForAll.SimpleSettings.Conversion
{
	internal class DefaultTypeConverter : ISettingsTypeConverter
	{
		public bool CanConvert(Type settingsType)
		{
			return true;
		}

		public object Convert(object value, Type settingsType)
		{
			return System.Convert.ChangeType(value, settingsType, CultureInfo.InvariantCulture);
		}
	}
}