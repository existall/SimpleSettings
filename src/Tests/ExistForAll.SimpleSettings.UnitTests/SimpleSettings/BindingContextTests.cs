namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	public class BindingContextTests
	{
		[Test]
		public async Task BindPropertySettings_ContextPropertyType_IsThePropertysOwnType()
		{
			var binder = new CapturingBinder();

			SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(binder))
				.GetSettings<ICaptureSettings>();

			// PropertyType must be each property's own type — not the declaring interface
			// (regression guard: it was previously set to propertyInfo.DeclaringType).
			await Assert.That(binder.Captured[nameof(ICaptureSettings.Name)].PropertyType).IsEqualTo(typeof(string));
			await Assert.That(binder.Captured[nameof(ICaptureSettings.Count)].PropertyType).IsEqualTo(typeof(int));
		}

		[Test]
		public async Task BindPropertySettings_ContextStillExposesDeclaringInterface()
		{
			var binder = new CapturingBinder();

			SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(binder))
				.GetSettings<ICaptureSettings>();

			var context = binder.Captured[nameof(ICaptureSettings.Name)];

			// Fixing PropertyType did not remove access to the declaring type: the settings
			// interface is on SettingsType, and the property's declaring type on PropertyInfo.
			await Assert.That(context.SettingsType).IsEqualTo(typeof(ICaptureSettings));
			await Assert.That(context.PropertyInfo.DeclaringType).IsEqualTo(typeof(ICaptureSettings));
		}

		private sealed class CapturingBinder : ISectionBinder
		{
			public Dictionary<string, BindingContext> Captured { get; } = new Dictionary<string, BindingContext>();

			public void BindPropertySettings(BindingContext context)
			{
				Captured[context.Key] = context;
			}
		}

		public interface ICaptureSettings
		{
			string Name { get; set; }
			int Count { get; set; }
		}
	}
}
