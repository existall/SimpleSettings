---
phase: 05-documentation
plan: 04
subsystem: docs
tags: [readme, nuget, packaging, markdown, dotnet-pack]

# Dependency graph
requires:
  - phase: 05-01
    provides: renamed docs/Extending SimpleSettings.md (ToC entry #6 target)
  - phase: 05-02
    provides: fixed Directory.Build.props <Description>/PackageTags (repacked into README-bearing .nupkg)
  - phase: 05-03
    provides: docs/Security.md deep security/behavior + migration page (README deep-link target)
provides:
  - Full README.md rewrite (canonical logo, dotnet-add install x3, 7-entry ToC, verified quickstart, feature overview, DI snippet)
  - Concise README "Security notes" (value-free invariant + both carve-outs) and "Breaking changes / migration (v1 -> v2)" sections
  - Phase-final DOC-VERIFICATION gate green (13 grep gates + dotnet build/pack -c Release from src/)
affects: [phase-06-beta-release, nuget-publish]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Verify-against-source before writing any example (every README token traced to RESEARCH ## Current Public API)"
    - "Concise-in-README, deep-in-docs (D-04): README summarizes, docs/Security.md carries depth"
    - "Absolute raw HTTPS logo URL so nuget.org renders it; local icon.png stays the PackageIcon"

key-files:
  created: []
  modified:
    - README.md

key-decisions:
  - "Logo uses the absolute raw URL https://raw.githubusercontent.com/existall/SimpleSettings/master/icon.png (nuget.org renders only absolute HTTPS images); <PackageIcon> keeps packing local icon.png"
  - "Quickstart call written as SettingsBuilder.CreateBuilder() on one line so the DOC-VERIFICATION grep token stays contiguous"
  - "Migration section names versions v1 -> v2, never the legacy product token, to keep the SimpleConfig sweep at 0 hits"

patterns-established:
  - "Pattern 1: DOC-VERIFICATION grep gates + dotnet build/pack are the phase gate (no unit tests for a docs phase)"
  - "Pattern 2: README carries the concise guarantee + caveats; docs/Security.md is the single deep target both README sections deep-link into"

requirements-completed: [DOC-01]

coverage:
  - id: D1
    description: "README structural rewrite — canonical logo, dropped legacy title tag, three dotnet add package install lines, 7-entry ToC repointed to existall/SimpleSettings docs (incl. renamed extension page + Security), verified [SettingsProperty(DefaultValue = ...)] quickstart, feature overview, DI snippet"
    requirement: "DOC-01"
    verification:
      - kind: other
        ref: "Task 1 automated gate: grep counts (raw.githubusercontent/existall/SimpleSettings>=1, dotnet add package x3, SettingsProperty(DefaultValue>=1, SettingsBuilder.CreateBuilder>=1, AddSimpleSettings>=1, Install-Package==0, [DefaultValue==0, existall/Shepherd==0)"
        status: pass
    human_judgment: false
  - id: D2
    description: "Concise README Security notes (value-free exception invariant WITH both carve-outs — author ValidationError text + DI validator constructors) and v1->v2 migration list (API-01/PKG-01/PKG-02/EXC-01), each linking into docs/Security.md"
    requirement: "DOC-01"
    verification:
      - kind: other
        ref: "Gate 11 presence checks: '## Security notes', '## Breaking changes / migration', SimpleSettingsException, Core.AspNet all present in README"
        status: pass
    human_judgment: false
  - id: D3
    description: "Phase-final DOC-VERIFICATION sweep — 13 gates (7 negative legacy greps + canonical-URL + install verbs + API tokens + guidance presence + docs-file existence) plus dotnet build -c Release and dotnet pack -c Release from src/"
    requirement: "DOC-01"
    verification:
      - kind: other
        ref: "Gates 1-12 grep sweep (all expected counts) + Gate 13 dotnet build/pack exit 0 (three .nupkg repacked with refreshed README + icon + Description)"
        status: pass
    human_judgment: false
  - id: D4
    description: "No README example echoes a bound value/secret; the redaction note is not over-claimed (paired with both caveats)"
    verification:
      - kind: manual_procedural
        ref: "Executor security-auditor pass: quickstart/DI examples use only non-secret placeholders (a URL, an int); redaction invariant stated with both carve-outs"
        status: pass
    human_judgment: true
    rationale: "Secret-leak / over-claim judgment on documentation prose is a human-judgment security check, not mechanically decidable by grep"

# Metrics
duration: 2min
completed: 2026-07-20
status: complete
---

# Phase 5 Plan 04: README Rewrite + Phase-Final Gate Summary

**Full README.md rewrite against the source-verified API (canonical logo/title, dotnet-add install x3, 7-entry ToC, correct [SettingsProperty(DefaultValue = …)] quickstart, DI snippet, concise Security + v1->v2 migration sections) with all 13 DOC-VERIFICATION gates and dotnet build/pack green.**

## Performance

- **Duration:** 2 min
- **Started:** 2026-07-20T09:22:29Z
- **Completed:** 2026-07-20T09:25:05Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Rewrote README.md end-to-end: replaced the broken existall/Shepherd logo with the canonical absolute raw icon URL, dropped the legacy product tag from the title, swapped the legacy PMC install for three `dotnet add package` lines, repointed all ToC entries to existall/SimpleSettings docs and added a "Security & Behavior" entry.
- Replaced the factually-wrong `[DefaultValue("SomeUrl")]` example with the verified `[SettingsProperty(DefaultValue = …)]` quickstart, trimmed the IOptions polemic to a tight blurb, and added a feature overview + DI snippet (`AddSimpleSettings` + `serviceProvider.ValidateSimpleSettings()`).
- Appended concise "Security notes" (value-free exception invariant paired with both carve-outs) and "Breaking changes / migration (v1 -> v2)" (API-01/PKG-01/PKG-02/EXC-01) sections, each deep-linking into docs/Security.md.
- Ran the phase-final gate: all 13 DOC-VERIFICATION grep gates return their expected counts, and `dotnet build -c Release` + `dotnet pack -c Release` from `src/` exit 0 — the three packages repacked cleanly with the refreshed README + icon + Description.

## Task Commits

Each task was committed atomically:

1. **Task 1: Structural README rewrite (logo, title, install, ToC, quickstart, positioning)** - `381c54f` (docs)
2. **Task 2: Concise Security notes + Migration sections; phase-final grep sweep and pack** - `e706f0b` (docs)

**Plan metadata:** committed with SUMMARY.md + STATE.md + ROADMAP.md (docs: complete plan)

## Files Created/Modified
- `README.md` - Full rewrite: canonical logo/title, dotnet-add install x3, 7-entry ToC repointed to existall/SimpleSettings, verified quickstart, feature overview, DI snippet, concise Security notes + v1->v2 migration sections.

## Decisions Made
- Logo uses the absolute raw URL `https://raw.githubusercontent.com/existall/SimpleSettings/master/icon.png` (nuget.org renders only absolute HTTPS image URLs); `<PackageIcon>` in Directory.Build.props still packs the local `icon.png` — untouched.
- Quickstart call written as `SettingsBuilder.CreateBuilder()` on a single line so the `SettingsBuilder.CreateBuilder` DOC-VERIFICATION grep token stays contiguous (see Issues Encountered).
- Migration section names versions as "v1 -> v2" and never the legacy product token, keeping the case-insensitive `\bSimpleConfig\b` sweep at 0 hits.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- The RESEARCH quickstart example formats the builder call fluently across three lines (`SettingsBuilder` / `.CreateBuilder()` / `.GetSettings<>()`). Copied verbatim, that split the `SettingsBuilder.CreateBuilder` token across a line break, so the Task 1 grep gate (`grep -c "SettingsBuilder.CreateBuilder"`) returned 0. Resolved by placing `SettingsBuilder.CreateBuilder()` on one line (still a faithful fluent call) — gate then passed. Not a deviation: the required token is present and the code is equivalent.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 5 (Documentation) plan set is complete: README + docs/ + Directory.Build.props are canonical, and the packaged README + icon + Description repack cleanly. Ready for the Phase 5 verify/ship gate and the Phase 6 beta release cut.
- No blockers.

## Self-Check: PASSED

- README.md — FOUND
- .planning/phases/05-documentation/05-04-SUMMARY.md — FOUND
- Commit 381c54f (Task 1) — FOUND
- Commit e706f0b (Task 2) — FOUND

---
*Phase: 05-documentation*
*Completed: 2026-07-20*
