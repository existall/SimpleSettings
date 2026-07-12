using BenchmarkDotNet.Running;

namespace ExistForAll.SimpleSettings.Benchmark
{
	public static class Program
	{
		// Run everything:   dotnet run -c Release
		// Filter one class: dotnet run -c Release -- --filter *ResolveBenchmark*
		// Fast smoke test:  dotnet run -c Release -- --job dry
		public static void Main(string[] args)
			=> BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
	}
}
