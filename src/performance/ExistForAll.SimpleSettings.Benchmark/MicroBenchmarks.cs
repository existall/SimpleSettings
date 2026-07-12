using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using ExistForAll.SimpleSettings.Binders;
using ExistForAll.SimpleSettings.Core.Reflection;

namespace ExistForAll.SimpleSettings.Benchmark
{
	/// <summary>
	/// Q1 — <c>SettingsCollection.GetEnumerator</c>. The old implementation rebuilt an entire
	/// <c>Dictionary</c> via <c>ToDictionary</c> on every enumeration; the new one yields over the
	/// backing store. The ~2000 <c>IPerformanceInterfaceN</c> scan fixtures give a collection large
	/// enough that the per-enumeration <c>Dictionary</c> allocation is unmistakable under MemoryDiagnoser.
	/// The collection is built once in setup so only enumeration is measured.
	/// </summary>
	[MemoryDiagnoser]
	public class EnumerateBenchmark
	{
		private ISettingsCollection _collection = null!;

		[GlobalSetup]
		public void Setup()
			=> _collection = SettingsBuilder.CreateBuilder()
				.ScanAssemblies(typeof(EnumerateBenchmark).Assembly);

		[Benchmark]
		public int Enumerate()
		{
			// Explicit foreach (not LINQ Count) so enumeration always runs — no ICollection short-circuit.
			var count = 0;
			foreach (var _ in _collection)
				count++;
			return count;
		}
	}

	/// <summary>
	/// Q3 — <c>EnvironmentVariableBinder.BindPropertySettings</c> on the fast path (no prefix, no
	/// formatter — the common case). The old code allocated a <c>StringBuilder</c> per property and did a
	/// double lookup (<c>Contains</c> then indexer); the new code uses <c>context.Key</c> directly and a
	/// single lookup. The <see cref="BindingContext"/> is built once in setup so the only per-op
	/// allocation is whatever the binder itself does.
	/// </summary>
	[MemoryDiagnoser]
	public class EnvBinderBenchmark
	{
		private const string Key = "BENCH_ENV_KEY";
		private EnvironmentVariableBinder _binder = null!;
		private BindingContext _context = null!;

		[GlobalSetup]
		public void Setup()
		{
			// The binder snapshots the environment in its constructor, so set the variable first.
			Environment.SetEnvironmentVariable(Key, "value");
			_binder = new EnvironmentVariableBinder();

			var property = typeof(IProps1).GetProperty(nameof(IProps1.P0))!;
			_context = new BindingContext("Props1", Key, typeof(IProps1), property, null);
		}

		[Benchmark]
		public void BindFastPath() => _binder.BindPropertySettings(_context);
	}

	/// <summary>
	/// Q4 — warm <c>SettingsClassGenerator.GenerateType</c> (cache hit). The old code re-derived the
	/// mangled type name and did a reflective <c>Assembly.GetType</c> lookup on every call; the new code
	/// returns from a <c>ConcurrentDictionary</c> keyed by interface <c>Type</c> before building the name.
	/// Primed in setup so the measured call is a pure cache hit.
	/// </summary>
	[MemoryDiagnoser]
	public class GenerateTypeBenchmark
	{
		private SettingsClassGenerator _generator = null!;

		[GlobalSetup]
		public void Setup()
		{
			_generator = new SettingsClassGenerator();
			_generator.GenerateType(typeof(IProps50)); // emit + cache the type once
		}

		[Benchmark]
		public Type GenerateWarm() => _generator.GenerateType(typeof(IProps50));
	}
}
