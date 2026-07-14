# Phase 2: Binding Correctness & Engine Test Hardening — Context

**Gathered:** 2026-07-14
**Status:** Ready for planning (partial — COLL-01 deferred pending the C1 decision)
**Source:** Synthesized from `FIX-PLAN.md` (frozen) + `REQUIREMENTS.md`, reconciled to current git (post-#29)

<domain>
## Phase Boundary

Harden the binding engine's test coverage and lock its correctness contracts. The engine already
ships; this phase adds the missing unit tests around **value population / binder precedence**
(TEST-01), **type conversion** (TEST-02), and **converter residuals** (TEST-03), and makes the
one open collection-support decision (COLL-01).

**Already delivered (verify-only):** ENG-01/T7 (generator concurrency race) shipped pre-GSD via
#29 — do NOT re-implement.

**Deferred this phase:** COLL-01/C1 (the `List<T>`/`IList<T>`/`ICollection<T>` broaden-vs-throw
decision) is deferred by the owner; it and its `List<T>` positive/doc test are excluded from these
plans.

Not in scope: any new binding features, the held `Validations`/`EqualityCompererCreator` code (D1/D2).
</domain>

<decisions>
## Implementation Decisions

### ENG-01 / T7 — DONE (verify-only)
- Merged #29 (double-checked locking; one `_generationGate` over all generation; warm cache-hit
  path lock-free). Do NOT re-implement. If a plan references ENG-01 it is **verify-only**: confirm
  the fix in `SettingsClassGenerator.cs` and the 2 stress tests in `SettingsClassGeneratorTests.cs`
  satisfy success criterion #4 (concurrent same-interface generation → one `ReferenceEquals` impl,
  no duplicate-`DefineType`).

### TEST-01 / T4 — `ValuesPopulator` tests
- New file: `src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/ValuesPopulatorTests.cs` (uses the
  internal ctor + fake binders/converters).
- Cases: binder-throws ⇒ `SettingsBindingException`; a later binder overrides an earlier one
  (last-writer-wins precedence); no binder sets a value ⇒ the `[SettingsProperty]` attribute default
  survives; conversion-throws ⇒ `SettingsPropertyValueException`.
- **⚠ CONTRACT UPDATE (post-S1/C2) — FIX-PLAN's T4 wording is STALE.** FIX-PLAN §T4 says the value
  exception "carries type/value/property" with "inner preserved." That is no longer true.
  `SettingsPropertyValueException` now takes the failure **`Type`**, carries **no bound value**, and
  **chains no inner** (`InnerException == null`). Assert the *current* leak-safe contract: property
  name + target type + failure-type name present; the bound value ABSENT from the whole
  `ex.ToString()` chain. The value-free "required missing" path is `SettingsPropertyNullException`.
  `SettingsBindingException` chains the binder's inner but stores only primitives
  (`BinderType`/`Section`/`Key`), never the `BindingContext`.

### TEST-02 / T5 — `TypeConverter` tests
- New file: `src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs`.
- Cases: `null` → value-type default; `null` → `IEnumerable<int>` empty (not null); `Nullable<int>`
  strip-and-convert; `AllowEmpty = false` + no value ⇒ throw (`SettingsPropertyNullException`);
  attribute `ConverterType` bypasses the collection converter.

### TEST-03 / T6 — Converter residual (Uri/DateTime)
- P4 (#25) already added `Conversion/CollectionConversionTests.cs` (array/enumerable incl.
  int/string/enum/DateTime/Uri elements). **Scope T6 to the RESIDUAL only** — verify existing
  coverage first, then add `Uri`/`DateTime` scalar-conversion + edge tests where genuinely missing.
  Do NOT duplicate P4's coverage.
- The `List<int>` positive/doc test is **DEFERRED with COLL-01** — not in this phase.

### COLL-01 / C1 — DEFERRED (open decision, do not plan)
- The broaden-vs-document+throw decision for `List<T>`/`IList<T>`/`ICollection<T>` is deferred by the
  owner. Do NOT plan the implementation or the `List<T>` positive/doc test in this phase.
- Background for when it's decided: `TypeExtensions.IsEnumerable` (`TypeExtensions.cs:23-28`) matches
  only exact `IEnumerable<>`; other shapes fall through to `DefaultTypeConverter` and throw.
  `CollectionTypeConverter` already materializes a `T[]`; a `List<T>` satisfies
  `IList<T>`/`ICollection<T>`/`IReadOnlyList<T>`/`IReadOnlyCollection<T>` in one shot.
- This is an explicit gap — the requirements-coverage gate will (correctly) flag COLL-01 as
  intentionally deferred, not silently dropped.

### Claude's Discretion
- Test file layout, fixture interface shapes, `[Arguments]`/`[NotInParallel]` usage per existing TUnit
  conventions; whether ENG-01 verify-only warrants its own thin plan or a note inside another plan.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Work-item detail (frozen historical — mine, do not update)
- `FIX-PLAN.md` — §T4/T5/T6/C1 per-item detail. ⚠ Test specs predate S1/C2; trust the current
  source + the contract note above over FIX-PLAN's exception wording.

### Source under test
- `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs` — TEST-01 target (binder precedence,
  bind/convert exception wrapping)
- `src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypeConverter.cs` — TEST-02 target (convert orchestration)
- `src/Core/ExistForAll.SimpleSettings/Conversion/` — `PropertyConversion.cs` (null/AllowEmpty),
  `CollectionTypeConverter.cs`, `Enumerable/Array/Enum/DateTime/Uri/Default` converters — TEST-02/03
- `src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypeExtensions.cs:23-28` — `IsEnumerable` (C1 pivot; deferred)
- Exception contract (post-S1/C2): `SettingsPropertyValueException`, `SettingsPropertyNullException`,
  `SettingsBindingException`, `SimpleSettingsException`

### Test conventions + existing coverage (dedupe against these)
- `.planning/codebase/TESTING.md` — TUnit conventions
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsClassGeneratorTests.cs` — ENG-01 tests (done); pattern reference
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/CollectionConversionTests.cs` — P4 collection coverage (T6 overlap)
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/DefaultTypeConverterTests.cs` — existing converter test (T1 culture)
</canonical_refs>

<specifics>
## Specific Ideas
- Build/test from `src/` (TUnit on Microsoft.Testing.Platform via `global.json`). net10 runtime only
  locally → net8 build-only; CI runs both. Run one project: `dotnet test <proj> --framework net10.0 --no-build` (build first).
- Match TUnit conventions: `[Test] async Task`, `[Arguments(...)]`, `[NotInParallel("…")]` for
  process-global state (e.g. `CultureInfo.CurrentCulture`), `await Assert.That(...).IsEqualTo/.Throws<T>()`,
  nested `public interface` fixtures per class, build via `SettingsBuilder.CreateBuilder(x => …)`.
- New test files under `src/Tests/ExistForAll.SimpleSettings.UnitTests/`.
</specifics>

<deferred>
## Deferred Ideas
- **COLL-01 / C1** — `List<T>`/`IList<T>`/`ICollection<T>` support decision (broaden vs document+throw)
  and its `List<T>` positive/doc test. Deferred by owner; excluded from this phase's plans.
- **D1 Validations**, **D2 EqualityCompererCreator** — HELD; out of scope.
</deferred>

---
*Phase: 02-binding-correctness-engine-test-hardening*
*Context synthesized 2026-07-14 from FIX-PLAN.md (frozen) — GSD is now the source of truth*
