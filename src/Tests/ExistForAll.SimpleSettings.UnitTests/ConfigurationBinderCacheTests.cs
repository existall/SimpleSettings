using ExistForAll.SimpleSettings.Binders;
using Microsoft.Extensions.Configuration;

namespace ExistForAll.SimpleSettings.UnitTests
{
	// P5 — ConfigurationBinder resolves each section once and caches it per section name. These lock the
	// observable behavior: every property of a type still binds from the right (cached) section, with and
	// without a RootSection, and the cached section stays a live view over the configuration root (so a later
	// value change is picked up — caching the section object must not freeze values).
	public class ConfigurationBinderCacheTests
	{
		[Test]
		public async Task Bind_MultipleProperties_AllResolveFromCachedSection()
		{
			var config = BuildConfig(new Dictionary<string, string?>
			{
				["MultiProp:A"] = "a",
				["MultiProp:B"] = "b",
				["MultiProp:C"] = "c",
			});

			var result = Build<IMultiProp>(new ConfigurationBinder(config));

			await Assert.That(result.A).IsEqualTo("a");
			await Assert.That(result.B).IsEqualTo("b");
			await Assert.That(result.C).IsEqualTo("c");
		}

		[Test]
		public async Task Bind_WithRootSection_MultipleProperties_AllResolve()
		{
			var config = BuildConfig(new Dictionary<string, string?>
			{
				["MyRoot:MultiProp:A"] = "a",
				["MyRoot:MultiProp:B"] = "b",
				["MyRoot:MultiProp:C"] = "c",
			});

			var result = Build<IMultiProp>(new ConfigurationBinder(config, "MyRoot"));

			await Assert.That(result.A).IsEqualTo("a");
			await Assert.That(result.B).IsEqualTo("b");
			await Assert.That(result.C).IsEqualTo("c");
		}

		[Test]
		public async Task Bind_CachedSection_ReflectsLaterConfigChange()
		{
			// The section object is cached on the first populate; a later value change (the config indexer
			// writes through to the provider) must still be seen on the next populate. This pins that caching
			// the IConfigurationSection does not freeze values — it stays a live view over the root.
			var config = BuildConfig(new Dictionary<string, string?> { ["CacheProbe:Value"] = "before" });

			var builder = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new ConfigurationBinder(config)));

			var first = builder.GetSettings<ICacheProbe>();
			await Assert.That(first.Value).IsEqualTo("before");

			config["CacheProbe:Value"] = "after";

			var second = builder.GetSettings<ICacheProbe>();
			await Assert.That(second.Value).IsEqualTo("after");
		}

		private static IConfiguration BuildConfig(Dictionary<string, string?> data)
			=> new ConfigurationBuilder().AddInMemoryCollection(data).Build();

		private static T Build<T>(ISectionBinder binder) where T : class
			=> SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(binder)).GetSettings<T>();

		public interface IMultiProp
		{
			string A { get; set; }
			string B { get; set; }
			string C { get; set; }
		}

		public interface ICacheProbe
		{
			string Value { get; set; }
		}
	}
}
