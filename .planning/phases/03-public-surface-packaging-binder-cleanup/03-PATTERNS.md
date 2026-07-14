# Phase 3: Public Surface, Packaging & Binder Cleanup - Pattern Map

**Mapped:** 2026-07-14
**Files analyzed:** 6 modified + 2 test/verification + 3 removals
**Analogs found:** 6 / 6 (all in-repo; this phase edits existing files rather than creating new abstractions)

> This phase is almost entirely *editing existing files in place* and *removing* surface — not
> creating new files from a template. So the "closest analog" for most edits is the file itself
> (its own surrounding conventions) plus one sibling that already does the target thing. Excerpts
> below are the concrete lines a plan should copy/modify.

## File Classification

| Modified/Removed/New File | Role | Data Flow | Closest Analog | Match Quality |
|---------------------------|------|-----------|----------------|---------------|
| `src/Core/ExistForAll.SimpleSettings/SettingsHolder.cs` | model (DTO) | transform | `src/Core/ExistForAll.SimpleSettings/ISettingsHolder.cs` (already internal) | exact |
| `src/Directory.Packages.props` | config (CPM) | build | RESEARCH Pattern 1 (verified conditional `PackageVersion`) | exact (verified excerpt) |
| `src/SimpleSettings.slnx` | config (solution) | build | self (existing `<Project Path=...>` lines) | exact |
| `src/Tests/.../ExistForAll.SimpleSettings.UnitTests.csproj` | config (test proj) | build | self (existing `<ProjectReference>` lines) | exact |
| `src/Core/.../CommandLineSettingsBinder.cs` (`Parse`/`SplitByDelimiter`) | binder (service) | transform / batch | self + `SplitByDelimiter` existing logic | exact (in-place) |
| `src/Core/.../CommandLineSettingsBinderOptions.cs` (add `SkipFirstArgument`) | config (options) | request-response | self (`IsCaseSensitive` bool prop) | exact |
| `src/Core/.../SettingsBuilderFactoryExtensions.cs` (`AddCommandLine`) | extension (utility) | transform | self (`AddCommandLine` line 44) | exact — Open Q #1 |
| `src/Tests/.../Binders/CommandLine/ArgumentsTests.cs` (extend) OR new `CommandLineParseTests.cs` | test | transform | `ArgumentsTests.cs` (same dir) | exact |
| `src/Core/ExistForAll.SimpleSettings.Core.AspNet/` (REMOVE) | — | — | — | removal |

## Pattern Assignments

### `SettingsHolder.cs` (model — API-01, D-01)

**Analog:** `ISettingsHolder.cs:5` (already `internal`) — mirror its accessibility.

**The only change** (`SettingsHolder.cs:5`):
```csharp
// before
public class SettingsHolder : ISettingsHolder
// after
internal sealed class SettingsHolder : ISettingsHolder
```

**Convention to preserve** (rest of file, lines 1-16): block-scoped namespace, one public type
per file, ctor-assigned get-only props. No other edit — `InternalsVisibleTo` already granted in
`Info.cs`:
```csharp
[assembly:InternalsVisibleTo("ExistForAll.SimpleSettings.UnitTests")]
[assembly:InternalsVisibleTo("ExistForAll.SimpleSettings.Benchmark")]
```
No PublicAPI analyzer baseline exists in the repo → no surface file to update.

---

### `Directory.Packages.props` (config/CPM — PKG-02, D-03)

**Analog:** RESEARCH Pattern 1 (verified live-restore excerpt). Replaces lines 8-12.

**Current** (`Directory.Packages.props:7-13`):
```xml
<ItemGroup>
  <!-- Microsoft.Extensions -->
  <PackageVersion Include="Microsoft.Extensions.Configuration" Version="10.0.9" />
  <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="10.0.9" />
  <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.9" />
  <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.9" />
</ItemGroup>
```

**Target — per-TFM conditional items** (Option 1, latest-patch-per-package; each item MUST be
mutually-exclusively conditioned or NU1504 fires):
```xml
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
net8 patches differ per package (`8.0.0 / 8.0.1 / 8.0.1 / 8.0.2`) → the conditional-item shape,
NOT the shared-property shape. Leave TUnit/BenchmarkDotNet/MinVer ItemGroups (lines 15-28)
untouched. Owner may pick uniform `8.0.0` (Option 2) instead — surface both in the plan.

**Verification** (RESEARCH §Validation): `dotnet restore` then assert
`Core/ExistForAll.SimpleSettings/obj/project.assets.json` `targets["net8.0"]` shows
`Microsoft.Extensions.Configuration/8.0.0` (not `10.`) and `targets["net10.0"]` shows `10.0.9`.

---

### `SimpleSettings.slnx` + `UnitTests.csproj` (config — PKG-01, D-02)

**Analog:** the surrounding sibling lines (identical XML shape).

Remove **`SimpleSettings.slnx:13`**:
```xml
<Project Path="Core/ExistForAll.SimpleSettings.Core.AspNet/ExistForAll.SimpleSettings.Core.AspNet.csproj" />
```
Remove **`UnitTests.csproj:19`**:
```xml
<ProjectReference Include="..\..\Core\ExistForAll.SimpleSettings.Core.AspNet\ExistForAll.SimpleSettings.Core.AspNet.csproj" />
```
Then delete the directory `src/Core/ExistForAll.SimpleSettings.Core.AspNet/` (incl. stale
`obj/`+`bin/`). The test project's own `Core/AspNet/Environments.cs` (namespace
`...UnitTests.Core.AspNet`) is a local duplicate — **leave it**; it does not reference the package.
Post-removal: unlist the published `2.0.0-alpha.0.*` alphas via
`dotnet nuget delete <id> <version> -s https://api.nuget.org/v3/index.json --non-interactive`
(unlists, not deletes) under the `guy-lud` account (A1 — owner-optional).

---

### `CommandLineSettingsBinderOptions.cs` (options — SRC-02, D-05)

**Analog:** the existing `IsCaseSensitive` auto-prop with default (`CommandLineSettingsBinderOptions.cs:18`):
```csharp
public bool IsCaseSensitive { get; set; } = true;
```

**Add, same shape** (RESEARCH §Code Examples):
```csharp
/// <summary>Skip args[0] (the executable path) when parsing.
/// Default false — AddArguments(Main(string[])) binds exactly what it is handed.
/// AddCommandLine() sets this true internally (its GetCommandLineArgs() source has the exe at [0]).</summary>
public bool SkipFirstArgument { get; set; } = false;
```
Preserve the existing prefix/delimiter list conventions (`_argumentPrefixes`, `_delimiters`,
collection-expression init lines 9-10) — D-06 keeps all existing options unchanged.

---

### `CommandLineSettingsBinder.cs` — `Parse` / `SplitByDelimiter` (binder — SRC-02, D-04)

**Analog:** the file's own existing `Parse` loop + `SplitByDelimiter`; diverges from Microsoft's
`CommandLineConfigurationProvider` (which consumes next token *unconditionally* — SimpleSettings
does NOT). Keep the fix inside these two members; no new abstraction.

**Existing `Parse`** (`CommandLineSettingsBinder.cs:37-54`) — a plain `foreach` with no lookahead;
this is what changes to an index/enumerator loop:
```csharp
private void Parse(string[] args)
{
   _argumentStore.Clear();
   if (args == null) return;

   foreach (var arg in args)
   {
      var (key, value) = SplitByDelimiter(arg, _options)!;
      var name = key.TrimStart(_options.ArgumentPrefixes.ToArray());
      if (value != null)
         _argumentStore[name] = value;
   }
}
```

**Target behavior** (implement, ~15 lines):
1. If `_options.SkipFirstArgument` and `args.Length > 0`, start iteration at index 1 (D-05).
2. For each token: run `SplitByDelimiter` (existing inline `--k=v`/`--k:v` path stays).
3. If `value == null` AND the token was prefixed (had a stripped prefix) AND a next token exists
   that is **not** prefixed → consume next token as value, advance index (D-04 lookahead).
4. If the next token IS prefixed (`-`/`/`) → current key stays valueless → not stored (preserves
   today's "no value ⇒ skip" via the existing `if (value != null)` guard).

**`SplitByDelimiter` stays as-is** (`:56-90`) — min-index delimiter wins, `Substring(idx+1)` is the
value (matches Microsoft's first-`=`-wins semantics). Detecting "was prefixed" reuses
`key.TrimStart(_options.ArgumentPrefixes.ToArray())` — compare length before/after trim, or check
`_options.ArgumentPrefixes.Contains(token[0])`.

**Anti-patterns (RESEARCH):** no switch-mappings, no short-flag/boolean semantics, no manual
quote-stripping (shell already unquotes).

---

### `SettingsBuilderFactoryExtensions.cs` — `AddCommandLine` (extension — SRC-02, Open Q #1)

**Analog:** the method itself (`SettingsBuilderFactoryExtensions.cs:41-49`):
```csharp
public static T AddCommandLine<T>(this T target,
    Action<CommandLineSettingsBinderOptions>? action = null) where T : ISettingsBuilderFactory
{
    var args = Environment.CommandLine.Trim().Split(' ');   // <-- quote-unsafe
    target.AddArguments(args, action);
    return target;
}
```

**Recommended change (RESEARCH Open Q #1 / Pitfall 3):** replace
`Environment.CommandLine.Trim().Split(' ')` with `Environment.GetCommandLineArgs()` — properly
tokenized `string[]`, `[0]` = exe path (which `AddCommandLine` drops by setting `SkipFirstArgument=true` internally). This
makes the "quoted value with spaces" criterion true end-to-end for the convenience method.
**RESOLVED — in-scope** (Open Q #1). `AddArguments` (`:51-61`) keeps the option default `false` and binds exactly what it is handed; `AddCommandLine` wraps the caller action to set `SkipFirstArgument=true`.

---

### `ArgumentsTests.cs` (test — SRC-02)

**Analog / location:** `src/Tests/.../Binders/CommandLine/ArgumentsTests.cs` (extend in place, or
add sibling `CommandLineParseTests.cs` in the same dir).

**Existing test shape to copy** (`ArgumentsTests.cs:10-20`):
```csharp
[Test]
public async Task Build_WhenNameMatchKey_ShouldPlaceValue()
{
    var sut = SettingsBuilder.CreateBuilder(x =>
        x.AddArguments(Args.Split(' ')));
    var result = sut.GetSettings<ICommandLineInterface>();
    await Assert.That(result.Name).IsEqualTo("value");
    await Assert.That(result.Age).IsNotEqualTo(3);
}
```
Conventions: TUnit `[Test]`, `async Task`, `await Assert.That(...).IsEqualTo(...)`, nested
`ICommandLineInterface` with `[SettingsProperty("name")]`, fluent options via
`o => o.SetCaseSensitivity(false)`. **Fixture gotcha (Pitfall 4):** the existing `Args =
" name=value age:3"` has a leading space → `Split(' ')[0] == ""`, so under the refined default
`SkipFirstArgument=false` the empty token is parsed safely and not stored, and the suite stays
green as-is. Keep the fixture unchanged.

**New cases to add:** `--k v` (space-separated), `--k "v with spaces"` (single already-unquoted
token), `--k --other` (prefixed next → new key, current valueless → not stored),
`SkipFirstArgument` true vs false.

## Shared Patterns

### File & namespace conventions
**Source:** all `src/Core/**/*.cs`
**Apply to:** every edited/new `.cs`
- One public type per file; `I`-prefixed interfaces; block-scoped `namespace ExistForAll.SimpleSettings[.Sub] { }`.
- Multi-target `net8.0;net10.0` only — validate BOTH TFMs after every change.

### Options-object pattern
**Source:** `CommandLineSettingsBinderOptions.cs` (auto-props with defaults + fluent `Add*`/`Clear*` mutators)
**Apply to:** the new `SkipFirstArgument` option — plain `{ get; set; } = false;` mirrors the `IsCaseSensitive` bool-prop shape (the default value differs; `AddCommandLine` sets it true internally per refined D-05).

### Section-binder contract
**Source:** `CommandLineSettingsBinder : ISectionBinder` (ctor takes args+options, `Parse` fills `_argumentStore`, `BindPropertySettings` reads it)
**Apply to:** SRC-02 stays entirely within this contract — no new abstraction.

### CPM version policy
**Source:** `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`, `<PackageVersion Include=.../>`)
**Apply to:** PKG-02 — conditional items only; never mix an unconditional + conditional item for one package (NU1504).

### Test conventions (TUnit)
**Source:** `ArgumentsTests.cs`
**Apply to:** all new binder tests. Run via `--treenode-filter "/*/*/ArgumentsTests/*"` or unfiltered
(NOT `--filter`, which exits 5 on Microsoft.Testing.Platform).

## No Analog Found

None — every change targets an existing file or removes one. The only "new" artifact is the PKG-02
restore-assertion verification (RESEARCH §Validation provides a ready `python3` script; not source
code).

## Metadata

**Analog search scope:** `src/Core/**`, `src/Tests/**`, `src/Directory.Packages.props`, `src/SimpleSettings.slnx`
**Files scanned:** 9 (SettingsHolder, ISettingsHolder mention, Info.cs, Directory.Packages.props, slnx, UnitTests.csproj, CommandLineSettingsBinder, CommandLineSettingsBinderOptions, SettingsBuilderFactoryExtensions, ArgumentsTests)
**Pattern extraction date:** 2026-07-14
