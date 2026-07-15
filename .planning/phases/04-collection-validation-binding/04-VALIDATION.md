---
phase: 4
slug: collection-validation-binding
status: ready
nyquist_compliant: true
wave_0_complete: false
created: 2026-07-15
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Body sourced from `04-RESEARCH.md` § Validation Architecture (lines 427-500).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | TUnit 1.58.0 on Microsoft.Testing.Platform |
| **Config file** | `src/global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`) |
| **Quick run command** | `cd src && dotnet test "$SOLUTION" -c Release --no-build --treenode-filter "/*/*/<ClassName>/*"` |
| **Full suite command** | `cd src && dotnet restore && dotnet build -c Release --no-restore && dotnet test -c Release --no-build` |
| **Estimated runtime** | ~30-60 seconds (net10 locally; net8 via CI) |

> ⚠ TUnit gotcha: `dotnet test --filter "*X*"` exits 5 / zero tests. Use `--treenode-filter` or run unfiltered. Run from `src/`.

---

## Sampling Rate

- **After every task commit:** the `--treenode-filter` quick run for the touched test class.
- **After every plan wave:** full suite on net10 locally (`dotnet test -c Release --no-build`).
- **Before `/gsd-verify-work`:** full suite green on net8 + net10 (CI) **plus** `benchmark.yml` allocation gate green.
- **Max feedback latency:** ~60 seconds.

---

## Per-Requirement Verification Map

Task IDs are assigned by the planner; each task's `<automated>` verify must map to a row below.

| Requirement | Behavior | Threat Ref | Test Type | Automated Command (treenode-filter) | File Exists |
|-------------|----------|------------|-----------|-------------------------------------|-------------|
| COLL-01 | Delimited scalar → `List<int>`/`IList<int>`/`ICollection<int>`/`IReadOnlyList<int>`/`IReadOnlyCollection<int>` all materialize a `List<T>` | — | unit | `/*/*/CollectionConversionTests/*` | ✅ extend `Conversion/CollectionConversionTests.cs` |
| COLL-01 | `IEnumerable<T>` still returns a `T[]` (no regression) | — | unit | `/*/*/CollectionConversionTests/*` | ✅ pinned `Convert_ToIntEnumerable_MaterializesAnArray` — keep green |
| COLL-02 | Unbound `int[]` → empty array, not null | — | unit | `/*/*/TypeConverterTests/*` | ✅ extend `Core/TypeConverterTests.cs` |
| COLL-02 | Unbound `List<int>` → empty list, not null | — | unit | `/*/*/TypeConverterTests/*` | ❌ W0 |
| COLL-03 | Child-section sequence binds to `string[]`/`int[]`/`List<T>` | — | integration | `/*/*/SettingsBuilderConfigurationBinderIntegrationTests/*` | ✅ extend `ConfigBuilderConfigurationBinderIntegrationTests.cs` |
| COLL-03 | Children win when scalar + children both present | — | integration | same | ❌ W0 |
| COLL-03 | Comma-scalar still binds (regression guard, prod `MultiHost__CommonHosts`) | — | integration | same | ✅ add explicit assertion |
| COLL-03 | Empty / whitespace / empty-sequence → empty, never `[""]` | — | integration | same | ❌ W0 |
| COLL-03 | `[SettingsSection]` / root prefix honored on sequence | — | integration | same | ❌ W0 |
| **COLL-03 / S1** | **Secret in a sequence element absent from full `ex.ToString()` on convert failure** | **T-04-S1 (D-06)** | unit | `/*/*/ExceptionRedactionTests/*` | ✅ extend `Conversion/ExceptionRedactionTests.cs` |
| VAL-01 | Object-level `[SettingsValidator]` runs; failing validator throws `SettingsValidationException` | — | unit | `/*/*/*Validation*/*` | ❌ W0 (new test class) |
| VAL-01 | Property-level `ValidatorType` runs on the property value | — | unit | same | ❌ W0 |
| VAL-01 | Multiple failures aggregate into one exception's error list | — | unit | same | ❌ W0 |
| VAL-01 | Cross-property rule validates against the fully-populated instance | — | unit | same | ❌ W0 |
| VAL-01 | DI-resolved `ISettingValidation<T>` runs (per Q3 deferred-post-build mechanism) | — | integration | `/*/*/AddSimpleSettingsIntegrationTests/*` | ❌ W0 |
| VAL-01 / D-12 | `SettingsValidationException.ToString()` contains only author messages, no injected values | T-04-VAL (D-12) | unit | `/*/*/*Validation*/*` | ❌ W0 |
| VAL-02 | `AllowEmpty=false` rejects `""` and whitespace (not just null); value-free exception | — | unit | `/*/*/TypeConverterTests/*` or `/*/*/SettingsPropertyTests/*` | ✅ extend `Core/TypeConverterTests.cs` |
| VAL-02 | `AllowEmpty=true` still accepts empty/whitespace | — | unit | same | ❌ W0 |
| API-02 | `ISettingsCollection` resolvable via `GetRequiredService` after `AddSimpleSettings` | — | integration | `/*/*/AddSimpleSettingsIntegrationTests/*` | ✅ extend `DependencyInjection/AddSimpleSettingsIntegrationTests.cs` |
| API-02 | Return-value overload yields the same collection; `IServiceCollection` chaining preserved | — | integration | same | ❌ W0 |

*Status per task: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky (tracked in each plan's tasks)*

---

## Wave 0 Requirements

- [ ] `Conversion/CollectionConversionTests.cs` — add `List<T>` family + `IEnumerable`-still-array cases (COLL-01)
- [ ] `Core/TypeConverterTests.cs` — empty-not-null for array + `List<T>` (COLL-02); empty/whitespace rejection (VAL-02)
- [ ] `ConfigBuilderConfigurationBinderIntegrationTests.cs` — sequence bind, children-win, comma-scalar guard, empty→empty, prefix (COLL-03)
- [ ] `Conversion/ExceptionRedactionTests.cs` — **S1 sequence-element redaction** regression (COLL-03 / D-06 gate)
- [ ] New `SimpleSettings/SettingsValidationTests.cs` — object-level, property-level, aggregate, cross-property, redaction (VAL-01)
- [ ] `DependencyInjection/AddSimpleSettingsIntegrationTests.cs` — DI-resolved validator (Q3), `ISettingsCollection` resolution + return overload (VAL-01/API-02)
- [ ] Test fixtures: settings interfaces with `[SettingsValidator]` / `ValidatorType` / list-shaped + sequence-backed properties (co-locate per TESTING.md)

---

## Manual-Only Verifications

All phase behaviors have automated verification.

*(The two on-branch decisions — `List<T>` wrap-vs-direct-build and API-02 return-overload shape — are design choices resolved at code-review, not manual test verifications.)*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING (❌) references
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [x] `nyquist_compliant: true` set in frontmatter
- [ ] `wave_0_complete: true` — flips when the Wave-0 test stubs are authored during execution

**Approval:** approved 2026-07-15 (validation strategy; `wave_0_complete` flips during execution)
