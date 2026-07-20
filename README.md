<img src="https://raw.githubusercontent.com/existall/SimpleSettings/master/icon.png" alt="ExistForAll.SimpleSettings">

ExistForAll.SimpleSettings
==========================

Strongly-typed application settings for .NET. Declare a plain `public` interface, decorate it with defaults, and SimpleSettings binds your configuration into a runtime implementation you can inject anywhere — no concrete option classes, no per-type `services.Configure<>` wiring.

## Installation

```bash
dotnet add package ExistForAll.SimpleSettings
dotnet add package ExistForAll.SimpleSettings.Binders
dotnet add package ExistForAll.SimpleSettings.Extensions.GenericHost
```

- `ExistForAll.SimpleSettings` — the core binding engine and the direct `SettingsBuilder` API.
- `ExistForAll.SimpleSettings.Binders` — additional binders (in-memory, command-line, and more).
- `ExistForAll.SimpleSettings.Extensions.GenericHost` — dependency-injection integration (`AddSimpleSettings`).

## Table of Content

1. [Getting started](https://github.com/existall/SimpleSettings/blob/master/docs/getting_started.md)
2. [Building the collection](https://github.com/existall/SimpleSettings/blob/master/docs/building_the_collection.md)
3. [Building config interfaces](https://github.com/existall/SimpleSettings/blob/master/docs/Build%20Config%20Interface.md)
4. [Default Values](https://github.com/existall/SimpleSettings/blob/master/docs/Default%20Values.md)
5. [Build section binders](https://github.com/existall/SimpleSettings/blob/master/docs/Build%20a%20SectionBinder.md)
6. [Extending SimpleSettings](https://github.com/existall/SimpleSettings/blob/master/docs/Extending%20SimpleSettings.md)
7. [Security & Behavior](https://github.com/existall/SimpleSettings/blob/master/docs/Security.md)

## Why SimpleSettings

.NET ships `IOptions<>`, but it couples your application to a framework abstraction, forces every settings shape to be a concrete class rather than an interface, and requires a manual `services.Configure<>` call per type. SimpleSettings keeps the positioning of `IOptions<>` — configuration bound into typed objects — while letting you depend on a plain interface, discovered and bound automatically, and portable across DI containers. You inject the interface; SimpleSettings supplies the implementation.

## Quickstart

Declare a settings interface and bind it with the direct API:

```csharp
[SettingsSection]
public interface IEmailSenderSettings
{
    [SettingsProperty(DefaultValue = "https://smtp.example.com")]
    string ServiceUrl { get; set; }

    [SettingsProperty(DefaultValue = 3)]
    int Retries { get; set; }
}

var settings = SettingsBuilder.CreateBuilder()
    .GetSettings<IEmailSenderSettings>();
```

Every settings interface must be `public` — SimpleSettings emits a runtime implementation of the interface and cannot implement a non-public one.

## Feature overview

- Bind configuration into `public` settings **interfaces** — no concrete option classes.
- Discover settings via `[SettingsSection]`, the `ISettingsSection` marker base, or a `Settings` name suffix.
- Per-property defaults, key overrides, custom converters, and required-value enforcement via `[SettingsProperty(...)]`.
- Object-level and per-property validation through `ISettingValidation<T>`.
- First-class dependency-injection integration for the .NET Generic Host.
- Value-free exceptions on bind and conversion failures — bound values never surface in error messages (see [Security & Behavior](https://github.com/existall/SimpleSettings/blob/master/docs/Security.md)).

## Dependency injection

Register SimpleSettings with the Generic Host and let it discover every settings interface in the supplied assemblies:

```csharp
services.AddSimpleSettings(o =>
{
    o.AddAssemblies(new[] { typeof(IEmailSenderSettings).Assembly });
});

// after building the provider — opt-in, deferred DI validation:
serviceProvider.ValidateSimpleSettings();
```

`ValidateSimpleSettings()` extends `IServiceProvider` and must be called **after** `BuildServiceProvider()`; attribute and `ValidatorType` validators run inline during binding and need no such call. See [Security & Behavior](https://github.com/existall/SimpleSettings/blob/master/docs/Security.md) for the full validation model.
