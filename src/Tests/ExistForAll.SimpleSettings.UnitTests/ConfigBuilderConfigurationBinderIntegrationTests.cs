using System;
using System.IO;
using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.Binders;
using Microsoft.Extensions.Configuration;

namespace ExistForAll.SimpleSettings.UnitTests
{
	public class SettingsBuilderConfigurationBinderIntegrationTests
	{
		[Test]
		public async Task Build_WhenInterfaceHasSettingsNameSet_ShouldGetDataFromName()
		{
			var configuration = GetConfiguration();

			var sut = BuildSutWithBinder(new ConfigurationBinder(configuration));

			var settingsCollection = sut.ScanAssemblies(GetType().Assembly);

			var settings = settingsCollection.GetSettings<ISettingsInterfaceRootName>();

			await Assert.That(settings.Value).IsEqualTo(44);
		}

		[Test]
		public async Task Build_WhenInterfaceHasRootNameAndPropertyName_ShouldGetTheCorrectData()
		{
			var configuration = GetConfiguration();

			var sut = BuildSutWithBinder(new ConfigurationBinder(configuration));

			var settingsCollection = sut.ScanAssemblies(GetType().Assembly);

			var settings = settingsCollection.GetSettings<ISectionNameAndProperty>();

			await Assert.That(settings.Value).IsEqualTo("some-value");
		}

		[Test]
		public async Task Build_WhenDataIsInAppSettingsRootWithMatchingNames_ShouldGetValue()
		{
			var configuration = GetConfiguration();

			var sut = BuildSutWithBinder(new ConfigurationBinder(configuration));

			var result = sut.ScanAssemblies(GetType().Assembly);

			var settings = result.GetSettings<IRoot>();

			await Assert.That(settings.Value).IsEqualTo("albert");
		}

		[Test]
		public async Task Build_WhenConfigurationBinderHasDifferentRootName_ShouldGetDataFromInnerRootSections()
		{
			var configuration = GetConfiguration();

			var sut = BuildSutWithBinder(new ConfigurationBinder(configuration, "appSettings"));

			var result = sut.ScanAssemblies(GetType().Assembly);

			var settings = result.GetSettings<IRoot>();

			await Assert.That(settings.Value).IsEqualTo("value");
		}

		[Test]
		public async Task Build_WhenMemoryBinderSetValues_ShouldSetProperData()
		{
			var value = Guid.NewGuid().ToString();
			var collection = new InMemoryCollection();
			collection.Add("Root", "Value", value);

			var sut = BuildSutWithBinder(new InMemoryBinder(collection));

			var result = sut.ScanAssemblies(GetType().Assembly);

			var settings = result.GetSettings<IRoot>();

			await Assert.That(settings.Value).IsEqualTo(value);
		}

		private IConfiguration GetConfiguration()
		{
			return new ConfigurationBuilder()
				.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "../../../appSettings.json"))
				.Build();
		}

		private static SettingsBuilder BuildSutWithBinder(params ISectionBinder[] binders)
		{
			var sut = SettingsBuilder.CreateBuilder(x =>
			{
				foreach (var sectionBinder in binders)
				{
					x.AddSectionBinder(sectionBinder);
				}
			});

			return sut;
		}
	}
}
