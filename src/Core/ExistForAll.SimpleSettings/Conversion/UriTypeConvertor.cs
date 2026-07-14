namespace ExistForAll.SimpleSettings.Conversion
{
	internal class UriTypeConvertor : ISettingsTypeConverter
	{
		public bool CanConvert(Type settingsType)
		{
			return settingsType == typeof(Uri);
		}

		public object Convert(object value, Type settingsType)
		{
			return new Uri((string)value);
		}
	}
}