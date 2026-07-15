using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.Extensions.GenericHost;
using ExistForAll.SimpleSettings.Validations;
using Microsoft.Extensions.DependencyInjection;

namespace ExistForAll.SimpleSettings.UnitTests.DependencyInjection
{
	public class AddSimpleSettingsIntegrationTests
	{
		// Discovered by the "Settings" suffix; the default SectionNameFormatter -> "DiExampleSettings".
		private const string Section = "DiExampleSettings";

		[Test]
		public async Task AddSimpleSettings_ResolvesSettingsInterface_WithBoundValues()
		{
			var collection = new InMemoryCollection();
			collection.Add(Section, nameof(IDiExampleSettings.Name), "resolved-value");

			var services = new ServiceCollection();
			services.AddSimpleSettings(o =>
			{
				o.AddAssemblies([typeof(IDiExampleSettings).Assembly]);
				o.AddSectionBinder(new InMemoryBinder(collection));
			});

			var provider = services.BuildServiceProvider();
			var settings = provider.GetRequiredService<IDiExampleSettings>();

			await Assert.That(settings.Name).IsEqualTo("resolved-value");
		}

		[Test]
		public async Task AddSimpleSettings_RegistersSettingsAsSingleton()
		{
			var services = new ServiceCollection();
			services.AddSimpleSettings(o => o.AddAssemblies([typeof(IDiExampleSettings).Assembly]));

			var provider = services.BuildServiceProvider();
			var first = provider.GetRequiredService<IDiExampleSettings>();
			var second = provider.GetRequiredService<IDiExampleSettings>();

			// The scanned settings are registered as singleton instances.
			await Assert.That(ReferenceEquals(first, second)).IsTrue();
		}

		[Test]
		public async Task AddSimpleSettings_RegistersSettingsProvider_ThatBindsValues()
		{
			var collection = new InMemoryCollection();
			collection.Add(Section, nameof(IDiExampleSettings.Name), "from-provider");

			var services = new ServiceCollection();
			services.AddSimpleSettings(o =>
			{
				o.AddAssemblies([typeof(IDiExampleSettings).Assembly]);
				o.AddSectionBinder(new InMemoryBinder(collection));
			});

			var provider = services.BuildServiceProvider();
			var settingsProvider = provider.GetRequiredService<ISettingsProvider>();

			var settings = settingsProvider.GetSettings<IDiExampleSettings>();

			await Assert.That(settings.Name).IsEqualTo("from-provider");
		}

		[Test]
		public async Task AddSimpleSettings_Provider_ReturnsTheSameInstanceAsTheContainer()
		{
			var services = new ServiceCollection();
			services.AddSimpleSettings(o => o.AddAssemblies([typeof(IDiExampleSettings).Assembly]));

			var provider = services.BuildServiceProvider();
			var fromContainer = provider.GetRequiredService<IDiExampleSettings>();
			var settingsProvider = provider.GetRequiredService<ISettingsProvider>();

			var first = settingsProvider.GetSettings<IDiExampleSettings>();
			var second = settingsProvider.GetSettings<IDiExampleSettings>();

			// C3: the provider serves the startup-built singleton, so both resolution paths agree and
			// repeat resolves return the same cached instance instead of re-binding a fresh one.
			await Assert.That(ReferenceEquals(fromContainer, first)).IsTrue();
			await Assert.That(ReferenceEquals(first, second)).IsTrue();
		}

		[Test]
		public async Task AddSimpleSettings_NullAction_Throws()
		{
			var services = new ServiceCollection();

			await Assert.That(() => services.AddSimpleSettings(null!)).Throws<ArgumentNullException>();
		}

		[Test]
		public async Task AddSimpleSettings_RegistersSettingsCollection_ServingTheContainerInstance()
		{
			var services = new ServiceCollection();
			services.AddSimpleSettings(o => o.AddAssemblies([typeof(IDiExampleSettings).Assembly]));

			var provider = services.BuildServiceProvider();
			var collection = provider.GetRequiredService<ISettingsCollection>();

			// The collection serves the same startup-built instance the container resolves for the interface.
			await Assert.That(ReferenceEquals(collection.GetSettings<IDiExampleSettings>(),
				provider.GetRequiredService<IDiExampleSettings>())).IsTrue();
		}

		[Test]
		public async Task AddSimpleSettings_OutOverload_SurfacesBoundCollection_AndPreservesChain()
		{
			var collection = new InMemoryCollection();
			collection.Add(Section, nameof(IDiExampleSettings.Name), "out-value");

			var services = new ServiceCollection();
			var returned = services.AddSimpleSettings(out var settings, o =>
			{
				o.AddAssemblies([typeof(IDiExampleSettings).Assembly]);
				o.AddSectionBinder(new InMemoryBinder(collection));
			});

			await Assert.That(ReferenceEquals(returned, services)).IsTrue();
			await Assert.That(settings.GetSettings<IDiExampleSettings>().Name).IsEqualTo("out-value");
		}

		[Test]
		public async Task AddSimpleSettings_OutOverload_SurfacesSameInstanceAsResolvedSingleton()
		{
			var services = new ServiceCollection();
			services.AddSimpleSettings(out var settings, o => o.AddAssemblies([typeof(IDiExampleSettings).Assembly]));

			var provider = services.BuildServiceProvider();

			await Assert.That(ReferenceEquals(settings, provider.GetRequiredService<ISettingsCollection>())).IsTrue();
		}

		[Test]
		public async Task ValidateSimpleSettings_WhenDiValidatorFails_ThrowsAggregatedException()
		{
			var services = new ServiceCollection();
			services.AddSimpleSettings(o => o.AddAssemblies([typeof(IDiValidatedSettings).Assembly]));
			services.AddSingleton<ValidatorDependency>();
			services.AddSingleton<ISettingValidation<IDiValidatedSettings>, FailingDiValidator>();

			var provider = services.BuildServiceProvider();

			var exception = await Assert.That(() => provider.ValidateSimpleSettings())
				.Throws<SettingsValidationException>();

			// Same contract as the core path: an aggregated SettingsValidationException carrying the errors.
			await Assert.That(exception!.Errors.Count).IsEqualTo(1);
			await Assert.That(exception.Errors[0].ErrorMessage).IsEqualTo("di validation failed");
		}

		[Test]
		public async Task ValidateSimpleSettings_WhenDiValidatorPasses_DoesNotThrow_AndReturnsProvider()
		{
			var services = new ServiceCollection();
			services.AddSimpleSettings(o => o.AddAssemblies([typeof(IDiValidatedSettings).Assembly]));
			services.AddSingleton<ISettingValidation<IDiValidatedSettings>, PassingDiValidator>();

			var provider = services.BuildServiceProvider();

			var result = provider.ValidateSimpleSettings();

			await Assert.That(ReferenceEquals(result, provider)).IsTrue();
		}

		[Test]
		public async Task AddSimpleSettings_DoesNotRunDiValidators_UntilValidateIsCalled()
		{
			var services = new ServiceCollection();
			services.AddSimpleSettings(o => o.AddAssemblies([typeof(IDiValidatedSettings).Assembly]));
			services.AddSingleton<ValidatorDependency>();
			services.AddSingleton<ISettingValidation<IDiValidatedSettings>, FailingDiValidator>();

			// AddSimpleSettings and BuildServiceProvider must complete without running the DI validator.
			var provider = services.BuildServiceProvider();

			await Assert.That(() => provider.ValidateSimpleSettings()).Throws<SettingsValidationException>();
		}

		[Test]
		public async Task ValidateSimpleSettings_ResolvesValidatorFromFreshScope_ForScopedDependencies()
		{
			var services = new ServiceCollection();
			services.AddSimpleSettings(o => o.AddAssemblies([typeof(IScopedValidatedSettings).Assembly]));
			services.AddScoped<ScopedDependency>();
			services.AddScoped<ISettingValidation<IScopedValidatedSettings>, ScopedDependentValidator>();

			// Under scope validation, resolving the scoped validator from the root provider would throw; the
			// runner resolves it from a fresh scope, so this succeeds.
			var provider = services.BuildServiceProvider(validateScopes: true);

			var result = provider.ValidateSimpleSettings();

			await Assert.That(ReferenceEquals(result, provider)).IsTrue();
		}

		[Test]
		public async Task ValidateSimpleSettings_WhenValidatorThrows_RedactsBoundValue()
		{
			const string secret = "REDACTION-SENTINEL-8F3A2C";
			var config = new InMemoryCollection();
			config.Add(nameof(IDiSecretSettings).Substring(1), nameof(IDiSecretSettings.ApiKey), secret);

			var services = new ServiceCollection();
			services.AddSimpleSettings(o =>
			{
				o.AddAssemblies([typeof(IDiSecretSettings).Assembly]);
				o.AddSectionBinder(new InMemoryBinder(config));
			});
			services.AddSingleton<ISettingValidation<IDiSecretSettings>, ThrowingDiValidator>();

			var provider = services.BuildServiceProvider();

			// A throwing validator surfaces value-free: the invocation exception carries only type names.
			var exception = await Assert.That(() => provider.ValidateSimpleSettings())
				.Throws<SettingsValidatorInvocationException>();

			await Assert.That(exception!.ToString().Contains(secret)).IsFalse();
		}

		public interface IDiExampleSettings
		{
			string Name { get; set; }
		}

		public interface IDiValidatedSettings
		{
			string Name { get; set; }
		}

		public interface IScopedValidatedSettings
		{
			string Name { get; set; }
		}

		public interface IDiSecretSettings
		{
			string ApiKey { get; set; }
		}

		public sealed class ValidatorDependency
		{
		}

		public sealed class ScopedDependency
		{
		}

		public sealed class FailingDiValidator : ISettingValidation<IDiValidatedSettings>
		{
			public FailingDiValidator(ValidatorDependency dependency)
			{
			}

			public ValidationResult Validate(ValidationContext<IDiValidatedSettings> context)
			{
				var result = new ValidationResult();
				result.AddError(new ValidationError(nameof(IDiValidatedSettings.Name), "di validation failed"));
				return result;
			}
		}

		public sealed class PassingDiValidator : ISettingValidation<IDiValidatedSettings>
		{
			public ValidationResult Validate(ValidationContext<IDiValidatedSettings> context) => new();
		}

		public sealed class ScopedDependentValidator : ISettingValidation<IScopedValidatedSettings>
		{
			public ScopedDependentValidator(ScopedDependency dependency)
			{
			}

			public ValidationResult Validate(ValidationContext<IScopedValidatedSettings> context) => new();
		}

		public sealed class ThrowingDiValidator : ISettingValidation<IDiSecretSettings>
		{
			public ValidationResult Validate(ValidationContext<IDiSecretSettings> context)
				=> throw new InvalidOperationException($"boom with {context.Settings!.ApiKey}");
		}
	}
}
