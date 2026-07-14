# Codebase Structure

**Analysis Date:** 2026-07-13

## Directory Layout

```
SimpleSettings/
├── src/
│   ├── Core/
│   │   ├── ExistForAll.SimpleSettings/                      # Framework-independent core library
│   │   │   ├── Core/                                        # Extraction + reflection internals
│   │   │   │   └── Reflection/                              # Reflection.Emit type generation
│   │   │   ├── Binder/                                      # In-memory binder + collection
│   │   │   ├── Conversion/                                  # Type converters (string → typed)
│   │   │   └── Validations/                                 # Value validation abstractions
│   │   ├── ExistForAll.SimpleSettings.Extensions.Binders/   # Config/env/cmdline binders
│   │   ├── ExistForAll.SimpleSettings.Extensions.GenericHost/ # DI integration + provider
│   │   ├── ExistForAll.SimpleSettings.Core.AspNet/          # ASP.NET environment helpers
│   │   └── ExistForAll.SimpleSettings.DotNet.Frameworks/    # (legacy/framework support dir)
│   ├── Tests/
│   │   ├── ExistForAll.SimpleSettings.UnitTests/            # TUnit unit + integration tests
│   │   └── ExistForAll.SimpleSettings.Tests.Frameworks/     # framework-targeted tests
│   └── performance/
│       └── ExistForAll.SimpleSettings.Benchmark/            # BenchmarkDotNet benchmarks
├── docs/                                                    # Usage documentation (markdown)
├── .github/workflows/                                       # CI + release workflows
├── README.md
├── FIX-PLAN.md                                              # Active remediation/perf plan
└── SESSION-HANDOFF.md                                       # Session context handoff
```

## Directory Purposes

**`src/Core/ExistForAll.SimpleSettings/`:**
- Purpose: The core engine — orchestration, type generation, binding, conversion.
- Key files: `SettingsBuilder.cs`, `SettingsBuilderFactory.cs`, `ValuesPopulator.cs`, `SettingsPlan.cs`, `SettingsOptions.cs`, `SettingsCollection.cs`.

**`src/Core/ExistForAll.SimpleSettings/Core/`:**
- Purpose: Assembly scanning + reflection internals.
- Key files: `SettingsTypesExtractor.cs`, `SettingsOptionsValidator.cs`, `Reflection/SettingsClassGenerator.cs`, `Reflection/TypePropertiesExtractor.cs`, `Reflection/TypeConverter.cs`.

**`src/Core/ExistForAll.SimpleSettings/Conversion/`:**
- Purpose: One `ISettingsTypeConverter` per supported target type.
- Key files: `DefaultTypeConverter.cs`, `EnumTypeConverter.cs`, `ArrayTypeConverter.cs`, `CollectionTypeConverter.cs`, `EnumerableTypeConverter.cs`, `DateTimeTypeConverter.cs`, `UriTypeConvertor.cs`, `PropertyConversion.cs`, `TypeConvertersCollections.cs`.

**`src/Core/ExistForAll.SimpleSettings.Extensions.Binders/`:**
- Purpose: External value-source binders + factory extension methods.
- Key files: `ConfigurationBinder.cs`, `EnvironmentVariableBinder.cs`, `CommandLineSettingsBinder.cs`, `SettingsBuilderFactoryExtensions.cs`.

**`src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/`:**
- Purpose: DI wiring + `ISettingsProvider`.
- Key files: `ServicesSettingsBuilderExtensions.cs`, `SettingsProvider.cs`, `SettingsBuilderOptions.cs`.

## Key File Locations

**Entry Points:**
- `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs`: `AddSimpleSettings` DI entry.
- `src/Core/ExistForAll.SimpleSettings/SettingsBuilder.cs`: `CreateBuilder` direct API.

**Configuration:**
- `src/Core/ExistForAll.SimpleSettings/SettingsOptions.cs`: attribute type, base interface, suffix, delimiters, section-name formatter.
- `*.csproj` (per project): target frameworks `net8.0;net10.0`.

**Core Logic:**
- `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs`: binding + conversion loop.
- `src/Core/ExistForAll.SimpleSettings/Core/Reflection/SettingsClassGenerator.cs`: runtime impl generation.

**Testing:**
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/`: TUnit tests, organized in `Core/`, `Conversion/`, `Binders/`, `DependencyInjection/`, `SimpleSettings/` subfolders.

## Naming Conventions

**Files:**
- One public type per file, filename == type name (e.g. `SettingsBuilder.cs`).
- Interfaces prefixed `I` (`ISectionBinder.cs`, `ISettingsProvider.cs`).
- Exceptions suffixed `Exception` (`SettingsBindingException.cs`).

**Projects/Namespaces:**
- Root namespace `ExistForAll.SimpleSettings`; extensions add a sub-namespace (`.Binders`, `.Extensions.GenericHost`).
- Project directory name matches assembly name (exception: `Extensions.Binders` dir builds `ExistForAll.SimpleSettings.Binders.csproj`).

**Settings interfaces (consumer-side):**
- Recognized by `[SettingsSection]` attribute, `ISettingsSection` base, or a name ending in `Settings` (configurable via `SettingsOptions`).

## Where to Add New Code

**New binder (value source):**
- Implementation: `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/` implementing `ISectionBinder`.
- Wiring: add an extension method in `SettingsBuilderFactoryExtensions.cs`.

**New type converter:**
- Implementation: `src/Core/ExistForAll.SimpleSettings/Conversion/` implementing `ISettingsTypeConverter`.
- Register in `TypeConvertersCollections.cs` (order matters; `DefaultTypeConverter` stays last).

**New core feature:**
- Primary code: `src/Core/ExistForAll.SimpleSettings/` (or `Core/` for reflection/extraction internals).
- Tests: matching subfolder under `src/Tests/ExistForAll.SimpleSettings.UnitTests/`.

**New DI/host feature:**
- Implementation: `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/`.

## Special Directories

**`src/BenchmarkDotNet.Artifacts/`:**
- Purpose: Benchmark run output.
- Generated: Yes. Committed: No (gitignored).

**`bin/`, `obj/`:**
- Purpose: Build output. Generated: Yes. Committed: No.

**`.planning/`, `.claude/`, `.codex/`:**
- Purpose: GSD planning artifacts and agent tooling. Not part of the shipped library.

---

*Structure analysis: 2026-07-13*
