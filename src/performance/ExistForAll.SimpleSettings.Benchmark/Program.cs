using BenchmarkDotNet.Running;

namespace ExistForAll.SimpleSettings.Benchmark
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			//var results = new SimpleSettingsBenchmark().Run();

			var results = BenchmarkRunner.Run(typeof(SimpleSettingsBenchmark));
		}
	}
}