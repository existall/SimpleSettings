# Phase 3: Public Surface, Packaging & Binder Cleanup - Research

**Researched:** 2026-07-14
**Domain:** .NET library public-surface hygiene, NuGet Central Package Management (per-TFM floors), command-line argument parsing
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (API-01):** Flip `public class SettingsHolder` (`SettingsHolder.cs:5`) to **`internal sealed class`**. `ISettingsHolder` is already `internal`; sole consumer `SettingsCollection` is internal. Tests keep access via `InternalsVisibleTo` (`Info.cs:3-4` → UnitTests + Benchmark).
- **D-02 (PKG-01):** **Remove the `Core.AspNet` package entirely.** Only type is `internal static class Environments` (3 const strings) → zero public surface, duplicates ASP.NET Core's own `Environments`. Removal touches: the project directory, `SimpleSettings.slnx:13`, and the `ProjectReference` in `UnitTests.csproj:19`. Confirm no test consumes `Environments`.
- **D-02b:** Project is packable by SDK default. If a `Core.AspNet` prerelease alpha was pushed to NuGet, it stops publishing on removal; note deprecation/unlisting if it exists (pre-stable → low stakes).
- **D-03 (PKG-02):** Float the floor **per-TFM via CPM conditional `PackageVersion`** in `src/Directory.Packages.props` (`:8-12`): **net8 → latest `8.0.x` patch**, **net10 → `10.0.x` (currently `10.0.9`)**. All four packages: `Microsoft.Extensions.Configuration`, `.Configuration.Json`, `.DependencyInjection`, `.DependencyInjection.Abstractions`. Verify build + full suite green on **both** TFMs.
- **D-04 (SRC-02):** Add **space-separated `--key value`** support (lookahead pairing) *in addition to* inline `--key=value` / `--key:value`. Rule: a prefixed key followed by a **non-prefixed** token consumes it as value; a following token starting with a prefix (`-`/`/`) is a **new key**, not a value (current key then has no value → not stored, preserving today's "no value ⇒ skip" semantics).
- **D-05 (SRC-02) — REFINED (see "## Open Questions (RESOLVED)" below; authoritative in 03-CONTEXT.md):** Skip the exe path **at the entry point that carries it**. `AddCommandLine()` (reads `Environment.GetCommandLineArgs()`, `[0]` = exe) enables the skip internally; `AddArguments(string[])` binds exactly what it is handed. `SkipFirstArgument` on `CommandLineSettingsBinderOptions` is an explicit override with default **`false`** (a shared default `true` would silently drop `args[0]` for `AddArguments` callers).
- **D-06 (SRC-02):** Preserve existing options (prefixes `-`/`/`, delimiters `:`/`=`, `IsCaseSensitive`, `NameFormatter`). No manual quote-stripping (shell already unquotes).

### Claude's Discretion
- Exact CPM conditional syntax; the precise latest `8.0.x` version; the option name (`SkipFirstArgument` or similar); new binder test placement (`Tests/.../Binders/CommandLine/`); whether `arg[0]` skip is unconditional (default) vs exe-path-heuristic. All breaking changes are expected and batched pre-beta.

### Deferred Ideas (OUT OF SCOPE)
- **AOT-01** (reflection/AOT-trim annotations) and **DOC-01** (README refresh) — Phase 4.
- **REL-01** (cut the first `v2.0.0-beta`) — Phase 5.
- **COLL-01** (`List<T>`/`IList<T>`/`ICollection<T>`) — owner-deferred.
- **D1 Validations**, **D2 EqualityCompererCreator** — HELD.
- **CLI binder getopt-style short flags / boolean flags** — not adopted (a string-value binder doesn't model booleans). OUT OF SCOPE.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| API-01 | Make `SettingsHolder`/`ISettingsHolder` internal (breaking) | Verified: `SettingsHolder` referenced only inside its own assembly (`SettingsCollection.cs:10,14`); no test references it; no PublicAPI analyzer baseline to update. Flip to `internal sealed` is self-contained. |
| PKG-01 | `Core.AspNet` exposes a public type or the package is dropped | Verified: package's `Environments` is `internal` with **no** `InternalsVisibleTo`; the test project's `Environments` is a **local duplicate** in a different namespace → the `ProjectReference` is **dead**. Package **is published** on NuGet (`1.0.0` + `2.0.0-alpha.0.*`) → unlisting step documented. |
| PKG-02 | Float `Microsoft.Extensions.*` floor per-TFM | Verified empirically: conditional `PackageVersion` per TFM restores net8→`8.0.x`, net10→`10.0.9`, **no NU1504**, clean build. Exact latest `8.0.x` per package resolved below. |
| SRC-02 | CLI binder parses quoted values with spaces + skips `arg[0]` | Verified: parity source (dotnet/runtime `CommandLineConfigurationProvider`) quoted; arg[0] rationale confirmed — `AddCommandLine` uses `Environment.CommandLine.Split(' ')` which **includes the exe path** as `args[0]`. |
</phase_requirements>

## Summary

All four items are small, well-scoped, and independently verifiable; three are pure edits with no new dependencies, and the fourth (PKG-02) is a Central Package Management mechanics question that I settled **empirically** by running a throwaway multi-target restore. There are no research unknowns blocking planning — every "HOW" is resolved with a verified answer.

The one genuinely non-obvious finding: the **latest `8.0.x` patch differs per package** (`Configuration` = `8.0.0`, `.Configuration.Json` = `8.0.1`, `.DependencyInjection` = `8.0.1`, `.DependencyInjection.Abstractions` = `8.0.2`), so the four conditional entries carry three different net8 versions. Conditional `<PackageVersion Include=... Condition="'$(TargetFramework)'=='net8.0'">` duplicated per TFM is valid CPM and produces **no NU1504** because NuGet restore is per-TFM and evaluates the conditions — I confirmed this with a live restore that resolved net8→8.0.x and net10→10.0.9. A second finding for SRC-02: `Core.AspNet` **has been published** to NuGet.org as `1.0.0` and a long run of `2.0.0-alpha.0.N` prereleases, so removal should be paired with unlisting the alphas (low stakes, pre-stable).

For the CLI binder, the decided lookahead rule (**D-04**) *deliberately diverges* from Microsoft's `CommandLineConfigurationProvider`, which consumes the next token as a value **unconditionally** (even if it starts with a prefix). SimpleSettings instead treats a prefixed next-token as a new key. This is a correct, intentional design choice — plan the tests around SimpleSettings' rule, not exact Microsoft parity. Separately, the `AddCommandLine()` convenience method's `Environment.CommandLine.Trim().Split(' ')` is the real reason `AddCommandLine()` must skip `arg[0]` (the exe path) — under refined D-05 it sets `SkipFirstArgument = true` internally (the option default is `false`) — and that split is itself quote-unsafe (see Pitfall 3), which is why it moves to `GetCommandLineArgs()`.

**Primary recommendation:** Ship all four as one PR on `chore/gsd-ownership-cutover` (never direct to `master` — every master push publishes a throwaway alpha). Use conditional `PackageVersion` items (two per package) for PKG-02; validate PKG-02 by inspecting `obj/project.assets.json` per TFM. Keep the binder fix inside `Parse`/`SplitByDelimiter` + one new option; add TUnit tests under `Binders/CommandLine/`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| `SettingsHolder` visibility (API-01) | Core library (`ExistForAll.SimpleSettings`) | Test assembly (via `InternalsVisibleTo`) | Internal DTO; owned entirely by the core assembly's collection type |
| `Core.AspNet` removal (PKG-01) | Solution / packaging | NuGet.org (unlist) | Build-graph + package-registry concern, not runtime code |
| Dependency floor (PKG-02) | Build / NuGet (`Directory.Packages.props`) | CI restore (both TFMs) | Central Package Management owns version policy per TFM |
| CLI argument parsing (SRC-02) | Binders extension (`ISectionBinder`) | Options type | Stays inside the existing binder contract; no new abstraction |

## Standard Stack

No new packages are introduced by this phase. The stack is unchanged; only **versions** move (PKG-02) and one project is **removed** (PKG-01).

### Core (unchanged, versions adjusted by PKG-02)
| Library | net8 floor | net10 | Purpose | Why Standard |
|---------|-----------|-------|---------|--------------|
| Microsoft.Extensions.Configuration | **8.0.0** | 10.0.9 | Config abstractions consumed by `ConfigurationBinder` | Microsoft first-party (BCL-adjacent) |
| Microsoft.Extensions.Configuration.Json | **8.0.1** | 10.0.9 | JSON config source (test fixtures + consumers) | Microsoft first-party |
| Microsoft.Extensions.DependencyInjection | **8.0.1** | 10.0.9 | DI container integration (`AddSimpleSettings`) | Microsoft first-party |
| Microsoft.Extensions.DependencyInjection.Abstractions | **8.0.2** | 10.0.9 | DI contracts (`IServiceCollection`) | Microsoft first-party |

*All four net8 patches `[VERIFIED: nuget.org flatcontainer index, 2026-07-14]` — the highest stable `8.0.x` for each package. All four net10 `[VERIFIED: nuget.org]` — `10.0.9` is the current highest `10.0.x` (matches the existing pin).*

**Note on the version choice (Claude's Discretion per CONTEXT):** the decision says "latest `8.0.x` patch". Two coherent options:
1. **Latest-patch-per-package (recommended, matches decision):** the table above (`8.0.0 / 8.0.1 / 8.0.1 / 8.0.2`). Verified to restore coherently — NuGet unifies the mixed `8.0.x` sub-versions.
2. **Uniform `8.0.0` for all four:** simpler, lowest possible floor (friendliest to net8 consumers), enables a single shared MSBuild property. Equally valid; only choose if uniformity is preferred over "latest patch".
Either is correct; the planner should surface both and let the owner pick. The default is Option 1.

### Testing / Build (unchanged)
| Library | Version | Purpose |
|---------|---------|---------|
| TUnit | 1.58.0 | Test framework on Microsoft.Testing.Platform |
| BenchmarkDotNet | 0.15.8 | Allocation gate (net10 only) |
| MinVer | 7.0.0 | git-tag-driven versioning (`v` prefix, `2.0` floor) |

**Version verification:**
```bash
# net8 floors (run per package; highest stable 8.0.x):
curl -s https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration/index.json            # -> 8.0.0 (only stable 8.0.x)
curl -s https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.json/index.json       # -> 8.0.1
curl -s https://api.nuget.org/v3-flatcontainer/microsoft.extensions.dependencyinjection/index.json      # -> 8.0.1
curl -s https://api.nuget.org/v3-flatcontainer/microsoft.extensions.dependencyinjection.abstractions/index.json  # -> 8.0.2
```

## Package Legitimacy Audit

No external packages are **added** in this phase (PKG-02 only changes versions of already-present Microsoft first-party packages; PKG-01 **removes** a project). All four `Microsoft.Extensions.*` are first-party, billions of downloads, source at `github.com/dotnet/runtime`.

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|-------------|---------|-------------|
| Microsoft.Extensions.Configuration | NuGet | 7+ yrs | billions | github.com/dotnet/runtime | OK | Approved (version change only) |
| Microsoft.Extensions.Configuration.Json | NuGet | 7+ yrs | billions | github.com/dotnet/runtime | OK | Approved (version change only) |
| Microsoft.Extensions.DependencyInjection | NuGet | 7+ yrs | billions | github.com/dotnet/runtime | OK | Approved (version change only) |
| Microsoft.Extensions.DependencyInjection.Abstractions | NuGet | 7+ yrs | billions | github.com/dotnet/runtime | OK | Approved (version change only) |

**Packages removed due to [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none
**New packages installed:** none — no `checkpoint:human-verify` gate required for this phase.

## Architecture Patterns

### System Architecture Diagram (CLI binder data flow — SRC-02)

```
Consumer                         SimpleSettings binder chain
--------                         ---------------------------
Main(string[] args)  ─┐
                      ├─► AddArguments(args, opts) ─┐
Environment.CommandLine (exe + args)                │
   .Trim().Split(' ') ─► AddCommandLine(opts) ──────┤
                                                     ▼
                                       new CommandLineSettingsBinder(args, options)
                                                     │
                                          Parse(args):                (SRC-02 change here)
                                          ├─ [SkipFirstArgument] skip args[0] (AddCommandLine sets true; option default false)   ◄── D-05 (refined)
                                          └─ for each arg (with lookahead):                    ◄── D-04
                                             ├─ inline "--k=v" / "--k:v"  → store (existing)
                                             ├─ "--k" + next non-prefixed → store k=next
                                             └─ "--k" + next prefixed     → k has no value → skip
                                                     │
                                                     ▼
                                       _argumentStore : Dictionary<key,value>
                                                     │
                          BindPropertySettings(context): key = NameFormatter?(section,key) ?? context.Key
                                                     │  TryGetValue → context.SetNewValue(value)
                                                     ▼
                                       ValuesPopulator (last-writer-wins across binders)
```

### Pattern 1: Per-TFM conditional `PackageVersion` (PKG-02) — VERIFIED
**What:** Two `<PackageVersion>` items per package, each gated on `$(TargetFramework)`.
**When to use:** CPM repo that multi-targets and needs a different floor per TFM.
**Example (drop-in replacement for `Directory.Packages.props:7-13`):**
```xml
<!-- Source: verified by live `dotnet restore` of an isolated multi-target project, 2026-07-14 -->
<ItemGroup>
  <!-- Microsoft.Extensions.* — per-TFM floor (net8 -> 8.0.x, net10 -> 10.0.9) -->
  <PackageVersion Include="Microsoft.Extensions.Configuration"                    Version="8.0.0"  Condition="'$(TargetFramework)' == 'net8.0'" />
  <PackageVersion Include="Microsoft.Extensions.Configuration"                    Version="10.0.9" Condition="'$(TargetFramework)' == 'net10.0'" />
  <PackageVersion Include="Microsoft.Extensions.Configuration.Json"               Version="8.0.1"  Condition="'$(TargetFramework)' == 'net8.0'" />
  <PackageVersion Include="Microsoft.Extensions.Configuration.Json"               Version="10.0.9" Condition="'$(TargetFramework)' == 'net10.0'" />
  <PackageVersion Include="Microsoft.Extensions.DependencyInjection"              Version="8.0.1"  Condition="'$(TargetFramework)' == 'net8.0'" />
  <PackageVersion Include="Microsoft.Extensions.DependencyInjection"              Version="10.0.9" Condition="'$(TargetFramework)' == 'net10.0'" />
  <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2"  Condition="'$(TargetFramework)' == 'net8.0'" />
  <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.9" Condition="'$(TargetFramework)' == 'net10.0'" />
</ItemGroup>
```
**Verified result** (live restore of an equivalent `net8.0;net10.0` project):
- `net8.0` target resolved `Microsoft.Extensions.Configuration/8.0.0`, `.Configuration.Json/8.0.1`.
- `net10.0` target resolved both at `10.0.9`.
- `dotnet build /p:TreatWarningsAsErrors=true` → **0 warnings, 0 errors** (no NU1504, no NU1008).

**Alternative pattern (property indirection)** — cleaner only if you adopt uniform `8.0.0`:
```xml
<PropertyGroup>
  <MsExtVersion Condition="'$(TargetFramework)' == 'net8.0'">8.0.0</MsExtVersion>
  <MsExtVersion Condition="'$(TargetFramework)' == 'net10.0'">10.0.9</MsExtVersion>
</PropertyGroup>
<ItemGroup>
  <PackageVersion Include="Microsoft.Extensions.Configuration" Version="$(MsExtVersion)" />
  ...
</ItemGroup>
```
This needs **one** version value shared across all four, so it only fits Option 2 (uniform `8.0.0`). Because the latest patches differ per package (Option 1), the conditional-item form above is the recommended shape.

### Pattern 2: CLI single-token lookahead (SRC-02, D-04)
**What:** When the current token is a prefixed key with no inline delimiter, peek the next token; consume it as the value **iff** it is not itself prefixed.
**Reference (dotnet/runtime `CommandLineConfigurationProvider.Load`, `main`):**
```csharp
// Source: github.com/dotnet/runtime .../CommandLineConfigurationProvider.cs
int separator = currentArg.IndexOf('=');
if (separator < 0) {
    if (keyStartIndex == 0) continue;              // no prefix, no '=' -> ignored
    // ... switch-mapping handling (SimpleSettings has none) ...
    key = currentArg.Substring(keyStartIndex);
    if (!enumerator.MoveNext()) continue;          // missing value -> ignore
    value = enumerator.Current;                    // <-- consumes NEXT token UNCONDITIONALLY
} else {
    key   = currentArg.Substring(keyStartIndex, separator - keyStartIndex);
    value = currentArg.Substring(separator + 1);   // first '=' splits; rest (incl '=') is value
}
data[key] = value;                                 // last-writer-wins
```
**Parity note (IMPORTANT):** Microsoft takes `enumerator.Current` as the value *without* checking whether it is prefixed. **D-04 deliberately diverges:** if the next token is prefixed (`-`/`/`), SimpleSettings treats it as a **new key** and leaves the current key valueless (→ not stored). Plan tests to SimpleSettings' rule. The `=`-splitting behavior (first delimiter wins, remainder is the value including further delimiters) **does** match SimpleSettings' existing `SplitByDelimiter` (min-index delimiter, `Substring(idx+1)`).

### Anti-Patterns to Avoid
- **Adding switch-mappings / short-flag semantics** — explicitly OUT OF SCOPE (deferred). Do not import Microsoft's `-x`→`--long` mapping or short-switch `FormatException`.
- **Manual quote-stripping in the binder** — the shell already unquotes; `Main(string[])` tokens arrive clean (D-06).
- **A single unconditional `PackageVersion` plus a second conditional one** — that *would* trip NU1504 (two active items for one package). Both items must be mutually-exclusively conditioned.
- **Hard-deleting the published `Core.AspNet` versions** — NuGet policy forbids true deletion; **unlist** instead.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Robust exe+args tokenization for `AddCommandLine` | `Environment.CommandLine.Split(' ')` (current) | `Environment.GetCommandLineArgs()` | `.Split(' ')` shreds quoted paths/values with spaces; `GetCommandLineArgs()` returns a properly tokenized `string[]` with `[0]` = exe path (see Pitfall 3). |
| Per-TFM version policy | ad-hoc `<Choose>`/`.targets` hacks | conditional `<PackageVersion>` in `Directory.Packages.props` | CPM natively supports it; verified NU1504-free. |
| Env-name constants | keeping `Core.AspNet.Environments` | ASP.NET Core's `Microsoft.Extensions.Hosting.Environments` | Framework already ships `Development`/`Staging`/`Production`. |

**Key insight:** the whole phase is *removing* hand-rolled/duplicated surface, not adding abstractions. The only "build" is ~15 lines in `Parse`.

## Runtime State Inventory

> PKG-01 removes a **published** package; that is external registry state, captured here.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — no datastore keys reference `SettingsHolder`/`Core.AspNet`/`Environments`. | none |
| Live service config | **NuGet.org registry:** `ExistForAll.SimpleSettings.Core.AspNet` is published: `1.0.0` (legacy) + `2.0.0-alpha.0.110 … 2.0.0-alpha.0.N` prereleases (pushed by `release.yml` on every master push). Removing the project stops **future** publishing but existing versions remain listed. | **Unlist** the `2.0.0-alpha.0.*` prereleases (and optionally `1.0.0`) via `dotnet nuget delete <id> <version> -s https://api.nuget.org/v3/index.json --non-interactive` (unlists, does not delete) or the nuget.org UI. Low stakes (pre-stable alphas). Do this with the publishing account. |
| OS-registered state | None. | none |
| Secrets/env vars | None affected — no key names change. | none |
| Build artifacts | Stale `obj/`+`bin/` under `src/Core/ExistForAll.SimpleSettings.Core.AspNet/` (contains a leftover `netstandard2.0` AssemblyInfo from an earlier multi-target). Removed with the directory. | Delete the project directory; `dotnet restore` regenerates the solution graph. |

**Canonical question — after every file is updated, what still carries the old package?** Only NuGet.org (the already-published alphas). Everything else is in-repo and removed by deleting the directory + two references.

## Common Pitfalls

### Pitfall 1: NU1504 duplicate `PackageVersion` (PKG-02)
**What goes wrong:** Two `PackageVersion` items for one package error out as duplicates.
**Why it happens:** NU1504 fires only when **both** items are active simultaneously (i.e., at least one is unconditional).
**How to avoid:** Ensure **every** item carries a mutually-exclusive `Condition` on `$(TargetFramework)`. Verified: conditioned duplicates restore cleanly with **0 warnings**.
**Warning signs:** `error NU1504: Duplicate 'PackageVersion' items found`. If seen, a condition is missing or non-exclusive.

### Pitfall 2: net8 consumer still forced to 10.x (PKG-02 regression)
**What goes wrong:** After the edit, a net8 consumer still pulls `Microsoft.Extensions.* 10.x`.
**Why it happens:** The library's net8 TFM group still declares `>= 10.x` (edit didn't take, or a condition mismatched the actual TFM string).
**How to avoid:** Validate by restoring and inspecting `obj/project.assets.json` — the `net8.0` target must list `8.0.x`, not `10.x`. (`<CentralPackageTransitivePinningEnabled>` is **not** set → default off, so only direct deps are pinned; this is correct and needs no change.)
**Warning signs:** `project.assets.json` `targets["net8.0"]` shows `Microsoft.Extensions.Configuration/10.0.9`.

### Pitfall 3: `AddCommandLine()` shreds quoted values / the arg[0] contract (SRC-02)
**What goes wrong:** `AddCommandLine()` uses `Environment.CommandLine.Trim().Split(' ')` — this (a) splits a quoted value/path with spaces into multiple tokens, and (b) includes the **exe path** as `args[0]`.
**Why it happens:** `Environment.CommandLine` is the raw, un-tokenized string; naive `Split(' ')` ignores quoting.
**How to avoid:**
- `AddCommandLine()` sets `SkipFirstArgument = true` internally — its `GetCommandLineArgs()` source has the exe at `[0]`. The option default is **`false`**.
- The `AddArguments(Main(string[] args))` path keeps the default `false` and binds exactly what it is handed (`Main` args already exclude the exe) — no silent `arg[0]` drop. Document the option's behavior either way.
- **Strongly consider** switching `AddCommandLine()` to `Environment.GetCommandLineArgs()` (properly tokenized, `[0]` = exe) so D-04's quoted-value fix actually helps the `AddCommandLine()` path too. This is arguably in-scope for "quoted value with spaces binds correctly" (see Open Questions).
**Warning signs:** `--name "John Doe"` binds `Name="John"` (or nothing) instead of `John Doe`.

### Pitfall 4: existing `ArgumentsTests` under the new default (SRC-02)
**What goes wrong (historical — resolved by refined D-05):** A *shared* `SkipFirstArgument` default-true silently drops a real first arg for `AddArguments`. Refined D-05 defaults the option to `false` (only `AddCommandLine` sets it true internally), so this no longer occurs.
**Why it happens (and why it's fine here):** The existing test uses `" name=value age:3".Split(' ')` = `["", "name=value", "age:3"]` — the **leading space** makes `args[0]` an empty string. Skipping `""` is harmless; `name`/`age` are at `[1]`/`[2]`. The suite stays green.
**How to avoid:** With the refined default `false`, the existing leading-space fixture stays green as-is (arg[0] is `""`, parsed safely and not stored). Add new cases for the space-separated form.
**Warning signs:** `Build_WhenNameMatchKey_ShouldPlaceValue` fails after the option lands.

### Pitfall 5: TUnit filter (project-wide, from TESTING.md)
**What goes wrong:** `dotnet test --filter "*CommandLine*"` exits 5 (zero tests).
**How to avoid:** Use `--treenode-filter "/*/*/ArgumentsTests/*"` or run unfiltered. Runner is Microsoft.Testing.Platform, not VSTest.

## Code Examples

### API-01 — the exact flip (`SettingsHolder.cs:5`)
```csharp
// before
public class SettingsHolder : ISettingsHolder
// after
internal sealed class SettingsHolder : ISettingsHolder
```
No other change needed: `ISettingsHolder` is already `internal`; `SettingsCollection` (internal) is the only consumer; `Info.cs` already grants `InternalsVisibleTo` to UnitTests + Benchmark. No PublicAPI analyzer/baseline exists in the repo (`grep` for `PublicAPI*.txt` / `Microsoft.CodeAnalysis.PublicApiAnalyzers` → none), so there is no API-surface file to update.

### PKG-01 — the three removal points
```
1. Delete dir:  src/Core/ExistForAll.SimpleSettings.Core.AspNet/
2. src/SimpleSettings.slnx  — delete line 13:
     <Project Path="Core/ExistForAll.SimpleSettings.Core.AspNet/ExistForAll.SimpleSettings.Core.AspNet.csproj" />
3. src/Tests/.../ExistForAll.SimpleSettings.UnitTests.csproj — delete line 19:
     <ProjectReference Include="..\..\Core\ExistForAll.SimpleSettings.Core.AspNet\..." />
```
The test project's own `Core/AspNet/Environments.cs` (namespace `...UnitTests.Core.AspNet`) is a **local duplicate** and stays; it does not reference the package (the package's `Environments` is `internal` with no `InternalsVisibleTo` to the test assembly). Verified: no `using ExistForAll.SimpleSettings.Core.AspNet` anywhere in `src/`.

### SRC-02 — new option (`CommandLineSettingsBinderOptions.cs`)
```csharp
/// <summary>Skip args[0] (the executable path) when parsing.
/// Default true — correct for Environment.CommandLine / GetCommandLineArgs().
/// Set false when passing Main(string[] args), which already excludes the exe.</summary>
public bool SkipFirstArgument { get; set; } = false; // AddCommandLine() sets true internally (refined D-05)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single unconditional pin (`10.0.9` all TFMs) | Per-TFM conditional `PackageVersion` | This phase | net8 consumers no longer forced to 10.x |
| `Core.AspNet.Environments` constants | ASP.NET Core's built-in `Environments` | This phase | One dead package removed |
| Inline `--k=v` only + drop bare tokens | `--k=v` **and** `--k v` lookahead | This phase | Quoted-with-spaces values bind |

**Deprecated/outdated:** `ExistForAll.SimpleSettings.Core.AspNet` package — unlist its NuGet alphas after removal.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Unlisting the `Core.AspNet` alphas is desired handling for a pre-stable package (vs leaving them listed). | Runtime State Inventory | Low — cosmetic registry hygiene; owner may choose to leave them. Requires the publishing (`guy-lud`) account. |
| A2 | `Benchmark` assembly does not reference `Core.AspNet` (only UnitTests + slnx do). | PKG-01 | Low — verified by grep of `*.csproj`; if a hidden ref exists the build would fail loudly at restore. |

**Note:** A1/A2 are the only non-VERIFIED items; both are low-risk registry/graph hygiene, not code correctness.

## Open Questions (RESOLVED)

> **Resolved 2026-07-14 (Phase-3 planning, owner-approved):**
> 1. **Q1 — RESOLVED:** `AddCommandLine()` switches to `Environment.GetCommandLineArgs()` (in-scope). The exe-skip is owned by `AddCommandLine()` internally (split-by-entry-point per refined D-05), *not* a shared `SkipFirstArgument` default — so `AddArguments(mainArgs)` binds exactly what it is handed. The "quoted value with spaces" success criterion holds end-to-end for the `AddCommandLine()` path.
> 2. **Q2 — RESOLVED:** net8 floor = **latest-patch-per-package** (Option 1): `Configuration` 8.0.0, `Configuration.Json` 8.0.1, `DependencyInjection` 8.0.1, `DependencyInjection.Abstractions` 8.0.2; net10 stays 10.0.9.

1. **Should `AddCommandLine()` switch from `Environment.CommandLine.Split(' ')` to `Environment.GetCommandLineArgs()`?**
   - What we know: the current split is quote-unsafe; the success criterion is "a quoted command-line value with spaces binds correctly". The D-04 lookahead fix only helps callers who pass already-tokenized args (`Main`/`GetCommandLineArgs`), **not** the `Environment.CommandLine.Split(' ')` path.
   - What's unclear: whether the owner considers `AddCommandLine()` in-scope for SRC-02 or only the binder `Parse`.
   - Recommendation: change `AddCommandLine()` to `Environment.GetCommandLineArgs()` (small, low-risk, and it makes the headline "quoted value with spaces" criterion true end-to-end for the convenience method). Flag for the discuss/plan step; treat as in-scope unless the owner defers it.

2. **Uniform `8.0.0` vs latest-patch-per-package for the net8 floor?** (see Standard Stack note) — recommend latest-patch (Option 1) to match the decision wording; owner may prefer uniform `8.0.0`.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/test/restore | ✓ | 10.0.100 (pinned in `src/global.json`) | — |
| net8.0 runtime/targeting | net8 test target + PKG-02 restore | ✓ (CI installs `8.0.x`) | 8.0.x | CI provides via `setup-dotnet` |
| NuGet.org access | PKG-02 restore, PKG-01 unlist | ✓ | — | — |
| `dotnet nuget delete` (unlist) | PKG-01 D-02b | ✓ (SDK CLI) | — | nuget.org web UI |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none blocking. (The local machine may lack the net8.0 **apphost** pack, but test projects set `UseAppHost=false` and libraries don't need it; CI installs the 8.0 runtime for the net8 test target.)

## Validation Architecture

> nyquist_validation is enabled — this section maps each success criterion to an automated check.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | TUnit 1.58.0 on Microsoft.Testing.Platform |
| Config file | `src/global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`) |
| Quick run command | `cd src && dotnet test SimpleSettings.slnx -c Release --treenode-filter "/*/*/ArgumentsTests/*"` |
| Full suite command | `cd src && dotnet restore SimpleSettings.slnx && dotnet build SimpleSettings.slnx -c Release --no-restore -p:ContinuousIntegrationBuild=true && dotnet test SimpleSettings.slnx -c Release --no-build` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| API-01 | `SettingsHolder` internal; build + suite green on both TFMs | build + suite | full suite command above | ✅ (green suite is the check; no new test needed — surface change is compile-verified by `InternalsVisibleTo`) |
| PKG-01 | `Core.AspNet` removed; solution builds; no test references it | build | `dotnet build SimpleSettings.slnx -c Release` | ✅ (build-green is the check) |
| PKG-02 | net8 resolves `8.0.x`, net10 resolves `10.0.9` | restore + assets inspection | restore, then assert `obj/**/project.assets.json` `targets["net8.0"]` shows `Microsoft.Extensions.Configuration/8.0.0` and **not** `10.` ; `targets["net10.0"]` shows `10.0.9` | ❌ Wave 0 (add a restore-assertion script / doc step) |
| SRC-02 | `--k=v`, `--k:v`, `--k v`, `--k "v with spaces"`, prefixed-next-token = new key, arg[0] skipped/kept per option | unit | `dotnet test ... --treenode-filter "/*/*/ArgumentsTests/*"` (extend) or a new `CommandLineParseTests` | ⚠️ Partial — `ArgumentsTests.cs` exists; add space-separated + arg[0] + prefixed-next-token cases |

### Sampling Rate
- **Per task commit:** targeted `--treenode-filter` for the touched test class.
- **Per wave merge:** full suite on both TFMs (CI runs net8 + net10).
- **Phase gate:** full suite green + PKG-02 restore-assertion green before `/gsd-verify-work`.

### PKG-02 restore/floor check (the one non-suite validation)
```bash
cd src
dotnet restore SimpleSettings.slnx
# assert net8 floor and net10 pin from the core library's assets:
python3 - <<'PY'
import json,glob,sys
ok=True
for f in glob.glob("Core/ExistForAll.SimpleSettings/obj/project.assets.json"):
    a=json.load(open(f))
    for tfm,libs in a["targets"].items():
        for k in libs:
            if k.lower().startswith("microsoft.extensions.configuration/"):
                v=k.split("/")[1]
                bad = (tfm.startswith("net8") and v.startswith("10.")) or (tfm.startswith("net10") and not v.startswith("10."))
                print(tfm, k, "BAD" if bad else "ok"); ok = ok and not bad
sys.exit(0 if ok else 1)
PY
```
*(This exact technique was used during research to verify the conditional-`PackageVersion` shape.)*

### Wave 0 Gaps
- [ ] Extend `src/Tests/.../Binders/CommandLine/ArgumentsTests.cs` (or add `CommandLineParseTests.cs`) — covers SRC-02: `--k v`, `--k "v with spaces"` (single token), `--k --other` (prefixed next → new key, no value), `SkipFirstArgument` true/false.
- [ ] Add the PKG-02 restore-assertion step above to the plan's verification (no framework install needed — `python3` is present).
- [ ] Framework install: none — TUnit + SDK already present.

## Security Domain

> security_enforcement enabled, ASVS L1. This phase is mostly surface-reduction (net security-positive).

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | — |
| V3 Session Management | no | — |
| V4 Access Control | no | — |
| V5 Input Validation | partial | CLI binder parses untrusted `string[]` into string config values only — no eval/injection sink; values flow to typed conversion which is already secret-safe (SEC-01). No new validation risk. |
| V6 Cryptography | no | — |
| V14 Config & Dependencies | yes | Per-TFM floors use Microsoft first-party versions; net8 floors (`8.0.0/8.0.1/8.0.2`) carry no known critical advisory for these specific packages. Lowering the floor does not add a vulnerable version; consumers can still float up. |

### Known Threat Patterns for this stack
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Reducing `SettingsHolder` visibility | (Info disclosure ↓) | API-01 shrinks public surface — positive |
| Dependency floor float | Supply chain / Tampering | First-party packages only; verified versions from nuget.org flatcontainer; CI restores from `api.nuget.org` |
| CLI arg parsing of untrusted input | Tampering | String-only config values; no command execution; existing secret-safe exception invariant (SEC-01) unchanged |

**Net effect:** security-neutral-to-positive. No `block_on: high` findings.

## Sources

### Primary (HIGH confidence)
- **Live `dotnet restore` + `dotnet build /p:TreatWarningsAsErrors=true`** of an isolated `net8.0;net10.0` project with conditional `PackageVersion` — proved NU1504-free and confirmed per-TFM resolution (net8→8.0.0/8.0.1, net10→10.0.9).
- **nuget.org flatcontainer index** (`api.nuget.org/v3-flatcontainer/<pkg>/index.json`) — exact `8.0.x` / `10.0.x` version lists per package; `Core.AspNet` published-version list.
- **dotnet/runtime `CommandLineConfigurationProvider.cs`** (`main`, raw GitHub) — authoritative parse algorithm quoted for parity.
- **Codebase reads/greps** — `SettingsHolder`/`ISettingsHolder`/`SettingsCollection`/`Info.cs`, `Directory.Packages.props`, `SimpleSettings.slnx`, `UnitTests.csproj`, `Core.AspNet/Environments.cs`, `CommandLineSettingsBinder.cs`, `SettingsBuilderFactoryExtensions.cs`, `ArgumentsTests.cs`; no PublicAPI analyzer present.

### Secondary (MEDIUM confidence)
- WebFetch summary of the CommandLine provider (cross-checked against the raw source quote above).

### Tertiary (LOW confidence)
- None.

## Metadata

**Confidence breakdown:**
- Standard stack / versions: HIGH — every version verified against nuget.org on 2026-07-14.
- PKG-02 mechanics: HIGH — verified by live restore, not just docs.
- CLI parity: HIGH — quoted from the runtime source; divergence from Microsoft explicitly noted.
- PKG-01 removal safety: HIGH — dead reference confirmed by grep; NuGet publish state confirmed by API.
- Unlisting handling: MEDIUM — standard practice, but low-stakes owner choice (A1).

**Research date:** 2026-07-14
**Valid until:** 2026-08-13 (versions may gain new servicing patches; re-check `8.0.x`/`10.0.x` if planning slips a month).
