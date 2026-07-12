using System;
using ExistForAll.SimpleSettings.Binder;

namespace ExistForAll.SimpleSettings.UnitTests.Conversion
{
	public class EnumConversionTests
	{
		// The default SectionNameFormatter strips the leading "I": IEnumSettings -> "EnumSettings".
		private const string Section = "EnumSettings";

		[Test]
		public async Task Build_EnumPropertyFromStringValue_BindsEnum()
		{
			var settings = BuildWith(nameof(IEnumSettings.Day), "Monday")
				.GetSettings<IEnumSettings>();

			await Assert.That(settings.Day).IsEqualTo(DayOfWeek.Monday);
		}

		[Test]
		public async Task Build_EnumPropertyFromDefaultValue_BindsEnum()
		{
			// No binder: the value comes from the attribute default and is already the target type,
			// so this path works even without the EnumTypeConverter registered — it guards the fix.
			var settings = SettingsBuilder.CreateBuilder()
				.GetSettings<IEnumWithDefault>();

			await Assert.That(settings.Day).IsEqualTo(DayOfWeek.Friday);
		}

		private static SettingsBuilder BuildWith(string key, string value)
		{
			var collection = new InMemoryCollection();
			collection.Add(Section, key, value);
			return SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));
		}

		public interface IEnumSettings
		{
			DayOfWeek Day { get; set; }
		}

		public interface IEnumWithDefault
		{
			[SettingsProperty(DefaultValue = DayOfWeek.Friday)]
			DayOfWeek Day { get; set; }
		}
	}
}
