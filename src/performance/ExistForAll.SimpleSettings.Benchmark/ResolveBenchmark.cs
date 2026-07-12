using BenchmarkDotNet.Attributes;
using ExistForAll.SimpleSettings;
using ExistForAll.SimpleSettings.Binder;
using Microsoft.Extensions.DependencyInjection;

namespace ExistForAll.SimpleSettings.Benchmark
{
	/// <summary>
	/// The three ways a bound settings instance reaches a caller, scaled by property count:
	/// <list type="bullet">
	/// <item><c>ColdBuild</c> — a fresh <see cref="SettingsBuilder"/> per call: IL-emit the implementation
	/// type into a new dynamic assembly + reflectively populate it (the per-type startup cost).</item>
	/// <item><c>WarmResolve_Provider</c> — a primed builder re-binding a fresh instance every call: the raw
	/// <c>SettingsBuilder.GetSettings</c> re-bind cost. P1 makes <c>ISettingsProvider</c> serve the cached
	/// collection for scanned settings instead, so its resolves drop toward <c>WarmResolve_DiSingleton</c>.</item>
	/// <item><c>WarmResolve_DiSingleton</c> — resolving the startup-built singleton from a DI container
	/// (what <c>AddSimpleSettings</c> registers via <c>AddSingleton(interface, instance)</c>).</item>
	/// </list>
	/// </summary>
	[MemoryDiagnoser]
	public class ResolveBenchmark
	{
		[Params(1, 10, 50)]
		public int PropertyCount;

		private System.Type _type = null!;
		private InMemoryCollection _collection = null!;
		private SettingsBuilder _warmBuilder = null!;
		private System.IServiceProvider _serviceProvider = null!;

		[GlobalSetup]
		public void Setup()
		{
			_type = PropertyCount switch
			{
				1 => typeof(IProps1),
				10 => typeof(IProps10),
				50 => typeof(IProps50),
				_ => throw new System.ArgumentOutOfRangeException(nameof(PropertyCount)),
			};

			// Seed an in-memory value for every property so the full populate + convert path runs.
			// Section name mirrors the engine's default formatter (strip a leading 'I').
			_collection = new InMemoryCollection();
			var section = _type.Name[0] == 'I' ? _type.Name.Substring(1) : _type.Name;
			foreach (var property in _type.GetProperties())
				_collection.Add(section, property.Name, "value");

			// Warm builder: prime it once so the implementation type is already emitted and cached;
			// the measured WarmResolve_Provider calls then pay only re-bind (populate) cost.
			_warmBuilder = SettingsBuilder.CreateBuilder(f => { f.AddInMemoryCollection(_collection); });
			_warmBuilder.GetSettings(_type);

			// A DI singleton is registered exactly as AddSimpleSettings does it — AddSingleton(interface,
			// startup-built instance). Registering directly keeps setup independent of assembly scanning;
			// the measured op (container resolve) is identical either way.
			var singleton = SettingsBuilder.CreateBuilder(f => { f.AddInMemoryCollection(_collection); }).GetSettings(_type);
			var services = new ServiceCollection();
			services.AddSingleton(_type, singleton);
			_serviceProvider = services.BuildServiceProvider();
		}

		[Benchmark(Baseline = true)]
		public object ColdBuild()
			=> SettingsBuilder.CreateBuilder(f => { f.AddInMemoryCollection(_collection); }).GetSettings(_type);

		[Benchmark]
		public object WarmResolve_Provider()
			=> _warmBuilder.GetSettings(_type);

		[Benchmark]
		public object? WarmResolve_DiSingleton()
			=> _serviceProvider.GetService(_type);
	}
}
