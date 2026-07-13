using System;

namespace ExistForAll.SimpleSettings
{
	// Raised when an ISectionBinder throws while binding a property. It carries the binder type, section, and
	// key as primitives and deliberately does NOT retain the BindingContext — the context holds the bound value
	// (context.CurrentValue), so keeping only primitives guarantees no property can ever surface that value
	// (the S1 secret-redaction invariant). The binder's own exception is chained; the built-in binders never
	// put a value in their messages, and a custom ISectionBinder must not either (see ISectionBinder).
	public class SettingsBindingException : SimpleSettingsException
	{
		public SettingsBindingException(ISectionBinder binder, BindingContext context, Exception innerException)
			: base(Resources.SettingsBindingExceptionMessage(binder, context.Section, context.Key), innerException)
		{
			BinderType = binder.GetType();
			Section = context.Section;
			Key = context.Key;
		}

		public Type BinderType { get; }

		public string Section { get; }

		public string Key { get; }
	}
}
