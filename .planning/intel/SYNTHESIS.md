# Synthesis Summary

_Entry point for `gsd-roadmapper`. Produced by gsd-doc-synthesizer. Mode: new._

## Doc counts by type
- SPEC: 1 (FIX-PLAN.md, precedence 0 — authoritative planning source)
- DOC: 8 (SESSION-HANDOFF.md=prec 1, README.md=prec 2, six docs/*.md=prec 3)
- ADR: 0
- PRD: 0
- Total: 9 (all `high` confidence; none UNKNOWN/low)

## Decisions
- ADR-locked decisions: **0** (no ADR-type docs; no precedence-lock in effect).
- Settled decisions captured from SPEC/DOC (not ADR-locked, treat as settled): 7 —
  DEC-s1-exception-invariant, DEC-c3-provider-cache, DEC-benchmark-gate-allocations,
  DEC-existforall-naming, DEC-m1-generated-names, DEC-pre-stable-window, DEC-d1-d2-held.
- File: decisions.md

## Requirements
- PRD-derived requirements: **0** (no PRD-type docs).
- Candidate requirements derived from the authoritative SPEC (open work items): 14 —
  REQ-secret-redaction (shipped/in-flight), REQ-public-exception-base, REQ-list-collection-support,
  REQ-aot-trim-strategy, REQ-aspnet-public-type, REQ-dependency-floor-per-tfm, REQ-hide-settingsholder,
  REQ-commandline-quoted-parsing, REQ-valuespopulator-tests, REQ-typeconverter-tests,
  REQ-converter-tests, REQ-generator-concurrency-tests, REQ-validations-feature (held),
  REQ-equalitycomparer-decision (held), plus REQ-readme-link-refresh.
- File: requirements.md
- Note: completed work (B1/B2/B4/B5/B9, P0–P5, Q1–Q5, A2/D3, C3) is recorded as delivered
  capability in constraints.md / context.md, not re-listed as requirements.

## Constraints (SPEC-derived)
- Total: 9 — CON-secret-redaction-invariant (nfr), CON-allocation-gate (nfr),
  CON-provider-cache-semantics (protocol), CON-target-frameworks (nfr), CON-aot-trim-unsupported (nfr),
  CON-pre-stable-breaking-window (protocol), CON-canonical-naming (schema),
  CON-versioning-release (protocol), CON-converter-chain (api-contract).
- Type breakdown: nfr 4, protocol 3, schema 1, api-contract 1.
- File: constraints.md

## Context topics (DOC-derived)
- 11 topics: what/why, getting started, DI/generic-host, building the collection, declaring
  interfaces, default values/SettingsProperty options, section binders, extending (SettingsOptions),
  current project state, ranked next priorities, working gotchas.
- File: context.md

## Conflicts
- Blockers: 0
- Competing variants: 0
- Auto-resolved (INFO): 2 (canonical-naming stale README; benign FIX-PLAN<->SESSION-HANDOFF cross-ref cycle)
- Detail: ../INGEST-CONFLICTS.md

## Per-type intel files
- decisions.md, requirements.md, constraints.md, context.md (all in this directory)

## Status
READY — no blockers, no competing variants. Safe to route to gsd-roadmapper.
Roadmapper note: the authoritative planning content is the SPEC (FIX-PLAN.md); requirements.md holds
SPEC-derived candidate requirements (no user-ratified PRD exists), and the "held" items (D1/D2) plus
completed work should be treated per constraints.md/decisions.md.
