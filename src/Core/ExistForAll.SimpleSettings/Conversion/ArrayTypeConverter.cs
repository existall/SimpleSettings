using System;

namespace ExistForAll.SimpleSettings.Conversion
{
	internal class ArrayTypeConverter : CollectionTypeConverter
	{
		public ArrayTypeConverter(SettingsOptions settingsOptions, TypeConvertersCollections converters)
			: base(settingsOptions, converters)
		{
		}

		public override bool CanConvert(Type settingsType)
		{
			return settingsType.IsArray;
		}

		protected override Type GetElementType(Type settingsType)
		{
			return settingsType.GetElementType()!;
		}
	}
}
