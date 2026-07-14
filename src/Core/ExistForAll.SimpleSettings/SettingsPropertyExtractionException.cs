namespace ExistForAll.SimpleSettings
{
	// Promoted to public and moved to the root namespace (was internal in ExistForAll.SimpleSettings.Core.Reflection):
	// it escapes the build path, so consumers must be able to catch it by type. See C2.
	public class SettingsPropertyExtractionException : SimpleSettingsException
	{
		public SettingsPropertyExtractionException(Type settingsType, Exception innerException)
			: base(Resources.SettingsPropertiesExtractionMessage(settingsType), innerException)
		{
			SettingsType = settingsType;
		}

		public Type SettingsType { get; }
	}
}
