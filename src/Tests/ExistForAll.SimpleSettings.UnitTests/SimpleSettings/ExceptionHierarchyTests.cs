using ExistForAll.SimpleSettings.Binder;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	// C2: every library exception derives from the public abstract SimpleSettingsException, the escaping types
	// are public, and each carries its context as structured (leak-safe) properties. See C2 in FIX-PLAN.md.
	public class ExceptionHierarchyTests
	{
		[Test]
		public async Task Base_IsPublicAndAbstract()
		{
			await Assert.That(typeof(SimpleSettingsException).IsPublic).IsTrue();
			await Assert.That(typeof(SimpleSettingsException).IsAbstract).IsTrue();
		}

		[Test]
		public async Task AllLibraryExceptions_DeriveFromSimpleSettingsException()
		{
			var baseType = typeof(SimpleSettingsException);

			// Any exception type in the library that does NOT derive from the common base is an offender —
			// this fails if a future exception is added without reparenting.
			var offenders = baseType.Assembly.GetTypes()
				.Where(t => typeof(Exception).IsAssignableFrom(t)
					&& t != baseType
					&& t.Namespace != null
					&& t.Namespace.StartsWith("ExistForAll.SimpleSettings"))
				.Where(t => !baseType.IsAssignableFrom(t))
				.Select(t => t.FullName)
				.ToArray();

			await Assert.That(offenders.Length).IsEqualTo(0);
		}

		[Test]
		public async Task ExceptionsThatEscapeTheBuild_ArePublic()
		{
			// InternalsVisibleTo lets this test project see internal types, so a plain reference can't prove
			// `public` — assert accessibility via reflection instead.
			await Assert.That(typeof(SettingsPropertyValueException).IsPublic).IsTrue();
			await Assert.That(typeof(SettingsPropertyNullException).IsPublic).IsTrue();
			await Assert.That(typeof(TypeGenerationException).IsPublic).IsTrue();
			await Assert.That(typeof(SettingsPropertyExtractionException).IsPublic).IsTrue();
		}

		[Test]
		public async Task NonInterfaceType_ThrowsTypedException_CatchableAsBase()
		{
			var builder = SettingsBuilder.CreateBuilder();

			var caught = CatchBase(() => builder.GetSettings(typeof(NotAnInterface)));

			await Assert.That(caught is SettingsTypeNotInterfaceException).IsTrue();
			await Assert.That(((SettingsTypeNotInterfaceException)caught!).SettingsType).IsEqualTo(typeof(NotAnInterface));
		}

		[Test]
		public async Task ConversionFailure_ExposesSafeStructuredMetadata_AndNoChainedInner()
		{
			var collection = new InMemoryCollection();
			collection.Add("IntSettings", nameof(IIntSettings.Value), "not-a-number");
			var builder = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));

			var caught = CatchBase(() => builder.GetSettings<IIntSettings>());

			await Assert.That(caught is SettingsPropertyValueException).IsTrue();
			var ex = (SettingsPropertyValueException)caught!;
			await Assert.That(ex.SettingsType).IsEqualTo(typeof(IIntSettings));
			await Assert.That(ex.PropertyName).IsEqualTo(nameof(IIntSettings.Value));
			await Assert.That(ex.TargetType).IsEqualTo(typeof(int));
			await Assert.That(ex.ConversionErrorType).IsEqualTo(typeof(FormatException));
			// The load-bearing S1 invariant: no value-bearing inner is chained.
			await Assert.That(ex.InnerException).IsNull();
		}

		[Test]
		public async Task BinderThrows_ExposesBinderContext()
		{
			var builder = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new ThrowingBinder()));

			var caught = CatchBase(() => builder.GetSettings<IIntSettings>());

			await Assert.That(caught is SettingsBindingException).IsTrue();
			var ex = (SettingsBindingException)caught!;
			await Assert.That(ex.BinderType).IsEqualTo(typeof(ThrowingBinder));
			await Assert.That(ex.Section).IsEqualTo("IntSettings");
			await Assert.That(ex.Key).IsEqualTo(nameof(IIntSettings.Value));
		}

		private static SimpleSettingsException? CatchBase(Action act)
		{
			try
			{
				act();
			}
			catch (SimpleSettingsException e)
			{
				return e;
			}

			return null;
		}

		public interface IIntSettings
		{
			int Value { get; set; }
		}

		public class NotAnInterface
		{
		}

		// Throws on every bind so ValuesPopulator wraps it in a SettingsBindingException.
		private class ThrowingBinder : ISectionBinder
		{
			public void BindPropertySettings(BindingContext context)
				=> throw new InvalidOperationException("binder failed");
		}
	}
}
