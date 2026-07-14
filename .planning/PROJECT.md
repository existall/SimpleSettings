# ExistForAll.SimpleSettings

## What This Is

A framework-independent .NET library that binds configuration into strongly-typed
settings **interfaces** the consumer defines (e.g. `IEmailServiceConfig`), rather than
coupling application code to `IOptions<>` or concrete option classes. It scans assemblies
for settings interfaces, emits concrete implementations at runtime via `Reflection.Emit`,
and populates them through an ordered chain of pluggable value sources (configuration,
environment variables, command line, in-memory) with a type-converter pipeline. For .NET
library authors and application developers who want interface-first, DI-friendly
configuration on modern .NET.

## Core Value

**Correctness of binding.** Config → strongly-typed settings must map accurately across
every supported shape — sections, arrays/enumerables, defaults, nullable, and custom
converters. When tradeoffs arise, binding correctness wins.

## Requirements

### Validated

<!-- Shipped and relied upon in the current codebase (brownfield baseline). -->

- [x] **BIND-01**: Consumer declares settings as interfaces (via `ISettingsSection` base, `[SettingsSection]` attribute, or `Settings` name suffix); the library emits a concrete impl at runtime
- [x] **BIND-02**: `SettingsBuilder.CreateBuilder` / `ScanAssemblies` map interface types to generated implementations (only exported interfaces scanned)
- [x] **BIND-03**: Values resolve through an ordered `ISectionBinder` chain (last-writer-wins), falling back to the property default when no binder sets a value
- [x] **BIND-04**: `[SettingsProperty]` options (`DefaultValue`, `Name`, `ConverterType`, `AllowEmpty`) control per-property binding
- [x] **CONV-01**: Values convert to the target type through an ordered `ISettingsTypeConverter` chain (user converters win; `EnumTypeConverter` before the catch-all; `DefaultTypeConverter` last; `InvariantCulture`)
- [x] **CONV-02**: Sections, defaults, enums, `DateTime`, `Uri`, arrays and `IEnumerable<T>` bind correctly
- [x] **CONV-03**: Array/enumerable conversion unified and de-reflected in `CollectionTypeConverter` (P4)
- [x] **SRC-01**: In-memory, `IConfiguration`, environment-variable, and command-line binders contribute values
- [x] **HOST-01**: `AddSimpleSettings(...)` registers each scanned interface as a DI singleton plus an `ISettingsProvider`
- [x] **HOST-02**: `ISettingsProvider` caches the built instance per type (C3 option 2) so DI resolution and `provider.GetSettings<T>()` return consistent objects
- [x] **PERF-01**: Per-type caching of generated types, extracted properties, and settings plans keeps warm resolves cheap (P1–P5)
- [x] **PERF-02**: BenchmarkDotNet CI gates PRs on allocated bytes (deterministic), not wall-clock time
- [x] **NAME-01**: Canonical `ExistForAll.SimpleSettings` naming across namespace and package identity (A2/D3); legacy `SimpleConfig` retired
- [x] **SEC-01**: Conversion-failure exceptions never leak the bound value or chain a value-bearing inner; only property name, target type, and failure type name surface; value-free "required missing" is `SettingsPropertyNullException` (S1, merged #27; structural via C2)
- [x] **SEC-02**: Sibling exception wrappers (`SettingsBindingException`, `SettingsExtractionException`, `TypeGenerationException`) audited — none embed bound values (S1/C2, #27/#28)
- [x] **EXC-01**: Public `abstract SimpleSettingsException` base in the root namespace; boundary exceptions public + structured (property/target/failure/binder/section/key); `SettingsTypeNotInterfaceException` replaces the `TypeIsNotInterface` throws (C2, merged #28, breaking)

### Active

<!-- Remaining open work from FIX-PLAN.md, batched toward the first v2.0.0-beta. -->

- [ ] **COLL-01** (C1): Decide + implement `List<T>`/`IList<T>`/`ICollection<T>` support (broaden converter) or document + throw a clear error, with a positive test
- [ ] **TEST-01** (T4): `ValuesPopulator` tests — binder precedence + bind/convert exception-wrapper contracts
- [ ] **TEST-02** (T5): `TypeConverter` tests — null/nullable/empty-enumerable/`AllowEmpty`/attribute-`ConverterType` paths
- [ ] **TEST-03** (T6): Converter tests residual — `Uri`/`DateTime` + the `List<T>` doc test tied to C1
- [x] **ENG-01** (T7): Fix the unsynchronized check-then-`DefineType` race in `SettingsClassGenerator` + concurrency stress tests *(merged #29 — double-checked locking; one gate over all generation)*
- [ ] **API-01** (A5): Make `SettingsHolder`/`ISettingsHolder` internal *(breaking)*
- [ ] **PKG-01** (A3): `Core.AspNet` exposes a public type (`Environments` public) or the package is dropped
- [ ] **PKG-02** (A4): Float `Microsoft.Extensions.*` floor per-TFM (`8.0.x` for net8) or justify the pin
- [ ] **SRC-02** (A6): Command-line binder parses quoted values with spaces correctly and skips `arg[0]`
- [ ] **AOT-01** (A1): Annotate reflection entry points (`[RequiresDynamicCode]`/`[RequiresUnreferencedCode]`) and/or document the AOT/trim limitation before stable
- [ ] **DOC-01**: Refresh README to canonical `ExistForAll.SimpleSettings` naming and current repo/package links
- [ ] **REL-01**: Cut the first `v2.0.0-beta` with all batched breaking changes; consistent package identity; suite green on net8 + net10

### Out of Scope

- **Classic .NET Framework support** — dropped; modern .NET only (`net8.0;net10.0`)
- **`IOptionsMonitor`-style reload of built instances** — deferred (C3 "option 3"); instances are snapshots by design
- **Opt-in "full diagnostics" exception knob** (restoring value/inner exception) — rejected as insecure-by-configuration (S1)
- **Full Roslyn source generator replacing `Reflection.Emit`** — future consideration only (may surface as an A1 option), not committed for this milestone

## Context

- **Brownfield.** The library already ships the full binding engine; this milestone is a
  hardening + pre-stable cleanup pass, not greenfield feature work. Codebase map lives in
  `.planning/codebase/`.
- **Pre-stable window.** No stable `v*` tag exists — only auto-alphas (`2.0.0-alpha.*`).
  Breaking changes are free right now and are deliberately batched before the first
  `v2.0.0-beta`.
- **Recently completed & merged:** performance track P0–P5, quick wins Q1–Q5, naming
  consolidation (A2/D3), the provider-cache decision (C3/P1), the allocation-gated
  benchmark harness, S1 secret redaction (#27), the public exception hierarchy (C2, #28),
  and the generator concurrency-race fix (ENG-01/T7, #29).
- **Test baseline:** 82 tests on net10 (incl. +5 S1 redaction, +6 C2 hierarchy). TUnit on
  Microsoft.Testing.Platform; run from `src/`. net8 is build-only locally (net10 runtime
  installed); CI runs both.
- **Known open concerns (source-verified):** missing engine tests (T4/T5),
  `IEnumerable<T>`-only collection support (C1), no AOT/trim annotations (A1), and the
  command-line quoted-value bug (A6). *(C2 public exception base — resolved #28; T7 generator race — resolved #29.)*

## Constraints

- **Security**: Conversion-failure exceptions must never carry the bound value or chain a value-bearing inner — only property name, target type, and failure type name. The value-free "required missing" case uses `SettingsPropertyNullException`. No opt-in flag to restore value/inner. — secrets land in host logs via `Exception.ToString()`.
- **Performance**: New/changed hot paths must add a gated benchmark; CI fails PRs on >10% allocated-byte regressions. The `gh-pages` baseline (`dev/bench/`) must not be deleted. — deterministic regression gating.
- **Tech stack**: Multi-target `net8.0;net10.0` only. `LangVersion` unset (net8→C#12, net10→C#14); no net8-breaking syntax. Nullable + ImplicitUsings enabled; block-scoped namespaces; central package management (add `PackageVersion`, never pin on `PackageReference`). — modern .NET only.
- **Protocol (versioning/release)**: MinVer, tag prefix `v`, baseline `2.0.0`, keyless NuGet Trusted Publishing. Every push to `master` publishes an alpha, so all changes go through PRs; never commit doc-only changes to `master`. — release automation.
- **API contract**: Converter chain order matters (user first, `DefaultTypeConverter` last, `EnumTypeConverter` before it); binder order = precedence (last `SetNewValue` wins). — binding correctness depends on ordering.
- **Compatibility**: `Microsoft.Extensions.*` currently pinned `10.0.9` across both TFMs, forcing net8 consumers onto 10.x (A4 target). — consumer dependency hygiene.

## Key Decisions

<!-- Settled decisions (from ingest intel + shipped work). Settled, not immutably locked. -->

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Secret-safe exception invariant: `SettingsPropertyValueException` carries no value and chains no inner; value-free "required missing" is a separate `SettingsPropertyNullException`; opt-in restore rejected | Bound values (secrets) reach logs via `Exception.ToString()`; a config flag to restore them is insecure-by-configuration | ✓ Good (merged #27; made structural by C2 #28) |
| Public exception hierarchy: all library exceptions derive from `public abstract SimpleSettingsException` in the root namespace (reflection invariant test enforces it); `SettingsTypeNotInterfaceException` replaces the `TypeIsNotInterface` `InvalidOperationException` throws | Consumers need one catchable category; the two unreachable "no converter" guards stay outside the family | ✓ Good (C2 #28; one runtime break → release notes) |
| Provider caches the built instance per type (C3 option 2); Core `GetSettings` unchanged; no reload path | Consistent objects between DI singletons and `provider.GetSettings<T>()`; `IOptionsMonitor`-style reload deferred as future "option 3" | ✓ Good (P1, #17) |
| Generator serializes ALL type generation behind one gate (double-checked locking; warm path lock-free); NOT `Lazy`-per-type | `Reflection.Emit` isn't thread-safe — concurrent `DefineType` of distinct interfaces also races the shared `ModuleBuilder`; per-type Lazy would reopen that race | ✓ Good (T7 #29; same/distinct-interface stress tests) |
| Benchmark CI gates on allocated bytes, not wall-clock time | Allocations are deterministic; time is noisy/informational | ✓ Good (merged #22) |
| Canonical naming `ExistForAll.SimpleSettings`; package renamed from legacy `SimpleConfig` | Consolidate three historical spellings onto one identity before stable | ✓ Good (A2 #15, D3 #11) |
| Keep the generated impl type name separate from `GetNormalizeInterfaceName` (section name) | The two serve different purposes (collision-safe impl name vs. config section name); merging would break section resolution | ✓ Good (M1 #21) |
| Breaking changes are free until the first `v2.0.0-beta`; batch them before cutting it | Only auto-alphas exist; no stable consumers yet, so this is the last cheap window for reparenting/removal | — Pending (this milestone) |
| `Validations/*` (D1) and `EqualityCompererCreator` (D2) are HELD — do NOT delete | Dead today but reserved for coming feature work; D1 reconciles with the `validate-settings` branch | — Pending (owner-driven feature) |

---
*Last updated: 2026-07-14 — ENG-01/T7 shipped (#29). GSD is now the source of truth; FIX-PLAN.md frozen as a historical reference. Reconciled from session handoff + git.*
