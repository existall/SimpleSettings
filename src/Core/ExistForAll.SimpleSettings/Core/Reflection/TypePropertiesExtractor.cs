using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal class TypePropertiesExtractor : ITypePropertiesExtractor
	{
		// ExtractTypeProperties is called repeatedly for a settings type (once per type-generation and
		// once per populate), so results are memoized. The cache is an injected dependency rather than a
		// static field, so its lifetime and scope are the caller's choice. Extraction also collapses the
		// previous O(n^2) inherited dedup (Where(p => properties.All(...))) to a single HashSet pass.
		private readonly ConcurrentDictionary<Type, PropertyInfo[]> _cache;

		public TypePropertiesExtractor(ConcurrentDictionary<Type, PropertyInfo[]> cache)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		public IEnumerable<PropertyInfo> ExtractTypeProperties(Type type)
		{
			return _cache.GetOrAdd(type, Extract);
		}

		private static PropertyInfo[] Extract(Type type)
		{
			try
			{
				var info = type.GetTypeInfo();

				var properties = info.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

				// Seed with the declared property names; first declaration wins (same as before), so a
				// base-interface property is added only when its name has not already been seen.
				var seen = new HashSet<string>(properties.Count);
				foreach (var property in properties)
					seen.Add(property.Name);

				foreach (var @interface in info.GetInterfaces())
				{
					foreach (var property in @interface.GetTypeInfo().GetProperties())
					{
						if (seen.Add(property.Name))
							properties.Add(property);
					}
				}

				return properties.ToArray();
			}
			catch (Exception e)
			{
				throw new SettingsPropertyExtractionException(type, e);
			}
		}
	}
}
