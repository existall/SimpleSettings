namespace ExistForAll.SimpleSettings
{
	public class SettingsTypeNotFoundException : SimpleSettingsException
	{
		public SettingsTypeNotFoundException(Type settingsType)
			: base(Resources.GetSettingsNotFoundMessageFormatMessage(settingsType))
		{
			SettingsType = settingsType;
		}

		public Type SettingsType { get; }
	}
}
