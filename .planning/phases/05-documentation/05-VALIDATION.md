---
phase: 5
slug: documentation
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-07-19
---

# Phase 5 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> This is a **documentation** phase — validation is framed as falsifiable **DOC-VERIFICATION gates**
> (shell grep/assertions + `dotnet build`/`pack`), not unit tests. Source: `05-RESEARCH.md` ## Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Shell `grep`/`rg` assertions + `dotnet build` / `dotnet pack` (no unit-test framework needed) |
| **Config file** | none — assertions are inline commands |
| **Quick run command** | the relevant grep gate(s) for the edited file (sub-second) |
| **Full suite command** | full grep-gate sweep + `dotnet build -c Release` + `dotnet pack -c Release` (both from `src/`) |
| **Estimated runtime** | grep gates ~1s; `build`+`pack` ~30–90s |

*Run grep gates from the repo root; run `dotnet` from `src/` (global.json → Microsoft.Testing.Platform).*

---

## Sampling Rate

- **After every task commit:** Run the grep gate(s) for the file just edited.
- **After every plan wave:** Run the full grep-gate sweep.
- **Before `/gsd-verify-work`:** Full grep sweep returns every expected count AND `dotnet build`/`pack` from `src/` succeed (README + icon + `<Description>` repack cleanly).
- **Max feedback latency:** ~90 seconds (build/pack); grep gates are sub-second.

---

## Per-Requirement Verification Map

Single phase requirement (**DOC-01**); the 13 gates below are its acceptance sweep. Per-task rows are finalized against the plan's tasks at execution — every task's `<automated>` verify must be one (or more) of these gates.

| # | Requirement | Behavior | Check type | Automated command (expect stated result) | Status |
|---|-------------|----------|-----------|-------------------------------------------|--------|
| 1 | DOC-01 | No legacy repo links | grep gate | `grep -rn "existall/SimpleConfig" README.md docs/ src/Directory.Build.props` → **0 hits** | ⬜ pending |
| 2 | DOC-01 | No standalone `SimpleConfig` product/tag refs | grep gate | `grep -rniE "\bSimpleConfig\b" README.md docs/ src/Directory.Build.props` → **0 hits** | ⬜ pending |
| 3 | DOC-01 | No legacy PMC install command | grep gate | `grep -rn "Install-Package" README.md docs/` → **0 hits** | ⬜ pending |
| 4 | DOC-01 | No stale attribute form | grep gate | `grep -rn "\[DefaultValue" README.md docs/` → **0 hits** | ⬜ pending |
| 5 | DOC-01 | No broken Shepherd logo | grep gate | `grep -rn "existall/Shepherd" README.md src/Directory.Build.props` → **0 hits** | ⬜ pending |
| 6 | DOC-01 | `<Description>` typo fixed | grep gate | `grep -n "appliaction" src/Directory.Build.props` → **0 hits** | ⬜ pending |
| 7 | DOC-01 | Renamed doc file — no dangling links | grep gate | `grep -rn "Extend%20Simple%20Config" README.md docs/` → **0 hits** AND the renamed file exists | ⬜ pending |
| 8 | DOC-01 | Canonical repo URL present | grep gate | `grep -rn "existall/SimpleSettings" README.md docs/` → **≥1 hit per doc that links out** | ⬜ pending |
| 9 | DOC-01 | Three package IDs + correct install verb | token match | README contains `dotnet add package ExistForAll.SimpleSettings`, `...Binders`, `...Extensions.GenericHost` | ⬜ pending |
| 10 | DOC-01 | Examples use real API tokens | token match | README/docs contain `[SettingsProperty(DefaultValue`, `SettingsBuilder.CreateBuilder`, `AddSimpleSettings`; `ValidateSimpleSettings()` shown on an `IServiceProvider` | ⬜ pending |
| 11 | DOC-01 | Mandated Phase 1–4 guidance present | presence check | README + docs together cover: secret-redaction; validator secret-safety (incl. constructor); opt-in/deferred `ValidateSimpleSettings`; validate⇒discoverable; `AddCommandLine` spaced values; Phase-3 breaking-change list (API-01/PKG-01/PKG-02/EXC-01) | ⬜ pending |
| 12 | DOC-01 | Internal doc links resolve | link check | every relative / `existall/SimpleSettings/blob/master/docs/<file>` link resolves to an existing `docs/` file | ⬜ pending |
| 13 | DOC-01 | Package content still builds/packs | build gate | from `src/`: `dotnet build -c Release` and `dotnet pack -c Release` succeed | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- No test-file gaps — validation is grep/build-based; no fixtures or framework install needed.
- *(Discretionary)* A CI legacy-reference + dead-link check could wrap gates 1–8/12; D-05 marks it nice-to-have, not required. If wired as a script, place it under a scratch/CI path — do not add runtime code.

*Existing infrastructure (grep + the existing `dotnet build`/`pack`) covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| No example echoes a bound value / secret; validator examples never log injected secrets | DOC-01 | Semantic — grep can't judge whether an example is secret-safe | Finished-docs review (light `security-auditor`/`code-reviewer` pass): read every code example in README + docs/; confirm none prints/echoes a bound value or an injected secret in a ctor/`ValidationError` |
| The six mandated guidance items are present AND correctly caveated (not over-claimed) | DOC-01 | Presence + correctness of caveats needs human/semantic read (gate 11 checks presence only) | Confirm the redaction section states its two caveats (author `ValidationError` text + validator constructors are outside the value-free guard) per SECURITY.md:75-78 |

---

## Validation Sign-Off

- [x] All requirement checks have an `<automated>` grep/build gate (11 automated) or a documented manual verification (2 semantic)
- [x] Sampling continuity: every edited-file commit has a relevant grep gate; no gap
- [x] Wave 0 covers all MISSING references (none — grep/build-based)
- [x] No watch-mode flags
- [x] Feedback latency < 90s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-07-19 (docs phase — grep/build DOC-VERIFICATION gates from 05-RESEARCH.md)
