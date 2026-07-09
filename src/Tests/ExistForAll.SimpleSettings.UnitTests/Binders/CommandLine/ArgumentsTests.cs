using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.Binders;

namespace ExistForAll.SimpleSettings.UnitTests.Binders.CommandLine
{
    public class ArgumentsTests
    {
        private const string Args = " name=value age:3";

        [Test]
        public async Task Build_WhenNameMatchKey_ShouldPlaceValue()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(Args.Split(' ')));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsEqualTo("value");
            await Assert.That(result.Age).IsNotEqualTo(3);
        }

        [Test]
        public async Task Build_WhenNameMatchKeyInSensitive_ShouldPlaceValue()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(Args.Split(' '),
                    o => o.SetCaseSensitivity(false)));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsEqualTo("value");
            await Assert.That(result.Age).IsEqualTo(3);
        }

        [Test]
        public async Task Build_WhenArgumentAreAfterInMemory_ShouldUseArgumentBinder()
        {
            var section = nameof(ICommandLineInterface).TrimStart('I');
            var collection = new InMemoryCollection();
            collection.Add(section, "name", "name");
            collection.Add(section, "Age", "15");

            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddInMemoryCollection(collection));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsEqualTo("name");
            await Assert.That(result.Age).IsEqualTo(15);

            sut = SettingsBuilder.CreateBuilder(x =>
                x.AddInMemoryCollection(collection)
                    .AddArguments(Args.Split(' '),
                        o => o.SetCaseSensitivity(false)));

            result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsEqualTo("value");
            await Assert.That(result.Age).IsEqualTo(3);
        }

        public interface ICommandLineInterface
        {
            [SettingsProperty("name")]
            string Name { get; set; }
            int Age { get; set; }
        }
    }
}
