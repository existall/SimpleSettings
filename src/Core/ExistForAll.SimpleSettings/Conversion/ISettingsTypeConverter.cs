namespace ExistForAll.SimpleSettings.Conversion
{
	/// <summary>
	/// Converts a bound settings value to a property's target type.
	/// </summary>
	/// <remarks>
	/// Implementations must be <b>stateless and thread-safe</b>: a converter is selected once per settings
	/// type and reused for every resolution of that type, including concurrent ones. Do not hold per-call
	/// state on the instance.
	/// </remarks>
	public interface ISettingsTypeConverter
	{
		bool CanConvert(Type settingsType);
		object Convert(object value, Type settingsType);
	}
}