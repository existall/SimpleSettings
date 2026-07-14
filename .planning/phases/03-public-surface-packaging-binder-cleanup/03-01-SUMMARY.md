---
phase: 03-public-surface-packaging-binder-cleanup
plan: 01
subsystem: packaging
tags: [dotnet, nuget, cpm, central-package-management, public-api, multi-targeting, net8, net10]

# Dependency graph
requires:
  - phase: 02
    provides: modern-only TFM baseline (net8.0;net10.0), CPM via Directory.Packages.props, MinVer versioning, TUnit test stack
provides:
  - SettingsHolder is internal sealed (removed from the public API surface)
  - Core.AspNet dead package removed from the solution graph and all references
  - per-TFM conditional Microsoft.Extensions.* floors (net8 8.0.x / net10 10.0.9)
affects: [03-02, packaging, public-surface, beta-release]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Per-TFM conditional PackageVersion ItemGroups in CPM (mutually-exclusive $(TargetFramework) gates)"
    - "Floor verification against the packable consumer projects' project.assets.json with a zero-entry guard"

key-files:
  created: []
  modified:
    - src/Core/ExistForAll.SimpleSettings/SettingsHolder.cs
    - src/SimpleSettings.slnx
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/ExistForAll.SimpleSettings.UnitTests.csproj
    - src/Directory.Packages.props

key-decisions:
  - "SettingsHolder flipped to internal sealed (API-01/D-01) — reachable from tests/benchmark via existing InternalsVisibleTo; no public-API baseline file to update"
  - "Core.AspNet removed from repo (PKG-01/D-02); published 2.0.0-alpha.0.* alphas left as owner-optional unlist follow-up (D-02b), not automated"
  - "net8 floors set at latest-patch-per-package Option 1 (Configuration 8.0.0, Configuration.Json 8.0.1, DI 8.0.1, DI.Abstractions 8.0.2); net10 stays 10.0.9 (PKG-02/D-03)"
  - "Per-TFM condition placed on the ItemGroup rather than each item — functionally mutually-exclusive, avoids NU1504, cleaner than per-item duplication"

patterns-established:
  - "Per-TFM conditional PackageVersion ItemGroups: two mutually-exclusive groups gated on $(TargetFramework), one item per package per group, no unconditional twin (NU1504-safe)"
  - "Consumer-floor assertion targets the packable projects (Binders/GenericHost) assets, not the core lib which references no Microsoft.Extensions.* — with a GUARD-zero-ms-ext check to prevent a vacuous green"

requirements-completed: [API-01, PKG-01, PKG-02]

coverage:
  - id: D1
    description: "SettingsHolder is internal sealed — removed from the public API surface, reachable from tests only via InternalsVisibleTo"
    requirement: "API-01"
    verification:
      - kind: unit
        ref: "dotnet test SimpleSettings.slnx -c Release --framework net10.0 (94/94 passed — tests reach SettingsHolder via InternalsVisibleTo)"
        status: pass
      - kind: other
        ref: "grep 'internal sealed class SettingsHolder' src/Core/ExistForAll.SimpleSettings/SettingsHolder.cs + Release build 0 errors both TFMs"
        status: pass
    human_judgment: false
  - id: D2
    description: "Core.AspNet is absent from the solution and no project references it; the solution builds without it"
    requirement: "PKG-01"
    verification:
      - kind: other
        ref: "test ! -e src/Core/ExistForAll.SimpleSettings.Core.AspNet && grep -rq Core.AspNet returns none in slnx + UnitTests.csproj"
        status: pass
      - kind: integration
        ref: "dotnet restore + dotnet build SimpleSettings.slnx -c Release (both TFMs, 0 errors)"
        status: pass
    human_judgment: false
  - id: D3
    description: "Microsoft.Extensions.Configuration (Binders package) and DependencyInjection.Abstractions (GenericHost package) resolve to 8.0.x on net8 and 10.0.9 on net10"
    requirement: "PKG-02"
    verification:
      - kind: integration
        ref: "project.assets.json floor check on Binders + GenericHost — floor-check-errors: [] (net8 8.0.0/8.0.2, net10 10.0.9)"
        status: pass
      - kind: integration
        ref: "dotnet restore -p:NuGetAuditMode=all — no NU1901-NU1904; build -p:TreatWarningsAsErrors=true 0 warnings both TFMs"
        status: pass
    human_judgment: false
  - id: D4
    description: "Unlist the published Core.AspNet 2.0.0-alpha.0.* prereleases on NuGet.org (owner-optional registry hygiene)"
    requirement: "PKG-01"
    verification: []
    human_judgment: true
    rationale: "Requires the guy-lud publishing account and a manual dotnet nuget delete against NuGet.org; deliberately NOT automated by this plan (D-02b). Owner-optional, not required for phase completion."

# Metrics
duration: 3min
completed: 2026-07-14
status: complete
---

# Phase 3 Plan 01: Public Surface + Packaging + Binder Cleanup Summary

**SettingsHolder made internal sealed, dead Core.AspNet package removed from the solution graph, and Microsoft.Extensions.* floors floated per-TFM so net8 consumers resolve 8.0.x while net10 stays 10.0.9 — full TUnit suite green.**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-07-14T12:40:27Z
- **Completed:** 2026-07-14T12:43:25Z
- **Tasks:** 3
- **Files modified:** 4 (+ 1 package directory removed)

## Accomplishments
- `SettingsHolder` flipped from `public class` to `internal sealed class` — an internal DTO removed from the public API surface (API-01/D-01), still reachable from tests + benchmark via the existing `InternalsVisibleTo`.
- `Core.AspNet` dead package fully removed: dropped from `SimpleSettings.slnx`, dropped from the UnitTests `ProjectReference` set, and its `src/Core/ExistForAll.SimpleSettings.Core.AspNet/` directory deleted (PKG-01/D-02). The test project's local `Core/AspNet/Environments.cs` duplicate (different namespace) was preserved.
- Four `Microsoft.Extensions.*` pins split into two mutually-exclusive per-TFM conditional `ItemGroup`s: net8 floors (Configuration 8.0.0, Configuration.Json 8.0.1, DI 8.0.1, DI.Abstractions 8.0.2), net10 all at 10.0.9 (PKG-02/D-03) — freeing net8 consumers of the packable Binders + GenericHost packages from being forced onto 10.x.
- Plan-completion gate green: CI build clean on both TFMs, restore audit (`NuGetAuditMode=all`) with no NU1901-NU1904 advisories, warnings-as-errors build 0 warnings, and the full TUnit suite passing 94/94 on net10.

## Task Commits

Each task was committed atomically:

1. **Task 1: Flip SettingsHolder to internal sealed (API-01/D-01)** - `1d2349e` (refactor)
2. **Task 2: Remove the dead Core.AspNet package (PKG-01/D-02)** - `774bc0e` (chore)
3. **Task 3: Float Microsoft.Extensions.* floor per-TFM (PKG-02/D-03)** - `2db3bdf` (chore)

_Task 2's commit was amended once to fold in the slnx + csproj edits after the initial commit captured only the git-tracked deletions (see Issues Encountered)._

## Files Created/Modified
- `src/Core/ExistForAll.SimpleSettings/SettingsHolder.cs` - `public class` -> `internal sealed class SettingsHolder : ISettingsHolder`
- `src/SimpleSettings.slnx` - removed the Core.AspNet `<Project>` entry
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/ExistForAll.SimpleSettings.UnitTests.csproj` - removed the Core.AspNet `<ProjectReference>`
- `src/Directory.Packages.props` - replaced the four unconditional Microsoft.Extensions.* pins with two per-TFM conditional ItemGroups
- `src/Core/ExistForAll.SimpleSettings.Core.AspNet/` - directory removed (Environments.cs + .csproj deleted via git rm; leftover bin/obj removed)

## Decisions Made
- **Per-TFM condition on the ItemGroup, not per-item.** The plan described per-item `Condition` attributes; using two mutually-exclusive `<ItemGroup Condition="...">` blocks is functionally identical (only one item per package is active per TFM, NU1504-safe) and cleaner. Verified equivalent: floor check resolves net8 8.0.x / net10 10.0.9 with `floor-check-errors: []`.
- **net8 floor = Option 1 (latest-patch-per-package).** Shipped per the decision wording; the uniform-8.0.0 alternative was left unused.
- **NuGet unlist not executed.** The published `Core.AspNet` 2.0.0-alpha.0.* alphas remain listed; unlisting is captured as an owner-optional follow-up (D-02b) requiring the guy-lud publishing account.

## Deviations from Plan

None - plan executed exactly as written. (One process hiccup during committing, not a plan deviation — see Issues Encountered.)

## Issues Encountered
- **Task 2 commit split.** The combined `git add` for Task 2 included the already-deleted directory pathspec, which returned a fatal and broke the `&&` chain, so the first commit (`c59d7b7`) captured only the git-tracked deletions. The commit was immediately amended (`774bc0e`) to fold in the `SimpleSettings.slnx` and UnitTests `.csproj` edits. Final commit contains all four file changes; verified via `git show --stat`.
- **Directory removal under sandbox.** `rm -rf` on the Core.AspNet directory tripped a bash pre-guard; removed the gitignored `bin/`+`obj/` and the empty directory via scoped `rm -rf bin obj` + `rmdir` instead. Directory confirmed absent.

## User Setup Required

**Owner-optional NuGet registry hygiene (NOT required for phase completion).** After this removal the published `ExistForAll.SimpleSettings.Core.AspNet` 2.0.0-alpha.0.* prereleases stop updating but remain listed on NuGet.org. To unlist them (unlists, does not delete):

```
dotnet nuget delete ExistForAll.SimpleSettings.Core.AspNet <version> -s https://api.nuget.org/v3/index.json --non-interactive
```

Requires the guy-lud publishing account. This plan deliberately did NOT execute any publish/unlist (D-02b).

## Next Phase Readiness
- Phase 3 success criteria #1 (public surface trimmed), #2 (Core.AspNet removed), and #3 (per-TFM floors) are satisfied and suite-verified on both TFMs.
- Ready for Plan 03-02.
- No blockers. The only outstanding item is the owner-optional NuGet unlist above.

## Self-Check: PASSED

- FOUND: 03-01-SUMMARY.md, SettingsHolder.cs, Directory.Packages.props
- REMOVED-OK: Core.AspNet directory absent
- FOUND commits: 1d2349e, 774bc0e, 2db3bdf

---
*Phase: 03-public-surface-packaging-binder-cleanup*
*Completed: 2026-07-14*
