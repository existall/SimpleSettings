using ExistForAll.SimpleSettings.Binder;

namespace ExistForAll.SimpleSettings.UnitTests.Conversion
{
	// Closes the scalar converter-coverage residual (TEST-03). CollectionConversionTests already proves
	// Uri/DateTime as ARRAY elements; this file locks the scalar POSITIVE parsing paths for both, plus a
	// single DateTime format-mismatch negative. The negative asserts ONLY the exception type — secret
	// redaction is owned by ExceptionRedactionTests and is deliberately not re-proved here.
	public class ScalarConversionTests
	{
		[Test]
		public async Task Convert_ScalarUri_ParsesToUri()
		{
			// UriTypeConvertor.Convert does new Uri((string)value); assert against new Uri(expected).
			var result = Build<IEndpoint>(nameof(IEndpoint.Url), "https://a.example/");

			await Assert.That(result.Url).IsEqualTo(new Uri("https://a.example/"));
		}

		[Test]
		public async Task Convert_ScalarDateTime_WithConfiguredFormat_Parses()
		{
			// Default DateTimeFormat is "yyyy-MM-dd" (see SettingsOptions); the input matches it exactly.
			var result = Build<IEndpoint>(nameof(IEndpoint.When), "2020-01-02");

			await Assert.That(result.When).IsEqualTo(new DateTime(2020, 1, 2));
		}

		[Test]
		public async Task Convert_ScalarDateTime_FormatMismatch_ThrowsSettingsPropertyValueException()
		{
			// "01/02/2020" does not match the configured "yyyy-MM-dd" format. Assert the exception TYPE
			// only — no assertion on message/ToString/value/inner (redaction is locked elsewhere).
			var collection = new InMemoryCollection();
			collection.Add("Endpoint", nameof(IEndpoint.When), "01/02/2020");

			var builder = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));

			await Assert.That(() => builder.GetSettings<IEndpoint>()).Throws<SettingsPropertyValueException>();
		}

		private static T Build<T>(string key, string value)
			where T : class
		{
			// Section name is "Endpoint" — the leading "I" of IEndpoint is stripped by the section-name formatter.
			var collection = new InMemoryCollection();
			collection.Add("Endpoint", key, value);

			var builder = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));

			return builder.GetSettings<T>();
		}

		public interface IEndpoint
		{
			Uri Url { get; set; }
			DateTime When { get; set; }
		}
	}
}
