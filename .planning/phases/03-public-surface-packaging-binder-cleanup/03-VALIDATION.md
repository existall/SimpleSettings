---
phase: 3
slug: public-surface-packaging-binder-cleanup
status: approved
nyquist_compliant: true
wave_0_complete: true
created: 2026-07-14
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Authored from `03-RESEARCH.md` "## Validation Architecture", with the PKG-02 floor check
> corrected per architect finding A1 (inspect the projects that actually reference the packages,
> not the core lib, and fail on zero `microsoft.extensions.*` entries).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | TUnit 1.58.0 on Microsoft.Testing.Platform |
| **Config file** | `src/global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`) |
| **Quick run command** | `cd src && dotnet test SimpleSettings.slnx -c Release --treenode-filter "/*/*/ArgumentsTests/*"` |
| **Full suite command** | `cd src && dotnet restore SimpleSettings.slnx && dotnet build SimpleSettings.slnx -c Release --no-restore -p:ContinuousIntegrationBuild=true && dotnet test SimpleSettings.slnx -c Release --no-build` |
| **Estimated runtime** | ~30–60 s (net10 local; net8 build-only locally, both TFMs in CI) |

> **TUnit filter caveat:** targeted runs use `--treenode-filter "/*/*/ClassNameTests/*"`. The
> `dotnet test --filter "*Name*"` form is REJECTED by Microsoft.Testing.Platform (exits 5, zero
> tests — looks green at a glance). Run `dotnet` from `src/` (global.json pins the platform).

---

## Sampling Rate

- **After every task commit:** Run the touched task's `<automated>` verify (targeted `--treenode-filter` for test tasks; `grep` + scoped `dotnet build` for source/packaging tasks).
- **After every plan wave:** Run the full suite command on both TFMs (CI runs net8 + net10).
- **Before `/gsd-verify-work`:** Full suite green on both TFMs AND the PKG-02 restore/floor + NuGet-audit assertion green.
- **Max feedback latency:** ~60 seconds.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 03-01-01 | 01 | 1 | API-01 | — | Public-surface reduction (net-positive) | source + build | `grep -q 'internal sealed class SettingsHolder' Core/ExistForAll.SimpleSettings/SettingsHolder.cs && dotnet build Core/ExistForAll.SimpleSettings/…csproj -c Release` | ✅ | ⬜ pending |
| 03-01-02 | 01 | 1 | PKG-01 | — | Removes a dormant, unserviced package id (unlist is owner-manual) | build | `test ! -e …Core.AspNet && test -f Tests/…/Core/AspNet/Environments.cs && ! grep -rq "Core.AspNet" SimpleSettings.slnx …UnitTests.csproj && dotnet restore && dotnet build SimpleSettings.slnx -c Release` | ✅ | ⬜ pending |
| 03-01-03 | 01 | 1 | PKG-02 | T-03-02 | No vulnerable floor: `NU1901`–`NU1904` restore-audit gate | restore + build + assets inspection | `dotnet restore -p:NuGetAudit=true -p:NuGetAuditMode=all` (fail on any `NU190[1-4]`) + `dotnet build … -p:TreatWarningsAsErrors=true` + floor check over **Binders** (`Configuration`) & **GenericHost** (`DependencyInjection.Abstractions`) `project.assets.json`, guard fails on zero `microsoft.extensions.*` per TFM | ✅ | ⬜ pending |
| 03-02-01 | 02 | 2 | SRC-02 | T-03-06 | An unconvertible CLI value never leaks into the exception `ToString()` chain (SEC-01 / S1) | unit (TUnit) | `dotnet test SimpleSettings.slnx -c Release --treenode-filter "/*/*/ArgumentsTests/*"` | ✅ (extended in place) | ⬜ pending |
| 03-02-02 | 02 | 2 | SRC-02 | — | `AddCommandLine()` tokenizes safely; exe-skip owned only by that entry point | source + build | `grep -q 'Environment.GetCommandLineArgs()' …SettingsBuilderFactoryExtensions.cs && grep -Eq 'SkipFirstArgument *= *true' …SettingsBuilderFactoryExtensions.cs && dotnet build SimpleSettings.slnx -c Release` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**SC → task coverage:** SC #1 → 03-01-01 (+ full-suite green); SC #2 → 03-01-02; SC #3 → 03-01-03; SC #4 → 03-02-01 (parse behavior + quoted-value + redaction) & 03-02-02 (`AddCommandLine` end-to-end).

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.* TUnit + Microsoft.Testing.Platform are already present; `Tests/ExistForAll.SimpleSettings.UnitTests/Binders/CommandLine/ArgumentsTests.cs` exists and is extended in place by task 03-02-01 (class name kept stable for the treenode-filter); the PKG-02 restore/floor + audit assertion is added directly inside task 03-01-03. No framework install and no separate test-stub wave are needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Published `ExistForAll.SimpleSettings.Core.AspNet` `2.0.0-alpha.0.*` prereleases are unlisted | PKG-01 (D-02b) | Registry-side action requiring the guy-lud publishing account; not automatable in-repo and owner-optional (not required for phase completion) | `dotnet nuget delete ExistForAll.SimpleSettings.Core.AspNet <version> -s https://api.nuget.org/v3/index.json --non-interactive` under the guy-lud account (unlists — does not delete) |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies — every task (03-01-01…03-02-02) carries a real `<automated>` verify.
- [x] Sampling continuity: no 3 consecutive tasks without automated verify.
- [x] Wave 0 covers all MISSING references — N/A (no MISSING; infrastructure present).
- [x] No watch-mode flags.
- [x] Feedback latency < 60s.
- [x] `nyquist_compliant: true` set in frontmatter.

**Approval:** approved 2026-07-14
