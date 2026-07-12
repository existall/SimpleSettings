using System.Linq;
using BenchmarkDotNet.Attributes;
using ExistForAll.SimpleSettings;

namespace ExistForAll.SimpleSettings.Benchmark
{
	public class SimpleSettingsBenchmark
	{
		[Benchmark]
		public void Run()
        {
            var settingsBuilder = SettingsBuilder.CreateBuilder();
            var settingsCollection = settingsBuilder.ScanAssemblies(GetType().Assembly);
			var count = settingsCollection.Count();
		}
	}
}
