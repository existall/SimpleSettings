namespace ExistForAll.SimpleSettings
{
	public interface ISectionBinder
	{
		// Implementations must not throw exceptions whose message embeds a fetched configuration value:
		// SettingsBindingException chains the thrown exception, so a value-bearing message would reach logs
		// (a secret leak). The built-in binders only read and store, so they never do this. See S1 in FIX-PLAN.md.
		void BindPropertySettings(BindingContext context);
	}
}