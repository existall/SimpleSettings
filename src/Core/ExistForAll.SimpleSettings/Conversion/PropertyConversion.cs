namespace ExistForAll.SimpleSettings.Conversion
{
	// A per-property conversion resolved once (at plan build) instead of on every populate: the chosen
	// converter, the stripped target type, and the null-value outcome are all baked in here, so the hot path
	// is a single null-check plus one virtual Convert call — no attribute reads, no converter scan.
	// A readonly struct so it lives inline inside the PropertyPlan value array — no per-property heap object,
	// which keeps the cold at-scale scan (a plan per type, each built once) from regressing on allocations.
	internal readonly struct PropertyConversion
	{
		private readonly ISettingsTypeConverter _converter;
		private readonly Type _strippedType;
		private readonly bool _throwOnNull;
		private readonly object? _nullResult;
		private readonly string _propertyName;

		public PropertyConversion(ISettingsTypeConverter converter,
			Type strippedType,
			bool throwOnNull,
			object? nullResult,
			string propertyName)
		{
			_converter = converter;
			_strippedType = strippedType;
			_throwOnNull = throwOnNull;
			_nullResult = nullResult;
			_propertyName = propertyName;
		}

		public object? Convert(object? value)
		{
			if (value == null)
			{
				// AllowEmpty=false rejects a missing required value, value-free.
				if (_throwOnNull)
					throw new SettingsPropertyNullException(_propertyName);

				// A list null-result is a factory so each bind gets a fresh mutable List<T>; arrays /
				// IEnumerable / value-type defaults stay on the shared cached instance.
				return _nullResult is Func<object> listFactory ? listFactory() : _nullResult;
			}

			// AllowEmpty=false also rejects empty/whitespace strings, not just null.
			if (_throwOnNull && value is string s && string.IsNullOrWhiteSpace(s))
				throw new SettingsPropertyNullException(_propertyName);

			return _converter.Convert(value, _strippedType);
		}
	}
}
