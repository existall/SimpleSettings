namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	public class SettingsPropertyTests
	{
		[Test]
		public async Task Build_WhenAllowEmptyIsFalse_ShouldThrowException()
		{
			var sut = SettingsBuilder.CreateBuilder();

			// A required-but-missing value is a distinct, value-free failure (SettingsPropertyNullException),
			// not a conversion failure — so it keeps its full, informative message (property name). See S1.
			var exception = await Assert.That(() => sut.GetSettings<IWithNonNullInterface>())
				.Throws<SettingsPropertyNullException>();

			await Assert.That(exception!.Message.Contains(nameof(IWithNonNullInterface.Value))).IsTrue();
		}

		public interface IWithNonNullInterface
		{
			[SettingsProperty(AllowEmpty = false)]
			int Value { get; set; }
		}
	}
}
