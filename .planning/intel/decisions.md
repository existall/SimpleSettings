# Decisions (Intel)

_Synthesized by gsd-doc-synthesizer. Provenance is tracked per entry via `source:`._

## ADR-type decisions

**None ingested.** No ADR-type documents were present in the ingest set. Therefore no
precedence-locked (`locked: true`) decisions exist, and no LOCKED-vs-LOCKED evaluation applies.

## Decisions captured from higher-precedence sources (SPEC / DOC)

The following are durable, already-made engineering decisions recorded in the authoritative
planning SPEC (`FIX-PLAN.md`, precedence 0) and the running handoff (`SESSION-HANDOFF.md`,
precedence 1). They are NOT ADR-precedence-locked, but downstream (`gsd-roadmapper`) should treat
them as settled unless the user reopens them. Each is a decision, not open work.

### DEC-s1-exception-invariant — Secret-safe exception invariant
`SettingsPropertyValueException` deliberately **never carries the bound value and never chains a
framework inner exception** (both can embed secrets). The value-free "required value missing" case
is a distinct `SettingsPropertyNullException` that keeps its full message. A future opt-in
"full diagnostics" knob to restore value/inner was **explicitly rejected** as insecure-by-configuration.
- status: settled (shipped on branch `security/s1-redact-exception-value`, PR open)
- scope: exception hierarchy, secret redaction, ValuesPopulator throw sites
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (§S1)
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (Key decisions)

### DEC-c3-provider-cache — Provider-vs-singleton semantics
C3 resolved as **option 2**: cache the built instance in the provider only; Core
`SettingsBuilder.GetSettings` unchanged; no reload path added. `IOptionsMonitor`-style reload is
deferred as a future "option 3".
- status: settled (implemented via P1, #17)
- scope: DI / Generic-Host integration, ISettingsProvider
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (C3, P1)
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (Key decisions)

### DEC-benchmark-gate-allocations — CI gates on allocations, not time
The benchmark-tracking CI gates PRs on **allocated bytes** (deterministic), not wall-clock time
(informational only). Baseline lives on the `gh-pages` branch (`dev/bench/`).
- status: settled (merged #22)
- scope: benchmark harness, CI allocation gating
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (P0, progress)
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (Key decisions)

### DEC-existforall-naming — Canonical naming: ExistForAll.SimpleSettings
A2 consolidated three repo-wide spellings (`ExistAll`, `ExistsForAll`, `ExistForAll`) onto the
single canonical `ExistForAll`. The package was renamed from the legacy `SimpleConfig` to
`ExistForAll.SimpleSettings`.
- status: settled (merged #15; D3 namespace typo fixed #11)
- scope: namespace consolidation, package identity
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (A2, D3)

### DEC-m1-generated-names — Keep generated impl name separate from section name
The generated implementation type name (in `SettingsClassGenerator`) is namespace-qualified and
must stay **separate** from `GetNormalizeInterfaceName`, which drives the default config section
name. Do not merge them.
- status: settled (merged #21)
- scope: SettingsClassGenerator, section-name formatting
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (Key decisions)

### DEC-pre-stable-window — Breaking changes free until first v2.0.0-beta
No stable `v*` tag exists (only auto-alphas of `2.0.0-alpha.*`). Breaking changes (dead API removal,
namespace renames, exception reparenting) are free and should be batched **before** cutting the
first `v2.0.0-beta`.
- status: settled (project state)
- scope: versioning / release, breaking cleanups
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (How to work this plan)
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (TL;DR, Key decisions)

### DEC-d1-d2-held — Validations and EqualityCompererCreator are HELD (do NOT delete)
D1 (`Validations/*` API + `SettingsPropertyAttribute.ValidatorType`) and D2
(`EqualityCompererCreator`) are dead code but intentionally **held** for coming feature work;
D1 is to be reconciled with the `validate-settings` branch.
- status: settled (hold decision)
- scope: Validations API, EqualityCompererCreator
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (D1, D2)
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (Key decisions)
