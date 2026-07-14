# Phase 3: Public Surface, Packaging & Binder Cleanup - Context

**Gathered:** 2026-07-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Trim the public API and packages to only meaningful, correctly-scoped surface, and fix the
command-line binder to parse real-world arguments — the last breaking changes batched before the
first `v2.0.0-beta`. Requirements **API-01, PKG-01, PKG-02, SRC-02** are locked by ROADMAP; this
discussion only settles HOW.

**Not in scope:** AOT-01 (reflection/AOT-trim annotations) and DOC-01 (README refresh) — Phase 4;
REL-01 (cut the beta) — Phase 5; COLL-01 (`List<T>` support, still owner-deferred); held D1/D2.
</domain>

<decisions>
## Implementation Decisions

### API-01 — `SettingsHolder` → internal (breaking)
- **D-01:** Flip `public class SettingsHolder` (`SettingsHolder.cs:5`) to **`internal sealed class`**.
  `ISettingsHolder` is already `internal` (`ISettingsHolder.cs:5`); the sole consumer is
  `SettingsCollection` (`SettingsCollection.cs:10,14`), which is internal. Seal it — not designed for
  inheritance. Breaking, but free in the pre-stable window. Tests keep access via `InternalsVisibleTo`
  (`Info.cs:3-4` → UnitTests + Benchmark).

### PKG-01 — `Core.AspNet` → drop the package (breaking)
- **D-02:** **Remove the `Core.AspNet` package entirely.** Its only type is
  `internal static class Environments` (3 const strings) → it ships **zero public surface** and merely
  duplicates ASP.NET Core's own `Environments`/`EnvironmentName` constants. Removal touches three places:
  the project directory, its entry in `SimpleSettings.slnx:13`, and the `ProjectReference` in
  `UnitTests.csproj:19`. Confirm no test actually consumes `Environments` (remove any such use).
- **D-02b:** The project is packable by SDK default (no explicit `IsPackable`). If a `Core.AspNet`
  prerelease alpha was ever pushed to NuGet, it simply stops publishing on removal; note it for
  deprecation/unlisting if it exists (pre-stable alphas → low stakes; planner/researcher to confirm).

### PKG-02 — `Microsoft.Extensions.*` floor → float per-TFM (breaking-ish for consumers' floor)
- **D-03:** Float the floor **per-TFM via CPM conditional `PackageVersion`** in
  `src/Directory.Packages.props` (`:8-12`): **net8 → latest `8.0.x` patch**, **net10 → `10.0.x`**
  (currently `10.0.9`). Applies to all four packages: `Microsoft.Extensions.Configuration`,
  `.Configuration.Json`, `.DependencyInjection`, `.DependencyInjection.Abstractions`. This frees a net8
  consumer from being transitively forced onto 10.x. Researcher/planner resolves the exact current
  latest `8.0.x`. Verify build + full suite green on **both** TFMs afterward.

### SRC-02 — command-line binder (behavior change)
- **D-04:** Add **space-separated `--key value`** support (lookahead pairing) *in addition to* the
  existing inline `--key=value` / `--key:value`. Rule: a prefixed key followed by a **non-prefixed**
  token consumes that token as its value; a following token that itself starts with a prefix (`-`/`/`)
  is a **new key**, not a value (the current key then has no value → not stored, preserving today's
  string-only "no value ⇒ skip" semantics). This is the real "quoted value with spaces" fix — such a
  value arrives shell-unquoted as its own token, which the current parser drops.
- **D-05:** **Skip `arg[0]` (exe path) by default**, exposed as a toggle on
  `CommandLineSettingsBinderOptions` (e.g. `SkipFirstArgument`, default `true`) so callers passing
  `Main(string[])` args — which already exclude the exe — can turn it off.
- **D-06:** Preserve existing options (prefixes `-`/`/`, delimiters `:`/`=`, `IsCaseSensitive`,
  `NameFormatter`). No manual quote-stripping needed (the shell already unquotes).

### Claude's Discretion
- Exact CPM conditional syntax; the precise latest `8.0.x` version; the option name (`SkipFirstArgument`
  or similar); new binder test placement (`Tests/.../Binders/`); whether `arg[0]` skip is unconditional
  (default) vs exe-path-heuristic. All breaking changes are expected and batched pre-beta.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirement / roadmap
- `.planning/ROADMAP.md` — Phase 3 goal + 4 success criteria
- `.planning/REQUIREMENTS.md` — API-01 / PKG-01 / PKG-02 / SRC-02 + traceability
- `FIX-PLAN.md` (frozen historical) — §A5/A3/A4/A6 per-item detail; trust current source over any stale wording

### API-01 (SettingsHolder → internal)
- `src/Core/ExistForAll.SimpleSettings/SettingsHolder.cs:5` — `public class` to flip to `internal sealed`
- `src/Core/ExistForAll.SimpleSettings/ISettingsHolder.cs:5` — already `internal`
- `src/Core/ExistForAll.SimpleSettings/SettingsCollection.cs:10,14` — sole consumer
- `src/Core/ExistForAll.SimpleSettings/Info.cs:3-4` — `InternalsVisibleTo` (UnitTests, Benchmark)

### PKG-01 (drop Core.AspNet)
- `src/Core/ExistForAll.SimpleSettings.Core.AspNet/Environments.cs` — only file (`internal static Environments`)
- `src/Core/ExistForAll.SimpleSettings.Core.AspNet/ExistForAll.SimpleSettings.Core.AspNet.csproj` — project to remove
- `src/SimpleSettings.slnx:13` — solution entry to remove
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/ExistForAll.SimpleSettings.UnitTests.csproj:19` — `ProjectReference` to remove

### PKG-02 (per-TFM floor)
- `src/Directory.Packages.props:8-12` — the four `Microsoft.Extensions.*` pins @ `10.0.9`

### SRC-02 (command-line binder)
- `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/CommandLineSettingsBinder.cs` — `Parse` / `SplitByDelimiter` (add lookahead + arg[0] skip)
- `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/CommandLineSettingsBinderOptions.cs` — options (add `SkipFirstArgument`)
- `.planning/codebase/TESTING.md` — TUnit conventions for the new binder tests

### Release / CI context
- `.github/workflows/ci.yml` (net8 + net10), `release.yml` (alpha on master push — no paths filter), `benchmark.yml` (allocation gate)
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ISectionBinder` contract — the CLI binder fix stays entirely within it (no new abstraction).
- Central Package Management (`src/Directory.Packages.props`) — conditional `PackageVersion` per TFM is the mechanism for PKG-02.
- `InternalsVisibleTo` (`Info.cs`) — keeps the internal-flipped `SettingsHolder` reachable from tests.

### Established Patterns
- One public type per file; `I`-prefixed interfaces; block-scoped namespaces; `net8.0;net10.0` multi-target only.
- TUnit tests under `src/Tests/ExistForAll.SimpleSettings.UnitTests/` (add CLI-binder tests under `Binders/`); build + test from `src/`.
- **TUnit/Microsoft.Testing.Platform gotcha:** `dotnet test --filter "*X*"` exits 5 (zero tests); use `--treenode-filter "/*/*/ClassNameTests/*"` or run unfiltered.
- Breaking changes are free pre-stable and are batched before `v2.0.0-beta`.

### Integration Points
- `SimpleSettings.slnx` + `UnitTests.csproj` — remove `Core.AspNet` references (PKG-01).
- `Directory.Packages.props` — per-TFM float (PKG-02).
- `CommandLineSettingsBinder.Parse` loop — lookahead + arg[0] skip (SRC-02).
</code_context>

<specifics>
## Specific Ideas
- Ship Phase 3 on **this feature branch → PR** (never doc-only/direct to `master` — every master push publishes a throwaway alpha via `release.yml`). Push/PR via the `guy-lud` account (origin uses the `github-guy-lud` SSH alias; for `gh`, `gh auth switch --user guy-lud` then switch back to `guy-frontegg`).
- All four items are deliberate breaking/behavior changes, expected in this pre-beta cleanup.
</specifics>

<deferred>
## Deferred Ideas
- **AOT-01** (reflection/AOT-trim annotations) and **DOC-01** (README refresh) — Phase 4.
- **REL-01** (cut the first `v2.0.0-beta`) — Phase 5.
- **COLL-01** (`List<T>`/`IList<T>`/`ICollection<T>` support) — still owner-deferred (originally Phase 2).
- **D1 Validations**, **D2 EqualityCompererCreator** — HELD; out of scope.
- **CLI binder getopt-style short flags / boolean flags** — not adopted; a string-value binder doesn't model booleans. Out of scope.

None of the above are blockers for Phase 3.
</deferred>

---

*Phase: 3-public-surface-packaging-binder-cleanup*
*Context gathered: 2026-07-14*
