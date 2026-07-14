namespace ExistForAll.SimpleSettings
{
	public class SettingsOptionsArgumentNullException : SimpleSettingsException
	{
		public SettingsOptionsArgumentNullException()
			: base(Resources.SettingsOptionsArgumentNullMessage)
		{
		}
	}
}
