using BenchmarkDotNet.Attributes;
using ExistForAll.SimpleSettings.Binder;

namespace ExistForAll.SimpleSettings.Benchmark
{
	/// <summary>
	/// Warm build cost (populate + convert only) across representative property shapes: scalar/enum/
	/// DateTime/Uri conversion, delimited-string collections, and a deep interface-inheritance chain.
	/// All impl types are primed in setup so measurements exclude one-time IL emit.
	/// </summary>
	[MemoryDiagnoser]
	public class ShapeBenchmark
	{
		private SettingsBuilder _builder = null!;

		[GlobalSetup]
		public void Setup()
		{
			var collection = new InMemoryCollection();

			// ITypedBag -> section "TypedBag"
			collection.Add("TypedBag", "IntValue", "42");
			collection.Add("TypedBag", "DoubleValue", "1.5");
			collection.Add("TypedBag", "BoolValue", "true");
			collection.Add("TypedBag", "DateValue", "2024-01-15");   // matches the default "yyyy-MM-dd" format
			collection.Add("TypedBag", "UriValue", "https://example.com/");
			collection.Add("TypedBag", "EnumValue", "Monday");

			// IArrayBag -> section "ArrayBag" (default "," delimiter)
			collection.Add("ArrayBag", "Ints", "1,2,3,4,5");
			collection.Add("ArrayBag", "Strings", "alpha,beta,gamma");
			collection.Add("ArrayBag", "IntSeq", "10,20,30");

			// ILevel3 -> section "Level3"; L0..L2 are inherited from the base interfaces.
			collection.Add("Level3", "L0", "zero");
			collection.Add("Level3", "L1", "one");
			collection.Add("Level3", "L2", "two");
			collection.Add("Level3", "L3", "three");

			_builder = SettingsBuilder.CreateBuilder(f => { f.AddInMemoryCollection(collection); });

			// Prime each implementation type so the measured calls are warm (no IL emit).
			_builder.GetSettings(typeof(ITypedBag));
			_builder.GetSettings(typeof(IArrayBag));
			_builder.GetSettings(typeof(ILevel3));
		}

		[Benchmark]
		public object BuildTyped() => _builder.GetSettings(typeof(ITypedBag));

		[Benchmark]
		public object BuildArrayHeavy() => _builder.GetSettings(typeof(IArrayBag));

		[Benchmark]
		public object BuildDeepHierarchy() => _builder.GetSettings(typeof(ILevel3));
	}
}
