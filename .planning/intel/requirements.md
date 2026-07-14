# Requirements (Intel)

_Synthesized by gsd-doc-synthesizer._

## PRD-type requirements

**None ingested.** No PRD-type documents were present in the ingest set.

## Candidate requirements derived from the authoritative SPEC

No PRDs exist, but the authoritative planning SPEC (`FIX-PLAN.md`, precedence 0) enumerates the
**remaining open work** as self-contained, ordered items. These are surfaced here as candidate
requirements so `gsd-roadmapper` has concrete fodder. Provenance is the SPEC, not a PRD — the
roadmapper should treat acceptance criteria as SPEC-derived, not user-ratified.

Completed items (Phase 1 bugfixes B1/B2/B4/B5/B9, Phase 5 perf P0–P5, quick wins Q1–Q5, A2/D3
naming, C3 decision, S1 redaction) are recorded in context.md / constraints.md as delivered
capabilities and are NOT repeated here.

### REQ-secret-redaction — Redact secret values from exception messages (S1)
- description: Conversion-failure exceptions must not leak bound values or chain value-bearing
  framework inner exceptions; surface only property name, target type, and failure type name.
- acceptance: `SettingsPropertyValueException` carries no value and chains no inner; value-free
  "required missing" split into `SettingsPropertyNullException`; sentinel-secret bound to
  int/enum/DateTime/Uri/custom converter is absent from the whole `ex.ToString()` chain.
- status: SHIPPED / in-flight (branch `security/s1-redact-exception-value`, PR open); +5 tests
- scope: exception hierarchy, secret redaction, ValuesPopulator
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (§S1)

### REQ-public-exception-base — Public SimpleSettingsException base + structured data (C2)
- description: Add `public abstract class SimpleSettingsException : Exception`; reparent all library
  exceptions; make boundary-crossing ones public; expose context (binder/section/key/type) as
  properties; replace the bare `Exception` at `TypeConverter.cs:62`.
- acceptance: `catch (SimpleSettingsException)` succeeds; standard ctor set present; suite green.
  Pairs naturally with S1's new types (give them PropertyName/TargetType/FailureType props).
- status: open · Break · Sev Med · Eff M
- scope: exception hierarchy
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (C2)

### REQ-list-collection-support — List<T>/IList<T>/ICollection<T> support decision (C1)
- description: `TypeExtensions.IsEnumerable` matches only exact `IEnumerable<>`; `List<T>`,
  `IList<T>`, `ICollection<T>` fall through and throw. Decide: (a) document + throw clear error, or
  (b) broaden `EnumerableTypeConverter.CanConvert` to handle assignable collection types.
- acceptance: T6 doc/positive test covering `List<int>`; coordinate with the P4 CollectionTypeConverter.
- status: open · Sev Med · Eff M
- scope: type converters, collections
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (C1)

### REQ-aot-trim-strategy — AOT / trimming story (A1)
- description: The `Reflection.Emit` engine has zero `[RequiresDynamicCode]`/
  `[RequiresUnreferencedCode]` annotations and silently breaks under Native AOT / trimming. Decide:
  annotate public entry points and/or plan a source generator; at minimum document the limitation
  in the README before stable. Consider `AssemblyBuilderAccess.Run` vs `RunAndCollect`.
- acceptance: consumer-honest warnings on public entry points and/or documented limitation.
- status: open · Sev High · Eff M–L
- scope: SettingsClassGenerator, AOT/trim strategy
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (A1)

### REQ-aspnet-public-type — Core.AspNet exports a public type or is dropped (A3)
- description: `ExistForAll.SimpleSettings.Core.AspNet` ships no consumable public type
  (`Environments` is internal). Make `Environments` public, or drop the package.
- acceptance: package exposes a public type, or is removed from the solution.
- status: open · Sev Med · Eff S
- scope: Core.AspNet package
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (A3)

### REQ-dependency-floor-per-tfm — Float Microsoft.Extensions.* floor per-TFM (A4)
- description: `Directory.Packages.props` pins `Microsoft.Extensions.*` to `10.0.9` for both TFMs,
  forcing net8 consumers to 10.x assemblies. Float the floor to the lowest supported (`8.0.x`) via
  per-TFM `PackageVersion`, or justify the pin.
- acceptance: net8 consumers no longer forced to 10.x, or documented justification.
- status: open · Sev Med · Eff S
- scope: dependency management, packaging
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (A4)

### REQ-hide-settingsholder — Make SettingsHolder/ISettingsHolder internal (A5)
- description: `SettingsHolder`/`ISettingsHolder` are public but never appear in a public signature.
  Make them internal.
- acceptance: types internal; builds; suite green.
- status: open · Break · Sev Low · Eff S
- scope: public surface cleanup
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (A5)

### REQ-commandline-quoted-parsing — Fix command-line quoted-value parsing (A6)
- description: The command-line binder splits `Environment.CommandLine.Trim().Split(' ')`, breaking
  quoted values with spaces and including the executable path. Parse args properly (respect quotes;
  skip arg[0]).
- acceptance: quoted values with spaces bind correctly; arg[0] (exe path) skipped.
- status: open · Sev Med · Eff M
- scope: command-line binder
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (A6)

### REQ-valuespopulator-tests — ValuesPopulator unit tests (T4)
- description: binder-throws => `SettingsBindingException` (inner preserved); conversion-throws =>
  `SettingsPropertyValueException` (carries type/property, per S1 invariant no value); later binder
  overrides earlier; no binder => attribute default survives.
- status: open
- scope: tests, ValuesPopulator
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (T4)

### REQ-typeconverter-tests — TypeConverter unit tests (T5)
- description: null=>value-type default; null=>`IEnumerable<int>` empty (not null);
  `Nullable<int>` strip+convert; allow-empty-false throw; attribute `ConverterType` bypasses the collection.
- status: open
- scope: tests, TypeConverter
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (T5)

### REQ-converter-tests — Converter unit tests (T6, partially done)
- description: array/enumerable/Uri/DateTime converters + `List<T>` doc test (see C1). Largely
  covered by P4+P5 parity tests; residual = `List<T>` doc test tied to the C1 decision.
- status: mostly done (P4/P5); residual open
- scope: tests, converters
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (T6)

### REQ-generator-concurrency-tests — SettingsClassGenerator concurrency stress (T7)
- description: same interface twice => `ReferenceEquals`; extraction-fails => `TypeGenerationException`;
  `Parallel.For` stress on `GenerateType`. The unsynchronized check-then-`DefineType` race
  (`SettingsClassGenerator.cs:37-44`) is still open (Q4's ConcurrentDictionary did not close it).
- status: open (caching covered; concurrency race open)
- scope: tests, SettingsClassGenerator
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (T7)

### REQ-validations-feature — Validations API decision (D1, HELD)
- description: HELD — do NOT delete. Public `Validations/*` + `SettingsPropertyAttribute.ValidatorType`
  are dead but reserved for a validation feature; reconcile with the `validate-settings` branch.
- status: held (owner-driven feature)
- scope: Validations API
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (D1)

### REQ-equalitycomparer-decision — EqualityCompererCreator decision (D2, HELD)
- description: HELD — internal, dead, latent invalid-IL bug (`EqualityCompererCreator.cs:38`).
  Delete or fix+wire (value-equality on generated types).
- status: held
- scope: SettingsClassGenerator / Reflection.Emit
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (D2)

### REQ-readme-link-refresh — Refresh stale README links (handoff #5)
- description: README still links to legacy `existall/SimpleConfig` GitHub paths and uses the old
  "SimpleConfig" name in prose. Update to `ExistForAll.SimpleSettings` / current repo paths. (The
  `docs/` tutorials were already refreshed in #20.) See auto-resolved INFO in INGEST-CONFLICTS.md.
- status: open
- scope: documentation
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (Next priorities #5)
