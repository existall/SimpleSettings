using System.Reflection;
using System.Text;
using ExistForAll.SimpleSettings.Validations;

namespace ExistForAll.SimpleSettings
{
	internal class Resources
	{
		public const string SettingsOptionsArgumentNullMessage = @"Settings builder must have at least one type of indication to know what is a Settings, 
				the options are an attribute, Interface or type name suffix. You can use all of them or one but you need at least one. ExistForAll.SimpleSettings 
				also provide to override each of the options thus you can provide your own set of attribute or interface. allowing you to decouple this framework
				from you domains.";

		public static string SettingsOptionsArgumentMissingMessage(string argumentName) => $@"Settings argument [{argumentName}] must be set.";

		public static string GetSettingsNotFoundMessageFormatMessage(Type settingsType)
			=> $@"Settings types [{settingsType.Name}] was not found, this could be due to several reasons:
				1. The Settings type was not found in the assemblies you provided
				2. Settings indication (Attribute, Interface, Suffix) was not set correctly";

		public static string SettingsBindingExceptionMessage(ISectionBinder binder, string section,
			string key) => $@"An error has occurred while [{binder.GetType().FullName}] tried
				to bind section [{section}] with key [{key}], if this is custom binding check your implementation of ISectionBinder.
				If this is ExistForAll.Settings binder please let us know on github.";

		public static string SettingsExtractionsExceptionMessage(Type type) =>
			$@"An error occurred while trying to get the type [{type.FullName}]
				from it's assembly, we only check if this type has some of our Settings indications like attribute, an interface or suffix.
				see inner exception for more details";

		public static string SettingsClassGenerationException(Type type) =>
			$@"While trying to generate a class from interface [{type.FullName}] something went wrong.
				please see inner exception for more details";

		// The bound value is deliberately omitted (it may be a secret): we report only the property, its target
		// type, and the failing converter's exception type. See S1 in FIX-PLAN.md and SettingsPropertyValueException.
		public static string PropertySetterExceptionMessage(Type interfaceType, PropertyInfo property, string failureType) =>
			$@"Failed to set property [{property.Name}] of type [{property.PropertyType.Name}] on interface [{interfaceType.Name}]: the bound value could not be converted ([{failureType}]). The value is omitted to avoid leaking secrets into logs.";

		public static string SettingsPropertiesExtractionMessage(Type type) =>
			$@"An error has occurred while trying to extract
				all properties from type [{type.FullName}]";

		public static string PropertyNotAllowNullMessage(string propertyName) =>
			$@"[{propertyName}] is marked as Null not allowed, yet the value is missing (null, empty, or whitespace). please provide value via binder or attribute";

		public static string TypeIsNotInterface(string typeName) =>
			$@"[{typeName}] is not an interface, SimpleSettings supports only interfaces";

        public static string SettingsOptionAttributeTypeMessage(Type type) =>
            $"SimpleSettings support Attribute indication of interfaces, the type provided [{type.FullName}] is not an attribute.";

		// Composed ONLY from author-supplied ValidationError text (SettingsName + ErrorMessage). The inspected
		// settings values are deliberately never embedded — a validator may run against a secret, and this
		// message reaches logs via Exception.ToString(). See D-12/S1 and SettingsValidationException.
		public static string SettingsValidationExceptionMessage(IReadOnlyList<ValidationError> errors)
		{
			var builder = new StringBuilder();
			builder.Append("Settings validation failed with ").Append(errors.Count).Append(" error(s):");

			for (var i = 0; i < errors.Count; i++)
			{
				var error = errors[i];
				builder.Append(Environment.NewLine).Append(" - [").Append(error.SettingsName).Append("] ").Append(error.ErrorMessage);
			}

			return builder.ToString();
		}

		// Value-free like the rest of the family: names only the validator and the failure type, never the
		// settings value the validator inspected (which may be a secret). See S1.
		public static string SettingsValidatorInvocationExceptionMessage(Type validatorType, Type failureType) =>
			$"Validator [{validatorType.Name}] threw [{failureType.Name}] while validating. The settings value is omitted to avoid leaking secrets into logs.";

    }
}