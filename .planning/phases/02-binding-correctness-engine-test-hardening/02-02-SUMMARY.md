---
phase: 02-binding-correctness-engine-test-hardening
plan: 02
subsystem: testing
tags: [tunit, binding, type-conversion, uri, datetime, converter]

# Dependency graph
requires:
  - phase: 02
    plan: 01
    provides: "Core/ test conventions + the integration Build<T> pattern (ordered InMemoryBinder over InMemoryCollection) and the --treenode-filter TUnit invocation pattern"
  - phase: 01
    provides: "Value-free SettingsPropertyValueException contract — the negative case asserts its type only, never re-proving redaction"
provides:
  - "ScalarConversionTests: scalar Uri positive (new Uri(value)), scalar DateTime positive (ParseExact yyyy-MM-dd), one DateTime format-mismatch negative (SettingsPropertyValueException type-only)"
  - "Phase 2 success criterion #5 scalar coverage: Uri/DateTime scalar conversion proven on net10 (net8 via CI parity)"
affects: [binding-correctness, engine-test-hardening]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Reused the integration Build<T> pattern from CollectionConversionTests, scoped to scalar single-property binding (section name 'Endpoint' from IEndpoint via leading-I strip)"

key-files:
  created:
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/ScalarConversionTests.cs
  modified: []

key-decisions:
  - "Dedicated ScalarConversionTests.cs file (not an extension of an existing converter class) for a clean, greppable Scalar test surface — plan-preferred and CONTEXT-permitted."
  - "Single nested IEndpoint fixture carries both Uri and DateTime scalar properties; each positive test binds only its own key, leaving the other unbound (resolves to type default) with no interference."
  - "Negative case (When = '01/02/2020') asserts Throws<SettingsPropertyValueException>() type only — no message/ToString/value/inner assertion (redaction owned by ExceptionRedactionTests)."

patterns-established:
  - "Scalar converter coverage lives in Conversion/ alongside CollectionConversionTests; array element coverage stays in CollectionConversionTests — no cross-duplication."

requirements-completed: [TEST-03]

coverage:
  - id: D8
    description: "Scalar Uri positive: bound URL string resolves to new Uri(value)"
    requirement: "TEST-03"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/ScalarConversionTests.cs#Convert_ScalarUri_ParsesToUri"
        status: pass
    human_judgment: false
  - id: D9
    description: "Scalar DateTime positive: yyyy-MM-dd string resolves via ParseExact to the expected DateTime"
    requirement: "TEST-03"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/ScalarConversionTests.cs#Convert_ScalarDateTime_WithConfiguredFormat_Parses"
        status: pass
    human_judgment: false
  - id: D10
    description: "Scalar DateTime format-mismatch negative surfaces SettingsPropertyValueException (type only)"
    requirement: "TEST-03"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/ScalarConversionTests.cs#Convert_ScalarDateTime_FormatMismatch_ThrowsSettingsPropertyValueException"
        status: pass
    human_judgment: false

# Metrics
duration: 3min
completed: 2026-07-14
status: complete
---

# Phase 2 Plan 02: Scalar Converter-Coverage Residual (TEST-03) Summary

**One new TUnit file locks the missing scalar `Uri`/`DateTime` POSITIVE parsing paths plus a single DateTime format-mismatch negative (exception type only) — closing Phase 2 success criterion #5's scalar gap without duplicating P4's array coverage or ExceptionRedactionTests, and with no production code touched.**

## Performance

- **Duration:** ~3 min
- **Completed:** 2026-07-14
- **Tasks:** 1 (test-only)
- **Files modified:** 1 created

## Accomplishments
- TEST-03: `ScalarConversionTests` proves scalar `Uri` binding resolves to `new Uri("https://a.example/")`, scalar `DateTime` binding of `"2020-01-02"` resolves via `ParseExact` (default `yyyy-MM-dd`) to `new DateTime(2020, 1, 2)`, and a format-mismatch value (`"01/02/2020"`) surfaces `SettingsPropertyValueException`.
- The negative case asserts the exception TYPE only — no message/ToString/value/inner assertion — so the SEC-01 redaction invariant (locked by ExceptionRedactionTests) is neither re-proved nor weakened (threat T-02-02 mitigated as planned).
- Scalar coverage added on top of P4's array-of-Uri/DateTime coverage with zero duplication; no array-of-* cases introduced here.
- Full net10 unit suite green: 94/94 passing (3 new tests added by this plan; the 91 from Plan 01 unchanged).

## Task Commits

1. **Task 1: TEST-03 ScalarConversionTests (Uri/DateTime positive + format-mismatch negative)** - `41634fb` (test)

## Files Created/Modified
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/ScalarConversionTests.cs` - `public class ScalarConversionTests` in block-scoped namespace `...UnitTests.Conversion`; three `[Test]` methods over the integration `Build<T>` pattern; nested `IEndpoint { Uri Url; DateTime When; }` fixture; section name `"Endpoint"` (leading `I` stripped).

## Verification Evidence (success criterion #5 scalar coverage)
- **Build:** from `src/`, `dotnet build SimpleSettings.slnx -c Debug` → Build succeeded, 0 warnings, 0 errors (net8 + net10).
- **Scalar tests:** `dotnet test Tests/ExistForAll.SimpleSettings.UnitTests --framework net10.0 --no-build --treenode-filter "/*/*/ScalarConversionTests/*"` → 3/3 passed.
- **No regression:** full net10 suite → 94/94 passed (91 prior + 3 new).
- **No production diff:** only the one test file added under `src/Tests/`; no `src/Core/` changes (test-only phase invariant held).

## Decisions Made
- Dedicated `ScalarConversionTests.cs` file for a clean, greppable scalar surface (plan-preferred over extending an existing converter class).
- Both scalar properties share one `IEndpoint` fixture; each positive test binds only its own key (the unbound sibling resolves to its type default without interference).
- Negative test asserts the exception type only — redaction stays owned by ExceptionRedactionTests.

## Deviations from Plan
- **[Test-invocation only, not a code change]** The plan's `<verify>` block specified `--filter "*Scalar*"`, which Microsoft.Testing.Platform / TUnit rejects (zero tests, exit 5 — the gotcha Plan 01 recorded in STATE.md). Used the platform-native `--treenode-filter "/*/*/ScalarConversionTests/*"` selector instead. No test code was affected.

## Do-Not-Duplicate Compliance
- No array-of-Uri or array-of-DateTime cases (owned by CollectionConversionTests P4 coverage).
- The negative asserts only `Throws<SettingsPropertyValueException>()` — no redaction/message/value re-assertion (owned by ExceptionRedactionTests).

## Deferred This Phase
- **COLL-01** (`List<T>`/`IList<T>`/`ICollection<T>` support decision) remains **owner-deferred** — not implemented, per the plan's `<deferred>` section. Recorded here so it stays visible as an intentional deferral, not silently dropped.

## Next Phase Readiness
- Phase 2 plan set (2 of 2) is complete: TEST-01/TEST-02/ENG-01 (Plan 01) + TEST-03 (Plan 02). COLL-01 explicitly deferred.
- No blockers. No production code changed across the phase.

## Self-Check: PASSED

- FOUND: src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/ScalarConversionTests.cs
- FOUND: .planning/phases/02-binding-correctness-engine-test-hardening/02-02-SUMMARY.md
- FOUND commit: 41634fb (Task 1)

---
*Phase: 02-binding-correctness-engine-test-hardening*
*Completed: 2026-07-14*
