namespace ExistForAll.SimpleSettings
{
	internal interface IValuesPopulator
	{
		void PopulateInstanceWithValues(object instance,
			Type settings,
			SettingsOptions options,
			IEnumerable<ISectionBinder> binders);
	}
}