using System.Reflection;

namespace ExistForAll.SimpleSettings.Core
{
	internal class SettingsTypesExtractor : ISettingsTypesExtractor
	{
		public Type[] ExtractSettingsTypes(IEnumerable<Assembly> assemblies, SettingsOptions options)
		{
			if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));
			if (options == null) throw new ArgumentNullException(nameof(options));

			// The suffix is constant for the whole scan, so trim it once here rather than per candidate type.
			var suffix = options.SettingsSuffix.Trim();

			return assemblies.SelectMany(x=>x.GetExportedTypes())
				.Where(x => x.GetTypeInfo().IsInterface && IsFromOptions(x, options, suffix))
				.ToArray();
		}

		private static bool IsFromOptions(Type type, SettingsOptions options, string suffix)
		{
			try
			{
				var info = type.GetTypeInfo();

				if (info.GetCustomAttribute(options.AttributeType, true) != null)
					return true;

				if (options.InterfaceBase.GetTypeInfo().IsAssignableFrom(info))
					return true;

				if (info.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
					return true;

				return false;
			}
			catch (Exception e)
			{
				throw new SettingsExtractionException(type,e);
			}
		}
	}
}