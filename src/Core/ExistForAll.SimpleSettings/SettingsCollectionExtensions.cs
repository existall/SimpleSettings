namespace ExistForAll.SimpleSettings
{
    public static class SettingsCollectionExtensions
    {
        public static T GetSettings<T>(this ISettingsCollection target) where T : class
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            return (T) target.GetSettings(typeof(T));
        }
    }
}