using ExistForAll.SimpleSettings.Validations;

namespace ExistForAll.SimpleSettings
{
	// Aggregates every ValidationError produced by the declared validators into one exception. Like the rest of
	// the family it embeds NO bound value: the message is composed only from author-supplied ValidationError text
	// (SettingsName + ErrorMessage), never from the settings values the validators inspected (which may be
	// secrets). See D-12/S1 and SettingsPropertyValueException. The aggregate-and-throw is centralized in
	// ThrowIfAny so the core populate path and the DI-resolved path share one thrown contract.
	public class SettingsValidationException : SimpleSettingsException
	{
		public SettingsValidationException(IReadOnlyList<ValidationError> errors)
			: base(Resources.SettingsValidationExceptionMessage(errors))
		{
			Errors = errors;
		}

		public IReadOnlyList<ValidationError> Errors { get; }

		// The single aggregate-and-throw entry point shared by the core populate path (Plan 03) and the
		// DI-resolved runner (Plan 04): throws one aggregated exception when any error is present, otherwise
		// returns. Centralizing it keeps the thrown contract (type + Errors + value-free message) identical
		// across both paths.
		public static void ThrowIfAny(IReadOnlyList<ValidationError> errors)
		{
			if (errors == null) throw new ArgumentNullException(nameof(errors));

			if (errors.Count == 0)
				return;

			throw new SettingsValidationException(errors);
		}
	}
}
