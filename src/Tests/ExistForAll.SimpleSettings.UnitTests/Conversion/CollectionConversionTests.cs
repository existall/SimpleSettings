using System;
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

			await Assert.That(result.Values.SequenceEqual([1, 2, 3, 4])).IsTrue();
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

			await Assert.That(result.Values.SequenceEqual([1, 2])).IsTrue();
		}

		[Test]
		public async Task Convert_WithCustomDelimiter_SplitsOnThatDelimiter()
		{
			var result = Build<IIntArray>("IntArray", nameof(IIntArray.Values), "10;20;30", delimiter: ";");

			await Assert.That(result.Values.SequenceEqual([10, 20, 30])).IsTrue();
		}

		[Test]
		public async Task Convert_DefaultArray_IsPassedThrough()
		{
			// No binder: the [SettingsProperty] default array flows straight through the converter unchanged.
			var sut = SettingsBuilder.CreateBuilder();

			var result = sut.GetSettings<IDefaultIntArray>();

			await Assert.That(result.Values.SequenceEqual([7, 8, 9])).IsTrue();
		}

		[Test]
		public async Task Convert_UnboundEnumerable_NoDefault_YieldsEmptyArray()
		{
			// No binder and no default => the value stays null and PropertyConversion returns the precomputed
			// null result. This is the exact line P4 changed in TypeConverter.CreateNullResult: an empty
			// IEnumerable<T> is now Array.CreateInstance(elementType, 0) instead of Enumerable.Empty<T>().
			var sut = SettingsBuilder.CreateBuilder();

			var result = sut.GetSettings<IIntEnumerable>();

			await Assert.That(result.Values.Count()).IsEqualTo(0);
			await Assert.That(result.Values is int[]).IsTrue();
		}

		[Test]
		public async Task Convert_DelimitedString_ToEnumArray_ParsesEachElement()
		{
			var result = Build<IDayOfWeekArray>("DayOfWeekArray", nameof(IDayOfWeekArray.Values), "Monday,Friday");

			await Assert.That(result.Values.SequenceEqual(new[] { DayOfWeek.Monday, DayOfWeek.Friday })).IsTrue();
		}

		[Test]
		public async Task Convert_DelimitedString_ToDateTimeArray_ParsesWithConfiguredFormat()
		{
			// Default DateTimeFormat is "yyyy-MM-dd" (see SettingsOptions).
			var result = Build<IDateTimeArray>("DateTimeArray", nameof(IDateTimeArray.Values), "2020-01-02,2021-03-04");

			await Assert.That(result.Values.SequenceEqual(new[] { new DateTime(2020, 1, 2), new DateTime(2021, 3, 4) })).IsTrue();
		}

		[Test]
		public async Task Convert_DelimitedString_ToUriArray_ParsesEachElement()
		{
			var result = Build<IUriArray>("UriArray", nameof(IUriArray.Values), "https://a.example/,https://b.example/");

			await Assert.That(result.Values.SequenceEqual(new[] { new Uri("https://a.example/"), new Uri("https://b.example/") })).IsTrue();
		}

		[Test]
		public async Task Convert_NonConvertibleElement_ThrowsSettingsPropertyValueException()
		{
			// A bad element (non-numeric for an int[]) surfaces as the same wrapped exception the old converter
			// produced; P4 leaves the exception-wrapping contract intact.
			var collection = new InMemoryCollection();
			collection.Add("IntArray", nameof(IIntArray.Values), "1,x,3");

			var builder = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));

			await Assert.That(() => builder.GetSettings<IIntArray>()).Throws<SettingsPropertyValueException>();
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

		public interface IDayOfWeekArray
		{
			DayOfWeek[] Values { get; set; }
		}

		public interface IDateTimeArray
		{
			DateTime[] Values { get; set; }
		}

		public interface IUriArray
		{
			Uri[] Values { get; set; }
		}
	}
}
