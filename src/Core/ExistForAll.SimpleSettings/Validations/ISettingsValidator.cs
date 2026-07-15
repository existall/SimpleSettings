namespace ExistForAll.SimpleSettings.Validations
{
    public interface ISettingsValidator
    {
        ValidationResult Validate(ValidationContext context);
    }
}
