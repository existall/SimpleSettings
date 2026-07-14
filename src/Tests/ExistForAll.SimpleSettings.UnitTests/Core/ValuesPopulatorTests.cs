using ExistForAll.SimpleSettings.Binder;

namespace ExistForAll.SimpleSettings.UnitTests.Core
{
	// Locks the two genuinely-uncovered ValuesPopulator precedence behaviors (TEST-01): binder
	// last-writer-wins, a later silent binder not clobbering an earlier set value, and the
	// [SettingsProperty] DefaultValue surviving when binders run but none set the property. Uses the
	// integration build path (RESEARCH Open Question #1) — binder ORDER is expressible via AddSectionBinder,
	// so the internal ValuesPopulator ctor is not needed. Section name is the interface name with the
	// leading "I" stripped ("Sample"). Does NOT re-assert the exception-wrapper contracts (owned by
	// ExceptionHierarchyTests / ExceptionRedactionTests).
	public class ValuesPopulatorTests
	{
		private const string Section = "Sample";

		[Test]
		public async Task Populate_WhenTwoOrderedBindersSetSameProperty_LaterBinderWins()
		{
			var first = new InMemoryCollection();
			first.Add(Section, nameof(ISample.Name), "first");

			var second = new InMemoryCollection();
			second.Add(Section, nameof(ISample.Name), "second");

			var builder = SettingsBuilder.CreateBuilder(x =>
			{
				x.AddSectionBinder(new InMemoryBinder(first));
				x.AddSectionBinder(new InMemoryBinder(second)); // added later => wins
			});

			var result = builder.GetSettings<ISample>();

			await Assert.That(result.Name).IsEqualTo("second");
		}

		[Test]
		public async Task Populate_WhenLaterBinderIsSilentOnProperty_EarlierValueSurvives()
		{
			var first = new InMemoryCollection();
			first.Add(Section, nameof(ISample.Name), "first");

			// An active later binder that sets a DIFFERENT key and never touches Name: proves a silent
			// later binder does not clobber the earlier set value (still precedence, not the exception contract).
			var second = new InMemoryCollection();
			second.Add(Section, nameof(ISample.Label), "later-label");

			var builder = SettingsBuilder.CreateBuilder(x =>
			{
				x.AddSectionBinder(new InMemoryBinder(first));
				x.AddSectionBinder(new InMemoryBinder(second));
			});

			var result = builder.GetSettings<ISample>();

			await Assert.That(result.Name).IsEqualTo("first");
		}

		[Test]
		public async Task Populate_WhenBindersPresentButNoneSetProperty_AttributeDefaultSurvives()
		{
			// The binder runs (it sets a different key) but never sets Label, so Label keeps its
			// [SettingsProperty(DefaultValue = "fallback")].
			var collection = new InMemoryCollection();
			collection.Add(Section, nameof(ISample.Name), "set-name");

			var builder = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));

			var result = builder.GetSettings<ISample>();

			await Assert.That(result.Label).IsEqualTo("fallback");
		}

		public interface ISample
		{
			string Name { get; set; }

			[SettingsProperty(DefaultValue = "fallback")]
			string Label { get; set; }
		}
	}
}
