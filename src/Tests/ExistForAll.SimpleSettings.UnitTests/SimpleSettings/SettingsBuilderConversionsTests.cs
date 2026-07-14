using ExistForAll.SimpleSettings.Conversion;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	public class SettingsBuilderConversionsTests
	{
		[Test]
		public async Task Build_WhenAddGlobalConverter_ShouldReturnConverterValue()
		{
			var sut = SettingsBuilder.CreateBuilder(x => x.AddTypeConverter(new GuidSettingsConvertor()));
			var result = sut.GetSettings<IGuidInterface>();

			await Assert.That(result.Guid).IsNotEqualTo(Guid.Empty);
		}

		[Test]
		public async Task Build_WhenAddLocalConverter_ShouldReturnConverterValue()
		{
			var sut = SettingsBuilder.CreateBuilder();
			var result = sut.GetSettings<IGuidInterfaceWithConversionAttribute>();

			await Assert.That(result.Guid).IsNotEqualTo(Guid.Empty);
		}

		public interface IGuidInterface
		{
			[EmptyGuid]
			Guid Guid { get; set; }
		}

		public interface IGuidInterfaceWithConversionAttribute
		{
			[EmptyGuid(ConverterType = typeof(GuidSettingsConvertor))]
			Guid Guid { get; set; }
		}

		private class EmptyGuidAttribute : SettingsPropertyAttribute
		{
			public EmptyGuidAttribute()
				: base()
			{
				DefaultValue = Guid.Empty;
			}
		}

		private class GuidSettingsConvertor : ISettingsTypeConverter
		{
			public bool CanConvert(Type configType)
			{
				return configType == typeof(Guid);
			}

			public object Convert(object value, Type configType)
			{
				return Guid.NewGuid();
			}
		}
	}
}
