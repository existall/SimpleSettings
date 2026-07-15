---
phase: 04-collection-validation-binding
plan: 05
subsystem: conversion
tags: [validation, allow-empty, string, dotnet, tunit, redaction]

# Dependency graph
requires:
  - phase: 04-collection-validation-binding
    provides: "04-01 PropertyConversion.Convert null-branch factory dispatch (Func<object> list null-result) that VAL-02 extends in place"
provides:
  - "[SettingsProperty(AllowEmpty=false)] rejects null AND null-or-whitespace strings at bind (VAL-02/D-13), value-free via SettingsPropertyNullException (D-14)"
  - "AllowEmpty=true (default) still binds empty and whitespace strings as-is (no regression)"
affects: [validation]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Single _throwOnNull guard covering both null and C#-12-safe null-or-whitespace string (value is string s && string.IsNullOrWhiteSpace(s)) ahead of the 04-01 factory dispatch, so the reject path short-circuits before the shared/factory null-result branch"

key-files:
  created: []
  modified:
    - src/Core/ExistForAll.SimpleSettings/Conversion/PropertyConversion.cs
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsPropertyTests.cs

key-decisions:
  - "REUSE value-free SettingsPropertyNullException (property name only) rather than a new empty-specific type — it is already excluded from the ValuesPopulator.cs:122 redaction filter, so no filter change and the empty diagnostic is never redacted (Pitfall 3 avoided by construction)"
  - "Rejection guard placed BEFORE the 04-01 Func<object> factory dispatch and gated on _throwOnNull, leaving the AllowEmpty=true accept path (empty/whitespace -> _converter.Convert) and the null-result factory dispatch untouched"

patterns-established:
  - "AllowEmpty=false extends its reject predicate to null-or-whitespace strings inline at the conversion layer (D-14), no hot-path scan for the accepted case"

requirements-completed: [VAL-02]

coverage:
  - id: D1
    description: "[SettingsProperty(AllowEmpty=false)] bound to an empty string throws value-free SettingsPropertyNullException"
    requirement: "VAL-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsPropertyTests.cs#Build_WhenAllowEmptyIsFalse_EmptyString_ShouldThrowNullException"
        status: pass
    human_judgment: false
  - id: D2
    description: "[SettingsProperty(AllowEmpty=false)] bound to a whitespace-only string throws value-free SettingsPropertyNullException"
    requirement: "VAL-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsPropertyTests.cs#Build_WhenAllowEmptyIsFalse_WhitespaceString_ShouldThrowNullException"
        status: pass
    human_judgment: false
  - id: D3
    description: "The rejection exception message carries the property name and no bound value (value-free)"
    requirement: "VAL-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsPropertyTests.cs#Build_WhenAllowEmptyIsFalse_EmptyString_MessageIsValueFreeWithPropertyName"
        status: pass
    human_judgment: false
  - id: D4
    description: "AllowEmpty=true (default) bound to empty or whitespace does not throw and binds the value as-is"
    requirement: "VAL-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsPropertyTests.cs#Build_WhenAllowEmptyIsTrue_EmptyString_BindsValue, #Build_WhenAllowEmptyIsTrue_WhitespaceString_BindsValue"
        status: pass
    human_judgment: false

# Metrics
duration: 3min
completed: 2026-07-15
status: complete
---

# Phase 04 Plan 05: AllowEmpty String Rejection Summary

**[SettingsProperty(AllowEmpty=false)] now rejects empty and whitespace strings at bind (not just null) via the value-free SettingsPropertyNullException, reusing the existing type so the ValuesPopulator redaction filter needs no change.**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-07-15T13:53:15Z
- **Completed:** 2026-07-15T13:56:13Z
- **Tasks:** 1 (TDD)
- **Files modified:** 2

## Accomplishments
- `PropertyConversion.Convert` rejects a null-or-whitespace string (in addition to null) when `_throwOnNull` (AllowEmpty=false), throwing the value-free `SettingsPropertyNullException` (property name only).
- The reject guard sits ahead of 04-01's `Func<object>` list null-result factory dispatch and preserves it — the null path still returns a fresh-per-bind list, arrays/IEnumerable keep the shared empty instance.
- AllowEmpty=true (default) is unchanged: an empty or whitespace string falls through to the normal converter and binds as-is.
- No change to the `ValuesPopulator.cs:122` redaction filter or the binder catch — `SettingsPropertyNullException` is already excluded, so the empty diagnostic is never redacted (Pitfall 3).

## Task Commits

Each task was committed atomically (TDD RED -> GREEN):

1. **Task 1: Reject empty/whitespace strings for AllowEmpty=false (VAL-02/D-13, D-14)** — `b675dd3` (test) -> `795dd93` (feat)

**Plan metadata:** _(final docs commit — see git log)_

_No REFACTOR commit: the GREEN change was a single guard line; nothing to clean up._

## Files Created/Modified
- `Conversion/PropertyConversion.cs` — `Convert` gains a leading `_throwOnNull && (value == null || value is string s && string.IsNullOrWhiteSpace(s))` reject guard (C# 12-safe); the null-result factory dispatch and accept path are unchanged.
- `Tests/.../SimpleSettings/SettingsPropertyTests.cs` — 5 new tests + 2 fixture interfaces (`IRejectsEmpty` with AllowEmpty=false, `IAllowsEmpty` default) bound via `InMemoryCollection`/`InMemoryBinder`; the pre-existing int AllowEmpty=false test is retained.

## Decisions Made
None beyond the plan — implemented exactly as specified. Reused `SettingsPropertyNullException` (value-free) per D-14; placed the guard ahead of the 04-01 factory dispatch and gated it on `_throwOnNull` so both the accept path and the list null-result factory stay intact. The optional `_throwOnNull` rename was not performed (explicitly optional in the plan).

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. RED showed exactly the 3 AllowEmpty=false empty/whitespace tests failing while the two AllowEmpty=true accept-tests and the pre-existing int test passed, confirming the guard was the only missing behavior.

## Scope Verification
- `git diff HEAD~2 HEAD --name-only` after the feat commit lists only `PropertyConversion.cs` + the test file — VAL-02's own change set does not touch `ValuesPopulator.cs` (the `:122` redaction filter and binder catch are untouched by this plan, consistent with review N-1: Plan 03 legitimately edited ValuesPopulator.cs earlier in the wave).

## Verification
- `cd src && dotnet build SimpleSettings.slnx -c Release` -> Build succeeded, 0 Warnings, 0 Errors (net8.0 + net10.0 per-project TFMs).
- `SettingsPropertyTests` on net10 -> 6/6 pass (incl. the pre-existing AllowEmpty=false int test).
- Full suite on net10 -> 138/138 pass.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- VAL-02 is complete; ROADMAP success criterion 4 (empty/whitespace rejection at bind, `${ENV:-}` placeholder detection deferred per D-13) is met and proven green on net10.
- No blockers.

## Self-Check: PASSED

- FOUND: src/Core/ExistForAll.SimpleSettings/Conversion/PropertyConversion.cs
- FOUND: src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsPropertyTests.cs
- FOUND: .planning/phases/04-collection-validation-binding/04-05-SUMMARY.md
- FOUND commits: b675dd3 (test), 795dd93 (feat)

---
*Phase: 04-collection-validation-binding*
*Completed: 2026-07-15*
