namespace ExistForAll.SimpleSettings
{
	[AttributeUsage(AttributeTargets.Interface)]
	public class SettingsValidatorAttribute : Attribute
	{
		public Type ValidatorType { get; }

		public SettingsValidatorAttribute(Type validatorType)
		{
			ValidatorType = validatorType;
		}
	}
}
