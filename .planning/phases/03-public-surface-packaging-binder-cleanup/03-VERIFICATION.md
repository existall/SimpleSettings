---
phase: 03-public-surface-packaging-binder-cleanup
verified: 2026-07-14T13:20:00Z
status: passed
score: 4/4 must-haves verified
behavior_unverified: 0
overrides_applied: 0
---

# Phase 3: Public Surface, Packaging & Binder Cleanup Verification Report

**Phase Goal:** The public API and packages carry only meaningful, correctly-scoped surface, and the command-line binder parses real-world arguments correctly — the remaining breaking changes batched before beta.
**Verified:** 2026-07-14T13:20:00Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| #   | Truth (SC / REQ) | Status | Evidence |
| --- | ---------------- | ------ | -------- |
| 1 | API-01 / SC #1 — `SettingsHolder` is `internal sealed`, off the public surface, reachable from tests only via InternalsVisibleTo; suite green | ✓ VERIFIED | `SettingsHolder.cs:5` = `internal sealed class SettingsHolder : ISettingsHolder`; `ISettingsHolder.cs:5` = `internal interface`; `Info.cs` grants InternalsVisibleTo to UnitTests + Benchmark; only `public` match is the ctor of the internal type (not public surface); full suite 208/208 green (tests reach the type) |
| 2 | PKG-01 / SC #2 — `Core.AspNet` directory removed; no reference in slnx or UnitTests csproj; solution builds without it; test-local duplicate remains | ✓ VERIFIED | `Core/ExistForAll.SimpleSettings.Core.AspNet` DIR-ABSENT; `grep Core.AspNet` over slnx + UnitTests.csproj = NO-MATCHES; `Tests/.../Core/AspNet/Environments.cs` still present; CI Release build 0 errors both TFMs |
| 3 | PKG-02 / SC #3 — per-TFM `Microsoft.Extensions.*` floors resolve net8→8.0.x, net10→10.0.9 in the packable Binders/GenericHost assets; restore emits no NU1901–NU1904 | ✓ VERIFIED | assets floor check: Binders Configuration net8=8.0.0/net10=10.0.9, GenericHost DI.Abstractions net8=8.0.2/net10=10.0.9, `floor-check-errors: []` (guard passed); `dotnet restore -p:NuGetAuditMode=all` NO-ADVISORY; build `-p:TreatWarningsAsErrors=true` 0 warnings both TFMs |
| 4 | SRC-02 / SC #4 — CLI binder parses real-world args: space-separated `--key value`, quoted-value-with-spaces, prefixed-next-token=new key, SkipFirstArgument default false + AddArguments binds arg[0], AddCommandLine sources GetCommandLineArgs() and skips exe, empty-token safe, CLI-path redaction asserts no value leak | ✓ VERIFIED | 26 ArgumentsTests pass on both TFM DLLs incl. `Build_WhenSpaceSeparatedValueHasSpaces_BindsFullValue`, `Build_WhenNextTokenIsPrefixed_KeyIsNotStored`, `Build_WhenSkipFirstArgumentTrue_SkipsIndexZero`, `Build_WhenEmptyToken_ParsesSafelyAndBindsRest`, `Build_WhenSpaceSeparatedValueIsPrefixed_DoesNotBind`, `Build_WhenCliValueUnconvertible_ExceptionExcludesValue`; source confirms `SkipFirstArgument=false` default, empty-safe zero-alloc prefix detection (`Array.IndexOf`, `name.Length != key.Length`), `AddCommandLine` uses `Environment.GetCommandLineArgs()` + `SkipFirstArgument = true` |

**Score:** 4/4 truths verified (0 present, behavior-unverified)

All four truths are behavior-dependent (parsing state transitions, per-TFM restore resolution) and each is backed by a passing behavioral test or a live restore+assets inspection run in this verification — none passed on symbol presence alone.

### Required Artifacts

| Artifact | Expected | Status | Details |
| -------- | -------- | ------ | ------- |
| `src/Core/ExistForAll.SimpleSettings/SettingsHolder.cs` | internal sealed class | ✓ VERIFIED | Line 5 exact match; consumed by internal `SettingsCollection`; wired via suite |
| `src/Directory.Packages.props` | per-TFM conditional PackageVersion items | ✓ VERIFIED | Two mutually-exclusive `<ItemGroup Condition="'$(TargetFramework)' == ...">` blocks, one item per package per group, no unconditional twin (NU1504-safe) |
| `src/SimpleSettings.slnx` | no Core.AspNet Project entry | ✓ VERIFIED | Only core/Binders/GenericHost/UnitTests/Benchmark entries remain |
| `src/Tests/.../ExistForAll.SimpleSettings.UnitTests.csproj` | no Core.AspNet ProjectReference | ✓ VERIFIED | Three ProjectReferences (Binders, core, GenericHost); no Core.AspNet |
| `CommandLineSettingsBinderOptions.cs` | `public bool SkipFirstArgument { get; set; } = false;` | ✓ VERIFIED | Line 26 exact; ArgumentPrefixes carries the A4 XML-doc note |
| `CommandLineSettingsBinder.cs` | lookahead Parse, index-0 skip, empty-safe zero-alloc prefix detection | ✓ VERIFIED | Index loop lines 46-68; cached `prefixes` array; `Array.IndexOf(prefixes, next[0])`; no bare `token[0]`; SplitByDelimiter unchanged; BindPropertySettings store-only |
| `SettingsBuilderFactoryExtensions.cs` | AddCommandLine uses GetCommandLineArgs() + SkipFirstArgument=true | ✓ VERIFIED | Lines 44-50; wraps caller action so skip applied first then caller override |
| `ArgumentsTests.cs` | new TUnit cases for all SRC-02 behaviors | ✓ VERIFIED | 13 methods incl. all headline cases; class name `ArgumentsTests` stable; leading-space fixture preserved |

### Key Link Verification

| From | To | Via | Status | Details |
| ---- | -- | --- | ------ | ------- |
| `Info.cs` | UnitTests/Benchmark assemblies | InternalsVisibleTo | ✓ WIRED | Both assembly attributes present; internal SettingsHolder reachable — 208/208 suite green proves it |
| `Directory.Packages.props` | Binders/GenericHost assets | `$(TargetFramework)` conditional restore | ✓ WIRED | assets.json shows net8 8.0.x / net10 10.0.9 resolved per-TFM |
| `AddCommandLine` | `CommandLineSettingsBinder` | `GetCommandLineArgs()` → `AddArguments(args, o => o.SkipFirstArgument=true)` | ✓ WIRED | Exe-skip owned by entry point; verified in source + SkipFirstArgument=true test |
| `SkipFirstArgument` | `Parse` iteration start | `start = SkipFirstArgument && args.Length>0 ? 1 : 0` | ✓ WIRED | Line 44; true/false cases both pass |
| test-local `Core/AspNet/Environments.cs` | Core/AspNet tests | independent namespace `...UnitTests.Core.AspNet` | ✓ WIRED | Duplicate preserved; suite green after package removal |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ----------- | ----------- | ------ | -------- |
| API-01 | 03-01 | Make `SettingsHolder`/`ISettingsHolder` internal | ✓ SATISFIED | Both types internal (SettingsHolder internal sealed, ISettingsHolder internal interface) |
| PKG-01 | 03-01 | `Core.AspNet` dropped | ✓ SATISFIED | Directory + all references removed; solution builds |
| PKG-02 | 03-01 | Float `Microsoft.Extensions.*` floor per-TFM | ✓ SATISFIED | net8→8.0.x, net10→10.0.9; no advisories |
| SRC-02 | 03-02 | CLI binder parses quoted-with-spaces values and skips arg[0] | ✓ SATISFIED | 26 ArgumentsTests pass |

All four declared requirement IDs (API-01, PKG-01, PKG-02, SRC-02) are claimed by the plans and map exactly to the Phase 3 rows in REQUIREMENTS.md traceability. No orphaned requirements. REQUIREMENTS.md marks all four `[x]` Complete.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Restore audit clean | `dotnet restore -p:NuGetAuditMode=all` | exit 0, NO-ADVISORY | ✓ PASS |
| CI build warnings-as-errors | `dotnet build -c Release --no-restore -p:ContinuousIntegrationBuild=true -p:TreatWarningsAsErrors=true` | Build succeeded, 0 Warning(s), 0 Error(s) | ✓ PASS |
| Per-TFM floor resolution | assets.json floor check (Binders + GenericHost) | `floor-check-errors: []` | ✓ PASS |
| Targeted CLI binder tests | `dotnet test --treenode-filter "/*/*/ArgumentsTests/*"` | 26 passed, 0 failed (both TFM DLLs) | ✓ PASS |
| Full suite | `dotnet test SimpleSettings.slnx -c Release --no-build` | 208 passed, 0 failed, 0 skipped (both TFM DLLs) | ✓ PASS |

### Anti-Patterns Found

None. No TODO/FIXME/XXX/HACK/PLACEHOLDER markers in any of the four modified source files.

### Owner-Optional Follow-Up (Informational — NOT a phase gate)

The published `ExistForAll.SimpleSettings.Core.AspNet` 2.0.0-alpha.0.* prereleases remain listed on NuGet.org after the in-repo removal. Unlisting them (`dotnet nuget delete <id> <version> ...` under the guy-lud account) is documented in 03-01 `user_setup` and coverage item D4, explicitly marked owner-optional and NOT required for phase completion (D-02b). This is external registry hygiene outside the codebase and does not gate the phase goal; recorded here for visibility, not as a blocking human-verification item.

### Gaps Summary

No gaps. All four phase success criteria are observably true in the codebase and confirmed by live build, restore-audit, per-TFM assets inspection, and a green 208-test suite across both target-framework DLLs. The public surface is trimmed (`SettingsHolder`/`ISettingsHolder` internal), the dead `Core.AspNet` package is fully removed while the test-local duplicate is preserved, per-TFM `Microsoft.Extensions.*` floors resolve correctly with no security advisories, and the command-line binder parses real-world arguments (space-separated, quoted-with-spaces, prefixed-next-token semantics, entry-point-scoped exe skip, empty-token safety) with a CLI-path secret-redaction regression proving no value leak.

---

_Verified: 2026-07-14T13:20:00Z_
_Verifier: Claude (gsd-verifier)_
