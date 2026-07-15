using ExistForAll.SimpleSettings.Validations;
using Microsoft.Extensions.DependencyInjection;

namespace ExistForAll.SimpleSettings.Extensions.GenericHost
{
    internal sealed class SettingsValidationRunner : ISettingsValidationRunner
    {
        private readonly ISettingsCollection _settings;
        private readonly IServiceScopeFactory _scopeFactory;

        public SettingsValidationRunner(ISettingsCollection settings, IServiceScopeFactory scopeFactory)
        {
            _settings = settings;
            _scopeFactory = scopeFactory;
        }

        public void Validate()
        {
            // Resolve from a fresh scope so validators (or their dependencies) that are scoped resolve correctly.
            using var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;

            var errors = new List<ValidationError>();

            foreach (var pair in _settings)
            {
                var validatorType = typeof(ISettingValidation<>).MakeGenericType(pair.Key);

                foreach (var validator in provider.GetServices(validatorType))
                {
                    if (validator is null)
                        continue;

                    ValidationResult result;
                    try
                    {
                        // Dispatch through ISettingsValidator; its default implementation forwards to the typed overload.
                        result = ((ISettingsValidator)validator).Validate(new ValidationContext(pair.Value));
                    }
                    catch (Exception e)
                    {
                        // A validator threw: surface value-free (types only) — the inner may embed a secret, never chain it.
                        throw new SettingsValidatorInvocationException(validator.GetType(), e.GetType());
                    }

                    foreach (var error in result.Errors)
                        errors.Add(error);
                }
            }

            // Aggregate through the shared helper so the DI-path exception is contract-identical to the core path.
            SettingsValidationException.ThrowIfAny(errors);
        }
    }
}
