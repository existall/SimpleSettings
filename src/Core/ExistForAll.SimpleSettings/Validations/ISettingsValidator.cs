namespace ExistForAll.SimpleSettings.Validations
{
    public interface ISettingsValidator
    {
        Task<ValidationResult> Validate(ValidationContext context);
    }
}