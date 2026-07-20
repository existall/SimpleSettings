---
phase: 05-documentation
plan: 02
subsystem: infra
tags: [msbuild, nuget, packaging, metadata, directory-build-props]

# Dependency graph
requires:
  - phase: 04
    provides: collection binding + validation engine (the shipped surface the package metadata describes)
provides:
  - Correctly-spelled, legacy-name-free packaged <Description>
  - Canonical <PackageTags> (SimpleSettings replaces legacy SimpleConfig token)
affects: [05-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Shared MSBuild props (src/Directory.Build.props) inject package metadata into all three published packages"

key-files:
  created: []
  modified:
    - src/Directory.Build.props

key-decisions:
  - "Replaced the legacy SimpleConfig PackageTags token with SimpleSettings (canonical) rather than dropping it, keeping tag count stable"

patterns-established:
  - "Package-metadata token fixes are proven by a green dotnet build -c Release from src/ (props-parse gate)"

requirements-completed: [DOC-01]

coverage:
  - id: D1
    description: "Packaged <Description> reads with correct spelling ('application') and no legacy product name"
    requirement: "DOC-01"
    verification:
      - kind: other
        ref: "grep -c 'appliaction' src/Directory.Build.props -> 0"
        status: pass
    human_judgment: false
  - id: D2
    description: "<PackageTags> carries no legacy SimpleConfig token; canonical SimpleSettings present"
    requirement: "DOC-01"
    verification:
      - kind: other
        ref: "grep -riEc '\\bSimpleConfig\\b' src/Directory.Build.props -> 0"
        status: pass
    human_judgment: false
  - id: D3
    description: "Shared props still parse; all three packages build cleanly under Release"
    requirement: "DOC-01"
    verification:
      - kind: integration
        ref: "cd src && dotnet build -c Release -> exit 0, 0 warnings 0 errors"
        status: pass
    human_judgment: false

# Metrics
duration: 3min
completed: 2026-07-20
status: complete
---

# Phase 5 Plan 02: Package Metadata Token Fix Summary

**Corrected the misspelled 'appliaction' in the packaged <Description> and replaced the legacy SimpleConfig <PackageTags> token with SimpleSettings in src/Directory.Build.props, verified by a green Release build.**

## Performance

- **Duration:** 3 min
- **Started:** 2026-07-20
- **Completed:** 2026-07-20
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Fixed the `<Description>` typo `appliaction` -> `application` (consumer-visible on the nuget.org package page)
- Replaced the trailing legacy `SimpleConfig` token in `<PackageTags>` with the canonical `SimpleSettings`, keeping all other tags intact
- Proved the shared props still parse: `dotnet build -c Release` from `src/` exits 0 with 0 warnings / 0 errors across all three packages

## Task Commits

Each task was committed atomically:

1. **Task 1: Correct the Description typo and replace the legacy PackageTags token** - `066cb99` (fix)

**Plan metadata:** (recorded in final docs commit)

## Files Created/Modified
- `src/Directory.Build.props` - Two token edits: `<Description>` typo corrected, `<PackageTags>` legacy token replaced. No structural change; URLs, icon, README, versioning, and pack ItemGroup untouched (all verified canonical).

## Decisions Made
- Replaced the legacy `SimpleConfig` tag with `SimpleSettings` rather than dropping it, preserving the tag list shape while removing the stale product name.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Package metadata (`<Description>`, `<PackageTags>`) is now canonical and legacy-name-free.
- The README logo fix (broken `existall/Shepherd` URL) and the phase-final cross-file grep sweep + `dotnet pack` gate remain owned by 05-04.

## Self-Check: PASSED

---
*Phase: 05-documentation*
*Completed: 2026-07-20*
