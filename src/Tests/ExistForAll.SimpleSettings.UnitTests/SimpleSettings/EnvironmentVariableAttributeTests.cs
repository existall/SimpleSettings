using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.Binders;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	[NotInParallel("EnvironmentVariables")]
	public class EnvironmentVariableAttributeTests
	{
		public const string EnvironmentVariable = "env-var";

		[Test]
		public async Task Build_WhereVariableHasValue_ShouldSetProperty()
		{
			var guid = Guid.NewGuid().ToString();

			using (new DisposableEnvironmentVariable(EnvironmentVariable, guid))
			{
				var sut = SettingsBuilder.CreateBuilder(x => x.AddEnvironmentVariable());

				var result = sut.ScanAssemblies(GetType().Assembly);

				var settings = result.GetSettings<IWithEnvironmentVariable>();

				await Assert.That(settings.EnvironmentVariable).IsEqualTo(guid);
			}
		}

		[Test]
		public async Task Build_WithPrefix_ShouldLookUpPrefixedVariable()
		{
			const string prefix = "PREFIX_";
			var guid = Guid.NewGuid().ToString();

			using (new DisposableEnvironmentVariable(prefix + EnvironmentVariable, guid))
			{
				var sut = SettingsBuilder.CreateBuilder(x =>
					x.AddEnvironmentVariable(o => o.Prefix = prefix));

				var result = sut.ScanAssemblies(GetType().Assembly);

				var settings = result.GetSettings<IWithEnvironmentVariable>();

				await Assert.That(settings.EnvironmentVariable).IsEqualTo(guid);
			}
		}

		[Test]
		public async Task TryGetValue_WhenEnvVariableIsLast_ShouldSetValueFromEnv()
		{
			var guid = Guid.NewGuid().ToString();
			var badGuid = Guid.NewGuid().ToString();

			using (new DisposableEnvironmentVariable(EnvironmentVariable, guid))
			{
				var collection = new InMemoryCollection();
				collection.Add(nameof(IWithEnvironmentVariable).TrimStart('I'),
					EnvironmentVariable, badGuid);

				var sut = SettingsBuilder.CreateBuilder(x =>
				{
					x.AddInMemoryCollection(collection)
					.AddEnvironmentVariable();
				});

				var settings = sut.GetSettings<IWithEnvironmentVariable>();

				await Assert.That(settings.EnvironmentVariable).IsEqualTo(guid);
			}
		}

		[Test]
		public async Task TryGetValue_WhenMemoryCollectionIsLast_ShouldSetValueFromMemoryBinder()
		{
			var guid = Guid.NewGuid().ToString();
			var badGuid = Guid.NewGuid().ToString();

			using (new DisposableEnvironmentVariable(EnvironmentVariable, badGuid))
			{
				var collection = new InMemoryCollection();
				collection.Add(nameof(IWithEnvironmentVariable).TrimStart('I'),
					EnvironmentVariable, guid);

				var sut = SettingsBuilder.CreateBuilder(x =>
				{
					x.AddEnvironmentVariable()
						.AddInMemoryCollection(collection);
				});

				var settings = sut.GetSettings<IWithEnvironmentVariable>();

				await Assert.That(settings.EnvironmentVariable).IsEqualTo(guid);
			}
		}
	}
}
