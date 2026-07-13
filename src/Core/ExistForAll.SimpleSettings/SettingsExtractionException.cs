using System;

namespace ExistForAll.SimpleSettings
{
	// Moved to the root namespace (was ExistForAll.SimpleSettings.Core) so the public exception surface is
	// coherent — consumers catch it without a using for an internal-looking sub-namespace. See C2.
	public class SettingsExtractionException : SimpleSettingsException
	{
		public SettingsExtractionException(Type settingsType, Exception innerException)
			: base(Resources.SettingsExtractionsExceptionMessage(settingsType), innerException)
		{
			SettingsType = settingsType;
		}

		public Type SettingsType { get; }
	}
}
