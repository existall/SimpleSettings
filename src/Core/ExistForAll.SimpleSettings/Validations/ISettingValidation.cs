namespace ExistForAll.SimpleSettings.Validations
{
    public interface ISettingValidation<T> : ISettingsValidator
    {
        ValidationResult Validate(ValidationContext<T> context);

        // Bridges the non-generic base call to the generic overload so callers dispatch via a plain
        // ISettingsValidator.Validate cast — no reflection. Authors implement only the generic Validate.
        ValidationResult ISettingsValidator.Validate(ValidationContext context)
            => Validate(new ValidationContext<T>((T)context.Settings!));
    }
}
