---
phase: 2
slug: binding-correctness-engine-test-hardening
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-07-14
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. This is a test-only phase
> (no production code beyond fixtures), so every requirement is validated by the tests it adds.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | TUnit 1.58.0 on Microsoft.Testing.Platform (opted in via `src/global.json`) |
| **Config file** | `src/global.json`; `src/Tests/ExistForAll.SimpleSettings.UnitTests/ExistForAll.SimpleSettings.UnitTests.csproj` |
| **Quick run command** | `dotnet test src/Tests/ExistForAll.SimpleSettings.UnitTests --framework net10.0` (net10 installed locally) |
| **Full suite command** | `dotnet test src/ExistForAll.SimpleSettings.slnx` (CI runs net8.0 + net10.0) |
| **Estimated runtime** | ~10–20 s (unit suite) |

---

## Sampling Rate

- **After every task commit:** Run the quick run command (the new test file, net10)
- **After every plan wave:** Run the full suite command
- **Before `/gsd-verify-work`:** Full suite green on net10 (CI confirms net8 parity)
- **Max feedback latency:** ~20 s

---

## Per-Task Verification Map

> Populated by the planner/executor once plan + task IDs exist. Each new test task maps to its
> requirement and asserts a concrete, automated behavior (see RESEARCH.md `## Validation Architecture`
> for the specific gaps: binder precedence, null→value-type default, `Nullable<int>` strip+convert,
> `ConverterType`-over-collection, scalar `Uri`/`DateTime` positive parse).

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| (pending planner) | — | — | TEST-01/02/03 | — | N/A (test-only) | unit | `dotnet test … --framework net10.0` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] New test files under `src/Tests/ExistForAll.SimpleSettings.UnitTests/` (created by the plan tasks themselves — no separate stub wave)

*Existing infrastructure (TUnit, `[InternalsVisibleTo]` from `Info.cs:3`, existing fixtures) covers all phase requirements; no framework install needed.*

---

## Manual-Only Verifications

*All open phase behaviors have automated verification (unit tests). ENG-01/T7 is already verified by committed concurrency stress tests (#29 — verify-only); COLL-01/C1 is deferred (not in scope).*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 20s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
