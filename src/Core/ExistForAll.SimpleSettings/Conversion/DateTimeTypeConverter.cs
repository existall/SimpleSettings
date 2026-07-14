using System.Globalization;

namespace ExistForAll.SimpleSettings.Conversion
{
	internal class DateTimeTypeConverter : ISettingsTypeConverter
	{
		private readonly SettingsOptions _settingsOptions;

		public DateTimeTypeConverter(SettingsOptions settingsOptions)
		{
			_settingsOptions = settingsOptions;
		}

		public bool CanConvert(Type settingsType)
		{
			return settingsType == typeof(DateTime);
		}

		public object Convert(object value, Type settingsType)
		{
			return DateTime.ParseExact((string)value, _settingsOptions.DateTimeFormat, CultureInfo.InvariantCulture);
		}
	}
}