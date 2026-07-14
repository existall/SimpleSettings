using System.Reflection;
using ExistForAll.SimpleSettings.Core.Reflection;

namespace ExistForAll.SimpleSettings.Conversion
{
	internal class EnumerableTypeConverter : CollectionTypeConverter
	{
		public EnumerableTypeConverter(SettingsOptions settingsOptions, TypeConvertersCollections converters)
			: base(settingsOptions, converters)
		{
		}

		public override bool CanConvert(Type settingsType)
		{
			return settingsType.IsEnumerable();
		}

		protected override Type GetElementType(Type settingsType)
		{
			return settingsType.GetTypeInfo().GetGenericArguments()[0];
		}
	}
}
