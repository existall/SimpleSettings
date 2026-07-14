## Conflict Detection Report

_Mode: new. Precedence: ADR > SPEC > PRD > DOC (per-doc overrides applied: FIX-PLAN.md=0,
SESSION-HANDOFF.md=1, README.md=2, docs/*=3). Ingest set: 1 SPEC + 8 DOC; no ADR, no PRD._

### BLOCKERS (0)

None.

- No ADR-type docs => no LOCKED decisions => no LOCKED-vs-LOCKED contradiction possible.
- Mode is `new` => no existing locked CONTEXT.md to contradict.
- All 9 classifications are `high` confidence => no UNKNOWN/low-confidence blockers.
- Cross-ref cycle check ran; the one detected cycle was assessed non-blocking (see INFO below).

### WARNINGS (0)

None.

- No PRD-type docs => no competing acceptance-criteria variants to preserve.

### INFO (2)

[INFO] Auto-resolved: canonical naming — SPEC/A2 wins over stale DOC references
  Found: README.md prose says "SimpleConfig (previously)" and links to legacy
    `github.com/existall/SimpleConfig/blob/master/docs/*` paths; docs/*.md cross-refs also point at
    `existall/SimpleConfig` and `existall/SimpleSettings` GitHub URLs.
  Note: The authoritative SPEC (FIX-PLAN.md, precedence 0) records A2 as done — canonical name is
    `ExistForAll.SimpleSettings`, legacy `SimpleConfig` retired. DOC (precedence 2/3) < SPEC, so the
    canonical name wins in synthesized intel (CON-canonical-naming in constraints.md). The stale
    README links are not a contradiction of fact, only unrefreshed documentation; captured as
    REQ-readme-link-refresh (requirements.md), matching handoff next-priority #5.
  source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/README.md
  source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (A2, D3)
  source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (Next priorities #5)

[INFO] Cross-ref cycle assessed non-blocking: FIX-PLAN.md <-> SESSION-HANDOFF.md
  Found: A directed 2-node cycle in the cross_refs graph — FIX-PLAN.md references SESSION-HANDOFF.md
    and SESSION-HANDOFF.md references FIX-PLAN.md. (All other cross_refs are external GitHub URLs, not
    nodes in the ingest set, so they form no intra-set edges; DFS three-color marking found exactly
    this one back-edge, traversal depth 2, well under the 50 cap.)
  Note: This is a benign bidirectional documentation link between a plan and its running handoff, not
    a semantic synthesis loop — both documents are content-complete and standalone, and per-type
    extraction is non-recursive (it does not follow refs), so no garbage-producing loop is possible.
    Synthesis proceeded on both docs. Recorded here for transparency rather than raised as a
    BLOCKER, which would gate a fresh bootstrap on a plan/handoff back-reference.
  source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (cross_refs)
  source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (cross_refs)
