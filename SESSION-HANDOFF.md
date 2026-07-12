# SESSION HANDOFF — SimpleSettings

_Last updated: 2026-07-12 · owner: Guy Ludvig (guy@frontegg.com)_

## TL;DR
We're working the three-specialist review fix plan (**`FIX-PLAN.md`**, repo root — read it for per-item file:line detail). The performance track is now through **P2 + quick wins Q1–Q4**, the `docs/` tutorials are refreshed, and every merged workstream branch is pruned. **`master` is clean with no open PRs** once the Q1–Q4 perf PR merges. Suite is **green — 55 tests on net10.0** locally (CI runs net8.0 + net10.0).

Still **pre-stable** (no `v*` tag; only auto-alphas published), so breaking changes remain free — keep doing breaking cleanup now.

## Current state
- On **`master`** (the Q1–Q4 perf PR merge — **verify with `git log` first**; this file can lag reality). Clean tree, no open PRs. Next work branches off `master`.
- Remote now holds only **legacy / held** branches (`validate-settings`, `version-7.x`, and older pre-#8 feature branches); the whole #8–#20 workstream was pruned. Deleting remote branches needs the `guy-lud` push identity.

## What shipped (recent → older)
- **Perf quick wins Q1–Q4** (current PR). Q1 `SettingsCollection.GetEnumerator` now yields over its backing dictionary (was rebuilding a whole `Dictionary` per enumeration); Q2 `SettingsTypesExtractor` suffix match → `EndsWith(…, OrdinalIgnoreCase)` with the trimmed suffix hoisted out of the per-type predicate (also kills a `ToLower` CurrentCulture smell); Q3 `EnvironmentVariableBinder` fast-paths `context.Key` when there's no prefix/formatter (no `StringBuilder`) and does a single `IDictionary` lookup; Q4 `SettingsClassGenerator` caches the generated impl by interface `Type` (`ConcurrentDictionary`) instead of a per-call mangled-name `Assembly.GetType`. **Q5 was already resolved by B4** (the dead null-checks are gone). +3 regression tests → 55/TFM.
- **#20 — docs tutorials refresh.** All six `docs/*.md` rewritten against the current public API + the `SimpleConfig`→`SimpleSettings` rename (settles the A2 docs debt).
- **#18 — P2.** Memoized `TypePropertiesExtractor.ExtractTypeProperties` and replaced its O(n²) inherited-dedup with a single `HashSet<string>` pass. The cache is a **private instance field** on the extractor — not static, not injected (per review).
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
1. **Perf:** **P3** — cached compiled "settings plan" (emit setters into the generated class, hoist section/key names once per type, cache the chosen converter per property). Biggest remaining ceiling, Eff L. Then **P4** (de-reflect array/enumerable converters) → **P5** (resolve the config section once per type, not per property).
2. **Engine tests:** T4 `ValuesPopulator`, T5 `TypeConverter`, T6 converters, T7 generator concurrency stress — note the unsynchronized check-then-`DefineType` in `SettingsClassGenerator.GenerateType`: the Q4 `ConcurrentDictionary` made the cache thread-safe but did **not** close that generation race (still a T7 item).
3. **Architecture:** A1 (AOT/trim `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` — HIGH, it's a `Reflection.Emit` lib), C1 (`List<T>`/`IList<T>` support), C2 (public `SimpleSettingsException` base), A3 (`Core.AspNet` public type or drop the package), A4 (float `Microsoft.Extensions.*` floor per-TFM), A5 (make `SettingsHolder` internal), A6 (command-line quoted-arg parsing).
4. **README** links — give the repo/brand links a pass (the `docs/` tutorials were done in #20; the README may still have stale `existall/SimpleConfig` links).
5. **D1 validations feature** — owner-driven; reconcile the `validate-settings` branch.

## How releasing works (unchanged — durable)
- **`ci.yml`** — on PRs to `master`: build + test (net8.0 + net10.0). **`release.yml`**: push to `master` → auto-publishes a MinVer height-based `-alpha` to nuget.org; manual **Release** (`workflow_dispatch`, `channel` beta/rc/stable + `bump` patch/minor/major) computes the next version, tags `v*`, publishes, creates a GitHub Release (`dry_run: true` previews).
- **Versioning = MinVer**, tag prefix `v`, baseline **2.0.0**, keyless publish via NuGet Trusted Publishing (OIDC). First real release: Actions → Release → `channel: beta` (→ `v2.0.0-beta.1`); use `dry_run` first.
- Both workflows invoke the solution through the `SOLUTION` env var (**`SimpleSettings.slnx`**).

## Gotchas a new session MUST know
- **Pushing / PRs:** the active `git`/`gh` identity (`guy-frontegg`) is **read-only** on this repo; push/PR/merge via the **`guy-lud`** account — SSH alias `github-guy-lud` for `git push`, `gh auth switch --user guy-lud` for `gh` writes (switch back to `guy-frontegg` after). Full recipe in the assistant's private project memory (`simplesettings-push-access`).
- **Run `dotnet` from `src/`** (global.json opts into Microsoft.Testing.Platform for TUnit). Only the net10 runtime is installed locally → net8 is **build-only** locally; CI runs both. Do NOT prefix `cd <repo-root>` before `dotnet`.
- **`FIX-PLAN.md`** (repo root) is the full, prioritized plan with per-item file:line detail — open it explicitly; it is not auto-injected.
- Commits/PRs here **omit** the Co-Authored-By / Generated-with trailer (project preference).
