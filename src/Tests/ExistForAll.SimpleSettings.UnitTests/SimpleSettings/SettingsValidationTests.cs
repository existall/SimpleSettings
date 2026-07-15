using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.Validations;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	// Fixtures are intentionally NON-settings-indicated (no "Settings" suffix, no [SettingsSection]/
	// ISettingsSection) and resolved via GetSettings<T>() so a deliberately-failing validator never trips
	// ScanAssemblies-based tests elsewhere in the suite.
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
		public async Task PropertyValidator_RunsAgainstThatPropertyValue()
		{
			await Assert.That(() => Build<IPropertyValidated>()).Throws<SettingsValidationException>();
		}

		[Test]
		public async Task MultipleErrors_AggregateIntoOneExceptionWithAllErrors()
		{
			var exception = await Assert.That(() => Build<ITwoErrors>())
				.Throws<SettingsValidationException>();

			await Assert.That(exception!.Errors.Count).IsEqualTo(2);
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
		public async Task NoValidators_PopulatesAndReturnsWithoutThrowing()
		{
			var result = Build<INoValidators>();

			await Assert.That(result).IsNotNull();
		}

		private const string Sentinel = "SUPER_SECRET_API_KEY_VALUE";

		private static T Build<T>(InMemoryCollection? collection = null) where T : class
		{
			var builder = collection == null
				? SettingsBuilder.CreateBuilder()
				: SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));

			return builder.GetSettings<T>();
		}

		[SettingsValidator(typeof(FailingObjectValidator))]
		public interface IObjectValidated
		{
			string Name { get; set; }
		}

		[SettingsValidator(typeof(PassingObjectValidator))]
		public interface IObjectValid
		{
			string Name { get; set; }
		}

		public interface IPropertyValidated
		{
			[SettingsProperty(ValidatorType = typeof(PositivePortValidator))]
			int Port { get; set; }
		}

		[SettingsValidator(typeof(TwoErrorValidator))]
		public interface ITwoErrors
		{
			string Name { get; set; }
		}

		[SettingsValidator(typeof(CrossPropertyValidator))]
		public interface ICrossProperty
		{
			int Min { get; set; }
			int Max { get; set; }
		}

		[SettingsValidator(typeof(RedactionValidator))]
		public interface IRedaction
		{
			string ApiKey { get; set; }
		}

		public interface INoValidators
		{
			string Name { get; set; }
		}

		private class FailingObjectValidator : ISettingValidation<IObjectValidated>
		{
			public ValidationResult Validate(ValidationContext context)
				=> Validate((ValidationContext<IObjectValidated>)context);

			public ValidationResult Validate(ValidationContext<IObjectValidated> context)
			{
				var result = new ValidationResult();
				result.AddError(new ValidationError(nameof(IObjectValidated.Name), "name is invalid"));
				return result;
			}
		}

		private class PassingObjectValidator : ISettingValidation<IObjectValid>
		{
			public ValidationResult Validate(ValidationContext context)
				=> Validate((ValidationContext<IObjectValid>)context);

			public ValidationResult Validate(ValidationContext<IObjectValid> context) => new();
		}

		private class PositivePortValidator : ISettingValidation<int>
		{
			public ValidationResult Validate(ValidationContext context)
				=> Validate((ValidationContext<int>)context);

			public ValidationResult Validate(ValidationContext<int> context)
			{
				var result = new ValidationResult();
				if (context.Settings <= 0)
					result.AddError(new ValidationError("Port", "port must be positive"));
				return result;
			}
		}

		private class TwoErrorValidator : ISettingValidation<ITwoErrors>
		{
			public ValidationResult Validate(ValidationContext context)
				=> Validate((ValidationContext<ITwoErrors>)context);

			public ValidationResult Validate(ValidationContext<ITwoErrors> context)
			{
				var result = new ValidationResult();
				result.AddError(new ValidationError("Name", "first failure"));
				result.AddError(new ValidationError("Name", "second failure"));
				return result;
			}
		}

		private class CrossPropertyValidator : ISettingValidation<ICrossProperty>
		{
			public ValidationResult Validate(ValidationContext context)
				=> Validate((ValidationContext<ICrossProperty>)context);

			public ValidationResult Validate(ValidationContext<ICrossProperty> context)
			{
				var result = new ValidationResult();
				var settings = context.Settings!;
				if (settings.Min > settings.Max)
					result.AddError(new ValidationError("Min", "Min must not exceed Max"));
				return result;
			}
		}

		private class RedactionValidator : ISettingValidation<IRedaction>
		{
			public ValidationResult Validate(ValidationContext context)
				=> Validate((ValidationContext<IRedaction>)context);

			public ValidationResult Validate(ValidationContext<IRedaction> context)
			{
				var result = new ValidationResult();
				result.AddError(new ValidationError("ApiKey", "ApiKey failed policy"));
				return result;
			}
		}
	}
}
