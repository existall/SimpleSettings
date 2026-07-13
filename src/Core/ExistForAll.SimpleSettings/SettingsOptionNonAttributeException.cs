using System;

namespace ExistForAll.SimpleSettings
{
	public class SettingsOptionNonAttributeException : SimpleSettingsException
	{
		public SettingsOptionNonAttributeException(Type optionType)
			: base(Resources.SettingsOptionAttributeTypeMessage(optionType))
		{
			OptionType = optionType;
		}

		public Type OptionType { get; }
	}
}
