using System.Reflection;
using ExistForAll.SimpleSettings.Core;
using ExistForAll.SimpleSettings.Core.Reflection;

namespace ExistForAll.SimpleSettings
{
    public class SettingsBuilder
    {
        private readonly ISettingsTypesExtractor _settingsTypesExtractor;
        private readonly ISettingsOptionsValidator _settingsOptionsValidator;
        private readonly ISettingsClassGenerator _settingsClassGenerator;
        private readonly IValuesPopulator _valuesPopulator;

        private SettingsOptions Options { get; }
        private IEnumerable<ISectionBinder> SectionBinders { get; }

        internal SettingsBuilder(SettingsOptions options,
            IEnumerable<ISectionBinder> sectionBinders)
            : this(options,
                sectionBinders,
                new SettingsTypesExtractor(),
                new SettingsOptionsValidator(),
                new SettingsClassGenerator(),
                new ValuesPopulator())
        { }

        internal SettingsBuilder(SettingsOptions options,
            IEnumerable<ISectionBinder> sectionBinders,
            ISettingsTypesExtractor settingsTypesExtractor,
            ISettingsOptionsValidator settingsOptionsValidator,
            ISettingsClassGenerator settingsClassGenerator,
            IValuesPopulator valuesPopulator)
        {
            Options = options;
            // Materialize once so the populator's `binders as ISectionBinder[]` fast-path hits instead of
            // re-allocating via ToArray on every populate (the factory hands us a SortedList.Values view).
            SectionBinders = sectionBinders as ISectionBinder[] ?? sectionBinders.ToArray();
            _settingsTypesExtractor = settingsTypesExtractor;
            _settingsOptionsValidator = settingsOptionsValidator;
            _settingsClassGenerator = settingsClassGenerator;
            _valuesPopulator = valuesPopulator;
        }

        public static SettingsBuilder CreateBuilder(Action<ISettingsBuilderFactory> buildAction)
        {
            if (buildAction == null) throw new ArgumentNullException(nameof(buildAction));

            var factory = new SettingsBuilderFactory();

            buildAction(factory);

            return new SettingsBuilder(factory.Options, factory.Binders);
        }

        public static SettingsBuilder CreateBuilder()
        {
            var factory = new SettingsBuilderFactory();

            return new SettingsBuilder(factory.Options, factory.Binders);
        }

        public object GetSettings(Type settingsType)
        {
            if (settingsType == null) throw new ArgumentNullException(nameof(settingsType));

            if (!settingsType.GetTypeInfo().IsInterface)
            {
                throw new SettingsTypeNotInterfaceException(settingsType);
            }

            return InnerBuild(settingsType);
        }

        public ISettingsCollection ScanAssemblies(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

            var extractedSettingsTypes = _settingsTypesExtractor.ExtractSettingsTypes(assemblies, Options);

            return InnerBuild(extractedSettingsTypes);
        }

        private ISettingsCollection InnerBuild(IEnumerable<Type> interfaces)
        {
            _settingsOptionsValidator.ValidateOptions(Options);

            var collection = new SettingsCollection();

            foreach (var settingsInterface in interfaces)
            {
                var instance = InnerBuild(settingsInterface);

                collection.Add(settingsInterface, instance);
            }

            return collection;
        }

        internal object InnerBuild(Type @interface)
        {
            var generateType = _settingsClassGenerator.GenerateType(@interface);

            var instance = Activator.CreateInstance(generateType)!;

            _valuesPopulator.PopulateInstanceWithValues(instance, @interface, Options, SectionBinders);

            return instance;
        }
    }
}