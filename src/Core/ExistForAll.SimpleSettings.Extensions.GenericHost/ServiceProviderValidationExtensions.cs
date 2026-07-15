using Microsoft.Extensions.DependencyInjection;

namespace ExistForAll.SimpleSettings.Extensions.GenericHost
{
    public static class ServiceProviderValidationExtensions
    {
        /// <summary>
        /// Runs every DI-registered <see cref="Validations.ISettingValidation{T}"/> against its scanned settings
        /// instance and throws a single aggregated <see cref="SettingsValidationException"/> if any validator
        /// reports an error.
        /// </summary>
        /// <remarks>
        /// This is opt-in and deferred: DI-resolved validators cannot run during <c>AddSimpleSettings</c> because
        /// the container is not built yet, so the host must call this explicitly after
        /// <c>BuildServiceProvider()</c> (typically at startup). Attribute-declared validators
        /// (<c>[SettingsSection(ValidatorType = ...)]</c> and <c>[SettingsProperty(ValidatorType = ...)]</c>) run
        /// automatically during binding and do not require this call. Validators are resolved from a fresh scope,
        /// so validators with scoped dependencies are supported.
        /// </remarks>
        public static IServiceProvider ValidateSimpleSettings(this IServiceProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            provider.GetRequiredService<ISettingsValidationRunner>().Validate();
            return provider;
        }
    }
}
