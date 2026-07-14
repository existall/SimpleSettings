using BenchmarkDotNet.Attributes;
using ExistForAll.SimpleSettings.Binder;

namespace ExistForAll.SimpleSettings.Benchmark
{
	/// <summary>
	/// P3 — warm re-populate of a settings type through a primed builder, scaled by property count. The impl
	/// type is emitted and the per-type <c>SettingsPlan</c> (section name resolved once, per-property keys +
	/// defaults + converters cached) are built once in setup, so each measured call pays only: allocate the
	/// instance + run the populate off the cached plan.
	/// This is the hot path P3 targets — repeated resolves that previously re-read attributes, rescanned the
	/// converter list, rebuilt the section name, and set each property reflectively. It mirrors
	/// <c>ResolveBenchmark.WarmResolve_Provider</c> but is a dedicated, cold-noise-free entry so CI can gate
	/// its allocations. (New benchmark: no gh-pages baseline yet, so it only starts alerting once master
	/// records the first point.)
	/// </summary>
	[MemoryDiagnoser]
	public class PlanPopulateBenchmark
	{
		[Params(1, 10, 50)]
		public int PropertyCount;

		private Type _type = null!;
		private SettingsBuilder _builder = null!;

		[GlobalSetup]
		public void Setup()
		{
			_type = PropertyCount switch
			{
				1 => typeof(IProps1),
				10 => typeof(IProps10),
				50 => typeof(IProps50),
				_ => throw new ArgumentOutOfRangeException(nameof(PropertyCount)),
			};

			// Seed an in-memory value for every property so the whole bind + convert + set path runs.
			// Section name mirrors the engine's default formatter (strip a leading 'I').
			var collection = new InMemoryCollection();
			var section = _type.Name[0] == 'I' ? _type.Name.Substring(1) : _type.Name;
			foreach (var property in _type.GetProperties())
				collection.Add(section, property.Name, "value");

			// Prime once: emit the impl type and build + cache the SettingsPlan. The measured calls then pay
			// only the per-resolve populate cost, which is what P3 optimizes.
			_builder = SettingsBuilder.CreateBuilder(f => { f.AddInMemoryCollection(collection); });
			_builder.GetSettings(_type);
		}

		[Benchmark]
		public object Populate() => _builder.GetSettings(_type);
	}
}
