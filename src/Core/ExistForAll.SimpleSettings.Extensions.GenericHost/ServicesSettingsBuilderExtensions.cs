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

            var settingsCollection = settingsBuilder.ScanAssemblies(options!.Assemblies);

            // Register each scanned interface as its startup-built instance, and hand that same
            // collection to the provider so ISettingsProvider.GetSettings<T>() returns the very same
            // instance as GetService<T>() (a lookup, not a re-bind). A future reload / IOptionsMonitor
            // path would swap these snapshots out here.
            foreach (var settings in settingsCollection)
            {
                services.AddSingleton(settings.Key, settings.Value);
            }

            services.AddSingleton<ISettingsProvider>(new SettingsProvider(settingsCollection, settingsBuilder));
        }
    }
}