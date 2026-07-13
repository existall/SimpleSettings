using System;
using System.Reflection;

namespace ExistForAll.SimpleSettings
{
	// Raised when a bound configuration value fails type conversion. The bound value is deliberately NOT put in
	// the message, and the converter's inner exception is NOT chained — both can embed the raw value (e.g. a
	// FormatException reads "The input string 'SECRET' was not in a correct format."), which would then reach
	// logs via Exception.ToString()/ILogger. Only leak-safe metadata is exposed: the settings type, property
	// name, target type, and the CLR type of the converter's failure. The ctor takes that failure's Type (not
	// the Exception object) so a value-bearing exception cannot cross this type's boundary — the S1 guarantee is
	// structural, not conventional. See S1/C2 in FIX-PLAN.md. For the "required value missing" case (no value at
	// all, so nothing to leak) see SettingsPropertyNullException.
	public class SettingsPropertyValueException : SimpleSettingsException
	{
		public SettingsPropertyValueException(Type settingsType, PropertyInfo property, Type conversionErrorType)
			: base(Resources.PropertySetterExceptionMessage(settingsType, property, conversionErrorType.Name))
		{
			SettingsType = settingsType;
			PropertyName = property.Name;
			TargetType = property.PropertyType;
			ConversionErrorType = conversionErrorType;
		}

		public Type SettingsType { get; }

		public string PropertyName { get; }

		public Type TargetType { get; }

		// The CLR type of the exception the converter threw (e.g. typeof(FormatException)) — a type identity,
		// never the failing value or the exception instance.
		public Type ConversionErrorType { get; }
	}
}
