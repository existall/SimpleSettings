using System;
using System.Reflection;

namespace ExistForAll.SimpleSettings
{
	// Raised when a bound configuration value fails type conversion. The bound value is deliberately NOT put
	// in the message, and the converter's inner exception is NOT chained: both can embed the raw value (e.g. a
	// FormatException reads "The input string 'SECRET' was not in a correct format."), which would then reach
	// logs via Exception.ToString()/ILogger. Only the failure's exception type name is surfaced — that is a
	// compile-time identifier and cannot carry a secret. See S1 in FIX-PLAN.md. For the distinct
	// "required value missing" case (no value at all, so nothing to leak) see SettingsPropertyNullException.
	internal class SettingsPropertyValueException : Exception
	{
		public SettingsPropertyValueException(
			Type interfaceType,
			PropertyInfo property,
			Exception conversionError)
			: base(Resources.PropertySetterExceptionMessage(interfaceType, property, conversionError.GetType().Name))
		{}
	}
}