using ExistForAll.SimpleSettings.Binder;

namespace ExistForAll.SimpleSettings.Binders
{
    public static class CommandLineSettingsBinderOptionsExtensions
    {
        public static CommandLineSettingsBinderOptions AddPrefix(this CommandLineSettingsBinderOptions options, char prefix)
        {
            options.AddArgumentPrefix(prefix);

            return options;
        }

        public static CommandLineSettingsBinderOptions AddNewDelimiter(this CommandLineSettingsBinderOptions options,
            string delimiter)
        {
            options.AddDelimiter(delimiter);

            return options;
        }

        public static CommandLineSettingsBinderOptions SetCaseSensitivity(this CommandLineSettingsBinderOptions options,
            bool isCaseSensitive)
        {
            options.IsCaseSensitive = isCaseSensitive;

            return options;
        }

        public static CommandLineSettingsBinderOptions SetNameFormatter(this CommandLineSettingsBinderOptions options,
            NameFormatter nameFormatter)
        {
            options.NameFormatter = nameFormatter;

            return options;
        }
        
        public static CommandLineSettingsBinderOptions ClearAllPrefixes(this CommandLineSettingsBinderOptions options,
            NameFormatter nameFormatter)
        {
            options.ClearArgumentPrefixes();

            return options;
        }
        
        public static CommandLineSettingsBinderOptions ClearAllDelimiters(this CommandLineSettingsBinderOptions options)
        {
            options.ClearDelimiters();

            return options;
        }
    }
}