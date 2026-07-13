using System.Collections.Generic;
using System.Linq;
using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.Binders;

namespace ExistForAll.SimpleSettings.UnitTests.Conversion
{
	// Exercises the array + IEnumerable<T> converter path end-to-end (bound delimited string -> collection),
	// which the P4 de-reflection rewrite shares behind CollectionTypeConverter. Locks the observable behavior:
	// element values, ordering, empty-entry handling, the custom delimiter, and that an IEnumerable<T> property
	// is satisfied by the materialized array.
	public class CollectionConversionTests
	{
		[Test]
		public async Task Convert_DelimitedString_ToIntArray_ParsesEachElement()
		{
			var result = Build<IIntArray>("IntArray", nameof(IIntArray.Values), "1,2,3,4");

			await Assert.That(result.Values.SequenceEqual(new[] { 1, 2, 3, 4 })).IsTrue();
		}

		[Test]
		public async Task Convert_DelimitedString_ToStringArray_KeepsOrder()
		{
			var result = Build<IStringArray>("StringArray", nameof(IStringArray.Values), "alpha,beta,gamma");

			await Assert.That(result.Values.SequenceEqual(new[] { "alpha", "beta", "gamma" })).IsTrue();
		}

		[Test]
		public async Task Convert_DelimitedString_ToIntEnumerable_YieldsElements()
		{
			var result = Build<IIntEnumerable>("IntEnumerable", nameof(IIntEnumerable.Values), "5,6,7");

			await Assert.That(result.Values.Count()).IsEqualTo(3);
			await Assert.That(result.Values).Contains(5);
			await Assert.That(result.Values).Contains(6);
			await Assert.That(result.Values).Contains(7);
		}

		[Test]
		public async Task Convert_ToIntEnumerable_MaterializesAnArray()
		{
			// The IEnumerable<T> converter now returns a T[] (assignable to IEnumerable<T>) instead of a
			// List<T>; this pins that deliberate P4 behavior so a regression to List<T> is caught.
			var result = Build<IIntEnumerable>("IntEnumerable", nameof(IIntEnumerable.Values), "5,6,7");

			await Assert.That(result.Values is int[]).IsTrue();
		}

		[Test]
		public async Task Convert_DelimitedString_RemovesEmptyEntries()
		{
			var result = Build<IIntArray>("IntArray", nameof(IIntArray.Values), "1,,2,");

			await Assert.That(result.Values.SequenceEqual(new[] { 1, 2 })).IsTrue();
		}

		[Test]
		public async Task Convert_WithCustomDelimiter_SplitsOnThatDelimiter()
		{
			var result = Build<IIntArray>("IntArray", nameof(IIntArray.Values), "10;20;30", delimiter: ";");

			await Assert.That(result.Values.SequenceEqual(new[] { 10, 20, 30 })).IsTrue();
		}

		[Test]
		public async Task Convert_DefaultArray_IsPassedThrough()
		{
			// No binder: the [SettingsProperty] default array flows straight through the converter unchanged.
			var sut = SettingsBuilder.CreateBuilder();

			var result = sut.GetSettings<IDefaultIntArray>();

			await Assert.That(result.Values.SequenceEqual(new[] { 7, 8, 9 })).IsTrue();
		}

		private static T Build<T>(string section, string key, string value, string? delimiter = null)
			where T : class
		{
			var collection = new InMemoryCollection();
			collection.Add(section, key, value);

			var builder = SettingsBuilder.CreateBuilder(x =>
			{
				if (delimiter != null)
					x.SetArraySplitDelimiter(delimiter);

				x.AddSectionBinder(new InMemoryBinder(collection));
			});

			return builder.GetSettings<T>();
		}

		public interface IIntArray
		{
			int[] Values { get; set; }
		}

		public interface IStringArray
		{
			string[] Values { get; set; }
		}

		public interface IIntEnumerable
		{
			IEnumerable<int> Values { get; set; }
		}

		public interface IDefaultIntArray
		{
			[SettingsProperty(DefaultValue = new[] { 7, 8, 9 })]
			int[] Values { get; set; }
		}
	}
}
