# SESSION HANDOFF — SimpleSettings

_Last updated: 2026-07-12 · owner: Guy Ludvig (guy@frontegg.com)_

## TL;DR
We ran a three-specialist code review (architecture · tests · performance) and have been shipping the fixes as small PRs. **Five PRs merged this session; one open (#13).** Test suite went **78 → 100 green** (net8.0 + net10.0). The full prioritized plan lives in **`FIX-PLAN.md`** (repo root, git-ignored/untracked — read it first for detail).

The repo is still **pre-stable** (no `v*` tag; only auto-alphas published), so **breaking changes are free** — do the breaking cleanup now.

## Current state
- Branch to be on next session: **`master`** (@ `9782f4b`, PR #12). Once #13 merges, `git checkout master && git pull`.
- **Open PR #13** — `chore/rename-solution-to-simplesettings` → renames `src/ExistAll.SimpleConfig.slnx` to `src/SimpleSettings.slnx` + updates the 11 workflow references. Merge it, then continue.
- Working tree carries two uncommitted, intentional-leftover edits (NOT part of any PR): `SESSION-HANDOFF.md` (this file) and `src/SimpleSettings.slnx` (a Solution-Items entry adding SESSION-HANDOFF.md — from the IDE). `FIX-PLAN.md` is untracked. Leave them unless you decide to commit.
- Orphaned remote branches from #8–#12 are still on origin (merged); safe to delete anytime.

## What shipped this session
Merged to `master` (all non-breaking except #11/#13 which are pre-stable breaking):
- **#8** — five correctness bugfixes + regression tests: register `EnumTypeConverter` (enum-from-string was throwing); `InvariantCulture` in `DefaultTypeConverter` (locale parse bug); `BindingContext.PropertyType` = property's type not declaring interface; guard the options validator when `AttributeType` is null; fix two broken `Resources` message templates.
- **#10** — `BindingContext.PropertyType` regression test (re-landed onto master; see stranding note below).
- **#11** — fix the `ExistsForAll` → `ExistForAll` namespace typo in the Binders package (one file was a letter off).
- **#12** — DI integration tests for `AddSimpleSettings` (headline feature, previously 0 coverage); wired the test project to `Extensions.GenericHost` + added `Microsoft.Extensions.DependencyInjection` via CPM.

Open: **#13** (solution rename, above).

## Key decisions & context (carry forward)
- **Validations API (D1) — HELD, do NOT delete.** The whole `Validations/*` namespace + `SettingsPropertyAttribute.ValidatorType` is public but never invoked. Owner intends to **build the validation feature in the near future** — there is already an existing **`validate-settings`** branch on origin to reconcile with. Wire it into `ValuesPopulator` (run validators after binding, aggregate errors, throw typed exception).
- **`EqualityCompererCreator` (D2) — HELD too** by the same logic (internal, dead, and has a latent invalid-IL bug at `EqualityCompererCreator.cs:38`). Delete only if the owner decides value-equality on generated types isn't wanted.
- **Pre-stable window:** no `v*` stable tag exists (the `version-*` tags are the dead legacy `ExistAll.SimpleConfig` package). Breaking API changes are free until the first `v2.0.0-beta`.
- **Stacked-PR lesson:** #9 (a BindingContext test) was stacked on #8's branch and merged *there*; because #8 was squash-merged to master separately, #9 never reached master. Re-landed as #10. **Takeaway:** don't stack PRs on a branch that will be squash-merged — branch off master, or merge the base first.

## Next priorities (ranked — detail in FIX-PLAN.md)
1. **Merge #13**, then optionally finish the `SimpleConfig` naming (A2): rename the Binders *folder* `Core/ExistAll.SimpleConfig.Extensions.Binders/` (updates the `.slnx` project path + test csproj ref), then `docs/*`, `README.md`, `PackageTags`, a benchmark comment.
2. **Validations feature (D1)** — owner-driven; reconcile with the `validate-settings` branch.
3. **Performance (Phase 5):** P0 (benchmark harness — `[MemoryDiagnoser]` + phased scenarios) → **P1 (cache the `ISettingsProvider` instance)** which also fixes the C3 provider-vs-singleton divergence → P2 (memoize `ExtractTypeProperties` + fix O(n²) dedup) → P3 (compiled setter plan).
4. **More engine tests:** T4 `ValuesPopulator` (precedence + exception wrappers), T5 `TypeConverter` (null/nullable/empty-enumerable/attribute), T6 converters, T7 generator caching + concurrency stress.
5. **Architecture calls:** A1 (AOT/trim `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` — HIGH, it's a `Reflection.Emit` lib on net8/net10), C1 (`List<T>`/`IList<T>` support), C2 (public `SimpleSettingsException` base), C3 (reload/`IOptionsMonitor` — pairs with P1), A3 (`Core.AspNet` exports no public type), A4 (float `Microsoft.Extensions.*` floor per-TFM), A5 (make `SettingsHolder` internal), A6 (command-line quoted-arg parsing).

## How releasing works (unchanged — durable)
- **`ci.yml`** — on PRs to `master`: build + test (net8.0 + net10.0). **`release.yml`**:
  - push to `master` → auto-publishes a MinVer height-based `-alpha` prerelease to nuget.org.
  - manual **Release** (`workflow_dispatch`) with `channel` (beta/rc/stable) + `bump` (patch/minor/major) → computes the next version, tags `v*`, publishes, creates a GitHub Release. `dry_run: true` previews.
- **Versioning = MinVer**, tag prefix `v`, baseline **2.0.0**. Keyless publish via NuGet Trusted Publishing (OIDC). First real release: Actions → Release → `channel: beta` (→ `v2.0.0-beta.1`); use `dry_run` first.
- Both workflows invoke the solution by name — now **`SimpleSettings.slnx`** (via #13).

## Gotchas a new session MUST know
- **Pushing / PRs:** the active `git`/`gh` identity is **read-only** on this repo; a separate account has push access. The exact recipe (SSH alias, account names, `gh` account-switch flow) lives in the assistant's **private project memory** (`simplesettings-push-access`) — deliberately kept out of this public file.
- **Run `dotnet` from `src/`** (global.json opts into Microsoft.Testing.Platform for TUnit). Only the net10 runtime is installed locally → net8 tests are build-only locally; CI runs both. Do NOT prefix `cd <repo-root>` before `dotnet` — the solution lives in `src/`.
- **`FIX-PLAN.md`** (repo root, untracked) is the full, prioritized fix plan with per-item file:line detail. It is NOT auto-injected — open it explicitly.
- Commits/PRs here **omit** the Co-Authored-By / Generated-with trailer (project preference).
