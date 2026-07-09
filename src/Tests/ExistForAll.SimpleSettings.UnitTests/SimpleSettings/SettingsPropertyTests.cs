namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	public class SettingsPropertyTests
	{
		[Test]
		public async Task Build_WhenAllowEmptyIsFalse_ShouldThrowException()
		{
			var sut = SettingsBuilder.CreateBuilder();

			await Assert.That(() => sut.GetSettings<IWithNonNullInterface>()).Throws<SettingsPropertyValueException>();
		}

		public interface IWithNonNullInterface
		{
			[SettingsProperty(AllowEmpty = false)]
			int Value { get; set; }
		}
	}
}
