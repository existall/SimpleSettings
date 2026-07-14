namespace ExistForAll.SimpleSettings
{
	public class SettingsOptionsArgumentMissingException : SimpleSettingsException
	{
		public SettingsOptionsArgumentMissingException(string argumentName)
			: base(Resources.SettingsOptionsArgumentMissingMessage(argumentName))
		{
			ArgumentName = argumentName;
		}

		public string ArgumentName { get; }
	}
}
