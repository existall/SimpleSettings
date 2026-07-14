# SESSION HANDOFF — SimpleSettings

_Last updated: 2026-07-14 · owner: Guy Ludvig (guy@frontegg.com)_

## TL;DR
**GSD is the source of truth** (`.planning/`); `FIX-PLAN.md` is frozen historical reference only.
**PR #30 merged** (squash, via `guy-lud`) — "GSD planning cutover + Phase 2 binding-correctness engine test hardening". `master` @ **`67aa72f`**, clean, **no open PRs, no work branch**. Suite **94 tests net10** (CI runs net8 + net10).

Milestone **v2.0.0** has 5 phases. **Phase 1** (S1 #27 / C2 #28 exception work) ✓ and **Phase 2** (engine test hardening) ✓ are complete and merged. **Next: Phase 3 — Public Surface, Packaging & Binder Cleanup.**

Still **pre-stable** (no `v*` tag; only auto-alphas) — breaking changes are free, which is the point of Phase 3 (make `SettingsHolder` internal, decide `List<T>` support, etc.) before the first `v2.0.0-beta`.

## Do this first (new session)
1. **Verify git state** (`git log -3`, `gh pr list`) — expect `master` @ `67aa72f` (PR #30 merged), **no open PRs**, clean tree except this uncommitted handoff refresh + untracked `.claude/`/`.codex/`.
2. **Run `/gsd-progress`** to see the roadmap and reconcile STATE.md. ⚠️ `.planning/STATE.md` has minor cosmetic drift from the phase-2 completion write (`completed_phases: 1` should read 2; `status: verifying` is stale) — GSD reconciles it on the next workflow command; don't hand-edit + commit it to `master` (see the doc-only-alpha gotcha).
3. **Start Phase 3:** `/gsd-discuss-phase 3` (recommended — no CONTEXT.md exists yet) → `/gsd-plan-phase 3` → `/gsd-execute-phase 3`. Ship it on a **feature branch + PR** (see gotchas), not on `master`.

## Current state
- On **`master`** @ `67aa72f`, clean. The merged branch `chore/gsd-ownership-cutover` is deleted locally; **it still exists on `origin`** (GitHub didn't auto-delete) — optional cleanup: `git push origin --delete chore/gsd-ownership-cutover` (needs the `guy-lud` push identity; origin already uses the alias).
- **Phases 1–2 complete + merged.** Build clean (0 warnings, both TFMs). Suite **94 net10** (Phase 2 added +7 engine-core + +3 scalar-converter over the prior 84).
- A **`gh-pages`** branch holds benchmark data (`dev/bench/`); do **not** delete it — the allocation baseline lives there.

## What shipped in Phase 2 (this session — test-only, zero `src/` production changes)
- **TEST-01** `Core/ValuesPopulatorTests.cs` — binder last-writer-wins, earlier-survives-when-later-silent, attribute `DefaultValue` survives.
- **TEST-02** `Core/TypeConverterTests.cs` — `null→0`, `int?` null→null, `"42"→42`, `ConverterType` bypasses the collection converter (via the internal `TypeConverter.CreateConversion` seam).
- **TEST-03** `Conversion/ScalarConversionTests.cs` — scalar `Uri` positive, `DateTime` positive, one format-mismatch negative (exception **type only** — redaction stays owned by `ExceptionRedactionTests`).
- **ENG-01** verify-only — confirmed `SettingsClassGenerator._generationGate` + concurrency stress tests satisfy success criterion #4 (shipped in #29/T7). The load-bearing proof is `..._IsRaceFree` (Barrier, genuine contention), not `..._ReturnsSingleSharedType` (timing-dependent). No code change.
- **COLL-01 owner-deferred** — the `List<T>`/`IList<T>`/`ICollection<T>` broaden-vs-document+throw decision is held; recorded in the plan's `<deferred>` section, `PROJECT.md`, and `02-VERIFICATION.md`. Not a gap.
- **Quality gates:** plan reviewed up front by `dotnet-architect` + `performance-analyst` + `security-auditor` (all PASS, no blockers); finished tests reviewed by `code-reviewer` (clean, 2 cosmetic NITs); `gsd-verifier` **passed 10/10 must-haves**.

## Next priorities — Phase 3: Public Surface, Packaging & Binder Cleanup
Goal: lock the public API surface and packaging before the first beta. Requirements (from ROADMAP / PROJECT.md Active):
1. **API-01 (A5)** — make `SettingsHolder`/`ISettingsHolder` internal *(breaking)*.
2. **PKG-01 (A3)** — `Core.AspNet` exposes a public type (`Environments` public) or drop the package.
3. **PKG-02 (A4)** — float `Microsoft.Extensions.*` floor per-TFM (`8.0.x` for net8) or justify the pin.
4. **SRC-02 (A6)** — command-line binder: parse quoted values with spaces, skip `arg[0]`.
5. **AOT-01 (A1, HIGH)** — annotate reflection entry points (`[RequiresDynamicCode]`/`[RequiresUnreferencedCode]`) or document the AOT/trim limitation before stable.
6. **DOC-01** — refresh README to canonical `ExistForAll.SimpleSettings` naming + current links.
7. **REL-01** — cut the first `v2.0.0-beta` once the breaking changes are batched; suite green net8 + net10.
- **COLL-01 (C1)** still pending the owner's broaden-vs-throw decision — surface it when Phase 3 is discussed.

## How releasing works (durable)
- **`ci.yml`** — PRs to `master`: build + test (net8.0 + net10.0). **`release.yml`**: push to `master` → auto-publishes a MinVer height-based `-alpha`; manual **Release** (`workflow_dispatch`, `channel` beta/rc/stable + `bump`) tags `v*`, publishes, creates a GitHub Release (`dry_run: true` previews).
- **`benchmark.yml`** — push to `master` + PRs: runs BDN, gates PRs on allocation regressions (baseline in `gh-pages`).
- **Versioning = MinVer**, tag prefix `v`, baseline **2.0.0**, keyless publish via NuGet Trusted Publishing (OIDC). Workflows use `SOLUTION=SimpleSettings.slnx`. **Every `master` push publishes an alpha → everything goes through PRs.**

## Gotchas a new session MUST know
- **TUnit test filtering:** `dotnet test --filter "*Name*"` is **rejected** by Microsoft.Testing.Platform/TUnit — exits **5** with zero tests run (looks green at a glance). Use `--treenode-filter "/*/*/ClassNameTests/*"` or run unfiltered and confirm the expected methods appear. Several `*-PLAN.md` `<verify>` blocks still carry the wrong `--filter` form. (Both Phase-2 executors hit this.) See project memory `[[simplesettings-test-stack]]`.
- **Run `dotnet` from `src/`** (global.json opts into Microsoft.Testing.Platform for TUnit). net10 runtime only locally → net8 is **build-only** locally; CI runs both. Don't `cd <repo-root>` before `dotnet`. Single project on net10: `dotnet test <proj> --framework net10.0 --no-build` (build first).
- **Pushing / PRs:** active `git`/`gh` identity (`guy-frontegg`) is **read-only** here; push/PR/merge via **`guy-lud`**. `origin` already uses SSH alias **`github-guy-lud`**, so **`git push` already uses guy-lud**. For `gh` writes: `gh auth switch --user guy-lud`, then switch back to `guy-frontegg` after. See project memory `[[simplesettings-push-access]]`.
- **Never commit docs to `master`:** `release.yml` fires on *every* `master` push with **no `paths` filter**, so a doc-only push burns a throwaway `-alpha`. **Wrap ritual:** refresh THIS file so it rides the next work branch; if everything is merged (no work branch), **leave it uncommitted** for the next session's first branch to carry — which is exactly its current state. See project memory `[[simplesettings-handoff-workflow]]`.
- **GSD branching:** `.planning/config.json` has `branching_strategy: none`, so `/gsd-execute-phase` commits on the *current* branch. Do NOT execute Phase 3 on `master` — create a feature branch first (that's how Phase 2 shipped), then PR. See `[[simplesettings-gsd-source-of-truth]]`.
- **Review workflow (per `[[dotnet-review-workflow]]`):** plan → review the plan with `dotnet-architect`/`performance-analyst`/`security-auditor` → implement → review finished code with `code-reviewer`. This session **all four kit agents fired cleanly** with real tool calls; historically they've been intermittently flaky (return a preamble with 0 tool calls), so still verify each returns substance — the `/code-review` skill is a reliable fallback for the code step.
- **Benchmarks:** run from `src/` — `dotnet run -c Release --project performance/ExistForAll.SimpleSettings.Benchmark -- --filter <glob> --job short`.
- Commits/PRs here **omit** the Co-Authored-By / Generated-with trailer (project preference).

## Key decisions & context (carry forward)
- **Exception-redaction invariant (S1+C2, locked).** `SettingsPropertyValueException` never carries the bound value and never chains an inner (ctor takes the failure `Type`, not the `Exception`). `SettingsBindingException` stores primitives, not the `BindingContext`. `SettingsPropertyNullException` is the value-free "required missing" case. Phase 2 tests were explicitly forbidden from re-asserting/weakening this.
- **Exception hierarchy (C2, done #28).** All library exceptions derive from `public abstract SimpleSettingsException` in the root namespace; a reflection invariant test enforces it.
- **Generator concurrency (ENG-01/T7, done #29).** `SettingsClassGenerator` serializes ALL generation behind one `_generationGate` (double-checked locking; warm path lock-free). Do NOT "optimize" to `Lazy<Type>`-per-type — `Reflection.Emit` isn't thread-safe, so concurrent `DefineType` of distinct interfaces also races the shared `ModuleBuilder`. Stress tests guard this.
- **Benchmark tracking gates on ALLOCATIONS, not time.** `gh-pages` (`dev/bench/`) holds the baseline.
- **C3 resolved — provider-level cache (option 2).** Reload/`IOptionsMonitor` is future "option 3".
- **Validations (D1) + `EqualityCompererCreator` (D2) — HELD, do NOT delete** (dead today, reserved for feature work).
- **Pre-stable window:** no `v*` stable tag; breaking changes free until the first `v2.0.0-beta`.

## Minor tracked follow-ups (non-blocking)
- **Cosmetic NITs** (from Phase 2 code review) in `Core/TypeConverterTests.cs`: add `using System;` (drop `System.Type` qualification); optionally rename `Convert_NullForNonNullableValueType_ReturnsTypeDefault` → `..._WithoutAttribute_...`.
- **REQUIREMENTS.md traceability:** 13 brownfield baseline IDs (`BIND-01…NAME-01`) appear in the body but not the traceability table (pre-existing; surfaces in `/gsd-progress` and `/gsd-audit-uat`).
