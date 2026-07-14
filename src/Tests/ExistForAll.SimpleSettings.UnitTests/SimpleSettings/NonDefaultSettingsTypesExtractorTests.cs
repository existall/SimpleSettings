using System.Reflection;
using ExistForAll.SimpleSettings.Core;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	public class NonDefaultSettingsTypesExtractorTests
	{
		[Test]
		public async Task ExtractSettingsTypes_WhenTypeHasNonDefaultAttributeIndication_ShouldExtractType()
		{
			var sut = new SettingsTypesExtractor();

			var assemblyCollection = MockAssemblies(typeof(INonDefaultAttributeInterface));

			var results = sut.ExtractSettingsTypes(assemblyCollection, new SettingsOptions()
			{
				AttributeType = typeof(SomeOtherAttribute)
			});

			await Assert.That(results).Contains(typeof(INonDefaultAttributeInterface));
		}

		[Test]
		public async Task ExtractSettingsTypes_WhenTypeHasNonDefaultInterfaceIndidation_ShouldExtractType()
		{
			var sut = new SettingsTypesExtractor();

			var assemblyCollection = MockAssemblies(typeof(INonDefaultInterfaceInterface));

			var results = sut.ExtractSettingsTypes(assemblyCollection, new SettingsOptions()
			{
				InterfaceBase = typeof(INonDefaultInterfaceIndication)
			});

			await Assert.That(results).Contains(typeof(INonDefaultInterfaceInterface));
		}

		[Test]
		public async Task ExtractSettingsTypes_WhenTypeHasNonDefaultSuffixIndidation_ShouldExtractType()
		{
			var sut = new SettingsTypesExtractor();

			var assemblyCollection = MockAssemblies(typeof(INonDefaultSuffixIndicationInterfaceSomeSuffix));

			var results = sut.ExtractSettingsTypes(assemblyCollection, new SettingsOptions()
			{
				SettingsSuffix = "SomeSuffix"
			});

			await Assert.That(results).Contains(typeof(INonDefaultSuffixIndicationInterfaceSomeSuffix));
		}

		private IEnumerable<Assembly> MockAssemblies(Type returnType)
		{
			return [returnType.GetTypeInfo().Assembly];
		}

		public interface INonDefaultSuffixIndicationInterfaceSomeSuffix
		{

		}

		public interface INonDefaultInterfaceIndication
		{

		}

		public interface INonDefaultInterfaceInterface : INonDefaultInterfaceIndication
		{

		}

		[SomeOther]
		public interface INonDefaultAttributeInterface
		{

		}

		public class SomeOtherAttribute : Attribute
		{

		}
	}
}
