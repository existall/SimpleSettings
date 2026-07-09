using System;

namespace ExistForAll.SimpleSettings
{
	[AttributeUsage(AttributeTargets.Property)]
	public class SettingsPropertyAttribute : Attribute
	{
		public object? DefaultValue { get; set; }

		public string? Name { get; set; }

		public Type? ConverterType { get; set; }

		public bool AllowEmpty { get; set; } = true;

		public Type? ValidatorType { get; set; }

		public SettingsPropertyAttribute(string name)
		{
			Name = name;
		}
		
		public SettingsPropertyAttribute() { }
	}
}
