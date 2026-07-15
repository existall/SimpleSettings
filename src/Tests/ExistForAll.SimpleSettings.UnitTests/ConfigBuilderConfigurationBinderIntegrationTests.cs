using System.Linq;
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

		[Test]
		public async Task Bind_ChildSequence_ToStringArray_BindsEachElementInOrder()
		{
			var settings = BindSequence<ISequenceStringSetting>(new Dictionary<string, string?>
			{
				["SequenceStringSetting:Values:0"] = "a",
				["SequenceStringSetting:Values:1"] = "b",
				["SequenceStringSetting:Values:2"] = "c",
			});

			await Assert.That(settings.Values.SequenceEqual(new[] { "a", "b", "c" })).IsTrue();
		}

		[Test]
		public async Task Bind_ChildSequence_ToIntArray_BindsEachElementInOrder()
		{
			var settings = BindSequence<ISequenceIntSetting>(new Dictionary<string, string?>
			{
				["SequenceIntSetting:Numbers:0"] = "1",
				["SequenceIntSetting:Numbers:1"] = "2",
				["SequenceIntSetting:Numbers:2"] = "3",
			});

			await Assert.That(settings.Numbers.SequenceEqual(new[] { 1, 2, 3 })).IsTrue();
		}

		[Test]
		public async Task Bind_ChildSequence_WithEmptyElements_DropsThemLikeCommaScalar()
		{
			// Empty entries are dropped for parity with the comma-scalar RemoveEmptyEntries, so an interspersed
			// empty element does not crash the int[] bind (review MED-2).
			var settings = BindSequence<ISequenceIntSetting>(new Dictionary<string, string?>
			{
				["SequenceIntSetting:Numbers:0"] = "1",
				["SequenceIntSetting:Numbers:1"] = "",
				["SequenceIntSetting:Numbers:2"] = "3",
			});

			await Assert.That(settings.Numbers.SequenceEqual(new[] { 1, 3 })).IsTrue();
		}

		[Test]
		public async Task Bind_ChildSequence_ToStringList_BindsEachElementInOrder()
		{
			var settings = BindSequence<ISequenceStringListSetting>(new Dictionary<string, string?>
			{
				["SequenceStringListSetting:Items:0"] = "x",
				["SequenceStringListSetting:Items:1"] = "y",
			});

			await Assert.That(settings.Items.SequenceEqual(new List<string> { "x", "y" })).IsTrue();
		}

		[Test]
		public async Task Bind_ScalarAndChildren_ChildrenWin()
		{
			var settings = BindSequence<ISequenceStringSetting>(new Dictionary<string, string?>
			{
				["SequenceStringSetting:Values"] = "scalar-should-lose",
				["SequenceStringSetting:Values:0"] = "a",
				["SequenceStringSetting:Values:1"] = "b",
			});

			await Assert.That(settings.Values.SequenceEqual(new[] { "a", "b" })).IsTrue();
		}

		[Test]
		public async Task Bind_CommaScalar_NoChildren_StillBinds()
		{
			var settings = BindSequence<ISequenceStringSetting>(new Dictionary<string, string?>
			{
				["SequenceStringSetting:Values"] = "a,b,c",
			});

			await Assert.That(settings.Values.SequenceEqual(new[] { "a", "b", "c" })).IsTrue();
		}

		[Test]
		public async Task Bind_WhitespaceScalar_ToStringArray_BindsEmpty()
		{
			var settings = BindSequence<ISequenceStringSetting>(new Dictionary<string, string?>
			{
				["SequenceStringSetting:Values"] = "   ",
			});

			await Assert.That(settings.Values.Length).IsEqualTo(0);
		}

		[Test]
		public async Task Bind_EmptySequence_BindsEmpty()
		{
			var settings = BindSequence<ISequenceStringSetting>(new Dictionary<string, string?>());

			await Assert.That(settings.Values.Length).IsEqualTo(0);
		}

		[Test]
		public async Task Bind_ChildSequence_WithRootSection_ResolvesUnderPrefix()
		{
			var settings = BindSequence<ISequenceStringSetting>(new Dictionary<string, string?>
			{
				["MyRoot:SequenceStringSetting:Values:0"] = "a",
				["MyRoot:SequenceStringSetting:Values:1"] = "b",
			}, "MyRoot");

			await Assert.That(settings.Values.SequenceEqual(new[] { "a", "b" })).IsTrue();
		}

		private static T BindSequence<T>(Dictionary<string, string?> data, string? rootSection = null) where T : class
		{
			var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

			return SettingsBuilder
				.CreateBuilder(x => x.AddSectionBinder(new ConfigurationBinder(configuration, rootSection)))
				.GetSettings<T>();
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

		public interface ISequenceStringSetting
		{
			string[] Values { get; set; }
		}

		public interface ISequenceIntSetting
		{
			int[] Numbers { get; set; }
		}

		public interface ISequenceStringListSetting
		{
			List<string> Items { get; set; }
		}
	}
}
