namespace ExistForAll.SimpleSettings.Validations
{
    public interface ISettingValidation<T> : ISettingsValidator
    {
        ValidationResult Validate(ValidationContext<T> context);
    }
}
