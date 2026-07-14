namespace ExistForAll.SimpleSettings
{
	// Raised when a type handed to the builder/collection is not an interface (SimpleSettings generates its
	// implementations from interfaces). Replaces the untyped InvalidOperationException that used to escape, so
	// it is catchable as part of the SimpleSettingsException family. See C2 in FIX-PLAN.md.
	public class SettingsTypeNotInterfaceException : SimpleSettingsException
	{
		public SettingsTypeNotInterfaceException(Type settingsType)
			: base(Resources.TypeIsNotInterface(settingsType.Name))
		{
			SettingsType = settingsType;
		}

		public Type SettingsType { get; }
	}
}
