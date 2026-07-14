using ExistForAll.SimpleSettings.Conversion;
using ExistForAll.SimpleSettings.Core.Reflection;

namespace ExistForAll.SimpleSettings
{
	public class SettingsOptions
	{
		internal TypeConvertersCollections Converters { get; }

		public Type AttributeType { get; set; } = typeof(SettingsSectionAttribute);
		public Type InterfaceBase { get; set; } = typeof(ISettingsSection);
		public string SettingsSuffix { get; set; } = "Settings";
		public string ArraySplitDelimiter { get; set; } = ",";
		public string DateTimeFormat { get; set; } = "yyyy-MM-dd";
		public Func<Type, string> SectionNameFormatter { get; set; } = (interfaceType) => interfaceType.GetNormalizeInterfaceName();

		public SettingsOptions()
		{
			Converters = new TypeConvertersCollections(this);
		}
	}
}