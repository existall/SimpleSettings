# SESSION HANDOFF — SimpleSettings

_Last updated: 2026-07-12 · owner: Guy Ludvig (guy@frontegg.com)_

## TL;DR
We're working the three-specialist review fix plan (**`FIX-PLAN.md`**, repo root — per-item file:line detail). The performance track is through **P2 + quick wins Q1–Q4** (merged), plus a namespace-collision fix (**M1**) and **isolated micro-benchmarks that prove the wins** (2.7×–32× on the repeated paths, allocations eliminated). A **per-push benchmark-tracking CI** that gates PRs on allocation regressions is set up. `master` is clean; **one PR open — #22 (benchmark tracking), green and ready to merge.** Suite green — **56 tests on net10.0** locally (CI runs net8.0 + net10.0).

Still **pre-stable** (no `v*` tag; only auto-alphas published), so breaking changes remain free — keep doing breaking cleanup now.

## Do this first (new session)
1. **Verify git state** (`git log`, `gh pr list`) — this file can lag.
2. **Merge #22** (benchmark tracking) via the `guy-lud` identity if it's still open — it's green/CLEAN. The first master run after merge records the allocation baseline on `gh-pages`.
3. `git checkout master && git pull`, then continue at **P3** (next perf item).

## Current state
- On **`master`** @ `4dd002a` (PR #21). Clean tree.
- **Open PR: #22** — the benchmark-tracking workflow (green). Merge it first.
- A **`gh-pages`** branch was bootstrapped to hold benchmark data (`dev/bench/`); do **not** delete it — the baseline lives there. Otherwise the remote holds only **legacy / held** branches (`validate-settings`, `version-7.x`, older pre-#8 feature branches). Deleting remote branches needs the `guy-lud` push identity.

## What shipped (recent → older)
- **#21 — Perf quick wins Q1–Q4 + M1 fix + micro-benchmarks.**
  - Q1 `SettingsCollection.GetEnumerator` yields over its dictionary (was rebuilding a whole `Dictionary` per enumeration); Q2 `SettingsTypesExtractor` suffix match → `EndsWith(…, OrdinalIgnoreCase)` + hoisted suffix (kills a `ToLower` CurrentCulture smell); Q3 `EnvironmentVariableBinder` fast-paths `context.Key` (no `StringBuilder`) + single lookup; Q4 `SettingsClassGenerator` caches the generated impl by interface `Type`. **Q5 was already done by B4.**
  - **M1 (found in the code review):** Q4's Type-keyed cache exposed a latent collision — the generated impl name was derived from the *simple* interface name, so `Foo.ISettings` + `Bar.ISettings` collided and aborted the scan. Fixed by namespace-qualifying the impl name **in the generator only**. ⚠️ `GetNormalizeInterfaceName` was left alone on purpose — it also backs the default config **section name** (`SettingsOptions.SectionNameFormatter`).
  - **Micro-benchmarks** (`MicroBenchmarks.cs`): `EnumerateBenchmark` (Q1), `EnvBinderBenchmark` (Q3), `GenerateTypeBenchmark` (Q4). The macro `ScanBenchmark` can't resolve these (IL-emit dominates), so they isolate each hot path. Proven before/after: **Q1 2.7× / 64 KB→88 B · Q3 2.65× / 152 B→0 · Q4 32× / 224 B→0**. The benchmark assembly now has `InternalsVisibleTo` (Info.cs) + a Binders project ref.
- **(open) #22 — benchmark tracking CI** (`.github/workflows/benchmark.yml`). Runs BDN (ShortRun) on push-to-master and PRs, `jq`-extracts per-benchmark **allocated bytes**, feeds `benchmark-action/github-action-benchmark` (`customSmallerIsBetter`) stored on `gh-pages`. PRs comment the allocation diff and **fail on a >10% regression**; time is informational only.
- **#20 — docs tutorials refresh.** All six `docs/*.md` rewritten against the current public API + the `SimpleConfig`→`SimpleSettings` rename (settles the A2 docs debt).
- **#18 — P2.** Memoized `TypePropertiesExtractor.ExtractTypeProperties` + `HashSet` dedup; cache is a **private instance field** (not static, not injected).
- **#17 — P1 + C3.** `ISettingsProvider.GetSettings` now serves the startup-built `ISettingsCollection` (same instance as the DI singleton); build fallback only for never-scanned types. **C3 = provider-level cache only** (Core `SettingsBuilder.GetSettings` unchanged; no reload).
- **#16 — P0 benchmark harness** (`[MemoryDiagnoser]` BDN: `ScanBenchmark` / `ResolveBenchmark` / `ShapeBenchmark`). Run: `dotnet run -c Release --project src/performance/ExistForAll.SimpleSettings.Benchmark` (smoke: `-- --job dry`).
- Earlier: #8 (5 bugfixes + tests), #10–#15 (tests, D3 typo, DI tests, solution rename, A2 naming).

## Key decisions & context (carry forward)
- **Benchmark tracking gates on ALLOCATIONS, not time.** Allocated bytes are deterministic → stable on shared CI runners → safe to fail a build on. Time is far too noisy to gate (this is also why the macro `ScanBenchmark` time didn't move for the quick wins). `gh-pages` (`dev/bench/`) holds the historical baseline.
- **M1 / generated names.** The generated impl type name (in `SettingsClassGenerator`) is namespace-qualified and must stay **separate** from `GetNormalizeInterfaceName`, which drives the default config section name. Don't merge them.
- **C3 resolved — option 2 (provider-level cache).** Reload / `IOptionsMonitor` is the future "option 3" if ever wanted.
- **Validations (D1) — HELD, do NOT delete.** Public `Validations/*` + `SettingsPropertyAttribute.ValidatorType` are dead but intended for a feature; reconcile with the `validate-settings` branch. Wire into `ValuesPopulator`.
- **`EqualityCompererCreator` (D2) — HELD** (internal, dead, latent invalid-IL bug at `EqualityCompererCreator.cs:38`).
- **Pre-stable window:** no `v*` stable tag (the `version-*` tags are the dead legacy package). Breaking changes free until the first `v2.0.0-beta`.

## Next priorities (ranked — detail in FIX-PLAN.md)
1. **Merge #22** if still open (see "Do this first").
2. **Perf:** **P3** — cached compiled "settings plan" (emit setters into the generated class, hoist section/key names once per type, cache the chosen converter per property). Biggest remaining ceiling, Eff L. Then **P4** (de-reflect array/enumerable converters) → **P5** (resolve the config section once per type).
3. **Engine tests:** T4 `ValuesPopulator`, T5 `TypeConverter`, T6 converters, T7 generator concurrency stress — the unsynchronized check-then-`DefineType` in `GenerateType` is **still open** (Q4's `ConcurrentDictionary` made the cache thread-safe but did not close that race).
4. **Architecture:** A1 (AOT/trim annotations — HIGH, `Reflection.Emit` lib), C1 (`List<T>`/`IList<T>` support), C2 (public `SimpleSettingsException` base), A3 (`Core.AspNet` public type or drop the package), A4 (float `Microsoft.Extensions.*` floor per-TFM), A5 (make `SettingsHolder` internal), A6 (command-line quoted-arg parsing).
5. **README** links — the `docs/` tutorials were done in #20; the README may still have stale `existall/SimpleConfig` links.
6. **D1 validations feature** — owner-driven; reconcile the `validate-settings` branch.

## How releasing works (unchanged — durable)
- **`ci.yml`** — on PRs to `master`: build + test (net8.0 + net10.0). **`release.yml`**: push to `master` → auto-publishes a MinVer height-based `-alpha` to nuget.org; manual **Release** (`workflow_dispatch`, `channel` beta/rc/stable + `bump` patch/minor/major) computes the next version, tags `v*`, publishes, creates a GitHub Release (`dry_run: true` previews).
- **`benchmark.yml`** — on push to `master` + PRs: runs BDN, gates PRs on allocation regressions (see Key decisions).
- **Versioning = MinVer**, tag prefix `v`, baseline **2.0.0**, keyless publish via NuGet Trusted Publishing (OIDC). First real release: Actions → Release → `channel: beta` (→ `v2.0.0-beta.1`); use `dry_run` first.
- Workflows invoke the solution through the `SOLUTION` env var (**`SimpleSettings.slnx`**). **Any push to `master` publishes an alpha** — so everything goes through PRs.

## Gotchas a new session MUST know
- **Pushing / PRs:** the active `git`/`gh` identity (`guy-frontegg`) is **read-only** on this repo; push/PR/merge via the **`guy-lud`** account — SSH alias `github-guy-lud` for `git push`, `gh auth switch --user guy-lud` for `gh` writes (switch back to `guy-frontegg` after). Full recipe in the assistant's private project memory (`simplesettings-push-access`).
- **Run `dotnet` from `src/`** (global.json opts into Microsoft.Testing.Platform for TUnit). Only the net10 runtime is installed locally → net8 is **build-only** locally; CI runs both. Do NOT prefix `cd <repo-root>` before `dotnet`.
- **Benchmarks:** run from `src/` — `dotnet run -c Release --project performance/ExistForAll.SimpleSettings.Benchmark -- --filter <glob> --job short`. Output dir (`BenchmarkDotNet.Artifacts/`) is now gitignored. Micro-benchmarks depend on the benchmark assembly's `InternalsVisibleTo` (Info.cs) + the Binders project ref.
- **`FIX-PLAN.md`** (repo root) is the full, prioritized plan with per-item file:line detail — open it explicitly; it is not auto-injected.
- Commits/PRs here **omit** the Co-Authored-By / Generated-with trailer (project preference).
