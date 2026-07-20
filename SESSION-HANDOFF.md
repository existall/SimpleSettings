# SESSION HANDOFF — SimpleSettings

_Last updated: 2026-07-20 (Phase 5 complete) · owner: Guy Ludvig (guy@frontegg.com)_

## TL;DR
**GSD is the source of truth** (`.planning/`); `FIX-PLAN.md` frozen historical reference only.
**Phases 1–5 are COMPLETE.** Phase 5 (**Documentation / DOC-01**) is done + verified (4/4 must-haves). **Phase 6 (REL-01 — first `v2.0.0-beta`) is the ONLY remaining milestone work.**
This session: rescoped Phase 5 to **DOC-01 only** and **deferred AOT-01 to a future v2.1 milestone** (additive/non-breaking annotations needn't batch pre-beta), then ran the full GSD loop (discuss → plan → execute → verify) for Phase 5.
**Everything — the held Phase-4 closeout docs AND all of Phase 5 — sits on branch `gsd/phase-4-closeout`, 22 commits ahead of `master` @ `0c858fa`, UNMERGED.** No alpha published this session yet (nothing merged to master).

## Do this first (new session)
1. **Verify git state:** on branch **`gsd/phase-4-closeout`**, **22 commits ahead of `master` @ `0c858fa`**; `git status` clean; `gh pr list` shows no open PRs.
2. **IMMEDIATE NEXT STEP = open the PR** `gsd/phase-4-closeout` → `master` (via **`guy-lud`**). This is the moment the held Phase-4 closeout docs "ride to master with Phase 5's PR": **Phase 5 changed *packaged* content** (README via `PackageReadmeFile` + `Directory.Build.props` `<Description>`/`<PackageTags>`), so it's a legitimate release-worthy merge — NOT a throwaway doc-only push. On merge: publishes **one** `2.0.0-alpha.0.*` (add to unlist list); CI runs net8+net10; benchmark gate (docs-only → 0-byte diff expected).
3. **THEN Phase 6 = REL-01** (cut the first `v2.0.0-beta`): discuss → plan → execute, then the manual **Release** (`workflow_dispatch`) tags `v2.0.0-beta`. Needs everything on `master` first (step 2).
4. **Push/PR only via `guy-lud`** (`gh auth switch --user guy-lud` … then switch back to `guy-frontegg`). `git push` uses the `github-guy-lud` SSH alias. See `[[simplesettings-push-access]]`.

## What shipped this session (Phase 5 — Documentation / DOC-01)
- **Scope change:** Phase 5 renamed "AOT/Trim Honesty & Documentation" → **"Documentation"**; **AOT-01 deferred to v2.1** (recorded in ROADMAP + REQUIREMENTS; traceability + coverage reconciled, incl. correcting Phase-4/API-02 to complete).
- **05-01** — docs/ canonicalization: `git mv "Extend Simple Config.md" → "Extending SimpleSettings.md"` (history preserved), repointed 5 inbound links, dropped the `getting_started.md` "(previously SimpleConfig)" line. (docs/ was already ~90% canonical — the README was the real work.)
- **05-02** — `src/Directory.Build.props` metadata: `<Description>` "appliaction"→"application"; `<PackageTags>` legacy `SimpleConfig`→`SimpleSettings`. `dotnet build -c Release` 0/0.
- **05-03** — new **`docs/Security.md`**: the 6 mandated Phase 1–4 items with correct caveats (secret-redaction stated value-free WITH both carve-outs — author `ValidationError` text + validator **constructors** — not over-claimed; `ValidateSimpleSettings()` on **`IServiceProvider`**; validate⇒discoverable; `AddCommandLine` spaced values; v1→v2 breaking-change list).
- **05-04** — README full rewrite: absolute-URL logo, `dotnet add package` ×3, correct `[SettingsProperty(DefaultValue=…)]` quickstart, ToC into the renamed page + Security.md, concise Security + migration sections. **Phase-final gate green:** 13/13 DOC-VERIFICATION grep gates + `dotnet build`/`pack` exit 0 (packed `2.0.0-alpha.0.158` locally).
- **Verification:** gsd-verifier PASSED **4/4 must-haves** (re-ran all 13 gates + traced every API token to source + semantic no-secret-echo check). Regression: **153/153 net10** green. VERIFICATION.md written.

## Cadence / mechanics
- **Fresh branch off master per chunk → PR (src + .planning) → merge.** Each master merge burns a throwaway `-alpha` (pre-stable, acceptable). Reviews happen BEFORE the PR (see STANDING review rule).
- **Doc-only planning changes ride with the next code/package PR** — a doc-only master push burns a wasted alpha. Phase 5's README/metadata ARE packaged content, so Phase 5's PR is legitimate and carries the held Phase-4 closeout docs. See `[[simplesettings-handoff-workflow]]`.
- **Subagents run async in the background** (notify on completion). Keep their output out of the main context.
- **STATE.md is prose-style; GSD `state.*` SDK handlers (`advance-plan`, `record-session`, argless `begin-phase`) mis-parse/regress it** (repeatedly hit this session — `record-session`/argless `begin-phase` even reset `current_phase`). Prefer targeted edits + `state.validate`; `state.begin-phase --phase N --name X --plans K` (with args) and `phase.complete N` work correctly.

## STANDING review rule (memory `[[dotnet-review-workflow]]`)
Plan-review trio (**dotnet-architect + performance-analyst + security-auditor**) up front on design BEFORE code. Before EACH PR: review code with **both** `gsd-code-review` AND `dotnet-claude-kit:code-reviewer`; tests with **`dotnet-claude-kit:test-engineer`**. Be proportional — for the docs phase, the gsd-verifier's source-traced accuracy + no-secret-echo read covered it; no separate code-review pass was needed.

## Remaining roadmap
- **Phases 1–5: ✓ COMPLETE.**
- **Phase 6 (NEXT — the finish line):** REL-01 — cut the first `v2.0.0-beta`; consistent identity across the 3 packages (`ExistForAll.SimpleSettings` / `.Binders` / `.Extensions.GenericHost` — Core.AspNet dropped in Phase 3); suite green net8 + net10 at the tagged commit. **The milestone's plan ENDS at the beta**, not a stable GA — there is no explicit stable-`v2.0.0` phase yet (add one if stable GA is the real target).
- **Deferred to v2.1:** **AOT-01** (annotate reflection entry points `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` — the generator uses `Reflection.Emit`+`RunAndCollect`, not AOT-safe; annotations are additive/non-breaking so safe to add post-beta). **Validator-dispatch caching** (perf, code-review M2) — stays deferred, NOT pulled into Phase 5.
- **Held:** EQ-01 (D2 EqualityComparerCreator). **Deferred:** PERF-03 (compiled setter); `${ENV:-}` placeholder detection (VAL-02, D-13).

## How releasing works (durable)
- **`ci.yml`** — PRs to `master`: build + test (net8.0 + net10.0). **`release.yml`**: push to `master` → auto-publishes a MinVer height-based `-alpha` (NO `paths` filter — **every master push publishes an alpha**). Manual **Release** (`workflow_dispatch`) tags `v*`.
- **`benchmark.yml`** — push to `master` + PRs: BDN, gates PRs on **allocation** regressions only (baseline in `gh-pages`; time recorded, not gated).
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
- **Canonical docs facts (Phase 5, source-verified):** `ValidateSimpleSettings()` extends **`IServiceProvider`** (returns the provider), opt-in/deferred (call after `BuildServiceProvider()`); attribute/`ValidatorType` validators run inline. Repo stays `existall/SimpleSettings`; only the legacy `SimpleConfig` repo/product name is purged. `docs/Security.md` is the canonical deep security/behavior reference + README deep-link target.
- **Exception-redaction invariant (S1+C2, locked).** Value-free; `ValidationError` text + validator constructors are OUTSIDE the guard (docs must caveat, not over-claim). Don't weaken.
- **`SettingsPlan.HasValidators` zero-alloc short-circuit** protects the validator-free warm path (benchmark gate).
- **Generator concurrency (T7, #29):** one `_generationGate` (double-checked lock; warm path lock-free). Don't switch to `Lazy`-per-type.
- **Pre-stable window:** no `v*` tag; breaking changes free until the first `v2.0.0-beta`.

## Minor tracked follow-ups (non-blocking)
- **Owner:** unlist published `2.0.0-alpha.0.*` prereleases on NuGet.org (guy-lud) — **will grow by 1 when the Phase-4-closeout+Phase-5 PR merges** (running total incl. prior #33/#34/#35).
- **PROJECT.md `### Active` requirements list is stale** (still lists API-01/PKG-01/PKG-02/SRC-02/COLL-01/DOC-01 etc. as `[ ]` though complete). REQUIREMENTS.md traceability IS authoritative + correct. Reconcile PROJECT.md wholesale at Phase-6 / milestone wrap. (New this session.)
- **`.claude/`/`.codex/` tracked on `master`**: add to `.gitignore` + `git rm --cached` on a branch/PR (never on `master`).
- **REQUIREMENTS.md traceability:** 13 brownfield baseline IDs (`BIND-01…NAME-01`) in the body but not the traceability table (pre-existing).
- **Codebase-map drift:** root files (README, LICENSE, icon.png, etc.) predate the map — refresh via `/gsd-map-codebase` when convenient. (Plan-time drift precheck flagged this; non-blocking.)
- **`SettingsValidatorInvocationException`** is a public exception never registered as its own threat ID — handled value-free + tested; register it if the threat model is revisited.
