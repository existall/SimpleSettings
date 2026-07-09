using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExistForAll.SimpleSettings
{
	internal class SettingsCollection : ISettingsCollection
	{
		private readonly Dictionary<Type, ISettingsHolder> _settingsHolders = new Dictionary<Type, ISettingsHolder>();

        internal void Add(Type settingsType, object impl)
		{
			_settingsHolders.Add(settingsType, new SettingsHolder(settingsType, impl));
		}

		public object GetSettings(Type type)
		{
			if (!type.GetTypeInfo().IsInterface)
			{
				throw new InvalidOperationException(Resources.TypeIsNotInterface(type.Name));
			}
			
			return _settingsHolders.TryGetValue(type, out var holder) ? holder.SettingsImplementation : throw new SettingsTypeNotFoundException(type);
		}

        public bool TryGetSettings(Type type, out object? settings)
        {
            if (!type.GetTypeInfo().IsInterface)
            {
                throw new InvalidOperationException(Resources.TypeIsNotInterface(type.Name));
            }

            if(_settingsHolders.TryGetValue(type, out var holder))
            {
                settings = holder.SettingsImplementation;
                return true;
            }

            settings = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
		{
			var dictionary = _settingsHolders.ToDictionary(x => x.Key, y => y.Value.SettingsImplementation);

			return dictionary.GetEnumerator();
		}
	}
}