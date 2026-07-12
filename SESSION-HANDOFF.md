# SESSION HANDOFF — SimpleSettings

_Last updated: 2026-07-12 · owner: Guy Ludvig (guy@frontegg.com)_

## TL;DR
We're working the three-specialist review fix plan (**`FIX-PLAN.md`**, repo root — read it for per-item file:line detail). This session cleared the top of the plan and opened the performance track. **`master` is clean at `ef25761`, no open PRs.** Suite is **green — 51 tests on net10.0** locally (CI runs net8.0 + net10.0).

Still **pre-stable** (no `v*` tag; only auto-alphas published), so breaking changes remain free — keep doing breaking cleanup now.

## Current state
- On **`master`** @ `ef25761` (PR #17). Clean tree, no open PRs. Next work branches off `master`.
- Orphaned merged remote branches (from #8–#17) can be pruned anytime; deleting remote branches needs the `guy-lud` push identity.

## What shipped (recent → older)
- **#17 — P1 + C3.** `ISettingsProvider.GetSettings` used to re-bind a fresh instance on every call while DI registered startup-built singletons (the C3 divergence). The provider now serves the startup-built `ISettingsCollection` — the *same* instance as the DI singleton — falling back to a build only for never-scanned types. **C3 contract = cache in the provider only** (Core's public `SettingsBuilder.GetSettings` unchanged; no reload — settings are immutable snapshots). + regression test (both paths `ReferenceEquals`).
- **#16 — P0 benchmark harness.** Rebuilt the benchmark into a `[MemoryDiagnoser]` BenchmarkDotNet harness: `ScanBenchmark.ColdScan`; `ResolveBenchmark` (`[Params]` 1/10/50 · `ColdBuild` / `WarmResolve_Provider` / `WarmResolve_DiSingleton`); `ShapeBenchmark` (typed / array / deep-hierarchy). Run: `dotnet run -c Release --project src/performance/ExistForAll.SimpleSettings.Benchmark` (fast smoke: append `-- --job dry`). Baseline numbers intentionally NOT committed (machine-specific).
- **#15 — A2 naming consolidation.** All code/projects now spell the org **`ExistForAll`** (was a mix of `ExistAll` / `ExistsForAll`). Renamed the Binders folder (csproj + package id unchanged), the benchmark project, `Company` / `PackageTags`, and the README brand line.
- **#13 — solution file** `ExistAll.SimpleConfig.slnx` → `SimpleSettings.slnx` (first half of A2); the workflows now reference it through a `SOLUTION` env var.
- Earlier: #8 (five correctness bugfixes + tests), #10 (BindingContext test), #11 (D3 namespace typo), #12 (DI integration tests), #14 (fix plan + prior handoff).

## Key decisions & context (carry forward)
- **C3 resolved — option 2 (provider-level cache).** Core builder unchanged; no runtime reload. A reload / `IOptionsMonitor`-style path (change tokens) is the future "option 3" feature if it's ever wanted.
- **Validations (D1) — HELD, do NOT delete.** The public `Validations/*` namespace + `SettingsPropertyAttribute.ValidatorType` are dead but intended for an upcoming feature; reconcile with the existing **`validate-settings`** branch on origin. Wire into `ValuesPopulator` (run validators after binding, aggregate errors, throw typed).
- **`EqualityCompererCreator` (D2) — HELD** (internal, dead, latent invalid-IL bug at `EqualityCompererCreator.cs:38`). Delete only if value-equality on generated types is unwanted.
- **Pre-stable window:** no `v*` stable tag (the `version-*` tags are the dead legacy `ExistAll.SimpleConfig` package). Breaking changes free until the first `v2.0.0-beta`.

## Next priorities (ranked — detail in FIX-PLAN.md)
1. **P2** — memoize `TypePropertiesExtractor.ExtractTypeProperties` (shared `ConcurrentDictionary<Type, PropertyInfo[]>`) and replace its O(n²) dedup with a `HashSet<string>`. Sev High, Eff S, no decision; visible on the P0 harness. *(in progress this session)*
2. **Rest of perf:** quick wins **Q1–Q5** (GetEnumerator `yield`, `OrdinalIgnoreCase` suffix match, env-binder fast path, generated-type cache, dead null-checks) → **P3** (cached compiled "settings plan" — biggest ceiling) → P4 (de-reflect array/enumerable converters) → P5 (resolve config section once per type).
3. **Engine tests:** T4 `ValuesPopulator`, T5 `TypeConverter`, T6 converters, T7 generator caching + concurrency stress.
4. **Architecture:** A1 (AOT/trim `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` — HIGH, it's a `Reflection.Emit` lib), C1 (`List<T>`/`IList<T>` support), C2 (public `SimpleSettingsException` base), A3 (`Core.AspNet` public type or drop the package), A4 (float `Microsoft.Extensions.*` floor per-TFM), A5 (make `SettingsHolder` internal), A6 (command-line quoted-arg parsing).
5. **Docs debt (deferred from A2):** the `SimpleConfig` → `SimpleSettings` refresh — the `docs/*.md` tutorials and the README links still pointing at `existall/SimpleConfig`.
6. **D1 validations feature** — owner-driven; reconcile the `validate-settings` branch.

## How releasing works (unchanged — durable)
- **`ci.yml`** — on PRs to `master`: build + test (net8.0 + net10.0). **`release.yml`**: push to `master` → auto-publishes a MinVer height-based `-alpha` to nuget.org; manual **Release** (`workflow_dispatch`, `channel` beta/rc/stable + `bump` patch/minor/major) computes the next version, tags `v*`, publishes, creates a GitHub Release (`dry_run: true` previews).
- **Versioning = MinVer**, tag prefix `v`, baseline **2.0.0**, keyless publish via NuGet Trusted Publishing (OIDC). First real release: Actions → Release → `channel: beta` (→ `v2.0.0-beta.1`); use `dry_run` first.
- Both workflows invoke the solution through the `SOLUTION` env var (**`SimpleSettings.slnx`**).

## Gotchas a new session MUST know
- **Pushing / PRs:** the active `git`/`gh` identity (`guy-frontegg`) is **read-only** on this repo; push/PR/merge via the **`guy-lud`** account — SSH alias `github-guy-lud` for `git push`, `gh auth switch --user guy-lud` for `gh` writes (switch back to `guy-frontegg` after). Full recipe in the assistant's private project memory (`simplesettings-push-access`).
- **Run `dotnet` from `src/`** (global.json opts into Microsoft.Testing.Platform for TUnit). Only the net10 runtime is installed locally → net8 is **build-only** locally; CI runs both. Do NOT prefix `cd <repo-root>` before `dotnet`.
- **`FIX-PLAN.md`** (repo root) is the full, prioritized plan with per-item file:line detail — open it explicitly; it is not auto-injected.
- Commits/PRs here **omit** the Co-Authored-By / Generated-with trailer (project preference).
