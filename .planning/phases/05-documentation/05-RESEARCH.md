# Phase 5: Documentation - Research

**Researched:** 2026-07-20
**Domain:** Consumer-facing documentation accuracy (README + docs/ + package metadata) for a .NET settings-binding library
**Confidence:** HIGH (every API fact and legacy-reference is a direct read of the canonical source at a cited file:line; nothing here is from training data)

## Summary

This is a **documentation phase — no runtime/behavior change**. The research question is not "how do we build X" but "what is the exact current ground truth so every doc claim is accurate." I read the real source for every public API token the docs must show, and I greppe the whole consumer-facing surface (root `README.md`, all six `docs/*.md`, `src/Directory.Build.props`) for legacy naming and stale API forms.

**Key finding:** the `docs/` folder is already **substantially modernized** — five of six pages use canonical `existall/SimpleSettings` links and the correct `[SettingsProperty(DefaultValue = …)]` form, and every API token they reference (generic `GetSettings<T>()` helpers, `Set*` factory extensions, `AddInMemoryCollection`, `BindingContext`, `ISettingsTypeConverter`) was verified to exist in source. The **README is the stale artifact**: it still carries the broken `existall/Shepherd` logo, the legacy `Install-Package` PMC command, six dead `existall/SimpleConfig/blob/...` ToC links, the factually-wrong `[DefaultValue("SomeUrl")]` example, a "(previously SimpleConfig)" tag, and none of the Phase 1–4 security/behavior guidance. Residual legacy in docs/ is narrow: one "(previously SimpleConfig)" line in `getting_started.md`, and the file `docs/Extend Simple Config.md` still bears its legacy filename (its *content* is already titled "Extend SimpleSettings") with six inbound links pointing at it. In metadata, `Directory.Build.props` has the `<Description>` "appliaction" typo and a `SimpleConfig` `<PackageTags>` token; repo/package URLs are already canonical.

**Primary recommendation:** Rewrite the README against the verified API table in this document (never trust the old README); do a targeted purge (not a rewrite) of the six docs pages; rename `docs/Extend Simple Config.md` and repoint its six inbound links; fix the two `Directory.Build.props` tokens. Gate the phase on the mechanical DOC-VERIFICATION grep/build checks in the Validation Architecture section. Every security/behavior claim must be copied from the invariants documented here (sourced from Phase 4 `SECURITY.md`), because a paraphrase risks over-promising a guarantee.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**README shape — modernize, self-contained**
- **D-01:** Rewrite `README.md` to a modern, self-contained OSS shape: (1) canonical title (drop the "(previously SimpleConfig)" tag); (2) **install** via `dotnet add package ExistForAll.SimpleSettings` (plus `.Binders` / `.Extensions.GenericHost` where relevant), replacing the legacy `Install-Package` PMC command; (3) a ~30-second **quickstart** with a CORRECT minimal example; (4) a short **feature overview**; (5) **links into `docs/`** for depth. Trim the long "why `IOptions<>` is bad" polemic to a tight blurb — keep the positioning, drop the wall of text.
- **D-02 (correctness, not just naming):** The README code example MUST reflect the **real API** — a settings **interface** (discovered via `ISettingsSection` base / `[SettingsSection]` / `Settings` suffix) with **`[SettingsProperty(DefaultValue = "…")]`**, NOT the stale `[DefaultValue("…")]` shown today (that attribute form is factually wrong). Verify every API token against current source before writing.
- **D-03:** Fix all consumer-facing surface in the README: the broken `existall/Shepherd` logo, every dead `existall/SimpleConfig/blob/master/docs/*` ToC link (repoint to `existall/SimpleSettings`), the install command, and prose typos.

**Guidance placement — concise in README, deep in docs/**
- **D-04:** The README carries a **concise** "Security notes" + "Breaking changes / migration" section (visible to every nuget.org reader); the **deep detail lives in `docs/`**. The mandated content (LOCKED by the Phase-4 security sign-off — `04-CONTEXT` D-06/D-12, Phase 4 `SECURITY.md`) that MUST be documented:
  - **Secret-redaction invariant** — conversion/bind failures never surface the bound value or chain a value-bearing inner (S1/SEC-01).
  - **Validator authors must not echo secrets** — neither in `ValidationError` message text NOR in **validator constructors** (DI resolution runs *outside* the value-free guard, so a ctor that logs an injected secret leaks it).
  - **DI-path `ValidateSimpleSettings()` is opt-in / deferred** — must be called *after* `BuildServiceProvider()`; attribute / `ValidatorType` validators run **inline** in the bind pipeline.
  - **validate ⇒ discoverable coupling** — `[SettingsSection(ValidatorType=…)]` (object-level validator) also makes the type scan-discovered.
  - **Spaced secrets bind via `AddCommandLine`** — a quoted value containing spaces arrives as its own token; document the `--key value` lookahead + `arg[0]` skip behavior (SRC-02).
  - **Phase-3 breaking-change list** — `SettingsHolder`/`ISettingsHolder` internal (API-01); `Core.AspNet` package dropped (PKG-01); per-TFM `Microsoft.Extensions.*` floor (PKG-02); public exception hierarchy (`SimpleSettingsException` base, EXC-01).

**docs/ — refresh all six in place**
- **D-05:** Update **all six** `docs/` files in place: purge `SimpleConfig` product naming + dead `existall/SimpleConfig` links (repoint to `existall/SimpleSettings`), **rename `docs/Extend Simple Config.md`** to a canonical name (e.g. `Extending SimpleSettings.md`) and fix inbound links, correct stale API references (e.g. `[DefaultValue]`), and add the deep security/behavior + migration content the README summarizes. Preserve existing page structure/history (in-place, not a restructure).

**Metadata — fix alongside docs**
- **D-06:** Fix `src/Directory.Build.props` `<Description>` typos ("appliaction" → "application"; the "decouples the frameworks from your appliaction" prose), canonicalize/replace the broken `existall/Shepherd` logo reference, and verify `<RepositoryUrl>`/`<PackageProjectUrl>` (already `existall/SimpleSettings`) + the three `ExistForAll.*` NuGet package links resolve.

### Claude's Discretion
- Exact README section ordering/wording and how far to trim the IOptions polemic; the precise canonical name for the renamed `Extend Simple Config.md`; whether "migration" is a README subsection vs a dedicated `docs/` page; which `docs/` page holds the deep security guidance; whether to add a `CHANGELOG.md`; whether to add a CI legacy-reference / dead-link check (nice-to-have — not required by the criteria).

### Deferred Ideas (OUT OF SCOPE)
- **AOT-01** — annotate reflection entry points (`[RequiresDynamicCode]`/`[RequiresUnreferencedCode]`) and/or document the AOT/trim limitation. **Deferred to a future v2.1 milestone.** Non-breaking to add later.
- **Validator-dispatch caching** — deferred perf follow-up (code-review M2). A code change, not docs; not Phase 5.
- **REL-01** — cut the first `v2.0.0-beta`. Phase 6.
- **EQ-01** (HELD), **PERF-03** (deferred), **`${ENV:-}` placeholder detection** (VAL-02/D-13, deferred) — out of scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| DOC-01 | Refresh README + docs/ to canonical `ExistForAll.SimpleSettings` naming and current repo/package links; document the Phase 1–4 security/behavior guidance (secret-redaction; validator secret-safety incl. constructors; opt-in/deferred DI validation; validate⇒discoverable coupling; spaced-secrets binding; Phase-3 breaking-change list) | Legacy-Reference Inventory gives every file:line to fix; Current Public API (Ground Truth) gives copy-pasteable correct tokens for examples; Phase 1–4 Behavior Facts gives the exact, source-cited security/behavior invariants to document; Validation Architecture gives falsifiable DOC-VERIFICATION gates that assert DOC-01 completeness. |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

No project `./CLAUDE.md` or `./.claude/CLAUDE.md` file exists (config references `./.claude/CLAUDE.md` but the file is absent). The governing directives therefore come from the org-level policy and user memory:

- **No emojis in code/comments.** README is the documented exception (emojis permitted in README prose if desired). Docs `.md` files are documentation — treat prose freely, but keep code fences emoji-free.
- **Meaningful names, minimal comments** in any code snippet the docs show.
- **Build/test from `src/`** — `dotnet` commands run from the `src/` directory (test stack: TUnit on Microsoft.Testing.Platform).
- **Settings interfaces in examples must be `public`** — the proxy generator emits a runtime impl of the interface and cannot implement an `internal` interface (random-GUID dynamic assembly). Every example settings interface must be `public`.
- **No Claude attribution** in commits/PRs (omit Co-Authored-By / Generated-with).
- **Push/PR via the `guy-lud` account** (SSH alias `github-guy-lud`); never commit to `master`.
- **Ship on a feature branch** off `gsd/phase-4-closeout` so the held Phase-4 closeout docs ride to master on Phase 5's PR (this phase changes package content via `PackageReadmeFile`, so it earns a real master alpha).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Consumer-facing narrative + quickstart | `README.md` (packaged via `PackageReadmeFile`) | `docs/` | README ships in every `.nupkg` and is the nuget.org + GitHub landing page; it is the first read. |
| Deep API / extension / security reference | `docs/*.md` | — | D-04 places concise guidance in README, depth in docs/. |
| Package identity + logo + description | `src/Directory.Build.props` | — | Shared MSBuild props inject `<Description>`, URLs, icon, README into every published package. |
| API ground truth (the source of every claim) | `src/Core/**` source files | — | The library source is the single authority for what the current public API is; docs must match it, not the reverse. |
| Security/behavior invariants to document | `.planning/phases/04-*/SECURITY.md` + Phase 3/4 CONTEXT | Core exception source | The security sign-off is authoritative for the exact guarantee wording; over-claiming in docs is a real risk. |

## Current Public API (Ground Truth)

> Every token below is a direct read of source. Use these EXACT spellings in examples. Do not trust the current README (it is wrong on `[DefaultValue]`).

### Settings-interface declaration (three mechanisms — pick one)

`[VERIFIED: src/Core/ExistForAll.SimpleSettings/ISettingsSection.cs:3]` `public interface ISettingsSection {}` — empty marker base interface (namespace `ExistForAll.SimpleSettings`).

`[VERIFIED: src/Core/ExistForAll.SimpleSettings/SettingsSectionAttribute.cs:4-17]` `[SettingsSection]` — `[AttributeUsage(AttributeTargets.Interface)]`, class `SettingsSectionAttribute`. Members: `string? Name { get; set; }`, `Type? ValidatorType { get; set; }`. Ctors: `SettingsSectionAttribute()` and `SettingsSectionAttribute(string name)`.

`Settings` name-suffix — an interface whose name ends in `Settings` is discovered (default suffix, configurable). `[VERIFIED: docs/Extend Simple Config.md:11]` (default `SettingsSuffix = "Settings"` in `SettingsOptions`).

**Interfaces must be `public`** (only exported interfaces are scanned) `[VERIFIED: docs/building_the_collection.md:12]` and the proxy generator cannot implement a non-public interface (memory: proxy internal-interface limit).

### `[SettingsProperty]` — exact members

`[VERIFIED: src/Core/ExistForAll.SimpleSettings/SettingsPropertyAttribute.cs:4-22]` — `[AttributeUsage(AttributeTargets.Property)]`, class `SettingsPropertyAttribute`:

| Member | Type | Default | Notes |
|--------|------|---------|-------|
| `DefaultValue` | `object?` | `null` | The correct form is `[SettingsProperty(DefaultValue = "…")]` — NOT `[DefaultValue("…")]`. |
| `Name` | `string?` | `null` | Overrides the binder lookup key (defaults to the property name). |
| `ConverterType` | `Type?` | `null` | Per-property `ISettingsTypeConverter`. |
| `AllowEmpty` | `bool` | `true` | When `false`, binding throws `SettingsPropertyNullException` if no value resolves. |
| `ValidatorType` | `Type?` | `null` | Per-property `ISettingValidation<T>`; declaring it makes the type discoverable. |

Ctors: `SettingsPropertyAttribute(string name)` and `SettingsPropertyAttribute()`.

### Direct API — `SettingsBuilder`

`[VERIFIED: src/Core/ExistForAll.SimpleSettings/SettingsBuilder.cs]` (namespace `ExistForAll.SimpleSettings`, `public class SettingsBuilder`):
- `static SettingsBuilder CreateBuilder(Action<ISettingsBuilderFactory> buildAction)` (`:44`)
- `static SettingsBuilder CreateBuilder()` (`:55`)
- `object GetSettings(Type settingsType)` (`:62`) — throws `SettingsTypeNotInterfaceException` for a non-interface.
- `ISettingsCollection ScanAssemblies(IEnumerable<Assembly> assemblies)` (`:74`)

Generic + params helpers `[VERIFIED: src/Core/ExistForAll.SimpleSettings/SettingsBuilderExtensions.cs]`:
- `ISettingsCollection ScanAssemblies(this SettingsBuilder, Assembly assembly, params Assembly[] assemblies)` (`:7`)
- `T GetSettings<T>(this SettingsBuilder) where T : class` (`:21`)

`ISettingsBuilderFactory` `[VERIFIED: src/Core/ExistForAll.SimpleSettings/ISettingsBuilderFactory.cs]`: `void AddSectionBinder(ISectionBinder)`, `SettingsOptions Options { get; }`.

Factory extension helpers `[VERIFIED: src/Core/ExistForAll.SimpleSettings/SettingsBuilderFactoryExtensions.cs]` (all generic `<T> where T : ISettingsBuilderFactory`, all return `target`): `AddTypeConverter(ISettingsTypeConverter)` (`:8`), `AddInMemoryCollection(InMemoryCollection)` (`:15`), `SetupOptions(Action<SettingsOptions>)` (`:23`), `SetSettingsSuffix(string)` (`:30`), `SetArraySplitDelimiter(string)` (`:36`), `SetAttributeType(Type)` (`:43`), `SetDateTimeFormat(string)` (`:50`), `SetInterfaceBase(Type)` (`:56`), `SetSectionNameFormatter(Func<Type,string>)` (`:62`).

### `ISettingsCollection`

`[VERIFIED: src/Core/ExistForAll.SimpleSettings/ISettingsCollection.cs]` — `public interface ISettingsCollection : IEnumerable<KeyValuePair<Type, object>>`:
- `object GetSettings(Type type)`
- `bool TryGetSettings(Type type, out object? settings)`

Generic helper `[VERIFIED: src/Core/ExistForAll.SimpleSettings/SettingsCollectionExtensions.cs:5]`: `T GetSettings<T>(this ISettingsCollection) where T : class`.

### DI entry points (`ExistForAll.SimpleSettings.Extensions.GenericHost`)

`[VERIFIED: src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs]` — `public static class ServicesSettingsBuilderExtensions`, all extend `IServiceCollection`:
- `AddSimpleSettings(this IServiceCollection)` (`:7`)
- `AddSimpleSettings(this IServiceCollection, Action<ISettingsBuilderOptions> action)` (`:13`)
- `AddSimpleSettings(this IServiceCollection, out ISettingsCollection settings, Action<ISettingsBuilderOptions>? action = null)` (`:21`) — the API-02 exposure overload.

Registration side-effects (`:46-53`): each scanned interface added as a singleton of its startup-built instance; plus singletons for `ISettingsCollection`, `ISettingsProvider` (a `SettingsProvider`), and `ISettingsValidationRunner` (a `SettingsValidationRunner`). No validators are invoked at registration time.

`ISettingsBuilderOptions` `[VERIFIED: .../ISettingsBuilderOptions.cs]`: `interface ISettingsBuilderOptions : ISettingsBuilderFactory { void AddAssemblies(IEnumerable<Assembly> assemblies); }`. Helper extensions `[VERIFIED: .../SettingsBuilderOptionsExtensions.cs]`: `AddAssembly(Assembly)`, `AddAssembly<T>()`, `AddAssemblies(Assembly, params Assembly[])`.

**Opt-in / deferred DI validation** `[VERIFIED: src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServiceProviderValidationExtensions.cs:20]`:
```
public static IServiceProvider ValidateSimpleSettings(this IServiceProvider provider)
```
- **Exact receiver:** extends `IServiceProvider` (NOT `IServiceCollection`, NOT `IHost`).
- **Namespace:** `ExistForAll.SimpleSettings.Extensions.GenericHost`, class `ServiceProviderValidationExtensions`.
- **Returns** the same `IServiceProvider` (chainable; returns `provider` at `:29`).
- Throws `InvalidOperationException` if `AddSimpleSettings` was never called (`:24-26`).
- Must be called **after** `BuildServiceProvider()`; the XML doc (`:12-19`) states it is opt-in and deferred, resolves validators from a **fresh scope** (scoped deps supported), and that attribute/`ValidatorType` validators run **inline during binding** and do NOT need this call.

`ISettingsProvider` `[VERIFIED: .../ISettingsProvider.cs]`: `object GetSettings(Type type)`; generic helper `T GetSettings<T>(this ISettingsProvider)` `[VERIFIED: .../SettingsProviderExtensions.cs:5]`.

### Validator authoring surface (`ExistForAll.SimpleSettings.Validations`)

`[VERIFIED: src/Core/ExistForAll.SimpleSettings/Validations/]`:
- `ISettingsValidator` (`ISettingsValidator.cs`): `ValidationResult Validate(ValidationContext context)`.
- `ISettingValidation<T> : ISettingsValidator` (`ISettingValidation.cs`): `ValidationResult Validate(ValidationContext<T> context)`. Authors implement ONLY the generic overload; a default interface method bridges the non-generic call (no reflection).
- `ValidationResult` (`ValidationResult.cs`): `IEnumerable<ValidationError> Errors`, `bool IsValid`, `void AddError(ValidationError)`.
- `ValidationError` (`ValidationError.cs`): ctor `ValidationError(string settingsName, string errorMessage)`; props `SettingsName`, `ErrorMessage`.
- `ValidationContext` (`ValidationContext.cs`): `object? Settings`. `ValidationContext<T>` (`ValidationContextOfT.cs`): `T? Settings`.

### Command-line binder (`ExistForAll.SimpleSettings.Binders`)

`[VERIFIED: src/Core/ExistForAll.SimpleSettings.Extensions.Binders/SettingsBuilderFactoryExtensions.cs]`:
- `AddCommandLine<T>(this T, Action<CommandLineSettingsBinderOptions>? action = null)` (`:40`) — sources `Environment.GetCommandLineArgs()` and sets `SkipFirstArgument = true` internally (`:43-49`) because `GetCommandLineArgs()[0]` is the exe path.
- `AddArguments<T>(this T, string[] args, Action<CommandLineSettingsBinderOptions>? action = null)` (`:54`) — binds exactly the array handed in (`SkipFirstArgument` default `false`).

`CommandLineSettingsBinderOptions` `[VERIFIED: .../CommandLineSettingsBinderOptions.cs]`: `ArgumentPrefixes` default `'-'`,`'/'`; `Delimiters` default `":"`,`"="`; `IsCaseSensitive = true`; `SkipFirstArgument = false`. Spaced-value behavior `[VERIFIED: CommandLineSettingsBinder.cs:32-63]`: for a prefixed key token with no inline delimiter, the parser looks ahead to the **next** token and binds it as the value **unless** that next token itself starts with a prefix char (then it is treated as a new key). So a quoted value containing spaces (`--Key "a b c"`) arrives from the shell as a single token and binds; but a *value* that begins with `-`/`/` will not bind via the space form — use the inline `--Key=value` delimiter form for those.

### Exception hierarchy (for the secret-redaction section)

`[VERIFIED: src/Core/ExistForAll.SimpleSettings/*.cs]`, all in namespace `ExistForAll.SimpleSettings`:
- `SimpleSettingsException` (`SimpleSettingsException.cs:8`) — `public abstract class ... : Exception`; the common catch-all base (EXC-01). Two forwarding ctors only; no `[Serializable]` (BinaryFormatter obsolete on net8/net10).
- `SettingsPropertyValueException` (`:13`) — conversion failure; carries `SettingsType`, `PropertyName`, `TargetType`, `ConversionErrorType` (the failure's `Type`, never the value or a chained inner).
- `SettingsPropertyNullException` (`:7`) — required-value-missing (`AllowEmpty = false`); carries `PropertyName` only (value-free by construction — there is no value).
- `SettingsValidationException` (`:10`) — aggregates `IReadOnlyList<ValidationError> Errors`; message composed only from author text. Static `ThrowIfAny(errors)` is the shared throw point for both validation paths.
- `SettingsValidatorInvocationException` (`:6`) — a validator threw; carries `ValidatorType` + `FailureType` only.
- (also present: `SettingsPropertyExtractionException`, `SettingsTypeNotInterfaceException`.)

### The three shipping packages

`[VERIFIED: src/Core/**/*.csproj]` — three packable projects (all `TargetFrameworks = net8.0;net10.0`); the benchmark is `IsPackable = false`; **no `Core.AspNet` project exists** (dropped in Phase 3, PKG-01):
- `ExistForAll.SimpleSettings` (Core)
- `ExistForAll.SimpleSettings.Binders` (project file `ExistForAll.SimpleSettings.Binders.csproj`; namespace `ExistForAll.SimpleSettings.Binders`)
- `ExistForAll.SimpleSettings.Extensions.GenericHost`

`Core.AspNet`-string hits exist only in **test fixtures** (`ASPNETCORE_ENVIRONMENT` env-var test, `UnitTests.Core.AspNet` test namespace) — NOT in consumer docs or shipping packages. No docs impact.

## Legacy-Reference Inventory (complete, every file:line DOC-01 must fix)

> Grepped README.md + docs/ + src/Directory.Build.props for `existall/SimpleConfig`, standalone `SimpleConfig`, `Install-Package`, `[DefaultValue]`, and the raw `Shepherd` logo URL.

### `README.md` — the primary stale artifact (rewrite per D-01/D-02/D-03)

| Line | Issue | Fix |
|------|-------|-----|
| 1 | `<img src="https://raw.githubusercontent.com/existall/Shepherd/master/art/logo.png">` — broken `existall/Shepherd` logo | Replace with canonical logo (local `icon.png` already packed, or a canonical raw URL) |
| 3 | Title `ExistForAll.SimpleSettings (previously SimpleConfig)` | Drop the "(previously SimpleConfig)" tag |
| 7 | `Install-Package ExistForAll.SimpleSettings` (legacy PMC) | `dotnet add package ExistForAll.SimpleSettings` |
| 11 | ToC link → `existall/SimpleConfig/blob/master/docs/getting_started.md` (dead) | repoint to `existall/SimpleSettings` |
| 12 | ToC link → `existall/SimpleConfig/.../building_the_collection.md` (dead) | repoint |
| 13 | ToC link → `existall/SimpleConfig/.../Build%20Config%20Interface.md` (dead) | repoint |
| 14 | ToC link → `existall/SimpleConfig/.../Default%20Values.md` (dead) | repoint |
| 15 | ToC link → `existall/SimpleConfig/.../Build%20a%20SectionBinder.md` (dead) | repoint |
| 16 | ToC link "Extending SimpleConfig" → `existall/SimpleConfig/.../Extend%20Simple%20Config.md` (dead + legacy filename) | repoint to `existall/SimpleSettings` + renamed file |
| 18 | Heading "Introduction - or why SimpleConfig was created" | rename product to SimpleSettings |
| 58 | "### TL;DR - or what SimpleConfig does?" | rename |
| 66 | `[DefaultValue("SomeUrl")]` — **factually wrong API** | `[SettingsProperty(DefaultValue = "SomeUrl")]` |
| 44, 60 | `IOption<EmailProviderConfiguratation>` — typo "Configuratation" (+ `IOption` should be `IOptions`) | fix in the trimmed polemic |
| 56 | broken URL `http://https://...` + typos "explenation", "SimpleInjecor" | fix or drop with the polemic trim |
| 53 | "scale-able" | prose cleanup (discretionary) |

The README has **no** Phase 1–4 security/behavior guidance yet — D-04 content is entirely additive.

### `docs/` — mostly already modernized; narrow residuals

| File | Legacy residual | Action |
|------|-----------------|--------|
| `docs/getting_started.md:4` | "SimpleSettings (previously SimpleConfig)" | drop the parenthetical |
| `docs/Extend Simple Config.md` | **filename** is legacy (content is already titled "Extend SimpleSettings", links already canonical) | **rename** file (e.g. `Extending SimpleSettings.md`) per D-05 |
| all six pages | already use `existall/SimpleSettings` links and correct `[SettingsProperty(DefaultValue=…)]` — **no dead `existall/SimpleConfig` links remain in docs/** | verify, then add the D-04 deep guidance |

**Inbound links to the renamed file (6 — must repoint on rename):**
- `README.md:16` (ToC)
- `docs/Build a SectionBinder.md:38` and `:60`
- `docs/Build Config Interface.md:39`
- `docs/building_the_collection.md:16` and `:45`

(All six currently point at `.../docs/Extend%20Simple%20Config.md`; five already use the `existall/SimpleSettings` host, README:16 still uses `existall/SimpleConfig`.)

### `src/Directory.Build.props` — metadata (D-06)

| Line | Issue | Fix |
|------|-------|-----|
| 16 | `<Description>` typo "appliaction" (appears in "from your appliaction"); prose "decouples the frameworks from your appliaction" | "application"; tidy prose |
| 21 | `<PackageTags>` ends with `..., Options, SimpleConfig` — legacy `SimpleConfig` tag | drop or replace with `SimpleSettings` |
| 17-18 | `<PackageProjectUrl>` / `<RepositoryUrl>` = `https://github.com/existall/SimpleSettings` | **already canonical** — verify only |
| 22, 29-30 | `<PackageIcon>icon.png</PackageIcon>` packs `../icon.png` (repo-root `icon.png` exists, 8320 bytes) | canonical local icon; ensure README logo aligns (README:1 uses the broken Shepherd URL, props uses local icon) |

**Note:** the broken `existall/Shepherd` logo lives in `README.md:1`, NOT in `Directory.Build.props` (props already uses the local `icon.png`). D-06's "canonicalize the logo" work is really a README fix; keep them consistent.

## Phase 1–4 Behavior Facts to Document (source-cited — do not paraphrase loosely)

> These are the D-04 mandated invariants. Wording is derived from Phase 4 `SECURITY.md` and the exception source. Documenting an over-strong guarantee is a real risk — keep the caveats.

### Secret-redaction invariant (S1 / SEC-01) `[CITED: 04-collection-validation-binding/SECURITY.md:24-27]`
No bound configuration value (which may be a secret) appears in any library exception's `ToString()` chain that reaches logs. Every library exception is **value-free** (Type/property metadata only) and never chains an inner exception that saw the value. Concretely `[VERIFIED: SettingsPropertyValueException.cs:5-22]`: conversion failures surface only settings type, property name, target type, and the CLR **Type** of the converter's failure — the ctor takes the failure's `Type`, not the `Exception`, so the guarantee is *structural*, not conventional.

### Validator authors must not echo secrets — incl. constructors `[CITED: SECURITY.md:75-78 (accepted residual)]`
The library's value-free guarantee does NOT extend to author-supplied text: `ValidationError.ErrorMessage` reaches `SettingsValidationException.ToString()` by design. If a validator author echoes a secret into their own error message, it surfaces. **Additionally**, DI-resolved validators are constructed by the container *outside* the value-free bind guard — so a validator **constructor** that logs/echoes an injected secret leaks it. Document both: (1) never put a bound value in `ValidationError` text; (2) never log secrets in a validator constructor.

### DI-path `ValidateSimpleSettings()` is opt-in / deferred `[VERIFIED: ServiceProviderValidationExtensions.cs:20; CITED: SECURITY.md:49 (T-04-06)]`
DI-registered `ISettingValidation<T>` validators cannot run during `AddSimpleSettings` (container not built yet). The host must call `provider.ValidateSimpleSettings()` explicitly **after** `BuildServiceProvider()`. Attribute/`ValidatorType` validators (`[SettingsSection(ValidatorType=…)]`, `[SettingsProperty(ValidatorType=…)]`) run **inline during binding** and need no such call. Registration adds only the runner singleton; it never invokes validators (proven: counter 0→1 test).

### validate ⇒ discoverable coupling `[VERIFIED: SettingsSectionAttribute.cs:8; CITED: 04-CONTEXT D-11]`
Declaring an object-level validator via `[SettingsSection(ValidatorType = typeof(...))]` also marks the type as scan-discovered (it carries `SettingsSectionAttribute`). So attaching a validator to a type is sufficient to make it a discovered settings section — a side effect authors should know.

### Spaced secrets bind via `AddCommandLine` (SRC-02) `[VERIFIED: CommandLineSettingsBinder.cs:32-63, CommandLineSettingsBinderOptions.cs:12-24]`
`AddCommandLine` sources `Environment.GetCommandLineArgs()` and skips `arg[0]` (the exe path) internally (`SkipFirstArgument = true`). For a prefixed key with no inline delimiter, the parser looks ahead to the next token and binds it as the value — **unless** that next token itself starts with a prefix char (`-`/`/`), in which case it is a new key. A shell-quoted value with spaces (`--Key "a b c"`) is one token and binds. `AddArguments(args)` binds the array as-is (`SkipFirstArgument` default `false`, because `Main(string[])` already excludes the exe).

### Phase-3 breaking-change list (for the migration section) `[CITED: REQUIREMENTS.md:55-58 + 03-CONTEXT]`
- **API-01:** `SettingsHolder` / `ISettingsHolder` made **internal** (breaking).
- **PKG-01:** `Core.AspNet` package **dropped** (it exposed no public type).
- **PKG-02:** `Microsoft.Extensions.*` dependency floor **per-TFM** (`8.0.x` on net8, current on net10).
- **EXC-01:** public **`abstract SimpleSettingsException`** base; boundary exceptions made public + structured; the bare `Exception` throw removed. `SettingsTypeNotInterfaceException` replaces the old `TypeIsNotInterface` throw.

## Standard Stack

This phase writes documentation and edits MSBuild metadata — it installs **no packages** and adds **no runtime code**. The only tools are the existing toolchain.

### Core (tools, not libraries)
| Tool | Version | Purpose | Why Standard |
|------|---------|---------|--------------|
| `dotnet` SDK | 10.0.301 (verified present) | `dotnet build` / `dotnet pack` sanity for the metadata + README-packaging change | Repo targets `net8.0;net10.0`; SDK already the build authority |
| `grep` / `ripgrep` | present (both) | Legacy-reference DOC-VERIFICATION gates | Mechanical, falsifiable, zero-dependency |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Hand-maintained ToC | a docs generator (DocFX) | Out of scope — D-05 says refresh in place, not restructure; not worth the tooling weight for six pages |
| Manual dead-link check | a markdown link-checker CI action | Discretionary (D-05 lists CI link-check as nice-to-have); the grep gate covers the criteria |

**Installation:** none. (No `npm install` / `dotnet add package` — documentation-only.)

## Package Legitimacy Audit

**Not applicable — this phase installs no external packages.** It edits documentation (`README.md`, `docs/*.md`) and MSBuild metadata (`src/Directory.Build.props`) only; no `<PackageReference>` is added or changed. No SLOP/SUS surface exists.

## Architecture Patterns

### Documentation Flow

```
                        src/Core/**  (library source — the SINGLE source of truth)
                             │  (verify every token against this, never the old README)
                             ▼
   ┌──────────────────────────────────────────────────────────┐
   │  Authoring pass                                            │
   │                                                            │
   │  README.md ──concise──►  Security notes + Migration        │
   │     │  links into                                          │
   │     ▼                                                      │
   │  docs/*.md ──deep──►    full security/behavior + migration │
   │     ▲                                                      │
   │  Directory.Build.props  (Description, URLs, icon, README)  │
   └──────────────────────────────────────────────────────────┘
                             │  dotnet pack
                             ▼
   README.md + icon.png + <Description>  ──►  .nupkg  ──►  nuget.org page
   docs/*.md  ──►  GitHub repo (existall/SimpleSettings)
```

Entry point for a consumer = the README (nuget.org + GitHub landing). README links resolve into `docs/` on `existall/SimpleSettings`. `Directory.Build.props` injects the packaged README + icon + description into all three packages.

### Pattern 1: Verify-against-source before writing any example
**What:** For every API token an example uses, confirm the exact spelling/signature in the cited source file before writing it.
**When to use:** Every code fence in README/docs.
**Example:** The current README shows `[DefaultValue("SomeUrl")]`; source proves the real form is `[SettingsProperty(DefaultValue = "SomeUrl")]` `[VERIFIED: SettingsPropertyAttribute.cs:6]`.

### Pattern 2: Concise-in-README, deep-in-docs (D-04)
**What:** README gets a short "Security notes" + "Breaking changes / migration" section; the full treatment lives in a docs/ page.
**When to use:** All the Phase 1–4 mandated guidance.

### Pattern 3: Rename-with-backlink-repoint
**What:** When renaming `docs/Extend Simple Config.md`, update all six inbound links in the same change.
**When to use:** The file rename in D-05. A DOC-VERIFICATION gate must assert zero links point at the old filename.

### Anti-Patterns to Avoid
- **Copying the old README's example forward** — it is factually wrong (`[DefaultValue]`). Rebuild from the verified API table.
- **Over-claiming the redaction guarantee** — it does NOT cover author-supplied `ValidationError` text or validator constructors. State the caveat.
- **Restructuring docs/** — D-05 says in-place refresh; preserve page structure/history.
- **Documenting `Core.AspNet` as available** — it was dropped (PKG-01).
- **Showing an `internal` example interface** — the proxy generator cannot implement it; examples must be `public`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Confirming the current API | Trust memory / the old README | Read the cited source file | The old README is already provably wrong; only source is authoritative |
| Legacy-reference completeness | Eyeballing the files | `grep`/`rg` gates (Validation Architecture) | Mechanical, falsifiable, repeatable |
| Package metadata plumbing | New props/targets | Existing `Directory.Build.props` | It already injects README+icon+description into all packages |

**Key insight:** In a docs-accuracy phase the "custom solution" trap is *paraphrasing from memory*. Every claim must trace to a source file:line or a Phase 4 sign-off line.

## Runtime State Inventory

> This is a documentation/metadata phase, not a rename/refactor of runtime code. The one rename is a **documentation file** (`docs/Extend Simple Config.md`), which has no runtime state — only inbound markdown links.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — no datastore keys/collections reference any doc name (verified: docs are static files) | none |
| Live service config | None — no external service embeds a doc filename (verified: repo is a library, no live services) | none |
| OS-registered state | None — no OS registration references docs (verified) | none |
| Secrets/env vars | None — the only `ASPNETCORE_ENVIRONMENT` hits are test fixtures, unrelated to docs (verified grep) | none |
| Build artifacts | `icon.png` (repo root) is packed into each `.nupkg` via `Directory.Build.props:30`; README is packed via `:29` — a README/metadata edit changes package **content** (rebuild/repack picks it up automatically, no stale artifact) | `dotnet pack` sanity check |
| Doc cross-links (the rename) | 6 inbound links to `Extend%20Simple%20Config.md` (README:16; Build a SectionBinder.md:38,60; Build Config Interface.md:39; building_the_collection.md:16,45) | repoint all six when the file is renamed |

## Common Pitfalls

### Pitfall 1: Trusting the current README example
**What goes wrong:** Copying `[DefaultValue("SomeUrl")]` forward — it does not compile against the real API.
**Why it happens:** The README predates the `[SettingsProperty]` API; it was never corrected.
**How to avoid:** Use `[SettingsProperty(DefaultValue = "…")]` from `SettingsPropertyAttribute.cs`. Add a DOC-VERIFICATION gate: `[DefaultValue(` returns zero hits in README+docs.
**Warning signs:** Any `[DefaultValue(` token; any `Install-Package`.

### Pitfall 2: Renaming the docs file but leaving dangling links
**What goes wrong:** `docs/Extend Simple Config.md` renamed, but the 6 inbound links 404.
**How to avoid:** Repoint all six in the same change; gate on zero references to the old filename.
**Warning signs:** grep for `Extend%20Simple%20Config` returns > 0 after the rename.

### Pitfall 3: `ValidateSimpleSettings()` documented on the wrong receiver
**What goes wrong:** Writing `services.ValidateSimpleSettings()` or `host.ValidateSimpleSettings()`.
**Why it happens:** It reads like a registration call.
**How to avoid:** It extends **`IServiceProvider`** and runs **after** `BuildServiceProvider()`. Verified at `ServiceProviderValidationExtensions.cs:20`.
**Warning signs:** the call shown before the provider is built.

### Pitfall 4: Over-stating the redaction guarantee
**What goes wrong:** Docs claim "secrets never appear in any exception," omitting the author-text and constructor carve-outs.
**How to avoid:** Always pair the invariant with the two caveats (validator message text + validator constructor). Sourced from `SECURITY.md:75-78`.

### Pitfall 5: Spaced-value command-line claim too broad
**What goes wrong:** Claiming any spaced value binds. A value beginning with `-`/`/` is treated as a new key.
**How to avoid:** State the lookahead rule precisely (`CommandLineSettingsBinder.cs:55-59`); recommend the inline `--Key=value` form for prefix-leading values.

## Code Examples

> Copy-pasteable, verified against source. Use these in the README quickstart / docs.

### Minimal quickstart (direct API) — the CORRECT replacement for the stale README example
```csharp
// Source: SettingsPropertyAttribute.cs, SettingsSectionAttribute.cs, SettingsBuilder.cs, SettingsBuilderExtensions.cs
[SettingsSection]
public interface IEmailSenderSettings
{
    [SettingsProperty(DefaultValue = "https://smtp.example.com")]
    string ServiceUrl { get; set; }

    [SettingsProperty(DefaultValue = 3)]
    int Retries { get; set; }
}

var settings = SettingsBuilder
    .CreateBuilder()
    .GetSettings<IEmailSenderSettings>();
```

### DI (generic host)
```csharp
// Source: ServicesSettingsBuilderExtensions.cs, SettingsBuilderOptionsExtensions.cs
services.AddSimpleSettings(o =>
{
    o.AddAssemblies(new[] { typeof(IEmailSenderSettings).Assembly });
});

// after building the provider — opt-in, deferred DI validation:
// Source: ServiceProviderValidationExtensions.cs:20
serviceProvider.ValidateSimpleSettings();
```

### Install (replaces `Install-Package`)
```bash
dotnet add package ExistForAll.SimpleSettings
dotnet add package ExistForAll.SimpleSettings.Binders
dotnet add package ExistForAll.SimpleSettings.Extensions.GenericHost
```

### A validator (secret-safe)
```csharp
// Source: Validations/ISettingValidation.cs, ValidationError.cs, ValidationResult.cs
public class EmailSettingsValidator : ISettingValidation<IEmailSenderSettings>
{
    public ValidationResult Validate(ValidationContext<IEmailSenderSettings> context)
    {
        var result = new ValidationResult();
        if (context.Settings!.Retries < 0)
            result.AddError(new ValidationError(nameof(IEmailSenderSettings.Retries), "Retries must be >= 0"));
        return result; // never put a bound value (possible secret) in the message
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `[DefaultValue("…")]` | `[SettingsProperty(DefaultValue = "…")]` | current API (pre-Phase-5) | README example must change |
| `Install-Package …` (PMC) | `dotnet add package …` | modern .NET tooling | README install must change |
| `SimpleConfig` product / `existall/SimpleConfig` repo | `ExistForAll.SimpleSettings` / `existall/SimpleSettings` | NAME-01 (shipped) | all naming + links |
| `Core.AspNet` package | dropped | Phase 3 (PKG-01) | do not document it |
| bare/ad-hoc exceptions | `abstract SimpleSettingsException` family | Phase 1 (EXC-01) | document the catch-all base |

**Deprecated/outdated:**
- The entire current `README.md` body (naming, install, example, links, logo) — stale.
- Any reference to `SimpleConfig` as a product name or repo.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | The `existall` GitHub org and the three `ExistForAll.*` package pages resolve on nuget.org | Metadata / D-06 | Docs link to a 404. Mitigation: the DOC-VERIFICATION link check + a manual/CI resolve; URLs in props are already canonical (`existall/SimpleSettings`). |
| A2 | Replacing the README logo with the packed local `icon.png` (or a canonical raw URL) is acceptable as "canonicalize the logo" | README / D-06 | Wrong logo choice. Low risk — discretion is granted (D-06 says "canonicalize/replace"); confirm the intended logo asset with the user if ambiguous. |

**Everything else in this research is `[VERIFIED]` against source or `[CITED]` from a Phase 3/4 sign-off — no user confirmation needed for the API facts, the legacy inventory, or the security invariants.**

## Open Questions

1. **Which docs/ page holds the deep security guidance?**
   - What we know: D-04 says depth lives in docs/; D-05 says refresh in place. `Extend Simple Config.md` (extension/behavior) or a new dedicated page are both viable.
   - What's unclear: page placement (explicitly Claude's discretion).
   - Recommendation: add a "Security & behavior" section to the renamed extend page, or a short new `docs/Security.md` if it grows; planner picks.
2. **README logo asset** (see A2).
   - Recommendation: default to the packed local `icon.png`; confirm with user if a hosted brand logo is preferred.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| `dotnet` SDK | `dotnet build`/`pack` metadata sanity | ✓ | 10.0.301 | — |
| `grep` | legacy-reference gates | ✓ | system | `rg` |
| `ripgrep` (`rg`) | legacy-reference gates | ✓ | present | `grep` |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none needed.

## Validation Architecture

> Nyquist is enabled. This is a docs phase — validation is framed as **DOC-VERIFICATION gates**, not unit tests. Each is a falsifiable command/assertion a plan-checker or executor can run. Run all from the repo root unless noted.

### Test "Framework"
| Property | Value |
|----------|-------|
| Framework | Shell grep/`rg` assertions + `dotnet build`/`pack` (no unit-test framework needed) |
| Config file | none — assertions are inline commands |
| Quick run command | the grep gates below (sub-second) |
| Full suite command | grep gates + `dotnet build -c Release` from `src/` + `dotnet pack -c Release` from `src/` |
| Phase gate | all grep gates return the expected count AND `dotnet pack` succeeds before `/gsd-verify-work` |

### Phase Requirements → DOC-VERIFICATION Map
| Req ID | Behavior | Check type | Automated command (expect the stated result) |
|--------|----------|-----------|-----------------------------------------------|
| DOC-01 | No legacy repo links | grep gate | `grep -rn "existall/SimpleConfig" README.md docs/ src/Directory.Build.props` → **0 hits** |
| DOC-01 | No standalone `SimpleConfig` product/tag references | grep gate | `grep -rniE "\bSimpleConfig\b" README.md docs/ src/Directory.Build.props` → **0 hits** |
| DOC-01 | No legacy PMC install command | grep gate | `grep -rn "Install-Package" README.md docs/` → **0 hits** |
| DOC-01 | No stale attribute form | grep gate | `grep -rn "\[DefaultValue" README.md docs/` → **0 hits** |
| DOC-01 | No broken Shepherd logo | grep gate | `grep -rn "existall/Shepherd" README.md src/Directory.Build.props` → **0 hits** |
| DOC-01 | Description typo fixed | grep gate | `grep -n "appliaction" src/Directory.Build.props` → **0 hits** |
| DOC-01 | Renamed doc file — no dangling links | grep gate | `grep -rn "Extend%20Simple%20Config" README.md docs/` → **0 hits** (after rename) AND the new file exists |
| DOC-01 | Canonical repo URL present | grep gate | `grep -rn "existall/SimpleSettings" README.md docs/ src/Directory.Build.props` → **≥ 1 hit per doc that links out** |
| DOC-01 | Three package IDs canonical + correct install verb | grep gate | README contains `dotnet add package ExistForAll.SimpleSettings`, `...Binders`, `...Extensions.GenericHost` |
| DOC-01 | Example uses real API tokens | token match | README/docs examples contain `[SettingsProperty(DefaultValue` and `SettingsBuilder.CreateBuilder` and `AddSimpleSettings`; `ValidateSimpleSettings()` shown on an `IServiceProvider` |
| DOC-01 | Mandated guidance present | presence check | README + docs together mention: secret-redaction, validator secret-safety (incl. constructor), opt-in/deferred `ValidateSimpleSettings`, validate⇒discoverable, `AddCommandLine` spaced values, and the Phase-3 breaking-change list (API-01/PKG-01/PKG-02/EXC-01) |
| DOC-01 | Internal doc links resolve | link check | every relative/`existall/SimpleSettings/blob/master/docs/<file>` link resolves to an existing `docs/` file |
| DOC-01 | Package content still builds/packs | build gate | from `src/`: `dotnet build -c Release` and `dotnet pack -c Release` succeed (README+icon+Description repack cleanly) |

### Sampling Rate
- **Per task commit:** the relevant grep gate(s) for the file just edited.
- **Per wave merge:** full grep-gate sweep.
- **Phase gate:** full grep sweep + `dotnet build`/`pack` from `src/` green.

### Wave 0 Gaps
- [ ] No test-file gaps — validation is grep/build-based, no fixtures needed.
- [ ] (Discretionary) A CI legacy-reference + dead-link check could wrap these gates — D-05 marks it nice-to-have, not required.

*If a plan wires the grep sweep as a script, place it under a scratch/CI path — do not add runtime code.*

## Security Domain

> `security_enforcement: true`, `security_asvs_level: 1`. This is a documentation phase — it introduces **no code, no new attack surface, no packages**. The security relevance is *documenting existing guarantees correctly*, not enforcing new controls.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | n/a — docs |
| V3 Session Management | no | n/a — docs |
| V4 Access Control | no | n/a — docs |
| V5 Input Validation | no (no runtime input) | n/a — docs; but the docs must correctly describe the library's own validation surface |
| V6 Cryptography | no | n/a |
| V7 Error Handling & Logging | **yes (documentation of an existing control)** | Accurately document the secret-redaction invariant (value-free exceptions) and its two caveats (validator message text + validator constructor). Do not over-claim. |

### Known Threat Patterns for this phase

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Docs over-claim the redaction guarantee → author relies on it and echoes a secret in a `ValidationError` | Information Disclosure | Document the caveat explicitly (SECURITY.md:75-78): author text and validator constructors are outside the value-free guard |
| Docs show a secret-leaking example (value in exception/log/validator message) | Information Disclosure | Security/code-review pass on finished docs: no example echoes a bound value; validator examples never log injected secrets |
| Docs advise the insecure `AddCommandLine` spaced-value shape for a secret that starts with `-`/`/` | Information Disclosure / correctness | Document the lookahead rule and recommend inline `--Key=value` for prefix-leading values |

**Recommended finished-docs review (per CONTEXT specifics):** a light `security-auditor`/`code-reviewer` pass confirming (1) no example echoes a secret, (2) all six mandated guidance items are present and correctly caveated.

## Sources

### Primary (HIGH confidence — direct source reads)
- `src/Core/ExistForAll.SimpleSettings/*.cs` — `SettingsBuilder`, `SettingsPropertyAttribute`, `SettingsSectionAttribute`, `ISettingsSection`, `ISettingsCollection`, exception hierarchy, `SettingsBuilder*Extensions`
- `src/Core/ExistForAll.SimpleSettings/Validations/*.cs` — validator authoring surface
- `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/*.cs` — `AddSimpleSettings`, `ValidateSimpleSettings`, `ISettingsProvider`
- `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/*.cs` — `AddCommandLine`/`AddArguments`, `CommandLineSettingsBinder(Options)`
- `src/Directory.Build.props`, three `*.csproj` files — package identity + metadata
- `README.md`, all six `docs/*.md` — legacy-reference inventory (grepped)
- `.planning/phases/04-collection-validation-binding/SECURITY.md` — secret-redaction invariant + residuals

### Secondary (MEDIUM confidence)
- `.planning/REQUIREMENTS.md`, `03-CONTEXT.md`, `04-CONTEXT.md` — Phase-3 breaking-change list, D-06/D-11/D-12

### Tertiary (LOW confidence)
- none — no web/training claims in this research

## Metadata

**Confidence breakdown:**
- Current public API: HIGH — every token read from the cited source file:line this session.
- Legacy-reference inventory: HIGH — exhaustive grep across README + docs/ + props.
- Security/behavior invariants: HIGH — cited from Phase 4 `SECURITY.md` + exception source.
- Validation gates: HIGH — commands were run this session and returned the documented counts.
- Metadata (URL resolution on nuget.org): MEDIUM — URLs are canonical in-repo; external resolution is A1.

**Research date:** 2026-07-20
**Valid until:** ~2026-08-19 (30 days — stable; re-verify API tokens if Core source changes before planning)
