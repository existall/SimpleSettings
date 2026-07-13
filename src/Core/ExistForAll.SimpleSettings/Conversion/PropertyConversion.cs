using System;

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
				if (_throwOnNull)
					throw new SettingsPropertyNullException(_propertyName);

				return _nullResult;
			}

			return _converter.Convert(value, _strippedType);
		}
	}
}
