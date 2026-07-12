using BenchmarkDotNet.Attributes;
using ExistForAll.SimpleSettings;

namespace ExistForAll.SimpleSettings.Benchmark
{
	/// <summary>
	/// Cold, at-scale startup cost: discover every <c>[SettingsSection]</c> interface in this assembly
	/// (the ~2000 <c>IPerformanceInterfaceN</c> fixtures in <c>IPerformanceInterfaces.cs</c>) and build
	/// each one — IL-emit the implementation type + reflectively populate it. This is the dominant
	/// wall-clock phase; <see cref="ResolveBenchmark"/> isolates the single-type cost.
	/// </summary>
	[MemoryDiagnoser]
	public class ScanBenchmark
	{
		[Benchmark]
		public int ColdScan()
			=> SettingsBuilder.CreateBuilder()
				.ScanAssemblies(typeof(ScanBenchmark).Assembly)
				.Count();
	}
}
