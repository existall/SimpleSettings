namespace ExistForAll.SimpleSettings
{
	internal interface ISettingsHolder
	{
		Type SettingsType { get; }
		object SettingsImplementation { get; }
	}
}