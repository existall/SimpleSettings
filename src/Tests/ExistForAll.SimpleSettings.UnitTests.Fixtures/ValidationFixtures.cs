using ExistForAll.SimpleSettings.Validations;

namespace ExistForAll.SimpleSettings.UnitTests.Fixtures
{
	// Separate, never-scanned assembly so these deliberately-failing validators dodge the whole-assembly ScanAssemblies tests.

	[SettingsSection(ValidatorType = typeof(FailingObjectValidator))]
	public interface IObjectValidated
	{
		string Name { get; set; }
	}

	[SettingsSection(ValidatorType = typeof(PassingObjectValidator))]
	public interface IObjectValid
	{
		string Name { get; set; }
	}

	public interface IPropertyValidated
	{
		[SettingsProperty(ValidatorType = typeof(PositivePortValidator))]
		int Port { get; set; }
	}

	[SettingsSection(ValidatorType = typeof(TwoErrorValidator))]
	public interface ITwoErrors
	{
		string Name { get; set; }
	}

	[SettingsSection(ValidatorType = typeof(BothObjectValidator))]
	public interface IBothValidated
	{
		[SettingsProperty(ValidatorType = typeof(PositivePortValidator))]
		int Port { get; set; }
	}

	[SettingsSection(ValidatorType = typeof(CrossPropertyValidator))]
	public interface ICrossProperty
	{
		int Min { get; set; }
		int Max { get; set; }
	}

	[SettingsSection(ValidatorType = typeof(RedactionValidator))]
	public interface IRedaction
	{
		string ApiKey { get; set; }
	}

	[SettingsSection(ValidatorType = typeof(ThrowingValidator))]
	public interface IThrowing
	{
		string ApiKey { get; set; }
	}

	public interface INoValidators
	{
		string Name { get; set; }
	}

	public class FailingObjectValidator : ISettingValidation<IObjectValidated>
	{
		public ValidationResult Validate(ValidationContext<IObjectValidated> context)
		{
			var result = new ValidationResult();
			result.AddError(new ValidationError(nameof(IObjectValidated.Name), "name is invalid"));
			return result;
		}
	}

	public class PassingObjectValidator : ISettingValidation<IObjectValid>
	{
		public ValidationResult Validate(ValidationContext<IObjectValid> context) => new();
	}

	public class PositivePortValidator : ISettingValidation<int>
	{
		public ValidationResult Validate(ValidationContext<int> context)
		{
			var result = new ValidationResult();
			if (context.Settings <= 0)
				result.AddError(new ValidationError("Port", "port must be positive"));
			return result;
		}
	}

	public class TwoErrorValidator : ISettingValidation<ITwoErrors>
	{
		public ValidationResult Validate(ValidationContext<ITwoErrors> context)
		{
			var result = new ValidationResult();
			result.AddError(new ValidationError("Name", "first failure"));
			result.AddError(new ValidationError("Name", "second failure"));
			return result;
		}
	}

	public class BothObjectValidator : ISettingValidation<IBothValidated>
	{
		public ValidationResult Validate(ValidationContext<IBothValidated> context)
		{
			var result = new ValidationResult();
			result.AddError(new ValidationError("Object", "object-level failure"));
			return result;
		}
	}

	public class CrossPropertyValidator : ISettingValidation<ICrossProperty>
	{
		public ValidationResult Validate(ValidationContext<ICrossProperty> context)
		{
			var result = new ValidationResult();
			var settings = context.Settings!;
			if (settings.Min > settings.Max)
				result.AddError(new ValidationError("Min", "Min must not exceed Max"));
			return result;
		}
	}

	public class RedactionValidator : ISettingValidation<IRedaction>
	{
		public ValidationResult Validate(ValidationContext<IRedaction> context)
		{
			var result = new ValidationResult();
			result.AddError(new ValidationError("ApiKey", "ApiKey failed policy"));
			return result;
		}
	}

	public class ThrowingValidator : ISettingValidation<IThrowing>
	{
		public ValidationResult Validate(ValidationContext<IThrowing> context)
			=> throw new InvalidOperationException($"boom with {context.Settings!.ApiKey}");
	}
}
