using System;
using System.Linq;
using System.Reflection;

namespace ExistForAll.SimpleSettings
{
	public static class SettingsBuilderExtensions
	{
		public static ISettingsCollection ScanAssemblies(this SettingsBuilder target, Assembly assembly, params Assembly[] assemblies)
		{
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            if (assemblies == null)
            {
                assemblies = [];
            }

            assemblies = assemblies.Concat([assembly]).ToArray();

            return target.ScanAssemblies(assemblies);
        }

        public static T GetSettings<T>(this SettingsBuilder target) where T : class
        {
            return (T) target.GetSettings(typeof(T));
        }
    }
}