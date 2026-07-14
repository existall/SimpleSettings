using System.Reflection;
using ExistForAll.SimpleSettings.Core;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	public class SettingsTypesExtractorTests
	{
		[Test]
		public async Task ExtractSettingsTypes_WhenTypeHasNoIndications_ShouldNotExtractType()
		{
			var sut = new SettingsTypesExtractor();

			var assemblyCollection = MockAssemblies(typeof(INonIndicationInterface));

			var results = sut.ExtractSettingsTypes(assemblyCollection, new SettingsOptions());

			await Assert.That(results).DoesNotContain(typeof(INonIndicationInterface));
		}

		[Test]
		public async Task ExtractSettingsTypes_WhenTypeHasAttributeIndications_ShouldExtractType()
		{
			var sut = new SettingsTypesExtractor();

			var assemblyCollection = MockAssemblies(typeof(IAttributeIndicationInterface));

			var results = sut.ExtractSettingsTypes(assemblyCollection, new SettingsOptions());

			await Assert.That(results).Contains(typeof(IAttributeIndicationInterface));
		}

		[Test]
		public async Task ExtractSettingsTypes_WhenTypeHasInterfaceIndications_ShouldExtractType()
		{
			var sut = new SettingsTypesExtractor();

			var assemblyCollection = MockAssemblies(typeof(IIndicationInterface));

			var results = sut.ExtractSettingsTypes(assemblyCollection, new SettingsOptions());

			await Assert.That(results).Contains(typeof(IIndicationInterface));
		}

		[Test]
		public async Task ExtractSettingsTypes_WhenTypeHasSettingsSuffixIndications_ShouldExtractType()
		{
			var sut = new SettingsTypesExtractor();

			var assemblyCollection = MockAssemblies(typeof(IIndicationInterfaceSettings));

			var results = sut.ExtractSettingsTypes(assemblyCollection, new SettingsOptions());

			await Assert.That(results).Contains(typeof(IIndicationInterfaceSettings));
		}

		[Test]
		public async Task ExtractSettingsTypes_WhenSuffixDiffersOnlyByCase_ShouldExtractType()
		{
			var sut = new SettingsTypesExtractor();

			// The name ends with "SETTINGS" while the default suffix is "Settings"; matching must be
			// case-insensitive (a case-sensitive EndsWith would miss this).
			var assemblyCollection = MockAssemblies(typeof(ICasingMismatchSETTINGS));

			var results = sut.ExtractSettingsTypes(assemblyCollection, new SettingsOptions());

			await Assert.That(results).Contains(typeof(ICasingMismatchSETTINGS));
		}

		private IEnumerable<Assembly> MockAssemblies(Type returnType)
		{
			return [returnType.GetTypeInfo().Assembly];
		}

		public interface ICasingMismatchSETTINGS
		{
		}
	}
}
