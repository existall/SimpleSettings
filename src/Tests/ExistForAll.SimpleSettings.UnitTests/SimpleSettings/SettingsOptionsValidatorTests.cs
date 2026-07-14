using ExistForAll.SimpleSettings.Core;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{

	public class SettingsOptionsValidatorTests
	{
		[Test]
		[Arguments(null, null, null)]
		[Arguments(null, null, "")]
		[Arguments(null, null, "  ")]
		public async Task ValidateOptions_WhenAttributeAndInterfaceAndSuffixAreMissing_ShouldThrowException(Type? attributeType,
			Type? interfaceType,
			string? suffix)
		{
			var options = new SettingsOptions()
			{
				AttributeType = attributeType!,
				InterfaceBase = interfaceType!,
				SettingsSuffix = suffix!
			};

			var sut = GetSut();

			await Assert.That(() => sut.ValidateOptions(options)).Throws<SettingsOptionsArgumentNullException>();
		}

		[Test]
		public async Task ValidateOptions_WhenAttributeTypeIsNotAnAttribute_ShouldThrowException()
		{
			var options = new SettingsOptions()
			{
				AttributeType = typeof(NotAttribute)
			};

			var sut = GetSut();

			await Assert.That(() => sut.ValidateOptions(options)).Throws<SettingsOptionNonAttributeException>();
		}

		[Test]
		public async Task ValidateOptions_WhenAttributeTypeIAnAttribute_ShouldPassValidation()
		{
			var options = new SettingsOptions()
			{
				AttributeType = typeof(SomeAttribute)
			};

			var sut = GetSut();

			await Assert.That(() => sut.ValidateOptions(options)).ThrowsNothing();
		}

		[Test]
		[Arguments((string?)null)]
		[Arguments(" ")]
		[Arguments("")]
		public async Task ValidateOptions_WhenHasNoArrayDelimiter_ShouldThrowException(string? delimiter)
		{
			var options = new SettingsOptions()
			{
				ArraySplitDelimiter = delimiter!
			};

			var sut = GetSut();

			await Assert.That(() => sut.ValidateOptions(options)).Throws<SettingsOptionsArgumentMissingException>();
		}

		[Test]
		[Arguments((string?)null)]
		[Arguments(" ")]
		[Arguments("")]
		public async Task ValidateOptions_WhenHasNoDateTimeFormat_ShouldThrowException(string? format)
		{
			var options = new SettingsOptions()
			{
				DateTimeFormat = format!
			};

			var sut = GetSut();

			await Assert.That(() => sut.ValidateOptions(options)).Throws<SettingsOptionsArgumentMissingException>();
		}

		[Test]
		public async Task ValidateOptions_WhenHasNoSectionNameFormater_ShouldThrowException()
		{
			var options = new SettingsOptions
			{
				SectionNameFormatter = null!
			};
			var sut = GetSut();

			await Assert.That(() => sut.ValidateOptions(options)).Throws<SettingsOptionsArgumentMissingException>();
		}

		private SettingsOptionsValidator GetSut()
		{
			return new SettingsOptionsValidator();
		}

		private class SomeAttribute : Attribute
		{

		}

		private class NotAttribute
		{

		}
	}
}
