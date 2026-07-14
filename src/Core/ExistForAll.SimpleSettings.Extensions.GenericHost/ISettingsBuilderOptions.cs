using System.Reflection;

namespace ExistForAll.SimpleSettings.Extensions.GenericHost
{
    public interface ISettingsBuilderOptions : ISettingsBuilderFactory
    {
        void AddAssemblies(IEnumerable<Assembly> assemblies);
    }
}