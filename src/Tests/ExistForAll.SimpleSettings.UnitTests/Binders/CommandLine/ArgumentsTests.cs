using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.Binders;

namespace ExistForAll.SimpleSettings.UnitTests.Binders.CommandLine
{
    public class ArgumentsTests
    {
        private const string Args = " name=value age:3";
        private const string CliSecretSentinel = "cli-S3CR3T-do-not-log";

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

        [Test]
        public async Task Build_WhenInlineEqualsAtIndexZero_BindsWithoutDrop()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(new[] { "--name=value" }));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsEqualTo("value");
        }

        [Test]
        public async Task Build_WhenInlineColon_BindsValue()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(new[] { "--name:value" }));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsEqualTo("value");
        }

        [Test]
        public async Task Build_WhenSpaceSeparated_BindsValue()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(new[] { "--name", "value" }));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsEqualTo("value");
        }

        [Test]
        public async Task Build_WhenSpaceSeparatedValueHasSpaces_BindsFullValue()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(new[] { "--name", "John Doe" }));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsEqualTo("John Doe");
        }

        [Test]
        public async Task Build_WhenNextTokenIsPrefixed_KeyIsNotStored()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(new[] { "--name", "--other" }));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsNull();
        }

        [Test]
        public async Task Build_WhenSkipFirstArgumentFalse_BindsIndexZero()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(new[] { "--name", "value" }));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsEqualTo("value");
        }

        [Test]
        public async Task Build_WhenSkipFirstArgumentTrue_SkipsIndexZero()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(new[] { "--name", "value" },
                    o => o.SkipFirstArgument = true));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsNull();
        }

        [Test]
        public async Task Build_WhenEmptyToken_ParsesSafelyAndBindsRest()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(new[] { "", "--name", "value" }));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsEqualTo("value");
        }

        [Test]
        public async Task Build_WhenSpaceSeparatedValueIsPrefixed_DoesNotBind()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(new[] { "--name", "/etc/hosts" }));

            var result = sut.GetSettings<ICommandLineInterface>();

            await Assert.That(result.Name).IsNull();
        }

        [Test]
        public async Task Build_WhenCliValueUnconvertible_ExceptionExcludesValue()
        {
            var sut = SettingsBuilder.CreateBuilder(x =>
                x.AddArguments(new[] { "--Age=" + CliSecretSentinel }));

            SettingsPropertyValueException? captured = null;
            try
            {
                sut.GetSettings<ICommandLineInterface>();
            }
            catch (SettingsPropertyValueException e)
            {
                captured = e;
            }

            await Assert.That(captured).IsNotNull();
            var full = captured!.ToString();
            await Assert.That(full.Contains(CliSecretSentinel)).IsFalse();
            await Assert.That(full.Contains(nameof(ICommandLineInterface.Age))).IsTrue();
            await Assert.That(full.Contains(nameof(System.Int32))).IsTrue();
        }

        public interface ICommandLineInterface
        {
            [SettingsProperty("name")]
            string Name { get; set; }
            int Age { get; set; }
        }
    }
}
