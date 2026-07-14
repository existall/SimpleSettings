namespace ExistForAll.SimpleSettings
{
	// Raised when a property marked [SettingsProperty(AllowEmpty = false)] resolves to no value. Distinct from
	// SettingsPropertyValueException (a value that failed conversion): here there is no bound value at all, so
	// the message (property name only) carries nothing sensitive and is surfaced in full — it is a common
	// misconfiguration and the detail aids diagnosis. See S1/C2 in FIX-PLAN.md.
	public class SettingsPropertyNullException : SimpleSettingsException
	{
		public SettingsPropertyNullException(string propertyName)
			: base(Resources.PropertyNotAllowNullMessage(propertyName))
		{
			PropertyName = propertyName;
		}

		public string PropertyName { get; }
	}
}
