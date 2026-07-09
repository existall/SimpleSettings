using System;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
    public class SettingsCollectionTests
    {
        private const string SomeName = "SomeName";

        [Test]
        public async Task GetSettings_WhenTypeIsNotInterface_ShouldThrowException()
        {
            var sut = SettingsBuilder.CreateBuilder();

            await Assert.That(() => sut.GetSettings<SettingsBuilder>()).Throws<InvalidOperationException>();
        }

        [Test]
        public async Task GetSettings_BuildingInterfaceNotFromAssembly_ShouldReturnInstanceWithValues()
        {
            var sut = SettingsBuilder.CreateBuilder();

            var result = sut.GetSettings<ISomeInterface>();

            await Assert.That(result.Name).IsEqualTo(SomeName);
        }

        public interface ISomeInterface
        {
            [SettingsProperty(DefaultValue = SomeName)]
            string Name { get; set; }
        }
    }
}
