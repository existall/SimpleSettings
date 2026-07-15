using System.Collections.Concurrent;
using ExistForAll.SimpleSettings.Core.Reflection;
using Microsoft.Extensions.Configuration;

namespace ExistForAll.SimpleSettings.Binders
{
	public class ConfigurationBinder : ISectionBinder
	{
		public string? RootSection { get; }

		private readonly IConfiguration _configuration;

		// Resolve each config section once and reuse it across every property of every settings type. P3 makes
		// context.Section the same per-type string, so all but the first property (and every repeated resolve)
		// is a cache hit. Reload-safe: GetSection returns a live view over the configuration root, so indexing
		// the cached section re-reads the providers on each access — a config reload is still reflected. Ordinal
		// key; bounded by the number of settings sections, so no eviction is needed (same shape as the P1–P3
		// caches).
		private readonly ConcurrentDictionary<string, IConfigurationSection> _sections = new();

		public ConfigurationBinder(IConfiguration configuration, string? rootSection = null)
		{
			RootSection = rootSection;
			_configuration = configuration;
		}

		public void BindPropertySettings(BindingContext context)
		{
			// static factory + factoryArgument overload so no per-call closure over `this` is allocated — a
			// capturing lambda would build a fresh delegate on every call and eat most of the win.
			var section = _sections.GetOrAdd(context.Section, static (name, self) => self.ResolveSection(name), this);

			var isCollection = context.PropertyType.IsCollectionShape();

			if (isCollection && TrySetChildSequence(section, context))
				return;

			var value = section[context.Key];

			if (isCollection)
			{
				if (!string.IsNullOrWhiteSpace(value))
					context.SetNewValue(value);

				return;
			}

			if (value != null)
				context.SetNewValue(value);
		}

		// Child-section sequence path (COLL-03): a collection property may be an indexed sub-section
		// (Key:0, Key:1, ...) rather than a delimited scalar. Enumerate the live config view ONCE (a second
		// pass could race a reload), collecting non-empty element values into a string[] so the element
		// converter chain runs per item. Empty entries are dropped for parity with the comma-scalar split
		// (RemoveEmptyEntries). Children win over the scalar; an all-empty sequence falls through to the scalar
		// path. No element value is placed in any exception (S1).
		private static bool TrySetChildSequence(IConfigurationSection section, BindingContext context)
		{
			var childSection = section.GetSection(context.Key);

			List<string>? values = null;
			foreach (var child in childSection.GetChildren())
			{
				var childValue = child.Value;
				if (!string.IsNullOrEmpty(childValue))
					(values ??= []).Add(childValue);
			}

			if (values is null)
				return false;

			context.SetNewValue(values.ToArray());
			return true;
		}

		private IConfigurationSection ResolveSection(string section)
		{
			return _configuration.GetSection(string.IsNullOrWhiteSpace(RootSection) ? section : $"{RootSection}:{section}");
		}
	}
}
