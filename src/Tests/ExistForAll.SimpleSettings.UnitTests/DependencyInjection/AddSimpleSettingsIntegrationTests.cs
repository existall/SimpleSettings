using System;
using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.Extensions.GenericHost;
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
				o.AddAssemblies(new[] { typeof(IDiExampleSettings).Assembly });
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
			services.AddSimpleSettings(o => o.AddAssemblies(new[] { typeof(IDiExampleSettings).Assembly }));

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
				o.AddAssemblies(new[] { typeof(IDiExampleSettings).Assembly });
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
			services.AddSimpleSettings(o => o.AddAssemblies(new[] { typeof(IDiExampleSettings).Assembly }));

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

		public interface IDiExampleSettings
		{
			string Name { get; set; }
		}
	}
}
