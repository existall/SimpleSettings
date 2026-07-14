using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.Conversion;

namespace ExistForAll.SimpleSettings
{
    public static class SettingsBuilderFactoryExtensions
    {
        public static T AddTypeConverter<T>(this T target, ISettingsTypeConverter settingsTypeConverter) where T : ISettingsBuilderFactory
        {
            if (settingsTypeConverter == null) throw new ArgumentNullException(nameof(settingsTypeConverter));
            target.Options.Converters.AddFirst(settingsTypeConverter);
            return target;
        }
        
        public static T AddInMemoryCollection<T>(this T target, InMemoryCollection collection) where T : ISettingsBuilderFactory
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            target.AddSectionBinder(new InMemoryBinder(collection));
            return target;
        }
        
        public static T SetupOptions<T>(this T target, Action<SettingsOptions> action) where T : ISettingsBuilderFactory
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            action(target.Options);
            return target;
        }

        public static T SetSettingsSuffix<T>(this T target, string suffix) where T : ISettingsBuilderFactory
        {
            target.SetupOptions(x => x.SettingsSuffix = suffix);
            return target;
        }

        public static T SetArraySplitDelimiter<T>(this T target, string arrayDelimiter) where T : ISettingsBuilderFactory
        {
            if (arrayDelimiter == null) throw new ArgumentNullException(nameof(arrayDelimiter));
            target.SetupOptions(x => x.ArraySplitDelimiter = arrayDelimiter);
            return target;
        }

        public static T SetAttributeType<T>(this T target, Type attribute) where T : ISettingsBuilderFactory
        {
            if (attribute == null) throw new ArgumentNullException(nameof(attribute));
            target.SetupOptions(x => x.AttributeType = attribute);
            return target;
        }

        public static T SetDateTimeFormat<T>(this T target, string dateTimeFormat) where T : ISettingsBuilderFactory
        {
            target.SetupOptions(x => x.DateTimeFormat = dateTimeFormat);
            return target;
        }

        public static T SetInterfaceBase<T>(this T target, Type interfaceType) where T : ISettingsBuilderFactory
        {
            target.SetupOptions(x => x.InterfaceBase = interfaceType);
            return target;
        }

        public static T SetSectionNameFormatter<T>(this T target, Func<Type, string> sectionNameFormatter) where T : ISettingsBuilderFactory
        {
            target.SetupOptions(x => x.SectionNameFormatter = sectionNameFormatter);
            return target;
        }
    }
}