---
phase: 04-collection-validation-binding
plan: 02
subsystem: binders
tags: [configuration-binder, collections, sequence-binding, internals-visible-to, redaction, security, dotnet, tunit]

# Dependency graph
requires:
  - phase: 04-collection-validation-binding
    provides: "IsCollectionShape/IsListLike shape predicates + COLL-02 empty-not-null collection default (Plan 01)"
provides:
  - "ConfigurationBinder binds collection properties from IConfiguration child-section sequences (Section:Key:0, :1, ...) as a string[] flowing the element converter chain (COLL-03/D-04)"
  - "Children-win precedence over a coexisting scalar; comma-scalar override preserved; whitespace/empty -> empty collection (D-05)"
  - "InternalsVisibleTo grant to the Binders assembly so Core's IsCollectionShape is the single source of shape truth"
  - "S1/SEC-01 secret-redaction invariant re-verified on the new sequence path across element position and both array/list converter shapes (D-06)"
affects: [validation, VAL-01, source-binding]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Manual two-pass right-sized string[] projection over IConfigurationSection.GetChildren() (count then fill) instead of LINQ Select/Where/ToArray on the bind path (review S-5)"
    - "Cross-assembly reuse of Core's internal shape predicate via InternalsVisibleTo rather than duplicating the predicate in the Binders package (no drift with the converter chain)"

key-files:
  created: []
  modified:
    - src/Core/ExistForAll.SimpleSettings/Info.cs
    - src/Core/ExistForAll.SimpleSettings.Extensions.Binders/ConfigurationBinder.cs
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/ConfigBuilderConfigurationBinderIntegrationTests.cs
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/ExceptionRedactionTests.cs

key-decisions:
  - "IVT grant targets the Binders project assembly SIMPLE NAME `ExistForAll.SimpleSettings.Binders` (its csproj has no <AssemblyName> override), NOT the directory name `...Extensions.Binders` — a wrong target leaves Core's internal invisible and ConfigurationBinder fails with CS0122 (review B-1)"
  - "GetChildren() -> string[] built with a manual count-then-fill loop, filtering to non-null .Value leaves, so the element converter chain runs per element with no LINQ allocation on the bind path (review S-5)"
  - "For collection-shape targets the scalar fall-through skips SetNewValue when IsNullOrWhiteSpace so COLL-02's empty-not-null default applies (avoids the [\" \"] whitespace-token pitfall); non-collection scalar behavior is byte-for-byte unchanged"

patterns-established:
  - "Collection binder branch: children win, empty sequence falls through to scalar, whitespace scalar yields the empty default"

requirements-completed: [COLL-03]

coverage:
  - id: D04
    description: "A collection property binds from a child-section sequence (Section:Key:0,:1,...) as a string[] flowing the element converter chain (string[]/int[]/List<string>)"
    requirement: "COLL-03"
    verification:
      - kind: integration
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/ConfigBuilderConfigurationBinderIntegrationTests.cs#Bind_ChildSequence_ToStringArray_BindsEachElement (+ ToIntArray, ToStringList)"
        status: pass
    human_judgment: false
  - id: D05a
    description: "When both a scalar and children exist for the key, children win"
    requirement: "COLL-03"
    verification:
      - kind: integration
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/ConfigBuilderConfigurationBinderIntegrationTests.cs#Bind_ScalarAndChildren_ChildrenWin"
        status: pass
    human_judgment: false
  - id: D05b
    description: "The comma-scalar form still binds unchanged (prod MultiHost__CommonHosts override preserved)"
    requirement: "COLL-03"
    verification:
      - kind: integration
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/ConfigBuilderConfigurationBinderIntegrationTests.cs#Bind_CommaScalar_NoChildren_StillBinds"
        status: pass
    human_judgment: false
  - id: D05c
    description: "Whitespace scalar and empty child-sequence -> empty collection (Count 0), never a single blank element"
    requirement: "COLL-03"
    verification:
      - kind: integration
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/ConfigBuilderConfigurationBinderIntegrationTests.cs#Bind_WhitespaceScalar_ToStringArray_BindsEmpty, #Bind_EmptySequence_BindsEmpty"
        status: pass
    human_judgment: false
  - id: D05d
    description: "RootSection/[SettingsSection] prefix honored on the sequence path exactly as on the scalar path"
    requirement: "COLL-03"
    verification:
      - kind: integration
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/ConfigBuilderConfigurationBinderIntegrationTests.cs#Bind_ChildSequence_WithRootSection_ResolvesUnderPrefix"
        status: pass
    human_judgment: false
  - id: D06
    description: "A secret carried in a sequence element is absent from the entire ex.ToString() chain on convert failure — across element position (first + later) and both array/list converter shapes (S1/T-04-S1)"
    requirement: "COLL-03"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/ExceptionRedactionTests.cs#Convert_SecretInSequenceElement_DoesNotLeakValue, #Convert_SecretInFirstSequenceElement_DoesNotLeakValue, #Convert_SecretInListSequenceElement_DoesNotLeakValue"
        status: pass
    human_judgment: true

# Metrics
duration: 12min
completed: 2026-07-15
status: complete
---

# Phase 04 Plan 02: ConfigurationBinder Child-Section Sequence Binding Summary

**ConfigurationBinder now binds collection properties from IConfiguration child-section sequences (children win, comma-scalar preserved, whitespace/empty -> empty), reusing Core's internal IsCollectionShape via an InternalsVisibleTo grant, with the S1 secret-redaction invariant re-proven on the new path across element position and both array/list converter shapes.**

## Performance

- **Duration:** ~12 min
- **Completed:** 2026-07-15
- **Tasks:** 2 (both TDD)
- **Files modified:** 4 (0 created, 4 modified)

## Accomplishments
- A collection property expressed as an indexed sub-section (`Section:Key:0`, `:1`, ...) binds as a `string[]` flowing the existing element converter chain; `string[]`, `int[]`, and `List<string>` all bind correctly (COLL-03/D-04).
- Children win over a coexisting scalar; the comma-scalar override still binds (prod `MultiHost__CommonHosts` regression guard preserved); whitespace scalar and empty sequence both yield an empty collection (Count 0), never a `[" "]` blank element; the RootSection prefix is inherited on the sequence path (D-05).
- The Binders assembly reuses Core's `internal` `IsCollectionShape` predicate via a new `InternalsVisibleTo("ExistForAll.SimpleSettings.Binders")` grant — one source of shape truth, no drift with the converter chain.
- The GetChildren() -> `string[]` projection uses a manual two-pass count-then-fill loop (no LINQ on the bind path; review S-5).
- D-06 security gate: the S1 secret-redaction invariant is re-proven on the new sequence path — a secret element that fails to convert is absent from the whole `SettingsPropertyValueException.ToString()` chain, pinned across the first-element and later-element positions and across the `int[]` and `List<int>` converter shapes (S-6).

## Task Commits

Each task was committed atomically (TDD RED -> GREEN):

1. **Task 1: GetChildren() child-sequence branch (COLL-03/D-04, D-05)** — `af63c18` (test) -> `26446f2` (feat)
2. **Task 2: D-06 sequence-element redaction regression (SECURITY GATE)** — `3402ac6` (test; regression pin on existing value-free wrapper, no production change needed)

**Plan metadata:** _(final docs commit — see git log)_

## Files Created/Modified
- `Core/ExistForAll.SimpleSettings/Info.cs` — added `InternalsVisibleTo("ExistForAll.SimpleSettings.Binders")` (assembly simple name; B-1).
- `Core/ExistForAll.SimpleSettings.Extensions.Binders/ConfigurationBinder.cs` — added the `IsCollectionShape`-guarded `TrySetChildSequence` branch (manual two-pass right-sized `string[]`), children-win early return, and the collection-target whitespace/empty scalar fall-through that defers to COLL-02's empty default; non-collection scalar path unchanged.
- `Tests/.../ConfigBuilderConfigurationBinderIntegrationTests.cs` — 8 new behavior tests + 3 fixture interfaces (sequence bind for string[]/int[]/List<string>, children-win, comma-scalar guard, whitespace->empty, empty-sequence->empty, prefix).
- `Tests/.../Conversion/ExceptionRedactionTests.cs` — 3 new D-06 redaction tests + a `CaptureSequenceConversionFailure` helper + 2 fixture interfaces (int[] later/first element, List<int>).

## Decisions Made
None beyond the plan — implemented exactly as specified. The IVT target was verified against the Binders csproj (`ExistForAll.SimpleSettings.Binders.csproj`, no `<AssemblyName>` override) so the simple name `ExistForAll.SimpleSettings.Binders` is correct, and the B-1 build gate proved the internal predicate is actually visible.

## Deviations from Plan

None - plan executed exactly as written.

## Security Gate (D-06)

- The D-06 secret-redaction regression test was implemented fully and autonomously (writing a test is implementation, not a human decision): 3 cases covering int[] secret-in-later-element, int[] secret-in-first-element, and List<int> secret-element. All assert the sentinel is absent from the entire `ex.ToString()` chain while the property name and target-type token are present. The S1 invariant (no config/element value enters any exception ToString()) is not weakened.
- **The formal security-auditor SIGN-OFF for D-06 (blocking, severity high) is PENDING the phase-level security gate** — the orchestrator runs `/gsd-secure-phase` after the phase. The `human_judgment: true` marker on coverage row D06 reflects that pending sign-off.

## Issues Encountered
- A solution-wide `dotnet build -f net8.0` still trips `NETSDK1005` on the net10-only Benchmark project (pre-existing, out of scope, documented in Plan 01). The authoritative `dotnet build SimpleSettings.slnx -c Release` (no `-f`) builds both net8.0 and net10.0 per-project TFMs with 0 warnings/0 errors — this is the B-1 gate and it passed, proving the IVT grant exposes Core's internal shape predicate to the Binders assembly.

## Verification
- **B-1 gate:** `cd src && dotnet build SimpleSettings.slnx -c Release` -> 0 warnings, 0 errors; `ExistForAll.SimpleSettings.Binders.dll` built for BOTH net8.0 and net10.0 (compiles against Core's `internal` `IsCollectionShape`).
- `SettingsBuilderConfigurationBinderIntegrationTests` -> 13/13 pass on net10 (5 pre-existing + 8 new).
- `ExceptionRedactionTests` -> 8/8 pass on net10 (5 pre-existing redaction tests + 3 new D-06 cases).
- Full suite net10 -> 125/125 pass.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Collection source binding (COLL-03) is complete; validation (VAL-01) can now inspect collection-typed properties bound from either scalar or sequence sources.
- D-06 security-auditor sign-off is queued for the phase-level `/gsd-secure-phase` gate.
- No blockers.

## Self-Check: PASSED
