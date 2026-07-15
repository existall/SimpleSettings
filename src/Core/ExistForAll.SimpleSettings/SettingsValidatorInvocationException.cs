namespace ExistForAll.SimpleSettings
{
	// A declared validator threw instead of returning a ValidationResult. Value-free like the rest of the
	// family: carries only the validator and failure types — never the inspected settings value (which may be a
	// secret) and never the chained inner. See S1 and SettingsValidationException.
	public class SettingsValidatorInvocationException : SimpleSettingsException
	{
		public SettingsValidatorInvocationException(Type validatorType, Type failureType)
			: base(Resources.SettingsValidatorInvocationExceptionMessage(validatorType, failureType))
		{
			ValidatorType = validatorType;
			FailureType = failureType;
		}

		public Type ValidatorType { get; }

		public Type FailureType { get; }
	}
}
