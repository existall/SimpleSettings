using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.UnitTests.Fixtures;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	// Fixtures live in the never-scanned ExistForAll.SimpleSettings.UnitTests.Fixtures project; resolved via GetSettings<T>().
	public class SettingsValidationTests
	{
		[Test]
		public async Task ObjectValidator_WhenItAddsError_ThrowsSettingsValidationException()
		{
			await Assert.That(() => Build<IObjectValidated>()).Throws<SettingsValidationException>();
		}

		[Test]
		public async Task ObjectValidator_WhenValid_DoesNotThrow()
		{
			var result = Build<IObjectValid>();

			await Assert.That(result).IsNotNull();
		}

		[Test]
		public async Task PropertyValidator_WhenValueInvalid_Throws()
		{
			// Bind an explicitly invalid value (-5) so the failure is distinct from the unbound default (0) —
			// this proves the validator observed the BOUND value, not just the default.
			var collection = new InMemoryCollection();
			collection.Add("PropertyValidated", nameof(IPropertyValidated.Port), "-5");

			await Assert.That(() => Build<IPropertyValidated>(collection)).Throws<SettingsValidationException>();
		}

		[Test]
		public async Task PropertyValidator_WhenValueValid_DoesNotThrow()
		{
			var collection = new InMemoryCollection();
			collection.Add("PropertyValidated", nameof(IPropertyValidated.Port), "8080");

			var result = Build<IPropertyValidated>(collection);

			await Assert.That(result.Port).IsEqualTo(8080);
		}

		[Test]
		public async Task MultipleErrors_AggregateIntoOneExceptionWithAllErrors()
		{
			var exception = await Assert.That(() => Build<ITwoErrors>())
				.Throws<SettingsValidationException>();

			await Assert.That(exception!.Errors.Count).IsEqualTo(2);
		}

		[Test]
		public async Task ObjectAndPropertyValidators_BothFail_AggregateWithObjectErrorFirst()
		{
			// A type carrying BOTH an object-level and a property-level validator, each failing, aggregates
			// into ONE exception; the object error is accumulated before the property error (runs object-first).
			var collection = new InMemoryCollection();
			collection.Add("BothValidated", nameof(IBothValidated.Port), "-1");

			var exception = await Assert.That(() => Build<IBothValidated>(collection))
				.Throws<SettingsValidationException>();

			await Assert.That(exception!.Errors.Count).IsEqualTo(2);
			await Assert.That(exception.Errors[0].SettingsName).IsEqualTo("Object");
			await Assert.That(exception.Errors[1].SettingsName).IsEqualTo("Port");
		}

		[Test]
		public async Task CrossPropertyValidator_ObservesEveryPropertySet()
		{
			var collection = new InMemoryCollection();
			collection.Add("CrossProperty", nameof(ICrossProperty.Min), "5");
			collection.Add("CrossProperty", nameof(ICrossProperty.Max), "10");

			var result = Build<ICrossProperty>(collection);

			await Assert.That(result.Min).IsEqualTo(5);
			await Assert.That(result.Max).IsEqualTo(10);
		}

		[Test]
		public async Task CrossPropertyValidator_WhenRuleViolated_Throws()
		{
			var collection = new InMemoryCollection();
			collection.Add("CrossProperty", nameof(ICrossProperty.Min), "10");
			collection.Add("CrossProperty", nameof(ICrossProperty.Max), "5");

			await Assert.That(() => Build<ICrossProperty>(collection)).Throws<SettingsValidationException>();
		}

		[Test]
		public async Task Exception_CarriesAuthorMessage_AndNoBoundValue()
		{
			var collection = new InMemoryCollection();
			collection.Add("Redaction", nameof(IRedaction.ApiKey), Sentinel);

			var exception = await Assert.That(() => Build<IRedaction>(collection))
				.Throws<SettingsValidationException>();

			var text = exception!.ToString();
			await Assert.That(text.Contains("ApiKey failed policy")).IsTrue();
			await Assert.That(text.Contains(Sentinel)).IsFalse();
		}

		[Test]
		public async Task ThrowingValidator_IsWrappedValueFree_NotLeaked()
		{
			// A validator that throws (rather than returning a result) with the secret in its message must be
			// surfaced value-free: the invocation is wrapped in SettingsValidatorInvocationException, whose
			// ToString() carries no bound value and does not chain the leaking inner.
			var collection = new InMemoryCollection();
			collection.Add("Throwing", nameof(IThrowing.ApiKey), Sentinel);

			var exception = await Assert.That(() => Build<IThrowing>(collection))
				.Throws<SettingsValidatorInvocationException>();

			await Assert.That(exception!.ToString().Contains(Sentinel)).IsFalse();
		}

		[Test]
		public async Task NoValidators_PopulatesAndReturnsWithoutThrowing()
		{
			var result = Build<INoValidators>();

			await Assert.That(result).IsNotNull();
		}

		[Test]
		public async Task ScanAssemblies_WhenSectionCarriesValidator_RunsItAndSurfacesFailure()
		{
			// Guards the scan path for the merged [SettingsSection(ValidatorType=...)]: scanning the Fixtures
			// assembly (whose discovered sections carry failing validators) must surface a validation failure.
			var builder = SettingsBuilder.CreateBuilder();

			await Assert.That(() => builder.ScanAssemblies(typeof(IObjectValidated).Assembly))
				.Throws<SimpleSettingsException>();
		}

		private const string Sentinel = "SUPER_SECRET_API_KEY_VALUE";

		private static T Build<T>(InMemoryCollection? collection = null) where T : class
		{
			var builder = collection == null
				? SettingsBuilder.CreateBuilder()
				: SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));

			return builder.GetSettings<T>();
		}
	}
}
