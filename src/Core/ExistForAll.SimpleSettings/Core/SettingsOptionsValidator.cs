using System;
using System.Reflection;

namespace ExistForAll.SimpleSettings.Core
{
	internal class SettingsOptionsValidator : ISettingsOptionsValidator
	{
		public void ValidateOptions(SettingsOptions settingsOptions)
		{
			if (settingsOptions.AttributeType == null &&
			    settingsOptions.InterfaceBase == null &&
			    string.IsNullOrWhiteSpace(settingsOptions.SettingsSuffix))
			{
				throw new SettingsOptionsArgumentNullException();
			}

            if (settingsOptions.AttributeType != null &&
                !typeof(Attribute).GetTypeInfo().IsAssignableFrom(settingsOptions.AttributeType)) 
                throw new SettingsOptionNonAttributeException(settingsOptions.AttributeType);


            if (string.IsNullOrWhiteSpace(settingsOptions.ArraySplitDelimiter))
				throw new SettingsOptionsArgumentMissingException(nameof(settingsOptions.ArraySplitDelimiter));

			if (string.IsNullOrWhiteSpace(settingsOptions.DateTimeFormat))
				throw new SettingsOptionsArgumentMissingException(nameof(settingsOptions.DateTimeFormat));

			if (settingsOptions.SectionNameFormatter == null)
				throw new SettingsOptionsArgumentMissingException("SectionNameFormatter");
		}
	}
}