# SESSION HANDOFF — SimpleSettings

_Last updated: 2026-07-19 (Phase 4 Wave 3 + closeout) · owner: Guy Ludvig (guy@frontegg.com)_

## TL;DR
**GSD is the source of truth** (`.planning/`); `FIX-PLAN.md` frozen historical reference only.
**Phases 1–4 are COMPLETE.** Phase 4 (Waves 1–3) is fully merged to `master` (@ `0c858fa`, PRs #33/#34/#35). Phase 4 **closeout (verify 6/6 + security gate 15/15, D-06 signed off) is DONE** but lives **doc-only on branch `gsd/phase-4-closeout`** (held, not merged — rides to master with Phase 5's first code PR, per the no-doc-only-alpha rule). **Next work = Phase 5** (AOT-01 + DOC-01), not started.
This session: executed Wave 3 (plan 04-04 — VAL-01 DI path + API-02), reviewed it (plan-review trio + 3 post-code reviews), merged **PR #35** (`0c858fa`), then ran the Phase 4 closeout (verify + secure + mark complete). **One `2.0.0-alpha.0.*` alpha auto-published** on the #35 merge — add to the unlist list.

## Do this first (new session)
1. **Verify git state:** on branch **`gsd/phase-4-closeout`** (holds the Phase-4 closeout docs — `VERIFICATION.md`, `SECURITY.md`, ROADMAP/STATE marked complete — plus THIS handoff; pushed to origin as a backup, NOT merged). `master` @ `0c858fa`; `git status` clean; `gh pr list` shows no open PRs.
2. **The closeout is doc-only and rides to master with Phase 5's first code PR** — do NOT give it its own PR (would burn a throwaway alpha). Branch Phase 5's first plan off `gsd/phase-4-closeout` (or cherry-pick these docs onto the Phase-5 branch) so they land with real code.
3. **Next work = Phase 5** (AOT-01 + DOC-01): discuss → plan → execute. See Remaining roadmap for the DOC-01 must-includes.
4. **Push/PR only via `guy-lud`** (`gh auth switch --user guy-lud` … then switch back to `guy-frontegg`). `git push` uses the `github-guy-lud` SSH alias. See `[[simplesettings-push-access]]`.

## What shipped this session
- **Phase 4 Wave 3 (plan 04-04, PR #35 @ `0c858fa`) — VAL-01 DI path + API-02:**
  - **API-02:** `AddSimpleSettings` registers `ISettingsCollection` as a resolvable DI singleton AND adds an `AddSimpleSettings(out ISettingsCollection, Action?)` overload (same instance, fluent chain preserved).
  - **VAL-01 DI path:** opt-in `IServiceProvider.ValidateSimpleSettings()` runs DI-registered `ISettingValidation<T>` in a deferred post-`BuildServiceProvider()` step. Internal `SettingsValidationRunner` resolves validators from a **fresh scope** (`IServiceScopeFactory.CreateScope()` — scoped-dependency validators work under `validateScopes:true`), dispatches via the **DIM bridge** (`(ISettingsValidator)v).Validate(...)`, no reflection), and aggregates through the shared `SettingsValidationException.ThrowIfAny` → **contract-identical to the core path**. Throwing validator → value-free `SettingsValidatorInvocationException(type,type)`.
  - Full suite **153/153** (net8 + net10); benchmark allocation gate **0-byte diff** (warm path untouched).
- **Phase 4 CLOSEOUT (on `gsd/phase-4-closeout`, held):** `VERIFICATION.md` (6/6 success criteria MET), `SECURITY.md` (15/15 threats closed, **D-06 secret-redaction gate SIGNED OFF**, T-04-VAL DI holds by construction), ROADMAP + STATE mark Phase 4 ✓ complete and advance to Phase 5.

## Cadence / mechanics
- **Fresh branch off master per chunk → PR (src + .planning) → merge.** Each master merge burns a throwaway `-alpha` (pre-stable, acceptable). Reviews happen BEFORE the PR (see STANDING review rule).
- **Doc-only changes ride with the next code PR** — a doc-only master push burns a wasted alpha. This is why the Phase-4 closeout + this handoff sit on `gsd/phase-4-closeout` unmerged. See `[[simplesettings-handoff-workflow]]`.
- **Subagents run async in the background** (notify on completion). Dispatch plan-review + code/test reviews in parallel; keep their output out of the main context.

## STANDING review rule (memory `[[dotnet-review-workflow]]`)
Plan-review trio (**dotnet-architect + performance-analyst + security-auditor**) up front on the design BEFORE writing code. Before EACH PR: review code with **both** `gsd-code-review` (gsd-code-reviewer) AND `dotnet-claude-kit:code-reviewer`, and review tests with **`dotnet-claude-kit:test-engineer`**. Be proportional — skip for trivial docs/cosmetic PRs.

## Remaining roadmap
- **Phase 4: ✓ COMPLETE** (Waves 1–3 merged; verify + secure done; closeout held on branch).
- **Phase 5 (NEXT):** AOT-01; **DOC-01 (README)** — MUST document: Phase-3 breaking-change list; "spaced secrets bind via `AddCommandLine`"; VAL-01 **validator authors must not echo secrets** in `ValidationError` text; **DI-path `ValidateSimpleSettings()` is opt-in / deferred** (must call after `BuildServiceProvider()`; attribute validators run inline); **validator CONSTRUCTORS must not echo injected secrets** (DI resolution runs outside the value-free guard — from the Wave-3 security sign-off); the **validate ⇒ discoverable coupling** (`[SettingsSection(ValidatorType=…)]` also makes the type scan-discovered). Fold in **validator-dispatch caching** (deferred perf, code-review M2).
- **Phase 6:** REL-01 (first `v2.0.0-beta`; suite green net8 + net10).
- **Held:** EQ-01 (D2 EqualityComparerCreator). **Deferred:** PERF-03 (compiled setter); `${ENV:-}` placeholder detection (VAL-02, D-13).

## How releasing works (durable)
- **`ci.yml`** — PRs to `master`: build + test (net8.0 + net10.0). **`release.yml`**: push to `master` → auto-publishes a MinVer height-based `-alpha` (NO `paths` filter — **every master push publishes an alpha**). Manual **Release** (`workflow_dispatch`) tags `v*`.
- **`benchmark.yml`** — push to `master` + PRs: BDN, gates PRs on **allocation** regressions only (baseline in `gh-pages`; time recorded but not gated). Green through #35 (0-byte diff).
- **Versioning = MinVer**, tag prefix `v`, baseline **2.0.0**, keyless NuGet Trusted Publishing (OIDC). `SOLUTION=SimpleSettings.slnx`. **Everything through PRs; never commit to `master` directly.**

## Gotchas a new session MUST know
- **TUnit test filtering:** `dotnet test --filter "*Name*"` exits **5** with zero tests (looks green). Use `--treenode-filter "/*/*/ClassNameTests/*"` or run unfiltered. See `[[simplesettings-test-stack]]`.
- **Run `dotnet` from `src/`** (global.json → Microsoft.Testing.Platform). Build first, then `dotnet test SimpleSettings.slnx -c Release -f net10.0 --no-build`.
- **Solution-wide `-f net8.0` build trips `NETSDK1005`** on the net10-only `Benchmark`. Use `dotnet build SimpleSettings.slnx -c Release` (no `-f`).
- **Pushing/PRs:** active `guy-frontegg` is **read-only**; push/PR/merge via **`guy-lud`**. Remote `origin` already uses the `github-guy-lud` SSH alias. See `[[simplesettings-push-access]]`.
- **Never commit to `master` directly.** Wrap ritual: refresh THIS file on the current work branch. See `[[simplesettings-handoff-workflow]]`.
- **GSD branching:** `branching_strategy: none` → executors commit on the *current* branch. Currently `gsd/phase-4-closeout`. See `[[simplesettings-gsd-source-of-truth]]`.
- **`pre-bash-guard` silently blocks `git reset --hard`.** Use temp-branch + merge / `-s ours` / `--ff-only`. See `[[simplesettings-bash-guard]]`.
- **Settings interfaces MUST be public** — the proxy generator can't implement internal interfaces. See `[[simplesettings-proxy-internal-interface]]`.
- Commits/PRs **omit** the Co-Authored-By / Generated-with trailer (project preference). See `[[no-claude-attribution]]`.

## Key decisions & context (carry forward)
- **Object validator = `[SettingsSection].ValidatorType`** (merged). One attribute; validate ⇒ discoverable. Core reads it in `ValuesPopulator.GetOrBuildPlan`.
- **VAL-01 dispatch = DIM bridge, no reflection** (both core AND the now-shipped DI runner). `ISettingValidation<T>` default-implements the base `Validate`; dispatch via the `ISettingsValidator` cast. DI runner is **internal**, resolves from a fresh scope, injects `IServiceScopeFactory`, and eagerly allocates the error list (ThrowIfAny rejects null).
- **Exception-redaction invariant (S1+C2, locked).** `SettingsValidatorInvocationException` = value-free Type-only wrap for a throwing validator; `SettingsValidationException` composes only author `ValidationError` text; null/property exceptions value-free; no inner chaining of anything that saw a value. D-06 signed off for the COLL-03 sequence path AND T-04-VAL for the DI path. Don't weaken.
- **`SettingsPlan.HasValidators` zero-alloc short-circuit** protects the validator-free warm path (benchmark gate). Don't add per-populate allocation before it.
- **Generator concurrency (T7, #29):** one `_generationGate` (double-checked lock; warm path lock-free). Don't switch to `Lazy`-per-type.
- **Pre-stable window:** no `v*` tag; breaking changes free until the first `v2.0.0-beta`.

## Minor tracked follow-ups (non-blocking)
- **Owner:** unlist published `2.0.0-alpha.0.*` prereleases on NuGet.org (guy-lud) — **grew by 1 this session** (#35 merge; running total incl. prior #33/#34).
- **`.claude/`/`.codex/` tracked on `master`**: add to `.gitignore` + `git rm --cached` on a branch/PR (never on `master`).
- **REQUIREMENTS.md traceability:** 13 brownfield baseline IDs (`BIND-01…NAME-01`) in the body but not the traceability table (pre-existing).
- **Codebase-map drift:** root files predate the map — refresh via `/gsd-map-codebase` when convenient.
- **`SettingsValidatorInvocationException`** is a public exception never registered as its own threat ID (security-audit note) — handled value-free + tested; register it if the threat model is revisited.
