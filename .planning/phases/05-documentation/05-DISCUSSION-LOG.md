# Phase 5: Documentation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-19
**Phase:** 05-documentation (renamed from "AOT/Trim Honesty & Documentation")
**Areas discussed:** Gray-area selection, AOT-01 deferral, README depth, Guidance placement, docs/ pass, Metadata scope

---

## Gray-area selection

| Option | Description | Selected |
|--------|-------------|----------|
| AOT honesty strength | How far on annotate vs document vs full analyzer sweep | |
| README depth | Link/naming fix vs substantive refresh + mandated guidance | ✓ |
| docs/ + metadata | Sweep docs/ + csproj/logo, or README-only | ✓ |
| AOT verification proof | CI AOT sample vs local smoke-check vs annotations-present | |

**User's choice:** README depth + docs/ + metadata. Free-text: "lets move aot to future plans like 2.1".
**Notes:** The free-text directive removed AOT-01 from Phase 5 entirely — handled as a scope change (below), not a discussion area.

---

## AOT-01 deferral

| Option | Description | Selected |
|--------|-------------|----------|
| v2.1 milestone (post-beta) | AOT ships in a future v2.1 cycle, after the first v2.0.0-beta; 'before stable' wording relaxes | ✓ |
| New late phase, this milestone | AOT gets its own phase after the beta but still inside v2.0.0 | |
| Backlog, no version yet | Record deferred/unscheduled; pick target later | |

**User's choice:** v2.1 milestone (post-beta).
**Notes:** Surfaced the trade-off — `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` are additive, non-breaking attributes, so deferring past the beta (even past stable) is low-risk. Formalized in ROADMAP.md (Phase 5 renamed "Documentation", AOT note) + REQUIREMENTS.md (AOT-01 → v2 Deferred; traceability + coverage updated; Phase 4/API-02 marked complete while there).

---

## README depth

| Option | Description | Selected |
|--------|-------------|----------|
| Modernize, self-contained | Modern OSS shape: install, quickstart, correct example, feature overview, links to docs/; trim IOptions polemic | ✓ |
| Refresh in place | Only fix naming, dead links, logo, install cmd, stale example | |
| Full inline rewrite | Document all features inline; docs/ becomes secondary | |

**User's choice:** Modernize, self-contained.
**Notes:** README ships to nuget.org via `PackageReadmeFile`, so it's the consumer-facing front door.

---

## Guidance placement

| Option | Description | Selected |
|--------|-------------|----------|
| Concise README + detail in docs/ | Tight Security + Breaking-changes/migration sections in README; deep detail in docs/ | ✓ |
| All in README | Every note fully inline | |
| docs/ only, README links out | Keep README lean; all guidance in a docs/ page | |

**User's choice:** Concise README + detail in docs/.
**Notes:** Confirms docs/ stays and must become canonical (README links into it; deep mandated guidance lives there).

---

## docs/ pass

| Option | Description | Selected |
|--------|-------------|----------|
| Refresh all 6 in place | Fix legacy naming/links, rename 'Extend Simple Config.md', correct stale API, add security/migration content | ✓ |
| Restructure the docs tree | Reorganize into a cleaner set; rewrite/merge old pages | |
| Minimal — only what README links | Only repoint linked pages + add the mandated page | |

**User's choice:** Refresh all 6 in place.
**Notes:** Preserves existing page structure/history.

---

## Metadata scope

| Option | Description | Selected |
|--------|-------------|----------|
| Fix metadata + logo too | Correct csproj `<Description>` typos, canonicalize/replace logo, verify repo/package URLs | ✓ |
| Naming/links only | Fix only strict naming/links; leave typos | |
| README + docs/ only | Treat csproj/package metadata as separate | |

**User's choice:** Fix metadata + logo too.
**Notes:** All of it is consumer-facing "canonical naming/links" surface.

---

## Claude's Discretion

- Exact README section ordering/wording; how far to trim the IOptions polemic.
- Canonical name for the renamed `Extend Simple Config.md`; whether migration is a README subsection vs a dedicated docs/ page; which docs/ page holds the deep security guidance.
- Whether to add a `CHANGELOG.md`; whether to add a CI legacy-reference / dead-link check.

## Deferred Ideas

- **AOT-01** → future v2.1 milestone (recorded in ROADMAP + REQUIREMENTS).
- **Validator-dispatch caching** (perf, code-review M2) — stays deferred; a code change, not docs.
- **REL-01** → Phase 6.
