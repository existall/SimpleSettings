using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using ExistForAll.SimpleSettings.Binders;
using Microsoft.Extensions.Configuration;

namespace ExistForAll.SimpleSettings.Benchmark
{
	/// <summary>
	/// P5 — <c>ConfigurationBinder.BindPropertySettings</c>. The old code called
	/// <c>IConfiguration.GetSection(...)</c> (a fresh <c>ConfigurationSection</c> allocation) on every property,
	/// plus a <c>$"{RootSection}:{Section}"</c> interpolation per property when a root is configured. The section
	/// is constant per settings type, so P5 resolves it once and caches it per section name. Two variants
	/// because only the root-set path pays the interpolation. The cache is primed in setup so the measured op is
	/// a steady-state hit — the repeated-populate path P5 targets. Mirrors <see cref="EnvBinderBenchmark"/> (Q3).
	/// </summary>
	[MemoryDiagnoser]
	public class ConfigBinderBenchmark
	{
		private ConfigurationBinder _noRoot = null!;
		private ConfigurationBinder _withRoot = null!;
		private BindingContext _context = null!;

		[GlobalSetup]
		public void Setup()
		{
			var config = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Props1:P0"] = "value",
					["Root:Props1:P0"] = "value",
				})
				.Build();

			_noRoot = new ConfigurationBinder(config);
			_withRoot = new ConfigurationBinder(config, "Root");

			var property = typeof(IProps1).GetProperty(nameof(IProps1.P0))!;
			_context = new BindingContext("Props1", "P0", typeof(IProps1), property, null);

			// Prime the section cache so each measured call is a pure hit (a harmless no-op on the pre-P5 code).
			_noRoot.BindPropertySettings(_context);
			_withRoot.BindPropertySettings(_context);
		}

		[Benchmark]
		public void BindNoRoot() => _noRoot.BindPropertySettings(_context);

		[Benchmark]
		public void BindWithRoot() => _withRoot.BindPropertySettings(_context);
	}
}
