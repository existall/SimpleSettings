using ExistForAll.SimpleSettings.Binder;

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

		[Test]
		public async Task Build_WhenAllowEmptyIsFalse_EmptyString_ShouldThrowNullException()
		{
			var sut = BuildWith(nameof(IRejectsEmpty.Value), "");

			await Assert.That(() => sut.GetSettings<IRejectsEmpty>())
				.Throws<SettingsPropertyNullException>();
		}

		[Test]
		public async Task Build_WhenAllowEmptyIsFalse_WhitespaceString_ShouldThrowNullException()
		{
			var sut = BuildWith(nameof(IRejectsEmpty.Value), "   ");

			await Assert.That(() => sut.GetSettings<IRejectsEmpty>())
				.Throws<SettingsPropertyNullException>();
		}

		[Test]
		public async Task Build_WhenAllowEmptyIsFalse_EmptyString_MessageIsValueFreeWithPropertyName()
		{
			var sut = BuildWith(nameof(IRejectsEmpty.Value), "   ");

			var exception = await Assert.That(() => sut.GetSettings<IRejectsEmpty>())
				.Throws<SettingsPropertyNullException>();

			await Assert.That(exception!.Message.Contains(nameof(IRejectsEmpty.Value))).IsTrue();
			await Assert.That(exception!.Message.Contains("   ")).IsFalse();
		}

		[Test]
		public async Task Build_WhenAllowEmptyIsTrue_EmptyString_BindsValue()
		{
			var sut = BuildWith(nameof(IAllowsEmpty.Value), "");

			var settings = sut.GetSettings<IAllowsEmpty>();

			await Assert.That(settings.Value).IsEqualTo("");
		}

		[Test]
		public async Task Build_WhenAllowEmptyIsTrue_WhitespaceString_BindsValue()
		{
			var sut = BuildWith(nameof(IAllowsEmpty.Value), "   ");

			var settings = sut.GetSettings<IAllowsEmpty>();

			await Assert.That(settings.Value).IsEqualTo("   ");
		}

		private static SettingsBuilder BuildWith(string key, string value)
		{
			var collection = new InMemoryCollection();
			collection.Add("RejectsEmpty", key, value);
			collection.Add("AllowsEmpty", key, value);

			return SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));
		}

		public interface IWithNonNullInterface
		{
			[SettingsProperty(AllowEmpty = false)]
			int Value { get; set; }
		}

		public interface IRejectsEmpty
		{
			[SettingsProperty(AllowEmpty = false)]
			string Value { get; set; }
		}

		public interface IAllowsEmpty
		{
			string Value { get; set; }
		}
	}
}
