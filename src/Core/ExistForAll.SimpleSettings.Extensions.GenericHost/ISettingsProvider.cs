namespace ExistForAll.SimpleSettings.Extensions.GenericHost
{
    public interface ISettingsProvider
    {
        object GetSettings(Type type);
    }
}