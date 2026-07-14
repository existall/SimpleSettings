---
phase: 02-binding-correctness-engine-test-hardening
plan: 01
subsystem: testing
tags: [tunit, binding, type-conversion, concurrency, nullable, converter]

# Dependency graph
requires:
  - phase: 01
    provides: "Secret-safe exception contract (S1 #27, C2 #28) — the value-free SettingsPropertyValueException / SettingsBindingException hierarchy these tests must not re-assert"
provides:
  - "ValuesPopulatorTests: binder precedence (last-writer-wins, later-silent-does-not-clobber) + [SettingsProperty] DefaultValue-survives coverage"
  - "TypeConverterTests: null->value-type-default, Nullable<int> strip+convert, and ConverterType-over-collection resolution coverage"
  - "ENG-01 verification: confirmation that SettingsClassGenerator concurrency gate (#29) + its two stress tests satisfy Phase 2 success criterion #4"
affects: [02-02, binding-correctness, engine-test-hardening]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Direct converter-orchestration seam: new TypeConverter().CreateConversion(prop, attr, new SettingsOptions()) then PropertyConversion.Convert(...) to unit-test resolution logic without a generated instance"
    - "Integration precedence proof via ordered AddSectionBinder(new InMemoryBinder(collection)) calls"

key-files:
  created:
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/ValuesPopulatorTests.cs
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs
  modified: []

key-decisions:
  - "Used the integration build path (SettingsBuilder + ordered InMemoryBinders) for TEST-01 precedence rather than the internal ValuesPopulator ctor — binder ORDER is expressible via AddSectionBinder (RESEARCH Open Question #1)."
  - "Split the Nullable<int> gap into two explicit tests (null->null and \"42\"->42) for a distinct null vs strip-and-convert assertion."
  - "SentinelConverter returns new[] { -1 } so a passing ConverterType test proves the attribute converter was chosen over CollectionTypeConverter."

patterns-established:
  - "New Core/ test subfolder mirrors the source ExistForAll.SimpleSettings.Core namespace; nested public interface fixtures + private helper converter at class bottom."
  - "Run TUnit filters via --treenode-filter \"/*/*/ClassName/*\" (Microsoft.Testing.Platform), not the legacy --filter flag."

requirements-completed: [TEST-01, TEST-02, ENG-01]

coverage:
  - id: D1
    description: "ValuesPopulator binder precedence: last-writer-wins across two ordered binders"
    requirement: "TEST-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/ValuesPopulatorTests.cs#Populate_WhenTwoOrderedBindersSetSameProperty_LaterBinderWins"
        status: pass
    human_judgment: false
  - id: D2
    description: "ValuesPopulator: a later silent binder does not clobber an earlier set value"
    requirement: "TEST-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/ValuesPopulatorTests.cs#Populate_WhenLaterBinderIsSilentOnProperty_EarlierValueSurvives"
        status: pass
    human_judgment: false
  - id: D3
    description: "ValuesPopulator: [SettingsProperty] DefaultValue survives when binders present but none set the property"
    requirement: "TEST-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/ValuesPopulatorTests.cs#Populate_WhenBindersPresentButNoneSetProperty_AttributeDefaultSurvives"
        status: pass
    human_judgment: false
  - id: D4
    description: "TypeConverter: null for non-nullable int resolves to type default (0)"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs#Convert_NullForNonNullableValueType_ReturnsTypeDefault"
        status: pass
    human_judgment: false
  - id: D5
    description: "TypeConverter: Nullable<int> resolves null->null and \"42\"->42 (strip + convert)"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs#Convert_NullForNullableValueType_ReturnsNull"
        status: pass
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs#Convert_NumericStringForNullableValueType_StripsAndConverts"
        status: pass
    human_judgment: false
  - id: D6
    description: "TypeConverter: ConverterType on an IEnumerable<int> property bypasses the collection converter (sentinel wins)"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs#Convert_WhenConverterTypeSetOnCollectionProperty_BypassesCollectionConverter"
        status: pass
    human_judgment: false
  - id: D7
    description: "ENG-01 verify-only: SettingsClassGenerator concurrency gate (#29) + two stress tests satisfy Phase 2 success criterion #4"
    requirement: "ENG-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsClassGeneratorTests.cs#GenerateType_ConcurrentSameInterface_ReturnsSingleSharedType"
        status: pass
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/SimpleSettings/SettingsClassGeneratorTests.cs#GenerateType_ConcurrentAcrossSameAndDistinctInterfaces_IsRaceFree"
        status: pass
    human_judgment: false

# Metrics
duration: 2min
completed: 2026-07-14
status: complete
---

# Phase 2 Plan 01: Engine-Core Correctness Test Hardening Summary

**Two new TUnit test files lock ValuesPopulator binder precedence + attribute-default-survives and the three uncovered TypeConverter resolution paths (null->default, Nullable<int> strip+convert, ConverterType-over-collection); ENG-01 concurrency fix verified green — no production code touched.**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-07-14T10:00:00Z
- **Completed:** 2026-07-14T10:01:51Z
- **Tasks:** 3 (2 code, 1 verify-only)
- **Files modified:** 2 created

## Accomplishments
- TEST-01: `ValuesPopulatorTests` proves last-writer-wins across two ordered binders, that a later silent binder does not clobber an earlier set value, and that a `[SettingsProperty(DefaultValue=...)]` survives when binders run but none set the property.
- TEST-02: `TypeConverterTests` proves `null`->value-type default (`int`->`0`), `Nullable<int>` null->`null` and `"42"`->`42`, and that an attribute `ConverterType` on an `IEnumerable<int>` property bypasses `CollectionTypeConverter` (sentinel `new[]{-1}` wins over the parsed `[1,2,3]`).
- ENG-01 (verify-only): confirmed the `_generationGate` double-checked lock is present in `SettingsClassGenerator.cs` (lines 27 + 53) and both concurrency stress tests pass — Phase 2 success criterion #4 is met by the pre-GSD fix (#29), no work performed.
- Full net10 unit suite green: 91/91 passing (7 new tests added by this plan).

## Task Commits

1. **Task 1: TEST-01 ValuesPopulator precedence + default-survives** - `5ff8647` (test)
2. **Task 2: TEST-02 TypeConverter null/nullable/ConverterType** - `42ff521` (test)
3. **Task 3: ENG-01 verify-only** - no commit (no code diff; evidence recorded here)

## Files Created/Modified
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/ValuesPopulatorTests.cs` - Three precedence/default tests via the integration build path (ordered `InMemoryBinder`s; section name `"Sample"`).
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs` - Four tests over the direct `TypeConverter.CreateConversion(...)` seam covering the three uncovered resolution paths, with a nested `SentinelConverter`.

## ENG-01 Verification Evidence (success criterion #4)
- **Gate present (no modification):** `grep -nE '_generationGate|lock \(' Core/ExistForAll.SimpleSettings/Core/Reflection/SettingsClassGenerator.cs` returns `27: private readonly object _generationGate = new();` and `53: lock (_generationGate)` — a single gate guarding all generation with a lock-free warm `TryGetValue` before the lock.
- **Stress tests green:** `dotnet test ... --treenode-filter "/*/*/*/*Concurrent*"` (net10) => 2/2 passed: `GenerateType_ConcurrentSameInterface_ReturnsSingleSharedType` (Parallel.For(0,128) => one distinct type) and `GenerateType_ConcurrentAcrossSameAndDistinctInterfaces_IsRaceFree` (32 threads + Barrier over 8 interfaces => zero failures, one shared impl each).
- **No diff:** `git diff --stat` shows no `src/` changes attributable to this task.

## Decisions Made
- Integration build path (not the internal ctor) for TEST-01 precedence — binder order is expressible via `AddSectionBinder`.
- Nullable<int> gap split into two explicit tests (null vs strip-and-convert).
- `SentinelConverter` returns a distinctive `new[]{-1}` so the ConverterType test unambiguously proves the collection converter was bypassed.

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
- The plan's automated-verify command used `--filter "*Name*"`, which Microsoft.Testing.Platform / TUnit rejects ("Zero tests ran", exit 5). Resolved by using the platform-native `--treenode-filter "/*/*/ClassName/*"` selector. This is a test-invocation detail only; no test code was affected. (Captured as a pattern for future plans.)

## Do-Not-Duplicate Compliance
- No assertion references `SettingsBindingException` or `SettingsPropertyValueException` (exception contract stays owned by ExceptionHierarchyTests / ExceptionRedactionTests).
- No empty-enumerable or `AllowEmpty=false` re-assertion (owned by CollectionConversionTests / SettingsPropertyTests).
- No `ThrowingBinder` copy; no new fake binder introduced.

## Next Phase Readiness
- Plan 02 (TEST-03 ScalarConversionTests) is unblocked; this plan established the `Core/` test subfolder convention and the `--treenode-filter` invocation pattern.
- No blockers. No production code changed (test-only phase invariant held).

## Self-Check: PASSED

- FOUND: src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/ValuesPopulatorTests.cs
- FOUND: src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs
- FOUND: .planning/phases/02-binding-correctness-engine-test-hardening/02-01-SUMMARY.md
- FOUND commit: 5ff8647 (Task 1)
- FOUND commit: 42ff521 (Task 2)

---
*Phase: 02-binding-correctness-engine-test-hardening*
*Completed: 2026-07-14*
