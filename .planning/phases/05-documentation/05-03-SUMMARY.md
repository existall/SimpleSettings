---
phase: 05-documentation
plan: 03
subsystem: docs
tags: [documentation, security, redaction, validation, migration, breaking-changes]

# Dependency graph
requires:
  - phase: 04
    provides: secret-redaction invariant + validation engine + opt-in DI validation (the guarantees this page documents)
  - phase: 03
    provides: public-surface / packaging breaking changes (API-01, PKG-01, PKG-02, EXC-01) documented in the migration section
provides:
  - "docs/Security.md — deep Security & Behavior guarantees page (five invariants with caveats)"
  - "docs/Security.md #migration — v1 -> v2 breaking-change list"
  - "Stable README deep-link target for the concise Security notes + Breaking changes sections (05-04 links here)"
affects: [05-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Concise-in-README, deep-in-docs (D-04): the security/behavior contract lives in a dedicated docs/Security.md the README summarizes"
    - "Verify-against-source: every API token copied from 05-RESEARCH ## Current Public API, never paraphrased"

key-files:
  created:
    - docs/Security.md
  modified: []

key-decisions:
  - "Placed the deep security/behavior guidance in a dedicated new docs/Security.md (Claude's-discretion placement, RESEARCH Open Question 1 RESOLVED) rather than the renamed extension guide — keeps the security contract separable and gives the README a stable deep-link target"
  - "Stated the redaction invariant WITH both carve-outs (author ValidationError text + DI validator constructors) and did not over-claim (RESEARCH Pitfall 4 / SECURITY.md:75-78)"

patterns-established:
  - "Pattern: pair every security invariant headline with its explicit carve-outs so a reader never relies on a guarantee the library does not make"

requirements-completed: [DOC-01]

coverage:
  - id: D1
    description: "docs/Security.md documents the secret-redaction value-free invariant with both carve-outs (author ValidationError text + DI validator constructors)"
    requirement: "DOC-01"
    verification:
      - kind: other
        ref: "grep -Eci 'value-free|redact' docs/Security.md >= 1 (=7); grep -ci 'constructor' docs/Security.md >= 1 (=3)"
        status: pass
    human_judgment: true
    rationale: "Security-auditor pass required: the redaction section must not over-claim and the validator example must echo no bound value — a judgment the grep gates cannot make"
  - id: D2
    description: "docs/Security.md documents opt-in/deferred ValidateSimpleSettings() on IServiceProvider after BuildServiceProvider(), validate=>discoverable coupling, and AddCommandLine spaced-value binding with the prefix-lookahead caveat"
    requirement: "DOC-01"
    verification:
      - kind: other
        ref: "grep -c ValidateSimpleSettings/IServiceProvider/BuildServiceProvider/'SettingsSection(ValidatorType'/AddCommandLine docs/Security.md — each >= 1"
        status: pass
    human_judgment: false
  - id: D3
    description: "docs/Security.md ends with a v1 -> v2 migration section covering all four Phase-3 breaking changes (API-01, PKG-01, PKG-02, EXC-01), carrying no legacy product name"
    requirement: "DOC-01"
    verification:
      - kind: other
        ref: "grep -c SettingsHolder/Core.AspNet/SimpleSettingsException docs/Security.md each >= 1; grep -riEc '\\bSimpleConfig\\b' docs/Security.md == 0"
        status: pass
    human_judgment: false

# Metrics
duration: 4min
completed: 2026-07-20
status: complete
---

# Phase 5 Plan 3: Security & Behavior Guidance Page Summary

**New docs/Security.md documenting the five Phase 1–4 security/behavior invariants with their exact caveats plus the v1 -> v2 breaking-change list — every API token verified against source, no bound value in any example, no legacy product name.**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-07-20T09:16:21Z
- **Completed:** 2026-07-20T09:20:00Z
- **Tasks:** 2
- **Files modified:** 1 (created)

## Accomplishments
- Authored `docs/Security.md` with five security/behavior subsections: secret redaction (value-free exceptions), validators must not echo secrets, opt-in/deferred DI validation, validators make a type discoverable, command-line values with spaces
- Stated the redaction invariant with BOTH carve-outs (author `ValidationError` text + DI validator constructors) without over-claiming — sourced from SECURITY.md:24-27 and 75-78
- Included the verified secret-safe validator example and the `ValidateSimpleSettings()`-after-`BuildServiceProvider()` DI snippet on the correct `IServiceProvider` receiver
- Appended a concise v1 -> v2 migration section covering all four Phase-3 breaking changes (SettingsHolder internal, Core.AspNet dropped, per-TFM Microsoft.Extensions floor, public SimpleSettingsException base)

## Task Commits

Each task was committed atomically:

1. **Task 1: Author the Security & Behavior guarantees section** - `1764530` (docs)
2. **Task 2: Append the v1 -> v2 migration / breaking-change section** - `c4fa856` (docs)

## Files Created/Modified
- `docs/Security.md` - New deep-guidance page: five security/behavior invariants with caveats + v1 -> v2 migration section

## Decisions Made
- Placed the deep guidance in a dedicated new `docs/Security.md` rather than the renamed extension guide (RESEARCH Open Question 1, resolved to 05-03) — keeps the security contract separable and gives 05-04's README a stable deep-link target
- Kept every API token copied verbatim from 05-RESEARCH ## Current Public API and the secret-safe validator/DI snippets from ## Code Examples — no loose paraphrase, no over-claim

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- `docs/Security.md` is a stable deep-link target ready for 05-04 to reference from the README's concise Security notes + Breaking changes / migration sections (anchor `#migration`).
- Recommended: a light security-auditor pass on the finished page (coverage D1 flagged `human_judgment: true`) confirming the redaction caveats are not over-claimed and no example echoes a bound value — the automated grep gates cannot make that judgment.

---
*Phase: 05-documentation*
*Completed: 2026-07-20*

## Self-Check: PASSED
- docs/Security.md — FOUND
- .planning/phases/05-documentation/05-03-SUMMARY.md — FOUND
- Commit 1764530 (Task 1) — FOUND
- Commit c4fa856 (Task 2) — FOUND
