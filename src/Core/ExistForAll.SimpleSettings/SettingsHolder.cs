using System;

namespace ExistForAll.SimpleSettings
{
	internal sealed class SettingsHolder : ISettingsHolder
	{
		public SettingsHolder(Type settingsType, object settingsImplementation)
		{
			SettingsType = settingsType;
			SettingsImplementation = settingsImplementation;
		}

		public Type SettingsType { get; }
		public object SettingsImplementation { get; }
	}
}
