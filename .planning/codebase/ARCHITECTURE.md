<!-- refreshed: 2026-07-13 -->
# Architecture

**Analysis Date:** 2026-07-13

## System Overview

```text
┌─────────────────────────────────────────────────────────────┐
│                     Consumer Entry Points                    │
├──────────────────────────┬──────────────────────────────────┤
│  Generic Host / DI        │  Direct API                     │
│  AddSimpleSettings(...)    │  SettingsBuilder.CreateBuilder  │
│  `ServicesSettingsBuilder  │  `SettingsBuilder.cs`           │
│   Extensions.cs`           │                                 │
└────────────┬──────────────┴─────────────────┬────────────────┘
             │                                 │
             ▼                                 ▼
┌─────────────────────────────────────────────────────────────┐
│                      SettingsBuilder                         │
│  `Core/ExistForAll.SimpleSettings/SettingsBuilder.cs`        │
│  Orchestrates: extract types → generate impl → populate      │
└──────┬───────────────┬───────────────┬──────────────┬────────┘
       │               │               │              │
       ▼               ▼               ▼              ▼
┌────────────┐  ┌────────────┐  ┌────────────┐  ┌─────────────┐
│SettingsType│  │SettingsClass│ │ValuesPopul-│  │ISectionBinder│
│sExtractor  │  │Generator    │ │ator + Plan │  │chain         │
│(scan asm)  │  │(Reflection. │ │(bind+conv) │  │(config/env/  │
│            │  │ Emit types) │ │            │  │ cmdline)     │
└────────────┘  └────────────┘  └─────┬──────┘  └─────────────┘
                                       │
                                       ▼
                          ┌─────────────────────────┐
                          │  Conversion pipeline     │
                          │  `Conversion/*.cs`       │
                          │  string → target type    │
                          └─────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| `SettingsBuilder` | Orchestrates the whole build: validate options, extract types, generate impls, populate values | `src/Core/ExistForAll.SimpleSettings/SettingsBuilder.cs` |
| `SettingsBuilderFactory` | Collects `SettingsOptions` and ordered `ISectionBinder`s during the build-action callback | `src/Core/ExistForAll.SimpleSettings/SettingsBuilderFactory.cs` |
| `SettingsTypesExtractor` | Scans assemblies, selects settings interfaces (attribute / base interface / suffix match) | `src/Core/ExistForAll.SimpleSettings/Core/SettingsTypesExtractor.cs` |
| `SettingsClassGenerator` | Emits a concrete impl class per interface via `Reflection.Emit`, caches by interface type | `src/Core/ExistForAll.SimpleSettings/Core/Reflection/SettingsClassGenerator.cs` |
| `TypePropertiesExtractor` | Extracts + dedups interface properties (incl. inherited), memoized per type | `src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypePropertiesExtractor.cs` |
| `ValuesPopulator` | Builds/caches a `SettingsPlan` per type, runs binders per property, converts, sets values | `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs` |
| `SettingsPlan` / `PropertyPlan` | Per-type immutable plan: section name + per-property key/default/conversion | `src/Core/ExistForAll.SimpleSettings/SettingsPlan.cs` |
| `ISectionBinder` chain | Pluggable value sources (in-memory, configuration, env var, command line) | `src/Core/ExistForAll.SimpleSettings/ISectionBinder.cs` |
| `BindingContext` | Per-property binding state passed to binders (section, key, current/new value) | `src/Core/ExistForAll.SimpleSettings/BindingContext.cs` |
| Conversion pipeline | Converts bound string values to strongly-typed property values | `src/Core/ExistForAll.SimpleSettings/Conversion/` |
| `SettingsCollection` / `SettingsHolder` | Holds built instances keyed by interface type; lookup + enumeration | `src/Core/ExistForAll.SimpleSettings/SettingsCollection.cs` |
| `ISettingsProvider` | DI-facing lookup that returns startup-built singletons (falls back to on-demand build) | `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/SettingsProvider.cs` |

## Pattern Overview

**Overall:** Builder + pluggable pipeline (chain of binders) with runtime type generation.

**Key Characteristics:**
- Consumer defines settings as **interfaces**; the library emits concrete implementations at runtime via `System.Reflection.Emit`.
- Value resolution is a **chain of `ISectionBinder`s** applied in registration order — later binders override earlier ones (last-writer-wins per property).
- Heavy **per-type caching**: generated types, extracted properties, and populate plans are all memoized to keep repeated resolves and cold assembly scans cheap.
- **Framework-independent core**: `ExistForAll.SimpleSettings` has no DI or configuration dependency; integrations live in separate extension projects.

## Layers

**Core (`ExistForAll.SimpleSettings`):**
- Purpose: Type extraction, impl generation, binding orchestration, conversion.
- Location: `src/Core/ExistForAll.SimpleSettings/`
- Contains: `SettingsBuilder`, `ValuesPopulator`, `Core/` (reflection + extraction), `Binder/`, `Conversion/`, `Validations/`.
- Depends on: BCL only.
- Used by: every extension project.

**Binder extensions (`ExistForAll.SimpleSettings.Binders`):**
- Purpose: `ISectionBinder` implementations for external sources.
- Location: `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/`
- Contains: `ConfigurationBinder`, `EnvironmentVariableBinder`, `CommandLineSettingsBinder` + their options + factory extension methods.
- Depends on: Core, `Microsoft.Extensions.Configuration`.

**Generic Host integration (`ExistForAll.SimpleSettings.Extensions.GenericHost`):**
- Purpose: `IServiceCollection.AddSimpleSettings(...)` DI wiring and `ISettingsProvider`.
- Location: `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/`
- Depends on: Core, `Microsoft.Extensions.DependencyInjection`.

**AspNet support (`ExistForAll.SimpleSettings.Core.AspNet`):**
- Purpose: ASP.NET environment helpers.
- Location: `src/Core/ExistForAll.SimpleSettings.Core.AspNet/`

## Data Flow

### Primary Path — DI startup scan

1. `AddSimpleSettings(services, action)` builds a `SettingsBuilder` and applies binder/options config (`ServicesSettingsBuilderExtensions.cs:26`).
2. `SettingsBuilder.ScanAssemblies` extracts settings interfaces from configured assemblies (`SettingsBuilder.cs:74`).
3. For each interface, `InnerBuild` generates the impl type, instantiates it, and populates values (`SettingsBuilder.cs:99`).
4. `ValuesPopulator.PopulateInstanceWithValues` gets/builds the cached `SettingsPlan`, then per property runs every binder in order, tracking overrides via `BindingContext` (`ValuesPopulator.cs:33`).
5. Each bound value is converted through the property's precomputed `PropertyConversion` and set on the instance (`ValuesPopulator.cs:60`).
6. Each interface + instance is registered as a DI singleton, and an `ISettingsProvider` wraps the same collection (`ServicesSettingsBuilderExtensions.cs:38`).

### Secondary Path — direct / on-demand resolve

1. `SettingsBuilder.CreateBuilder(buildAction)` collects options + binders (`SettingsBuilder.cs:46`).
2. `GetSettings(Type)` validates the type is an interface and calls `InnerBuild` for a single instance (`SettingsBuilder.cs:64`).
3. `SettingsProvider.GetSettings` returns the startup singleton if scanned, else builds on demand (`SettingsProvider.cs:16`).

**State Management:**
- Built instances are effectively singletons held in `SettingsCollection`; there is no reload/monitor path (see comments in `ServicesSettingsBuilderExtensions.cs`).
- `ConfigurationBinder` caches live `IConfigurationSection` views, so a config reload is still reflected on re-read even though instances are cached.

## Key Abstractions

**`ISectionBinder`:**
- Purpose: A value source contributing to property binding.
- Examples: `Binder/InMemoryBinder.cs`, `Extensions.Binders/ConfigurationBinder.cs`, `EnvironmentVariableBinder.cs`, `CommandLineSettingsBinder.cs`.
- Pattern: Called once per property with a `BindingContext`; calls `SetNewValue` to override.

**`ISettingsTypeConverter`:**
- Purpose: Converts a bound value to a target type.
- Examples: `Conversion/DefaultTypeConverter.cs`, `EnumTypeConverter.cs`, `ArrayTypeConverter.cs`, `DateTimeTypeConverter.cs`, `UriTypeConvertor.cs`.
- Pattern: `CanConvert(type)` probe + `Convert(value, type)`; registered in `TypeConvertersCollections` (a linked list, first match wins, `DefaultTypeConverter` is the catch-all).

**`SettingsPlan` / `PropertyPlan`:**
- Purpose: Immutable per-type recipe computed once (section name, per-property key, default, resolved conversion).
- File: `SettingsPlan.cs`.
- Pattern: `PropertyPlan` is a `readonly struct` held inline in an array to avoid per-property heap allocation.

## Entry Points

**DI registration:**
- Location: `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs`
- Triggers: Consumer calls `services.AddSimpleSettings(...)`.
- Responsibilities: Build, scan, register singletons + provider.

**Direct builder:**
- Location: `src/Core/ExistForAll.SimpleSettings/SettingsBuilder.cs`
- Triggers: `SettingsBuilder.CreateBuilder(...)` then `GetSettings`/`ScanAssemblies`.

## Architectural Constraints

- **Threading:** Build/populate is synchronous. Caches (`SettingsClassGenerator._generatedTypes`, `TypePropertiesExtractor._cache`, `ValuesPopulator._plans`, `ConfigurationBinder._sections`) are `ConcurrentDictionary` and rely on deterministic, idempotent builds — concurrent races are harmless last-writer-wins.
- **Runtime code generation:** Uses `Reflection.Emit` (`AssemblyBuilderAccess.RunAndCollect`); not AOT/trim friendly. Settings types **must be interfaces**.
- **Cache scoping:** `ValuesPopulator._plans` and `TypePropertiesExtractor._cache` are per-instance (not static) because a plan bakes in a specific `SettingsOptions`; exactly one `ValuesPopulator` exists per `SettingsBuilder`.
- **Binder order significance:** Binders are stored in a `SortedList<int,...>` keyed by insertion counter; order determines override precedence.
- **No reload:** Built instances are snapshots; there is no `IOptionsMonitor`-style live update of instances.

## Anti-Patterns

### Throwing value-bearing exceptions from binders/converters

**What happens:** An `ISectionBinder` or converter throws an exception whose message embeds the fetched configuration value.
**Why it's wrong:** `SettingsBindingException` chains the thrown exception, so a secret value could reach logs (see S1 in `FIX-PLAN.md`). `SettingsPropertyValueException` deliberately surfaces only the failure's type name, never the value or inner exception.
**Do this instead:** Only surface type/property metadata in exceptions; never the bound value. See `ValuesPopulator.ConvertPropertyValue` (`ValuesPopulator.cs:117`) and the contract note in `ISectionBinder.cs`.

### Non-interface settings types

**What happens:** Passing a concrete class as a settings type.
**Why it's wrong:** The generator only implements interfaces; `GetSettings` throws `InvalidOperationException`.
**Do this instead:** Define settings as interfaces (`SettingsBuilder.cs:66`).

## Error Handling

**Strategy:** Wrap low-level failures in domain-specific exceptions that preserve context but redact values.

**Patterns:**
- Extraction/generation failures wrapped: `SettingsExtractionException`, `SettingsPropertyExtractionException`, `TypeGenerationException`.
- Binding failures wrapped: `SettingsBindingException` (carries binder + context).
- Conversion failures wrapped as value-free `SettingsPropertyValueException`; `SettingsPropertyNullException` propagates unwrapped as the "required value missing" signal.

## Cross-Cutting Concerns

**Logging:** None built in.
**Validation:** `SettingsOptionsValidator` validates options pre-build; `Validations/` holds a settings-value validation abstraction (`ISettingsValidator`, `ValidationResult`).
**Authentication:** Not applicable.

---

*Architecture analysis: 2026-07-13*
