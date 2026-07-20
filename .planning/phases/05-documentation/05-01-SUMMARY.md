---
phase: 05-documentation
plan: 01
subsystem: docs
tags: [documentation, markdown, links, legacy-purge]

# Dependency graph
requires:
  - phase: 04-collection-validation
    provides: canonical library naming already applied across five of six docs pages
provides:
  - Canonical extension-guide filename (docs/Extending SimpleSettings.md) with git history preserved
  - All five in-docs inbound links repointed to the renamed page on existall/SimpleSettings
  - docs/ tree free of legacy product name (SimpleConfig) and dead legacy-repo links
affects: [05-04, README repoint, phase-final legacy sweep]

# Tech tracking
tech-stack:
  added: []
  patterns: [Rename-with-backlink-repoint (git mv + same-plan inbound-link repoint)]

key-files:
  created: []
  modified:
    - docs/Extending SimpleSettings.md (renamed from docs/Extend Simple Config.md via git mv)
    - docs/getting_started.md
    - docs/building_the_collection.md
    - docs/Build a SectionBinder.md
    - docs/Build Config Interface.md

key-decisions:
  - "Renamed via git mv (not delete+create) so page history is preserved and the new filename exists before any link is repointed at it (interface-first)."
  - "README.md ToC entry intentionally NOT touched here — it is repointed in 05-04 as part of the cross-file legacy sweep."

patterns-established:
  - "Rename-with-backlink-repoint: establish the canonical target with git mv first, then repoint every inbound link in the same plan so no dangling link is ever committed."

requirements-completed: [DOC-01]

coverage:
  - id: D1
    description: "Extension guide reachable at canonical space-encoded filename docs/Extending SimpleSettings.md with git history preserved and no legacy product name in its content"
    requirement: DOC-01
    verification:
      - kind: automated_ui
        ref: "test -f 'docs/Extending SimpleSettings.md' && test ! -f 'docs/Extend Simple Config.md' && git ls-files --error-unmatch 'docs/Extending SimpleSettings.md'"
        status: pass
      - kind: other
        ref: "grep -rniE '\\bSimpleConfig\\b' 'docs/Extending SimpleSettings.md' -> 0 hits"
        status: pass
    human_judgment: false
  - id: D2
    description: "All five in-docs inbound links repointed to Extending%20SimpleSettings.md and residual legacy parenthetical removed from getting_started.md; docs/ carries no SimpleConfig name or dead legacy-repo link"
    requirement: DOC-01
    verification:
      - kind: other
        ref: "grep -rn 'Extend%20Simple%20Config' docs/ -> 0; grep -rniE '\\bSimpleConfig\\b' docs/ -> 0; grep -rn 'existall/SimpleConfig' docs/ -> 0; grep -rn 'Extending%20SimpleSettings.md' docs/ -> 5"
        status: pass
    human_judgment: false

# Metrics
duration: 5min
completed: 2026-07-20
status: complete
---

# Phase 5 Plan 01: Docs Canonicalization Summary

**Renamed the legacy-named extension guide to `docs/Extending SimpleSettings.md` (history preserved), repointed all five in-docs inbound links, and purged the last `SimpleConfig` parenthetical from getting_started.md — docs/ is now legacy-name-free.**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-07-20
- **Completed:** 2026-07-20
- **Tasks:** 2
- **Files modified:** 5 (1 renamed, 4 edited)

## Accomplishments
- `git mv "docs/Extend Simple Config.md" "docs/Extending SimpleSettings.md"` — canonical filename established, git history preserved (100% rename similarity).
- Repointed all five in-docs inbound links to `Extending%20SimpleSettings.md` (SectionBinder x2, building_the_collection x2, Build Config Interface x1); host segment left untouched (already `existall/SimpleSettings`).
- Removed the residual `(previously SimpleConfig)` parenthetical from getting_started.md line 4.
- Verified docs/ has 0 references to the old filename, 0 `SimpleConfig` occurrences, 0 dead `existall/SimpleConfig` links, and 5 links to the new page.

## Task Commits

Each task was committed atomically:

1. **Task 1: Rename the extension guide to a canonical filename** - `80aa1b5` (docs, git mv rename)
2. **Task 2: Repoint in-docs inbound links + drop residual legacy parenthetical** - `f05f635` (docs)

## Files Created/Modified
- `docs/Extending SimpleSettings.md` - Renamed from `docs/Extend Simple Config.md` (git mv, content unchanged — already canonical)
- `docs/getting_started.md` - Removed `(previously SimpleConfig)` parenthetical (line 4)
- `docs/building_the_collection.md` - Repointed 2 extension-page links
- `docs/Build a SectionBinder.md` - Repointed 2 extension-page links
- `docs/Build Config Interface.md` - Repointed 1 extension-page link

## Decisions Made
- None beyond plan — executed as specified. Rename via `git mv` to preserve history; README ToC entry deferred to 05-04 per plan.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- After `git mv`, the initial staged-commit attempt used the old pathspec (`git add "docs/Extend Simple Config.md"`), which no longer exists on disk and errored. The rename was already staged by `git mv`, so committing the staged change directly resolved it. No content impact.

## Threat Surface
- T-05-01 (Tampering / docs link integrity) mitigated as planned: rename + all inbound repoints landed together; acceptance gate confirms zero references to the old filename and that the new file exists. No new security-relevant surface introduced.

## User Setup Required
None - documentation-only, no external service configuration required.

## Next Phase Readiness
- Canonical extension-page URL (`docs/Extending%20SimpleSettings.md`) is stable for the README repoint in 05-04.
- The cross-file legacy sweep spanning README + props remains the phase-final gate in 05-04 (out of scope here).

## Self-Check: PASSED

- FOUND: docs/Extending SimpleSettings.md
- FOUND commit 80aa1b5 (Task 1 rename)
- FOUND commit f05f635 (Task 2 repoints)

---
*Phase: 05-documentation*
*Completed: 2026-07-20*
