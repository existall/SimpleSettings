# Coding Conventions

**Analysis Date:** 2026-07-13

## Naming Patterns

**Files:**
- One public type per file, filename matches the type name: `SettingsBuilder.cs`, `ISectionBinder.cs`, `ValuesPopulator.cs`.
- Interfaces prefixed with `I`: `ISettingsCollection.cs`, `IValuesPopulator.cs`, `ITypeConverter.cs`.
- Exceptions suffixed with `Exception`: `SettingsBindingException.cs`, `SettingsPropertyValueException.cs`, `SettingsTypeNotFoundException.cs`.
- Attributes suffixed with `Attribute`: `SettingsSectionAttribute.cs`, `SettingsPropertyAttribute.cs`.
- Extension method holders suffixed with `Extensions`: `SettingsBuilderExtensions.cs`, `SettingsCollectionExtensions.cs`, `SettingsBuilderFactoryExtensions.cs`.
- Options POCOs suffixed with `Options`: `SettingsOptions.cs`, `CommandLineSettingsBinderOptions.cs`, `EnvironmentVariableBinderOptions.cs`.

**Types:**
- PascalCase for classes, interfaces, structs, enums.
- Public interfaces model the extension points (`ISectionBinder`, `ISettingsProvider`, `ITypeConverter`); concrete implementations are usually `internal` (e.g. `internal class ValuesPopulator : IValuesPopulator` in `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs`).

**Functions/Methods:**
- PascalCase: `PopulateInstanceWithValues`, `GetOrBuildPlan`, `ConvertPropertyValue`, `BindPropertySettings`.

**Variables:**
- Private fields: `_camelCase` with leading underscore (`_typePropertiesExtractor`, `_typeConverter`, `_plans`).
- Locals and parameters: `camelCase` (`sectionBinders`, `propertyPlan`, `tempValue`).
- Constants: PascalCase (`Secret`, `SettingsOptionsArgumentNullMessage`).

## Code Style

**Formatting:**
- No `.editorconfig` or formatter config is present in the repo. Style is by convention only.
- Indentation is inconsistent across the codebase: tabs in most core files (`ValuesPopulator.cs`, `SettingsBindingException.cs`, `Resources.cs`) vs. spaces in some (`SettingsBuilder.cs`). Match the surrounding file when editing; prefer tabs for new core files to match the majority.
- Allman brace style (opening brace on its own line) throughout.

**Language features:**
- `Nullable` is enabled solution-wide (`src/Directory.Build.props` → `<Nullable>enable</Nullable>`). Use `?` annotations and honor null-flow; e.g. `object?` return in `ConvertPropertyValue`.
- `ImplicitUsings` is enabled solution-wide (`<ImplicitUsings>enable</ImplicitUsings>`), yet source files still declare explicit `using System;` etc. Both coexist.
- `LangVersion` is intentionally unset — each TFM uses its default (net8.0 → C# 12, net10.0 → C# 14). Do not add version-specific syntax that breaks the net8.0 build.
- Namespaces are **block-scoped** (`namespace X { ... }`), not file-scoped. Match this.

**Linting:**
- No analyzer package or ruleset configured beyond the SDK defaults. `Deterministic` and `ContinuousIntegrationBuild` are set for reproducible packages, not for style enforcement.

## Import Organization

**Order (observed):**
1. `System.*` namespaces first.
2. Third-party / `Microsoft.Extensions.*`.
3. Project namespaces (`ExistForAll.SimpleSettings.*`).

**Path Aliases:**
- None. Standard `using` directives.
- Test project centralizes framework usings in `src/Tests/ExistForAll.SimpleSettings.UnitTests/GlobalUsings.cs` (`global using TUnit.Core;`, `TUnit.Assertions`, `TUnit.Assertions.Extensions`).

## Error Handling

**Patterns:**
- Custom exception types per failure mode, all deriving from `Exception`, all located in `src/Core/ExistForAll.SimpleSettings/` (`SettingsBindingException`, `SettingsPropertyValueException`, `SettingsPropertyNullException`, `SettingsTypeNotFoundException`, `SettingsOptionsArgument*Exception`).
- Exception messages are centralized as static factory methods / consts in `src/Core/ExistForAll.SimpleSettings/Resources.cs`, not inlined at throw sites.
- Wrap-and-rethrow with context: binder failures are caught and rethrown as `SettingsBindingException(binder, context, e)` (`ValuesPopulator.cs:59-62`).
- **Secret redaction is a hard rule (S1):** when a bound value fails conversion, the raw value must never appear in the exception message OR a chained inner exception. `ConvertPropertyValue` throws `SettingsPropertyValueException(settingsType, property, e)` carrying only the property, target type, and failing exception's type name — never the value or the inner exception (`ValuesPopulator.cs:117-131`, message in `Resources.PropertySetterExceptionMessage`). Preserve this when touching conversion/error paths.
- Exception filters are used to let value-free signals pass through: `catch (Exception e) when (e is not SettingsPropertyNullException)` so a "required value missing" signal propagates un-redacted.

## Logging

**Framework:** None. The library has no logging dependency; it surfaces failures as typed exceptions and leaves logging to the host. This is deliberate — see the redaction rationale (values are omitted precisely because they land in host logs via `Exception.ToString()`).

## Comments

**When to Comment:**
- Heavy use of explanatory comments justifying *why* a non-obvious choice was made (performance caching decisions, thread-safety reasoning, security redaction). See the block comments in `ValuesPopulator.cs` explaining the per-plan cache and last-writer-wins concurrency.
- Comments reference plan identifiers (`P2`, `P5`, `S1`) that cross-link to `FIX-PLAN.md`. Keep these references intact when editing nearby code.
- XML/TSDoc doc-comments are largely absent; prose comments dominate.

## Function Design

**Size:** Small, single-responsibility methods. Larger methods (`GetOrBuildPlan`) are broken by extracting helpers (`ConvertPropertyValue`).

**Parameters:**
- `in` modifier used for readonly struct passing on hot paths: `ConvertPropertyValue(..., in PropertyPlan propertyPlan)`.
- Defensive materialization of `IEnumerable` params with the `x as T[] ?? x.ToArray()` idiom to avoid re-enumeration allocations (`ValuesPopulator.cs:37`, `SettingsBuilder.cs`).

**Return Values:**
- Nullable annotations honored (`object?`).

## Module Design

**Exports:**
- Public surface = interfaces + attributes + exceptions + builder/options entry points. Implementations default to `internal`.
- Extension methods provide the fluent/registration API (`SettingsBuilderExtensions`, `ServicesSettingsBuilderExtensions`).

**Barrel Files:** None. One type per file.

**Project layout:** Core library (`ExistForAll.SimpleSettings`) plus thin satellite packages per integration (`.Extensions.Binders`, `.Extensions.GenericHost`, `.Core.AspNet`), each with its own `.csproj` and `RootNamespace`.

**Package management:** Central — versions live in `src/Directory.Packages.props` (`ManagePackageVersionsCentrally=true`). Never pin a `Version` on a `PackageReference`; add a `PackageVersion` entry instead.

---

*Convention analysis: 2026-07-13*
