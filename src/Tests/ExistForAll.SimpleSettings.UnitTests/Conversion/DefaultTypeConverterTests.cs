using System.Globalization;
using ExistForAll.SimpleSettings.Binder;

namespace ExistForAll.SimpleSettings.UnitTests.Conversion
{
	public class DefaultTypeConverterTests
	{
		// The default SectionNameFormatter strips the leading "I": INumericSettings -> "NumericSettings".
		private const string Section = "NumericSettings";

		[Test]
		[NotInParallel]
		public async Task Build_DoubleFromString_UnderGermanCulture_ParsesInvariant()
		{
			var original = CultureInfo.CurrentCulture;
			try
			{
				// In de-DE '.' is the group separator, so a culture-sensitive parse of "1.5" yields 15.
				CultureInfo.CurrentCulture = new CultureInfo("de-DE");

				var settings = BuildWith(nameof(INumericSettings.Value), "1.5")
					.GetSettings<INumericSettings>();

				await Assert.That(settings.Value).IsEqualTo(1.5d);
			}
			finally
			{
				CultureInfo.CurrentCulture = original;
			}
		}

		[Test]
		[NotInParallel]
		public async Task Build_DecimalFromString_UnderGermanCulture_ParsesInvariant()
		{
			var original = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = new CultureInfo("de-DE");

				var settings = BuildWith(nameof(INumericSettings.Amount), "1234.56")
					.GetSettings<INumericSettings>();

				await Assert.That(settings.Amount).IsEqualTo(1234.56m);
			}
			finally
			{
				CultureInfo.CurrentCulture = original;
			}
		}

		[Test]
		public async Task Build_IntFromString_BindsValue()
		{
			var settings = BuildWith(nameof(INumericSettings.Count), "42")
				.GetSettings<INumericSettings>();

			await Assert.That(settings.Count).IsEqualTo(42);
		}

		private static SettingsBuilder BuildWith(string key, string value)
		{
			var collection = new InMemoryCollection();
			collection.Add(Section, key, value);
			return SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));
		}

		public interface INumericSettings
		{
			double Value { get; set; }
			decimal Amount { get; set; }
			int Count { get; set; }
		}
	}
}
