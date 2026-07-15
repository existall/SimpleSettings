using System.Reflection;
using ExistForAll.SimpleSettings.Conversion;
using ExistForAll.SimpleSettings.Core.Reflection;

namespace ExistForAll.SimpleSettings.UnitTests.Core
{
	// Locks the three genuinely-uncovered TypeConverter resolution paths (TEST-02) via the direct
	// converter-orchestration seam (RESEARCH Pattern 2): null -> value-type default, Nullable<int>
	// strip-and-convert, and an attribute ConverterType bypassing the collection converter on an
	// IEnumerable<T> property. Reaches the internal seam via InternalsVisibleTo. Does NOT re-assert the
	// empty-enumerable or AllowEmpty paths (owned by CollectionConversionTests / SettingsPropertyTests).
	public class TypeConverterTests
	{
		[Test]
		public async Task Convert_NullForNonNullableValueType_ReturnsTypeDefault()
		{
			var conversion = ResolveConversion<ISample>(nameof(ISample.Count));

			await Assert.That(conversion.Convert(null)).IsEqualTo(0);
		}

		[Test]
		public async Task Convert_NullForNullableValueType_ReturnsNull()
		{
			// RESEARCH Pitfall 3: the null result is built on the ORIGINAL nullable type, so int? null -> null
			// (NOT 0).
			var conversion = ResolveConversion<ISample>(nameof(ISample.Maybe));

			await Assert.That(conversion.Convert(null)).IsNull();
		}

		[Test]
		public async Task Convert_NumericStringForNullableValueType_StripsAndConverts()
		{
			// StripIfNullable unwraps int? to int, DefaultTypeConverter parses "42" -> 42.
			var conversion = ResolveConversion<ISample>(nameof(ISample.Maybe));

			await Assert.That(conversion.Convert("42")).IsEqualTo(42);
		}

		[Test]
		public async Task Convert_WhenConverterTypeSetOnCollectionProperty_BypassesCollectionConverter()
		{
			// GetConverter returns the attribute's ConverterType before the collection-converter scan, even
			// for an IEnumerable<T> property, so the sentinel wins over the parsed [1,2,3].
			var conversion = ResolveConversion<IWithConverterOverride>(nameof(IWithConverterOverride.Values));

			var result = conversion.Convert("1,2,3");

			await Assert.That(result is int[]).IsTrue();
			await Assert.That(((int[])result!).SequenceEqual(new[] { -1 })).IsTrue();
		}

		[Test]
		public async Task Convert_NullForArrayProperty_ReturnsEmptyArray()
		{
			var conversion = ResolveConversion<IIntArrayProp>(nameof(IIntArrayProp.Values));

			var result = conversion.Convert(null);

			await Assert.That(result is int[]).IsTrue();
			await Assert.That(((int[])result!).Length).IsEqualTo(0);
		}

		[Test]
		public async Task Convert_NullForListProperty_ReturnsEmptyList()
		{
			var conversion = ResolveConversion<IIntListProp>(nameof(IIntListProp.Values));

			var result = conversion.Convert(null);

			await Assert.That(result is List<int>).IsTrue();
			await Assert.That(((List<int>)result!).Count).IsEqualTo(0);
		}

		[Test]
		public async Task Convert_NullForEnumerableProperty_ReturnsEmptyArray()
		{
			var conversion = ResolveConversion<IIntEnumerableProp>(nameof(IIntEnumerableProp.Values));

			var result = conversion.Convert(null);

			await Assert.That(result is int[]).IsTrue();
			await Assert.That(((int[])result!).Length).IsEqualTo(0);
		}

		[Test]
		public async Task Convert_NullForListProperty_YieldsFreshInstancePerBind()
		{
			var conversion = ResolveConversion<IIntListProp>(nameof(IIntListProp.Values));

			var first = (List<int>)conversion.Convert(null)!;
			var second = (List<int>)conversion.Convert(null)!;

			await Assert.That(ReferenceEquals(first, second)).IsFalse();

			first.Add(99);

			await Assert.That(second.Count).IsEqualTo(0);
		}

		private static PropertyConversion ResolveConversion<T>(string propertyName)
		{
			var options = new SettingsOptions(); // Converters auto-seeded: DateTime,Uri,Array,Enumerable,Enum,Default
			var conv = new TypeConverter();
			var prop = typeof(T).GetProperty(propertyName)!;

			return conv.CreateConversion(prop, prop.GetCustomAttribute<SettingsPropertyAttribute>(inherit: true), options);
		}

		public interface ISample
		{
			int Count { get; set; }
			int? Maybe { get; set; }
		}

		public interface IWithConverterOverride
		{
			[SettingsProperty(ConverterType = typeof(SentinelConverter))]
			IEnumerable<int> Values { get; set; }
		}

		public interface IIntArrayProp
		{
			int[] Values { get; set; }
		}

		public interface IIntListProp
		{
			List<int> Values { get; set; }
		}

		public interface IIntEnumerableProp
		{
			IEnumerable<int> Values { get; set; }
		}

		// Returns a distinctive sentinel so a passing test proves the attribute ConverterType was chosen
		// instead of CollectionTypeConverter.
		private class SentinelConverter : ISettingsTypeConverter
		{
			public bool CanConvert(System.Type settingsType) => true;

			public object Convert(object value, System.Type settingsType) => new[] { -1 };
		}
	}
}
