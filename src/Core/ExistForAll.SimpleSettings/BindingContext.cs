using System.Reflection;

namespace ExistForAll.SimpleSettings
{
	public class BindingContext
	{
		internal object? NewValue { get; private set; }

		internal bool HasNewValue { get; private set; }
		
		public Type SettingsType { get; }
		
		public Type PropertyType { get; }
		
		public PropertyInfo PropertyInfo { get; }
		
		public string Section { get; }
		public string Key { get; }

		public object? CurrentValue { get; }

		public BindingContext(string section,
			string key,
			Type settingsType,
			PropertyInfo propertyInfo,
			object? currentValue)
		{
			if (section == null) throw new ArgumentNullException(nameof(section));
			if (key == null) throw new ArgumentNullException(nameof(key));

			Section = section;
			Key = key;
			SettingsType = settingsType ?? throw new ArgumentNullException(nameof(settingsType));
			PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
			PropertyType = propertyInfo.PropertyType;
			CurrentValue = currentValue;
		}
		
		public void SetNewValue(object? newValue)
		{
			HasNewValue = true;
			NewValue = newValue;
		}
	}
}