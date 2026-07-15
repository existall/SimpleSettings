---
phase: 04-collection-validation-binding
plan: 03
subsystem: validation
tags: [validation, reflection, attributes, exception-redaction, dotnet, tunit, zero-allocation]

# Dependency graph
requires:
  - phase: 04-collection-validation-binding
    provides: "Plan 01 collection/List binding so validators inspect correctly-bound collection properties"
provides:
  - "Sync ISettingsValidator/ISettingValidation<T>.Validate contracts (Task<> dropped) + constructed ValidationContext/ValidationContext<T>"
  - "[SettingsValidator(typeof(X))] object-level attribute (AttributeTargets.Interface)"
  - "SettingsValidationException : SimpleSettingsException carrying IReadOnlyList<ValidationError> Errors, value-free message"
  - "SettingsValidationException.ThrowIfAny(IReadOnlyList<ValidationError>) shared aggregate-and-throw helper (reused by Plan 04 DI runner)"
  - "Core-path validation execution in ValuesPopulator: object + property validators run post-populate, aggregated into one exception"
  - "SettingsPlan.HasValidators/ObjectValidatorType + PropertyPlan.ValidatorType resolved once at plan build"
affects: [04-04, validation, DI-validator-path, API-02]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "HasValidators short-circuit: single field read guards the post-populate hook before any allocation (review B-2)"
    - "Reflective validator dispatch via GetMethod(name, new[]{closedContextType}) + Activator.CreateInstance(MakeGenericType) — avoids AmbiguousMatchException on the two-overload interface (review S-2)"
    - "Centralized aggregate-and-throw (ThrowIfAny) so core and DI paths share one thrown contract (review S-3)"
    - "Lazy error-list allocation: List<ValidationError> created only on first produced error"

key-files:
  created:
    - src/Core/ExistForAll.SimpleSettings/SettingsValidatorAttribute.cs
    - src/Core/ExistForAll.SimpleSettings/SettingsValidationException.cs
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsValidationTests.cs
  modified:
    - src/Core/ExistForAll.SimpleSettings/Validations/ISettingsValidator.cs
    - src/Core/ExistForAll.SimpleSettings/Validations/ISettingValidation.cs
    - src/Core/ExistForAll.SimpleSettings/Validations/ValidationContext.cs
    - src/Core/ExistForAll.SimpleSettings/Validations/ValidationContextOfT.cs
    - src/Core/ExistForAll.SimpleSettings/Resources.cs
    - src/Core/ExistForAll.SimpleSettings/SettingsPlan.cs
    - src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs

key-decisions:
  - "Validator's declared T is read from its implemented ISettingValidation<> interface to build ValidationContext<T> — uniform for object-level (T=settings interface) and property-level (T=property type)"
  - "RunValidators routes through SettingsValidationException.ThrowIfAny only when the lazy error list is non-null, keeping the null (no-error) path allocation-free while still using the shared throw helper for the error case"
  - "ValidationContext gained a required (object settings) ctor; the previously implicit parameterless ctor is removed — safe because the interfaces were dead scaffolding with no callers"

patterns-established:
  - "Plan-build attribute threading for validators (object-level + per-property), never on the hot populate path"
  - "Zero-allocation warm-path guard via a precomputed bool flag on the cached plan"
  - "Non-settings-indicated nested fixtures resolved via GetSettings<T>() so failing validators never trip ScanAssemblies tests"

requirements-completed: [VAL-01]

coverage:
  - id: D1
    description: "Object-level [SettingsValidator(typeof(X))] runs against the fully-populated instance; a failing validator throws SettingsValidationException, a passing one does not"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsValidationTests.cs#ObjectValidator_WhenItAddsError_ThrowsSettingsValidationException"
        status: pass
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsValidationTests.cs#ObjectValidator_WhenValid_DoesNotThrow"
        status: pass
    human_judgment: false
  - id: D2
    description: "Property-level [SettingsProperty(ValidatorType=typeof(X))] runs against that property's value via ValidationContext<TProperty>"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsValidationTests.cs#PropertyValidator_RunsAgainstThatPropertyValue"
        status: pass
    human_judgment: false
  - id: D3
    description: "Multiple failures aggregate into ONE SettingsValidationException carrying the full ValidationError list (Errors.Count == 2)"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsValidationTests.cs#MultipleErrors_AggregateIntoOneExceptionWithAllErrors"
        status: pass
    human_judgment: false
  - id: D4
    description: "Cross-property validator observes every property set (runs after the property-set loop) — valid pair does not throw, violated pair throws"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsValidationTests.cs#CrossPropertyValidator_ObservesEveryPropertySet"
        status: pass
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsValidationTests.cs#CrossPropertyValidator_WhenRuleViolated_Throws"
        status: pass
    human_judgment: false
  - id: D5
    description: "SettingsValidationException.ToString() carries only author-supplied ValidationError text and no injected bound value (D-12); catchable as SimpleSettingsException (ExceptionHierarchytests)"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsValidationTests.cs#Exception_CarriesAuthorMessage_AndNoBoundValue"
        status: pass
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/ExceptionHierarchyTests.cs#AllLibraryExceptions_DeriveFromSimpleSettingsException"
        status: pass
    human_judgment: false
  - id: D6
    description: "Validator-free type short-circuits (HasValidators == false) and populates without throwing or entering the gather path; zero-allocation guarantee enforced by the benchmark allocation gate in CI"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsValidationTests.cs#NoValidators_PopulatesAndReturnsWithoutThrowing"
        status: pass
    human_judgment: false

# Metrics
duration: 5min
completed: 2026-07-15
status: complete
---

# Phase 04 Plan 03: Core-Path Settings Validation Summary

**Declared object- and property-level validators now run in the core populate pipeline (VAL-01 core path): sync contracts, constructed contexts, a `[SettingsValidator]` attribute, and a value-free aggregate `SettingsValidationException` thrown through a shared `ThrowIfAny` helper — with a `HasValidators` zero-allocation short-circuit protecting the benchmark gate.**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-07-15T13:44:44Z
- **Completed:** 2026-07-15T13:49:58Z
- **Tasks:** 2 (Task 2 TDD: RED -> GREEN)
- **Files modified:** 10 (3 created, 7 modified)

## Accomplishments
- The dead `Validations/*` scaffolding is now live: `ISettingsValidator`/`ISettingValidation<T>.Validate` return `ValidationResult` (Task<> dropped, D-07), and `ValidationContext`/`ValidationContext<T>` carry the settings instance via constructors (D-08).
- `[SettingsValidator(typeof(X))]` (object-level, `AttributeTargets.Interface`) and `[SettingsProperty(ValidatorType=typeof(X))]` (property-level) are both read once at plan build and invoked after the property-set loop, so cross-property rules see the fully-populated instance (D-09/D-11).
- Every produced `ValidationError` aggregates into one `SettingsValidationException : SimpleSettingsException` (D-10) whose message is composed solely from author-supplied `ValidationError` text — no bound value is embedded (D-12), verified against a sentinel-secret binding.
- `SettingsValidationException.ThrowIfAny(errors)` is the single aggregate-and-throw entry point the core path calls and Plan 04's DI runner will reuse, so the thrown contract cannot drift (review S-3).
- `SettingsPlan.HasValidators` is computed once at build; the post-populate hook starts with `if (!plan.HasValidators) return;` before any allocation, and the error list is allocated lazily on first error only — the validator-free warm path stays byte-identical to master (review B-2).
- Reflective dispatch selects the correct `Validate` overload via `GetMethod(name, new[]{ closedContextType })` and builds the context with `MakeGenericType`, avoiding the `AmbiguousMatchException` the two-overload interface would otherwise raise (review S-2).

## Task Commits

Each task was committed atomically:

1. **Task 1: Validation contracts (sync interfaces, context ctors, attribute, aggregate exception + ThrowIfAny)** - `f039dc7` (feat)
2. **Task 2: Core-path validation execution in ValuesPopulator + plan-build threading + tests** - `ee7f481` (test / RED) -> `a6b290e` (feat / GREEN)

**Plan metadata:** _(final docs commit — see git log)_

## Files Created/Modified
- `SettingsValidatorAttribute.cs` (created) — object-level attribute, `Type ValidatorType`, `AttributeTargets.Interface`.
- `SettingsValidationException.cs` (created) — aggregate exception `: SimpleSettingsException` exposing `Errors`; static `ThrowIfAny` shared helper.
- `Validations/ISettingsValidator.cs` / `ISettingValidation.cs` — `Validate` returns `ValidationResult` (Task<> removed).
- `Validations/ValidationContext.cs` / `ValidationContextOfT.cs` — added `(object settings)` / `(T settings)` constructors.
- `Resources.cs` — `SettingsValidationExceptionMessage(IReadOnlyList<ValidationError>)` composed only from author text.
- `SettingsPlan.cs` — `SettingsPlan.ObjectValidatorType` + `HasValidators` (computed at build); `PropertyPlan.ValidatorType`.
- `ValuesPopulator.cs` — plan-build attribute threading, `RunValidators` post-populate hook (HasValidators guard, lazy list, ThrowIfAny), reflective `InvokeValidator`/`ResolveContextType`.
- `Tests/.../SimpleSettings/SettingsValidationTests.cs` (created) — 8 tests + non-indicated nested fixtures/validators.

## Decisions Made
- Derive each validator's `T` from its implemented `ISettingValidation<>` interface to build `ValidationContext<T>`, uniform across object-level (T = settings interface) and property-level (T = property type) validators. This keeps `InvokeValidator` a single reflective path.
- `RunValidators` calls `ThrowIfAny` only when the lazily-allocated error list is non-null; the shared helper still owns the throw for the error case, while the no-error path never allocates.
- Removing the implicit parameterless `ValidationContext` ctor (superseded by the required `(object settings)` ctor) is safe — the interfaces had no callers (confirmed by grep).

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Solution-wide `dotnet build -c Release -f net8.0` reports NETSDK1005 on the net10-only Benchmark project (pre-existing, out of scope per plan). The authoritative `dotnet build SimpleSettings.slnx -c Release` (no `-f`) succeeds for net8.0 AND net10.0 with 0 warnings / 0 errors.

## Verification
- `cd src && dotnet build SimpleSettings.slnx -c Release` -> 0 warnings, 0 errors (net8.0 + net10.0 per-project TFMs).
- `SettingsValidationTests` (net10) -> 8/8 pass.
- `ExceptionHierarchyTests` (net10) -> 6/6 pass (new exception joins the catchable family).
- Full suite (net10) -> 133/133 pass — no auto-discovered fixture regressed the ScanAssemblies-based tests.
- B-2 allocation gate: enforced by design (single-field `HasValidators` guard before any allocation on the validator-free warm path; error list allocated lazily). The BenchmarkDotNet allocation gate runs in CI and is expected byte-identical to master for the validator-free PlanPopulate/Resolve/Shape/ConvertArray/ConfigBinder benchmarks — not run locally (long-running).
- D-12: `Exception_CarriesAuthorMessage_AndNoBoundValue` asserts `ToString()` contains the author message and excludes the sentinel secret bound to the property.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Plan 04 (DI-resolved validator path + API-02) reuses `SettingsValidationException.ThrowIfAny` and the same aggregate + redaction contract established here — the shared helper guarantees the thrown contract stays byte-identical across the two paths.
- Sync validator contracts and constructed contexts are in place for the DI runner.
- No blockers.

## Self-Check: PASSED

- FOUND: src/Core/ExistForAll.SimpleSettings/SettingsValidatorAttribute.cs
- FOUND: src/Core/ExistForAll.SimpleSettings/SettingsValidationException.cs
- FOUND: src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsValidationTests.cs
- FOUND: .planning/phases/04-collection-validation-binding/04-03-SUMMARY.md
- FOUND commits: f039dc7, ee7f481, a6b290e

---
*Phase: 04-collection-validation-binding*
*Completed: 2026-07-15*
