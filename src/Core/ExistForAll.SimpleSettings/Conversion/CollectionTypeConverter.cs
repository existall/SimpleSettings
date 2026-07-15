namespace ExistForAll.SimpleSettings.Conversion
{
	// Shared conversion for the two collection shapes — arrays and IEnumerable<T> properties. Both split a
	// delimited string (or wrap a scalar), convert each element with the element type's converter, and
	// materialize a typed array. Building the array directly (Array.CreateInstance + indexed set) avoids the
	// List<T> + reflected Enumerable.ToArray round-trip the two converters used to share, and the element
	// converter is selected by walking the concrete LinkedList (struct enumerator, no boxing) rather than
	// LINQ First (which boxes the enumerator and allocates a closure) — this runs on every populate.
	internal abstract class CollectionTypeConverter : ISettingsTypeConverter
	{
		private readonly SettingsOptions _settingsOptions;
		private readonly TypeConvertersCollections _converters;

		protected CollectionTypeConverter(SettingsOptions settingsOptions, TypeConvertersCollections converters)
		{
			_settingsOptions = settingsOptions;
			_converters = converters;
		}

		public abstract bool CanConvert(Type settingsType);

		// The element type to convert each item to: the array's element type, or the IEnumerable<T> argument.
		protected abstract Type GetElementType(Type settingsType);

		public virtual object Convert(object value, Type settingsType)
		{
			var elementType = GetElementType(settingsType);
			var source = AsArray(value);
			var elementConverter = GetElementConverter(elementType);

			var result = Array.CreateInstance(elementType, source.Length);
			for (var i = 0; i < source.Length; i++)
			{
				result.SetValue(elementConverter.Convert(source.GetValue(i)!, elementType), i);
			}

			return result;
		}

		// Normalize the incoming value to an array we can size and index: split a delimited string, pass an
		// existing array straight through, or wrap a lone scalar. All three branches already produced an array
		// in the previous per-converter code.
		private Array AsArray(object value)
		{
			if (value is string text)
			{
				return text.Split([_settingsOptions.ArraySplitDelimiter], StringSplitOptions.RemoveEmptyEntries);
			}

			return value as Array ?? new[] { value };
		}

		private ISettingsTypeConverter GetElementConverter(Type elementType)
		{
			// Manual walk over the concrete LinkedList (not LINQ First) so the struct enumerator isn't boxed
			// onto the heap and no predicate closure is allocated — this runs per element-typed collection on
			// every populate.
			foreach (var converter in _converters)
			{
				if (converter.CanConvert(elementType))
					return converter;
			}

			// Unreachable in practice: DefaultTypeConverter.CanConvert always returns true. Kept so every path
			// returns and to mirror the old First(...) which also threw when nothing matched.
			throw new InvalidOperationException($"No converter found for type '{elementType}'.");
		}
	}
}
