using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal class TypePropertiesExtractor : ITypePropertiesExtractor
	{
		// A settings interface's property set is immutable for the process lifetime, and both the class
		// generator and the values populator ask for it (each news up its own extractor), so memoize
		// process-wide with a shared cache. Extraction also collapses the previous O(n^2) inherited
		// dedup (Where(p => properties.All(...))) to a single HashSet pass.
		private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Cache =
			new ConcurrentDictionary<Type, PropertyInfo[]>();

		public IEnumerable<PropertyInfo> ExtractTypeProperties(Type type)
		{
			return Cache.GetOrAdd(type, Extract);
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
