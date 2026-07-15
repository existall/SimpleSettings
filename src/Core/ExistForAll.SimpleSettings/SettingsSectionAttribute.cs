namespace ExistForAll.SimpleSettings
{
	[AttributeUsage(AttributeTargets.Interface)]
	public class SettingsSectionAttribute : Attribute
	{
		public string? Name { get; set; }

		public Type? ValidatorType { get; set; }

		public SettingsSectionAttribute()
		{
		}

		public SettingsSectionAttribute(string name)
		{
			Name = name;
		}
	}
}