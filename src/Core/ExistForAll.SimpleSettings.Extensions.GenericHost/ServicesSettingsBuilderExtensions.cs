using System;
using Microsoft.Extensions.DependencyInjection;

namespace ExistForAll.SimpleSettings.Extensions.GenericHost
{
    public static class ServicesSettingsBuilderExtensions
    {
        public static IServiceCollection AddSimpleSettings(this IServiceCollection services)
        {
            IntegrateSimpleSettings(services);
            return services;
        }

        public static IServiceCollection AddSimpleSettings(this IServiceCollection services,
            Action<ISettingsBuilderOptions> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            IntegrateSimpleSettings(services, action);
            return services;
        }

        private static void IntegrateSimpleSettings(this IServiceCollection services,
            Action<ISettingsBuilderOptions>? action = null)
        {
            SettingsBuilderOptions? options = null;

            var settingsBuilder = SettingsBuilder.CreateBuilder(factory =>
            {
                options = new SettingsBuilderOptions(factory);
                action?.Invoke(options);
            });

            var settingsProvider = settingsBuilder.ScanAssemblies(options!.Assemblies);

            // replace this with a replaceable settings provider to support time based Settingsuration

            foreach (var settings in settingsProvider)
            {
                services.AddSingleton(settings.Key, settings.Value);
            }

            services.AddSingleton<ISettingsProvider>(new SettingsProvider(settingsBuilder));
        }
    }
}