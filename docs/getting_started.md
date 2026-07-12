Getting Started
===============

SimpleSettings (previously SimpleConfig) uses a `SettingsBuilder` in order to create your settings objects. `SettingsBuilder.ScanAssemblies` returns an `ISettingsCollection` that holds a key value pair of `Type` and the generated implementation of the settings interface. Thus it can be easily registered to any IOC container of your liking.

## Installation
SimpleSettings ships as a set of NuGet packages. The core package is all you need to get started:

````C#
dotnet add package ExistForAll.SimpleSettings
````

`SettingsBuilder.CreateBuilder` is the entry point. Once you have a builder, `ScanAssemblies` accepts one or more Assemblies and returns the collection of generated settings.

````C#
var assemblies = new[] { typeof(Program).Assembly };

var settingsCollection = SettingsBuilder
    .CreateBuilder()
    .ScanAssemblies(assemblies);
````

the result is an `ISettingsCollection` where you can iterate all of the implementations of your settings interfaces.

## First Settings Interface

As written in the introduction SimpleSettings uses interfaces to pass values into services, thus we need to create our first interface.

````C#
[SettingsSection]
public interface IEmailSenderSettings
{
    [SettingsProperty(DefaultValue = "SomeUrl")]
    string EmailServiceUrl { get; set; }

    [SettingsProperty(DefaultValue = 3)]
    int Retries { get; set; }
}
````

When `ScanAssemblies` is invoked, it will search for every indication of a settings interface and use `Emit` to create a concrete class at run time. (Unfortunately Roslyn was not fast enough). The `SettingsProperty` attribute's `DefaultValue` will set the value into the property.

Now as the builder returns the `ISettingsCollection` we can explicitly request the interface like so

````C#
IEmailSenderSettings settings = settingsCollection.GetSettings<IEmailSenderSettings>();
````
and get the values we used as defaults or simply iterate over the items like so

````C#
foreach (var settingsItem in settingsCollection)
{
    Type interfaceType = settingsItem.Key;
    object settingsImplementation = settingsItem.Value;
}
````

If you only need a single interface, you can skip the collection and ask the builder directly:

````C#
var settings = SettingsBuilder
    .CreateBuilder()
    .GetSettings<IEmailSenderSettings>();
````

## Using Dependency Injection

Most applications will wire SimpleSettings into the .NET generic host. Add the `ExistForAll.SimpleSettings.Extensions.GenericHost` package and call `AddSimpleSettings`:

````C#
dotnet add package ExistForAll.SimpleSettings.Extensions.GenericHost
````

````C#
services.AddSimpleSettings(o =>
{
    o.AddAssemblies(new[] { typeof(IEmailSenderSettings).Assembly });
});
````

Every scanned interface is registered as a singleton, so you can inject it directly:

````C#
public class EmailSender
{
    public EmailSender(IEmailSenderSettings settings) { }
}
````

You can also resolve settings through the registered `ISettingsProvider`:

````C#
var provider = serviceProvider.GetRequiredService<ISettingsProvider>();
var settings = provider.GetSettings<IEmailSenderSettings>();
````

SimpleSettings is highly extendable and we will explain how to work with it on the next [page](https://github.com/existall/SimpleSettings/blob/master/docs/building_the_collection.md)
