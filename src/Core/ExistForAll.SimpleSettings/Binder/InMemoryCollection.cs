namespace ExistForAll.SimpleSettings.Binder
{
	public class InMemoryCollection : IInMemoryCollection
	{
		private readonly Dictionary<string, string> _inner = new Dictionary<string, string>();

		public void Add(string section, string key, string value)
		{
			if (section == null) throw new ArgumentNullException(nameof(section));
			if (key == null) throw new ArgumentNullException(nameof(key));

			_inner.Add(CreateKey(section, key), value);
		}

		internal bool TryGetValue(string section, string key, out string? value)
		{
			value = null;
			var hasValue = _inner.TryGetValue(CreateKey(section, key), out value);
			return hasValue;
		}

		private static string CreateKey(string section, string key)
		{
			return section + ":" + key;
		}
	}
}