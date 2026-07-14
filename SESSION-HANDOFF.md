# SESSION HANDOFF — SimpleSettings

_Last updated: 2026-07-14 · owner: Guy Ludvig (guy@frontegg.com)_

## TL;DR
**GSD is the source of truth** (`.planning/`); `FIX-PLAN.md` is frozen historical reference only.
Phases **1 & 2 are complete and merged** (PR #30 squash → `master` @ `67aa72f`). `master` is now @ **`8750359`** (a follow-up commit that tracks the `.claude/`/`.codex/` tooling + a handoff refresh — pushed, so it publishes a throwaway alpha; the "remove later" cleanup is still pending).

**Active work: Phase 3 — Public Surface, Packaging & Binder Cleanup**, on feature branch **`gsd/phase-3-public-surface-packaging-binder-cleanup`** (forked off `master` @ `8750359`). **Discuss is DONE** (CONTEXT + DISCUSSION-LOG committed) and **research is DONE** (`03-RESEARCH.md`, committed `d8e7a89`, HIGH confidence). **Planning resumes at the planner step** — re-run `/gsd-plan-phase 3` and it auto-uses the existing research. Suite **94 tests net10** (CI net8 + net10).

## Do this first (new session)
1. **Verify git state** (`git log -3`, `git branch --show-current`, `gh pr list`): expect to be on **`gsd/phase-3-…`**, `master` @ `8750359` (pushed), PR #30 merged, no open PRs. Working tree may carry an uncommitted handoff refresh + untracked build noise.
2. **Check Phase 3 planning progress:** `ls .planning/phases/03-public-surface-packaging-binder-cleanup/`
   - `03-CONTEXT.md` + `03-DISCUSSION-LOG.md` are committed (discuss done).
   - **If `03-RESEARCH.md` exists and NO `03-*-PLAN.md` yet** → resume planning: **`/gsd-plan-phase 3`** — it auto-uses the existing RESEARCH.md (no re-prompt) and continues: VALIDATION.md → pattern-mapper → gsd-planner → gsd-plan-checker (revision loop, max 3) → coverage gates → PLANNED.
   - **If `03-*-PLAN.md` files exist** → planning finished; run **`/gsd-execute-phase 3`**.
3. **Stay on the feature branch.** Ship Phase 3 as one PR to `master` via `guy-lud` after it verifies — never commit docs directly to `master` (burns an alpha).

## Phase 3 — locked decisions (from discuss; full detail + file:line refs in `03-CONTEXT.md`)
- **API-01:** `SettingsHolder` (`SettingsHolder.cs:5`) `public class` → **`internal sealed`**. `ISettingsHolder` already internal; only `SettingsCollection` uses it; tests keep access via `InternalsVisibleTo` (`Info.cs:3-4`).
- **PKG-01:** **DROP the `Core.AspNet` package** (its only type is `internal static Environments` → zero public surface). Remove `SimpleSettings.slnx:13` entry + `UnitTests.csproj:19` ProjectReference; confirm no test uses `Environments`.
- **PKG-02:** **Float `Microsoft.Extensions.*` per-TFM** in `Directory.Packages.props:8-12` (all four): net8 → **latest `8.0.x`**, net10 → `10.0.x`, via CPM conditional `PackageVersion`.
- **SRC-02:** CLI binder (`CommandLineSettingsBinder.cs`): **add space-separated `--key value`** (lookahead; a prefixed next token = new key) + keep inline `=`/`:`; **skip `arg[0]` by default** via a new `SkipFirstArgument` option (default true) on `CommandLineSettingsBinderOptions`.

## Phase 3 — verified research findings (feed the planner; full detail in `03-RESEARCH.md`)
- **PKG-02 (verified by live restore, 0 warnings, no NU1504):** per-TFM conditional `<PackageVersion Include=... Condition="'$(TargetFramework)'=='net8.0'">` works. **Exact net8 floors differ per package:** `Configuration`=`8.0.0`, `Configuration.Json`=`8.0.1`, `DependencyInjection`=`8.0.1`, `DependencyInjection.Abstractions`=`8.0.2`. net10 stays `10.0.9`. `CentralPackageTransitivePinningEnabled` stays off.
- **PKG-01:** `Core.AspNet` **is published on NuGet** (`1.0.0` + a `2.0.0-alpha.0.N` run) → pair removal with **unlisting** the alphas (owner choice, low-stakes). The `UnitTests.csproj:19` ProjectReference is **DEAD** (the package's `Environments` is internal w/ no `InternalsVisibleTo`; tests use a local duplicate) → safe to remove.
- **API-01:** flip is self-contained — no test references `SettingsHolder`; no PublicAPI analyzer/baseline in the repo.
- **SRC-02:** our lookahead **deliberately diverges** from Microsoft's provider (which consumes the next token unconditionally). The `arg[0]` skip is needed because `AddCommandLine()` uses `Environment.CommandLine.Split(' ')` (includes exe path + quote-unsafe). Existing `ArgumentsTests` stay green under `SkipFirstArgument=true` (fixture's leading space makes `args[0]` empty).
- **Open questions for planning:**
  1. **(recommend YES, in-scope)** Switch `AddCommandLine()` from `Environment.CommandLine.Split(' ')` → `Environment.GetCommandLineArgs()` so the "quoted value with spaces" criterion holds end-to-end for that entry point.
  2. **(resolved)** net8 floor = latest-patch-per-package (above), not a uniform `8.0.0`.

## Current state
- On **`gsd/phase-3-public-surface-packaging-binder-cleanup`**. Phase-3 commits so far: `e945d99` (03-CONTEXT + 03-DISCUSSION-LOG), `6cbf370` (STATE record-session). Research step ran after that.
- **Phases 1–2 merged.** Build clean (0 warnings, both TFMs). Suite **94 net10**.
- The merged branch `chore/gsd-ownership-cutover` is deleted locally but **still exists on `origin`** (optional cleanup via `guy-lud`).
- A **`gh-pages`** branch holds the benchmark baseline (`dev/bench/`) — do **not** delete it.

## What shipped in Phases 1–2 (merged via PR #30)
- **Phase 1** — S1 secret redaction (#27), C2 public exception hierarchy (#28): `SettingsPropertyValueException` carries no value / chains no inner; all exceptions derive from `public abstract SimpleSettingsException`.
- **Phase 2** — engine test hardening (test-only): `ValuesPopulatorTests` (TEST-01 precedence/default), `TypeConverterTests` (TEST-02 null/nullable/ConverterType), `ScalarConversionTests` (TEST-03 Uri/DateTime), ENG-01 verify-only (generator concurrency gate from #29 confirmed; `..._IsRaceFree` is the load-bearing proof). COLL-01 owner-deferred.

## Remaining roadmap (5 phases; 1–2 done)
- **Phase 3 (active):** API-01, PKG-01, PKG-02, SRC-02 — above.
- **Phase 4:** AOT-01 (reflection/AOT-trim annotations), DOC-01 (README refresh).
- **Phase 5:** REL-01 (cut the first `v2.0.0-beta`; suite green net8 + net10).
- **Owner-deferred / held:** COLL-01 (`List<T>` support), D1 Validations, D2 EqualityCompererCreator.

## How releasing works (durable)
- **`ci.yml`** — PRs to `master`: build + test (net8.0 + net10.0). **`release.yml`**: push to `master` → auto-publishes a MinVer height-based `-alpha` (NO `paths` filter — every push, incl. docs, publishes). Manual **Release** (`workflow_dispatch`, channel beta/rc/stable + bump) tags `v*`.
- **`benchmark.yml`** — push to `master` + PRs: BDN, gates PRs on allocation regressions (baseline in `gh-pages`).
- **Versioning = MinVer**, tag prefix `v`, baseline **2.0.0**, keyless NuGet Trusted Publishing (OIDC). Workflows use `SOLUTION=SimpleSettings.slnx`. **Everything goes through PRs.**

## Gotchas a new session MUST know
- **TUnit test filtering:** `dotnet test --filter "*Name*"` is **rejected** by Microsoft.Testing.Platform/TUnit — exits **5**, zero tests (looks green at a glance). Use `--treenode-filter "/*/*/ClassNameTests/*"` or run unfiltered. Several `*-PLAN.md` `<verify>` blocks still carry the wrong `--filter` form. See `[[simplesettings-test-stack]]`.
- **Run `dotnet` from `src/`** (global.json → Microsoft.Testing.Platform). net10 runtime only locally → net8 build-only locally; CI runs both. Don't `cd <repo-root>` before `dotnet`.
- **Pushing / PRs:** active `git`/`gh` identity (`guy-frontegg`) is **read-only** here; push/PR/merge via **`guy-lud`**. `origin` uses SSH alias **`github-guy-lud`** → `git push` already uses guy-lud. For `gh` writes: `gh auth switch --user guy-lud`, then switch back to `guy-frontegg`. See `[[simplesettings-push-access]]`.
- **Never commit docs to `master`** (release.yml has no `paths` filter → burns an alpha). Wrap ritual: refresh THIS file so it rides the current work branch (currently committed on `gsd/phase-3-…`). See `[[simplesettings-handoff-workflow]]`.
- **GSD branching:** `config.json` `branching_strategy: none` → `/gsd-execute-phase` commits on the *current* branch. Keep Phase 3 on its feature branch; do NOT execute on `master`. See `[[simplesettings-gsd-source-of-truth]]`.
- **Review workflow (`[[dotnet-review-workflow]]`):** plan → review the plan with `dotnet-architect`/`performance-analyst`/`security-auditor` → implement → review finished code with `code-reviewer`. All four kit agents fired cleanly recently; still verify each returns real tool calls; `/code-review` skill is a reliable fallback.
- Commits/PRs here **omit** the Co-Authored-By / Generated-with trailer (project preference).

## Key decisions & context (carry forward)
- **Exception-redaction invariant (S1+C2, locked).** `SettingsPropertyValueException` never carries the bound value / never chains an inner; `SettingsBindingException` stores primitives; `SettingsPropertyNullException` = value-free "required missing". Don't weaken.
- **Generator concurrency (ENG-01/T7, #29).** One `_generationGate` over all generation (double-checked lock; warm path lock-free). Don't switch to `Lazy`-per-type (distinct-interface `DefineType` also races the shared `ModuleBuilder`).
- **Benchmark tracking gates on ALLOCATIONS, not time** (`gh-pages` baseline).
- **C3** = provider-level cache (option 2); reload/`IOptionsMonitor` is future "option 3".
- **Pre-stable window:** no `v*` tag; breaking changes free until the first `v2.0.0-beta`.

## Minor tracked follow-ups (non-blocking)
- **`.claude/`/`.codex/` are tracked on `master`** (commit `8750359`, "remove later"). Cleanup: add to `.gitignore` + `git rm --cached` — do it on a **branch/PR** (on `master` = another alpha).
- **Cosmetic NITs** (Phase 2 code review) in `Core/TypeConverterTests.cs`: add `using System;`; optionally rename `Convert_NullForNonNullableValueType_ReturnsTypeDefault` → `..._WithoutAttribute_...`.
- **REQUIREMENTS.md traceability:** 13 brownfield baseline IDs (`BIND-01…NAME-01`) are in the body but not the traceability table (pre-existing; surfaces in `/gsd-progress`).
