---
phase: 04-collection-validation-binding
plan: 01
subsystem: conversion
tags: [type-conversion, collections, generics, reflection-elision, dotnet, tunit]

# Dependency graph
requires:
  - phase: 02-engine-core-tests
    provides: CollectionTypeConverter de-reflection base + PropertyConversion null-result seam
  - phase: 03-sources-cli
    provides: converter-chain ordering conventions (user-first / Default-last)
provides:
  - "ListTypeConverter: List/IList/ICollection/IReadOnlyList/IReadOnlyCollection<T> materialize a List<T> from a delimited scalar (COLL-01/D-01)"
  - "IsListLike + IsCollectionShape shape predicates (disjoint from IsArray/IsEnumerable) — IsCollectionShape is the single source of truth COLL-03 will reuse"
  - "Shape-aware CreateNullResult: unbound array/List/IEnumerable bind an empty collection, never null (COLL-02/D-02)"
  - "Fresh-per-bind empty List<T> null-result (reference-distinct per bind) via a baked factory delegate in the existing _nullResult slot"
affects: [collection-source-binding, validation, COLL-03, VAL-01]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Cached per-element-type Func<Array,object> materialization delegate (built once via generic-method CreateDelegate) instead of Activator.CreateInstance(MakeGenericType(...)) on the warm path"
    - "Factory-delegate null-result reusing the object? _nullResult slot to give mutable defaults fresh-per-bind semantics without growing the readonly struct"

key-files:
  created:
    - src/Core/ExistForAll.SimpleSettings/Conversion/ListTypeConverter.cs
  modified:
    - src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypeExtensions.cs
    - src/Core/ExistForAll.SimpleSettings/Conversion/CollectionTypeConverter.cs
    - src/Core/ExistForAll.SimpleSettings/Conversion/TypeConvertersCollections.cs
    - src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypeConverter.cs
    - src/Core/ExistForAll.SimpleSettings/Conversion/PropertyConversion.cs
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/CollectionConversionTests.cs
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs

key-decisions:
  - "ListTypeConverter subclasses CollectionTypeConverter and reuses base.Convert's array-build path, then copies into List<T> via a cached factory delegate — zero warm-path reflection (review S-4/A1)"
  - "IsEnumerable left unchanged (still only typeof(IEnumerable<>)); a separate disjoint IsListLike avoids EnumerableTypeConverter claiming List<T> and returning an unassignable T[] (Pitfall 1)"
  - "List null-result is a fresh-per-bind factory, not a shared instance — a shared mutable empty List<T> would be corrupted by a consumer mutating 'their' empty list (review B-3); arrays/IEnumerable keep the shared cached empty array (effectively immutable)"

patterns-established:
  - "Warm-path generic materialization via cached CreateDelegate factory keyed by element type"
  - "Mutable-default null-results carried as Func<object> in the existing _nullResult slot so PropertyPlan[] layout/byte-cost is unchanged (protects the B-2 benchmark gate)"

requirements-completed: [COLL-01, COLL-02]

coverage:
  - id: D1
    description: "List<T>/IList<T>/ICollection<T>/IReadOnlyList<T>/IReadOnlyCollection<T> bound from a delimited scalar materialize a List<T> with parsed elements (COLL-01/D-01)"
    requirement: "COLL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/CollectionConversionTests.cs#Convert_DelimitedString_ToIntList_MaterializesList (and IntIList/IntICollection/IntReadOnlyList/IntReadOnlyCollection/DayOfWeekList)"
        status: pass
    human_judgment: false
  - id: D2
    description: "IEnumerable<T> still materializes a T[] — no regression (pinned Convert_ToIntEnumerable_MaterializesAnArray)"
    requirement: "COLL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/CollectionConversionTests.cs#Convert_ToIntEnumerable_MaterializesAnArray"
        status: pass
    human_judgment: false
  - id: D3
    description: "Unbound array/List/IEnumerable collection properties bind an empty collection instead of null (COLL-02/D-02)"
    requirement: "COLL-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs#Convert_NullForArrayProperty_ReturnsEmptyArray, #Convert_NullForListProperty_ReturnsEmptyList, #Convert_NullForEnumerableProperty_ReturnsEmptyArray"
        status: pass
    human_judgment: false
  - id: D4
    description: "Each unset List<T> bind yields a fresh reference-distinct empty list; mutating one never affects another (review B-3)"
    requirement: "COLL-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs#Convert_NullForListProperty_YieldsFreshInstancePerBind"
        status: pass
    human_judgment: false

# Metrics
duration: 6min
completed: 2026-07-15
status: complete
---

# Phase 04 Plan 01: Collection Conversion Foundation Summary

**Full List<T>-family conversion (List/IList/ICollection/IReadOnlyList/IReadOnlyCollection<T>) plus an empty-not-null default for every collection shape, both with zero warm-path reflection and B-3 fresh-per-bind list semantics.**

## Performance

- **Duration:** ~6 min
- **Started:** 2026-07-15T14:24:00Z
- **Completed:** 2026-07-15T14:30:06Z
- **Tasks:** 2 (both TDD)
- **Files modified:** 8 (1 created, 7 modified)

## Accomplishments
- `ListTypeConverter` materializes a `List<T>` for the whole List family from a delimited scalar; the element converter chain still runs per element (enum/DateTime/Uri lists parse correctly).
- `IEnumerable<T>` still returns a `T[]` — the pinned `Convert_ToIntEnumerable_MaterializesAnArray` stays green (no regression).
- Unbound `int[]`, `List<int>`, and `IEnumerable<int>` now bind an empty collection rather than `null`.
- Two null-binds of an unset `List<int>` return reference-distinct, mutation-isolated empty lists (review B-3), while arrays/IEnumerable keep the shared cached empty array.
- No `Activator.CreateInstance`/`MakeGenericType`/`Invoke` on the per-populate warm path — materialization is a cached `Func<Array,object>` delegate keyed by element type (review S-4/A1).

## Task Commits

Each task was committed atomically (TDD RED → GREEN):

1. **Task 1: List<T>-family converter + disjoint shape predicates** — `7a91d1e` (test) → `5c2dab0` (feat)
2. **Task 2: Empty-not-null default for every collection shape** — `44b33d9` (test) → `1dfd464` (feat)

**Plan metadata:** _(final docs commit — see git log)_

## Files Created/Modified
- `Conversion/ListTypeConverter.cs` (created) — List-family converter; cached per-element `Func<Array,object>` factory built via generic-method `CreateDelegate`.
- `Core/Reflection/TypeExtensions.cs` — added `IsListLike` (disjoint) and `IsCollectionShape`; `IsEnumerable` unchanged.
- `Conversion/CollectionTypeConverter.cs` — `Convert` promoted to `virtual` (mechanical, internal, non-breaking).
- `Conversion/TypeConvertersCollections.cs` — registered `ListTypeConverter` after `EnumerableTypeConverter`, before `EnumTypeConverter`; existing order preserved.
- `Core/Reflection/TypeConverter.cs` — `CreateNullResult` branches `IsArray → IsEnumerable → IsListLike → value-type/null`; list branch bakes an empty-list factory delegate.
- `Conversion/PropertyConversion.cs` — `Convert` invokes the factory when `_nullResult` is `Func<object>`; all other shapes on the shared-instance path unchanged.
- `Tests/.../Conversion/CollectionConversionTests.cs` — 6 new tests + 6 fixture interfaces for the List family and per-element enum list.
- `Tests/.../Core/TypeConverterTests.cs` — 4 new null-result tests + 3 fixture interfaces (incl. the B-3 fresh-per-bind assertion).

## Decisions Made
None beyond the plan — implemented exactly as specified. The plan's `<action>` blocks superseded the PATTERNS.md `Activator.CreateInstance(MakeGenericType(...))` sketch with the cached-factory approach (review S-4/A1), and that plan-level guidance was followed.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- A solution-wide `dotnet build SimpleSettings.slnx -c Release -f net8.0` reports `NETSDK1005` on `ExistForAll.SimpleSettings.Benchmark` because that project is single-TFM `net10.0`. This is a pre-existing, out-of-scope condition of the benchmark project, not a result of these changes. The plan's authoritative command (`dotnet build SimpleSettings.slnx -c Release`, no `-f`) succeeds with 0 warnings/0 errors, and building the Core library directly with `-f net8.0` succeeds with 0 errors — confirming the shared code is C# 12-safe for net8.

## Verification
- `cd src && dotnet build SimpleSettings.slnx -c Release` → 0 warnings, 0 errors (net8.0 + net10.0 per-project TFMs).
- Core library `-f net8.0` build → 0 errors (C# 12-safe shared code confirmed).
- `CollectionConversionTests` → 18/18 pass (incl. all 5 list shapes, per-element enum list, and pinned enumerable-array test).
- `TypeConverterTests` → 8/8 pass (incl. array/list/enumerable empty defaults and B-3 fresh-per-bind).
- Full suite net10 → 114/114 pass.
- Acceptance greps: `IsEnumerable` body unchanged; no `Enumerable.Empty` reflection in `TypeConverter.cs`; no `Activator.CreateInstance`/`MakeGenericType` in `ListTypeConverter.cs` code; registration order preserved.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- `IsCollectionShape` is available as the single shape predicate COLL-03's source binder will reuse.
- COLL-02's empty default is in place, which COLL-03's empty-sequence→empty behavior (D-03) builds on.
- List-family and array/enumerable properties now bind correctly, unblocking VAL-01 validators that inspect collection properties.
- No blockers.

## Self-Check: PASSED

- FOUND: src/Core/ExistForAll.SimpleSettings/Conversion/ListTypeConverter.cs
- FOUND: .planning/phases/04-collection-validation-binding/04-01-SUMMARY.md
- FOUND commits: 7a91d1e, 5c2dab0, 44b33d9, 1dfd464

---
*Phase: 04-collection-validation-binding*
*Completed: 2026-07-15*
