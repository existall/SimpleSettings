# Building The Collection
To build the settings collection you must provide the assemblies SimpleSettings scans in order to get all the settings interfaces.

You pass those assemblies to `ScanAssemblies`, which has an overload that accepts an `IEnumerable<Assembly>` and another that accepts one required assembly followed by any number of additional ones.

````C#
var settingsCollection = SettingsBuilder
    .CreateBuilder()
    .ScanAssemblies(typeof(Program).Assembly);
````

SimpleSettings scans only the public (exported) interfaces in the assemblies you provide, so make sure the interfaces you want discovered are public.

## Options

To configure SimpleSettings, use the `CreateBuilder` overload that gives you a builder factory. The factory exposes `SetupOptions` along with a set of `Set*` helpers that mutate `SettingsOptions`. To better understand the options see the [Extend](https://github.com/existall/SimpleSettings/blob/master/docs/Extend%20Simple%20Config.md) section.

````C#
var settingsCollection = SettingsBuilder
    .CreateBuilder(factory =>
    {
        factory.SetDateTimeFormat("yyyy-MM-dd");
        factory.SetArraySplitDelimiter(";");
    })
    .ScanAssemblies(typeof(Program).Assembly);
````

## Binders
SimpleSettings can populate properties with values not only from the `SettingsProperty` default value but from any `Binder` you can create.

To add a Binder use the `AddSectionBinder` method on the builder factory like so.
````C#
SettingsBuilder.CreateBuilder(factory =>
{
    factory.AddSectionBinder(sectionBinder);
});
````

#### The order in which binders are added is important, the last binder will set the value into the property. If no value was set the default value will be set.

SimpleSettings provides several binders out of the box. `InMemoryCollection` lives in the core package and is added via `AddInMemoryCollection`. The `ExistForAll.SimpleSettings.Binders` package adds a `ConfigurationBinder` which can extract values from Microsoft .NET `IConfiguration` (via `AddConfiguration`), an `EnvironmentVariableBinder` (via `AddEnvironmentVariable`) and a command line binder (via `AddCommandLine` / `AddArguments`).

For more information about the Binders see the [Build a SectionBinder](https://github.com/existall/SimpleSettings/blob/master/docs/Build%20a%20SectionBinder.md) page.

To create new Binders see the [Extend section](https://github.com/existall/SimpleSettings/blob/master/docs/Extend%20Simple%20Config.md).

To continue on to Settings Interfaces click [here](https://github.com/existall/SimpleSettings/blob/master/docs/Build%20Config%20Interface.md).
