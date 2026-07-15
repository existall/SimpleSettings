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
            // Resolve validators from a fresh scope so a validator (or an injected dependency) that is scoped
            // does not trip the "cannot resolve scoped service from root provider" guard under scope validation.
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
                        // Dispatch through the non-generic ISettingsValidator; ISettingValidation<T>'s default
                        // implementation forwards to the author's generic overload, so no reflection is needed.
                        result = ((ISettingsValidator)validator).Validate(new ValidationContext(pair.Value));
                    }
                    catch (Exception e)
                    {
                        // A validator threw: surface value-free (the inner may embed a secret it read) — only the
                        // validator and failure types, never the instance and never a chained inner. See S1.
                        throw new SettingsValidatorInvocationException(validator.GetType(), e.GetType());
                    }

                    foreach (var error in result.Errors)
                        errors.Add(error);
                }
            }

            // Aggregate and throw through the same shared helper the core populate path uses, so the DI-path
            // exception is contract-identical (type + Errors + value-free message).
            SettingsValidationException.ThrowIfAny(errors);
        }
    }
}
