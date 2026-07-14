namespace ExistForAll.SimpleSettings.Extensions.GenericHost
{
    internal class SettingsProvider : ISettingsProvider
    {
        private readonly ISettingsCollection _settings;
        private readonly SettingsBuilder _settingsBuilder;

        public SettingsProvider(ISettingsCollection settings, SettingsBuilder settingsBuilder)
        {
            _settings = settings;
            _settingsBuilder = settingsBuilder;
        }

        public object GetSettings(Type type)
        {
            // Serve the startup-built instance (the same object registered as the DI singleton) so that
            // GetService<T>() and ISettingsProvider.GetSettings<T>() agree, and repeat resolves are a
            // dictionary lookup rather than a full re-bind. Types that were never scanned fall back to
            // building on demand.
            return _settings.TryGetSettings(type, out var settings)
                ? settings!
                : _settingsBuilder.GetSettings(type);
        }
    }
}
