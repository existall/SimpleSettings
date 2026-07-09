using System.Collections.Generic;
using System.Linq;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
    public class SettingsEnumerableTests
    {
        private const int N12 = 12;
        private const int N3 = 3;
        private const int N4 = 4;
        private const int N103 = 103;

        [Test]
        public async Task Build_WhereVariableHasValue_ShouldSetProperty()
        {
            var sut = SettingsBuilder.CreateBuilder();

            var result = sut.GetSettings<IEnumerableInterface>();

            await Assert.That(result.Values.Count()).IsEqualTo(4);

            await Assert.That(result.Values).Contains(N12);
            await Assert.That(result.Values).Contains(N3);
            await Assert.That(result.Values).Contains(N4);
            await Assert.That(result.Values).Contains(N103);
        }

        [SettingsSection]
        public interface IEnumerableInterface
        {
            [SettingsProperty(DefaultValue = new[] {N12, N3, N4, N103})]
            IEnumerable<int> Values { get; set; }
        }
    }
}
