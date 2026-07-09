using System;
using ExistsForAll.SimpleSettings.Binders;
using Microsoft.Extensions.Configuration;

namespace ExistForAll.SimpleSettings.Binders
{
    public static class SettingsBuilderFactoryExtensions
    {
        public static T AddConfiguration<T>(this T target, IConfiguration configuration) where T : ISettingsBuilderFactory
        {
            
            target.AddSectionBinder(new ConfigurationBinder(configuration));

            return target;
        }

        public static T AddEnvironmentVariable<T>(this T target) where T : ISettingsBuilderFactory
        {
            target.AddSectionBinder(new EnvironmentVariableBinder());

            return target;
        }

        public static T AddEnvironmentVariable<T>(this T target,
            Action<EnvironmentVariableBinderOptions> action) where T : ISettingsBuilderFactory
        {
            var options = new EnvironmentVariableBinderOptions();
            action(options);

            var binder = options.Prefix != null
                ? new EnvironmentVariableBinder(options.Prefix)
                : new EnvironmentVariableBinder();

            if (options.VariableNameFormatter != null)
                binder.VariableNameFormatter = options.VariableNameFormatter;

            target.AddSectionBinder(binder);

            return target;
        }

        public static T AddCommandLine<T>(this T target,
            Action<CommandLineSettingsBinderOptions>? action = null) where T : ISettingsBuilderFactory
        {
            var args = Environment.CommandLine.Trim().Split(' ');

            target.AddArguments(args, action);

            return target;
        }

        public static T AddArguments<T>(this T target,
            string[] args
            , Action<CommandLineSettingsBinderOptions>? action = null) where T : ISettingsBuilderFactory
        {
            var options = new CommandLineSettingsBinderOptions();
            action?.Invoke(options);

            target.AddSectionBinder(new CommandLineSettingsBinder(args, options));

            return target;
        }
    }
}