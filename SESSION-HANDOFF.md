# SESSION HANDOFF — SimpleSettings

_Last updated: 2026-07-14 · owner: Guy Ludvig (guy@frontegg.com)_

## TL;DR
**GSD is the source of truth** (`.planning/`); `FIX-PLAN.md` is frozen historical reference only.
Phases **1 & 2 merged** (PR #30 → `master` @ `67aa72f`; `master` now @ **`8750359`**). **Phase 3 — Public Surface, Packaging & Binder Cleanup — is COMPLETE and in open PR #31** (`https://github.com/existall/SimpleSettings/pull/31`, base `master`, head `gsd/phase-3-public-surface-packaging-binder-cleanup` @ `cf69197`). Verified **4/4 must-haves**, review-clean, suite **208/208** (net8+net10), build 0 warnings.

**Next milestone work is decided:** a **new engine phase goes in BEFORE the beta** to satisfy client requirements (approved 2026-07-14). Currently on a LOCAL staging branch **`gsd/phase-4-collection-validation-binding`** (forked off the Phase 3 tip `cf69197`, NOT pushed). The roadmap has NOT been restructured yet.

## Do this first (new session)
1. **Verify git state** (`git log -3`, `git branch --show-current`, `gh pr list`): expect current branch **`gsd/phase-4-collection-validation-binding`** (local), PR **#31 open** (Phase 3), PR #30 merged. `master` @ `8750359`.
2. **Decide the Phase-3 PR #31 disposition:** merge it to `master` via `guy-lud` (publishes a throwaway alpha — expected pre-stable), or leave open while stacking Phase 4. If #31 merges, **rebase `gsd/phase-4-collection-validation-binding` onto updated `master`** (it's currently stacked on the unmerged Phase 3 tip).
3. **Formalize the new engine phase** (client reqs — full detail in `.planning/backlog/client-requirements-pre-beta.md`):
   - Add 5 requirements to `REQUIREMENTS.md`: **COLL-02** (empty-collection default), **COLL-03** (YAML-sequence / indexed-child binding), **VAL-01** (wire `ISettingValidation<T>`/`ValidatorType`), **VAL-02** (tighter `AllowEmpty`), **API-02** (`AddSimpleSettings` exposes `ISettingsCollection`).
   - Insert them as **new Phase 4 "Collection & Validation Binding"** via `/gsd-phase` (use it for safe renumbering): current **Phase 4 (AOT/Trim & Docs) → Phase 5**, current **Phase 5 (beta) → Phase 6**. Beta stays last.
   - Then **`/gsd-discuss-phase 4`** → plan → execute (fresh session per phase; `/clear` between).

## Phase 3 — SHIPPED (in PR #31, awaiting merge)
- **API-01** `SettingsHolder` → `internal sealed`. **PKG-01** `Core.AspNet` package removed (+ dead test ref). **PKG-02** per-TFM `Microsoft.Extensions.*` floors (net8: `Configuration` 8.0.0, `DependencyInjection.Abstractions` 8.0.2; net10 10.0.9) + restore-time NU1901–1904 audit gate. **SRC-02** CLI binder: space-separated `--k v` + quoted-value binding, `SkipFirstArgument` (default **false**), `AddCommandLine()` → `GetCommandLineArgs()` owns exe-skip.
- **D-05 was refined mid-planning (owner-approved):** the exe-skip is split by entry point (NOT a shared default-true) — see `03-CONTEXT.md` D-05.
- **Owner follow-up (not automated):** unlist the published `Core.AspNet` `2.0.0-alpha.0.*` prereleases on NuGet.org (guy-lud account).

## New engine phase — client requirements (APPROVED; to formalize)
Source detail + provisional IDs + file locations + load-bearing constraints: **`.planning/backlog/client-requirements-pre-beta.md`**. Summary:
- **COLL-02 / COLL-03** pull deferred **COLL-01** forward and expand it (empty-collection default; sequence binding). **VAL-01** is the held **D1 Validations**.
- **Load-bearing constraints to preserve when planning:** (a) comma-scalar collection binding MUST keep working (prod env-var overrides `MultiHost__CommonHosts` depend on it); (b) **COLL-03 changes `ConfigurationBinder.BindPropertySettings`** — the redaction-critical method Phase 3 deliberately left untouched — so the **S1/SEC-01 secret-redaction invariant MUST be re-verified** when it lands; (c) collection converter chain must still run per element (int[]/enum arrays).
- Engine files in scope (different from Phase 3's CLI files): `TypeConverter.ConvertValue` / `ValidateNullAcceptance`, `ConfigurationBinder.BindPropertySettings`, `SettingsPropertyAttribute`/`ISettingValidation<T>`, the `AddSimpleSettings` DI extension.

## Remaining roadmap (after the insert/renumber)
- **Phase 4 (NEW, engine):** COLL-02, COLL-03, VAL-01, VAL-02, API-02.
- **Phase 5 (was 4):** AOT-01 (reflection/AOT-trim annotations), DOC-01 (README refresh — MUST add the review's "spaced secrets now bind via `AddCommandLine`" migration note + the Phase-3 breaking-change list).
- **Phase 6 (was 5):** REL-01 (cut the first `v2.0.0-beta`; suite green net8 + net10).
- **Still held:** D2 EqualityComparerCreator.

## How releasing works (durable)
- **`ci.yml`** — PRs to `master`: build + test (net8.0 + net10.0). **`release.yml`**: push to `master` → auto-publishes a MinVer height-based `-alpha` (NO `paths` filter — every push, incl. docs, publishes). Manual **Release** (`workflow_dispatch`, channel beta/rc/stable + bump) tags `v*`.
- **`benchmark.yml`** — push to `master` + PRs: BDN, gates PRs on allocation regressions (baseline in `gh-pages`).
- **Versioning = MinVer**, tag prefix `v`, baseline **2.0.0**, keyless NuGet Trusted Publishing (OIDC). Workflows use `SOLUTION=SimpleSettings.slnx`. **Everything goes through PRs.**

## Gotchas a new session MUST know
- **TUnit test filtering:** `dotnet test --filter "*Name*"` is **rejected** by Microsoft.Testing.Platform/TUnit — exits **5**, zero tests (looks green at a glance). Use `--treenode-filter "/*/*/ClassNameTests/*"` or run unfiltered. See `[[simplesettings-test-stack]]`.
- **Run `dotnet` from `src/`** (global.json → Microsoft.Testing.Platform). net10 runtime only locally (both TFM test DLLs run on it); net8 runs natively in CI. Don't `cd <repo-root>` before `dotnet`.
- **Pushing / PRs:** active `git`/`gh` identity (`guy-frontegg`) is **read-only** here; push/PR/merge via **`guy-lud`**. `origin` uses SSH alias **`github-guy-lud`** → `git push` already uses guy-lud. For `gh` writes: `gh auth switch --user guy-lud`, do the write, then `gh auth switch --user guy-frontegg`. See `[[simplesettings-push-access]]`.
- **Never commit docs to `master`** (release.yml has no `paths` filter → burns an alpha). Wrap ritual: refresh THIS file on the current work branch (now committed on `gsd/phase-4-…`). See `[[simplesettings-handoff-workflow]]`.
- **GSD branching:** `config.json` `branching_strategy: none` → `/gsd-execute-phase` commits on the *current* branch. Keep each phase on its own feature branch; never execute on `master`. See `[[simplesettings-gsd-source-of-truth]]`.
- **Review workflow (`[[dotnet-review-workflow]]`):** plan → review the plan with `dotnet-architect`/`performance-analyst`/`security-auditor` → implement → review finished code with `code-reviewer`. Phase 3 ran all of these; the architect caught a HIGH false-green (a vacuous verify), so **take the specialist reviews seriously — they earn their cost.**
- **VALIDATION.md gotcha (`[[gsd-plan-phase-validation-md]]`):** with Nyquist on, `/gsd-plan-phase` §5.5 writes only VALIDATION.md frontmatter → the plan-checker BLOCKS on the stub. Populate its body from RESEARCH's "## Validation Architecture" before the checker runs.
- Commits/PRs here **omit** the Co-Authored-By / Generated-with trailer (project preference).

## Key decisions & context (carry forward)
- **Exception-redaction invariant (S1+C2, locked).** `SettingsPropertyValueException` never carries the bound value / never chains an inner; `SettingsBindingException` stores primitives; `SettingsPropertyNullException` = value-free "required missing". Don't weaken. **Re-verify for COLL-03 (touches `BindPropertySettings`).**
- **Generator concurrency (ENG-01/T7, #29).** One `_generationGate` over all generation (double-checked lock; warm path lock-free). Don't switch to `Lazy`-per-type.
- **Benchmark tracking gates on ALLOCATIONS, not time** (`gh-pages` baseline). The CLI binder is NOT in the benchmark set.
- **Pre-stable window:** no `v*` tag; breaking changes free until the first `v2.0.0-beta`.

## Minor tracked follow-ups (non-blocking)
- **`.claude/`/`.codex/` tracked on `master`** (`8750359`, "remove later"): add to `.gitignore` + `git rm --cached` on a **branch/PR** (never on `master`).
- **REQUIREMENTS.md traceability:** 13 brownfield baseline IDs (`BIND-01…NAME-01`) are in the body but not the traceability table (pre-existing; surfaces in `/gsd-progress` and on `phase.complete`).
- **Phase 2 cosmetic NITs** in `Core/TypeConverterTests.cs`: add `using System;`; optional test rename.
- **Stale remote branch** `origin/chore/gsd-ownership-cutover` (merged) — safe to delete via `guy-lud`.
- **Codebase-map drift:** root files (README, LICENSE, SESSION-HANDOFF.md, …) predate the map — refresh via `/gsd-map-codebase` when convenient (non-blocking).
