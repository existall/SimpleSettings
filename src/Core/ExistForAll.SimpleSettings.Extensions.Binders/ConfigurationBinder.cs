using Microsoft.Extensions.Configuration;

namespace ExistForAll.SimpleSettings.Binders
{
	public class ConfigurationBinder : ISectionBinder
	{
		public string? RootSection { get; }

		private readonly IConfiguration _configuration;

		public ConfigurationBinder(IConfiguration configuration, string? rootSection = null)
		{
			RootSection = rootSection;
			_configuration = configuration;
		}

		private string GetSection(BindingContext context)
		{
			if (string.IsNullOrWhiteSpace(RootSection))
				return context.Section;

			return $"{RootSection}:{context.Section}";
		}

		public void BindPropertySettings(BindingContext context)
		{
			var configurationSection = _configuration.GetSection(GetSection(context));

			var value = configurationSection?[context.Key];

			if (value != null)
				context.SetNewValue(value);
		}
	}
}
