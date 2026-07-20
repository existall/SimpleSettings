# Extend SimpleSettings
## SettingsOptions
Let's look at `SettingsOptions` which is the way you may change SimpleSettings default behavior.

````C#
public class SettingsOptions
{
    public Type AttributeType { get; set; } = typeof(SettingsSectionAttribute);
    public Type InterfaceBase { get; set; } = typeof(ISettingsSection);
    public string SettingsSuffix { get; set; } = "Settings";
    public string ArraySplitDelimiter { get; set; } = ",";
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd";
    public Func<Type, string> SectionNameFormatter { get; set; } = (interfaceType) => interfaceType.GetNormalizeInterfaceName();
}
````

You don't set these properties directly; instead the builder factory exposes `SetupOptions` and a set of `Set*` helpers that mutate the options for you.

````C#
SettingsBuilder.CreateBuilder(factory =>
{
    factory.SetSettingsSuffix("Settings");
    factory.SetAttributeType(typeof(MyAttribute));
    factory.SetInterfaceBase(typeof(IMyMarker));
    factory.SetSectionNameFormatter(type => type.Name);
});
````

## Decoupling SimpleSettings
For good practice you always want your application to not be dependent on Frameworks such as SimpleSettings, except for the bootstrapping process (CompositionRoot) all of the data object, services and logic should be your own. This can be achieved using abstractions. SimpleSettings let you use your own types as indications of settings interfaces.

### AttributeType
SimpleSettings scans all the assemblies provided to `ScanAssemblies`, any Types with `SettingsSectionAttribute` will be added to the collection. By replacing the Type with your own (via `SetAttributeType`), SimpleSettings will scan for your Attribute.

### InterfaceBase
Same as `AttributeType`, SimpleSettings searches for the `ISettingsSection` interface as indication of a settings interface. By replacing the interface Type with your own (via `SetInterfaceBase`), SimpleSettings will scan for your interface.

### SettingsSuffix
Same as `AttributeType` and `SettingsSectionAttribute`, SimpleSettings searches for interfaces with names ending with the `Settings` suffix, you can provide any suffix you want via `SetSettingsSuffix`.

#### although SimpleSettings provides three ways as settings interface indications that does not mean you have to use all three, simply choose one -> "KISS".

## Type Converter Options

### ArraySplitDelimiter
SimpleSettings tries to convert the binders string value into the property type, when the type is an array SimpleSettings needs to know with what to split the values. By default SimpleSettings uses `','` (change it with `SetArraySplitDelimiter`) but I love giving you the choice to decide for yourself.

### DateTimeFormat
When SimpleSettings tries to parse DateTime from string it uses this property as a format, you can change this to anything your application uses via `SetDateTimeFormat`.

### Custom Type Converters
SimpleSettings converts the resolved value into the property type using a chain of `ISettingsTypeConverter`. You can plug in your own by implementing the interface and registering it with `AddTypeConverter` (your converter is added first, so it wins over the built-in ones).

````C#
public class MyConverter : ISettingsTypeConverter
{
    public bool CanConvert(Type settingsType) => settingsType == typeof(MyType);

    public object Convert(object value, Type settingsType) => MyType.Parse((string)value);
}

SettingsBuilder.CreateBuilder(factory => factory.AddTypeConverter(new MyConverter()));
````

## Binders

### SectionNameFormatter
SimpleSettings converts your settings interface name into a section name for the binders thus indicating and allowing you to know what to look for. As default SimpleSettings will trim the leading `I` from the interface name. If you like to replace this behavior simply provide a `Func<Type, string>` of your own (via `SetSectionNameFormatter`) to do so.

# Future Features

I would like to continue and develop SimpleSettings as people will use it more and more thus helping and creating feature requests.

Few ideas for future features are:
1. Diagnostics and reporting.
2. Roslyn support for those who will want to use it. (I did some performance tests with Roslyn and it was not good).
3. AOP.
