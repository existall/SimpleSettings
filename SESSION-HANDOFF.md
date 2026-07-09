# SESSION HANDOFF — SimpleSettings modernization

_Last updated: 2026-07-09 · owner: Guy Ludvig (guy@frontegg.com)_

## TL;DR
The modernization (xUnit → TUnit, Central Package Management, latest NuGets, `.slnx`, legacy cleanup) is **complete, committed, pushed, and open as PR #5**:
https://github.com/existall/SimpleSettings/pull/5

Build is green (**0 warnings / 0 errors**) and **39/39 tests pass**. **One item is deferred:** the GitHub Actions CI workflow was NOT updated (the available token lacked the `workflow` scope). The ready-to-apply YAML is in the appendix below.

## Environment
- Repo: `existall/SimpleSettings` · working dir: `/Users/guyludvig/frontegg/development/open-source/SimpleSettings`
- The solution lives under `src/` → `src/ExistAll.SimpleConfig.slnx`
- .NET SDK **10.0.301**; **only the .NET 10 runtime is installed locally** (net8.0 compiles but its tests can only run where a net8.0 runtime exists, e.g. CI)
- Target frameworks: libraries `net8.0;net10.0`; benchmark `net10.0`

## Current git state
- Branch: `modernize/tunit-cpm-upgrade` (base `master` @ `65f209d`)
- HEAD: `5b002e0` · working tree clean
- PR: **#5 OPEN** (base `master`)
- Commits (oldest → newest):
  | Hash | Subject |
  |------|---------|
  | `b4e8b37` | Drop classic .NET Framework projects |
  | `9ede951` | Adopt Central Package Management and shared build props; enable nullable |
  | `8273e9d` | Upgrade NuGet packages to latest |
  | `12a129d` | Migrate tests from xUnit to TUnit 1.58 |
  | `5b002e0` | Modernize solution to .slnx and remove legacy build tooling |

## Done
- **TUnit 1.58** on Microsoft.Testing.Platform; all 11 test files migrated (`[Fact]`/`[Theory]`→`[Test]`/`[Arguments]`, awaited `Assert.That(...)`). Dropped `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, and unused `NSubstitute`. Env-var tests carry `[NotInParallel("EnvironmentVariables")]` (TUnit parallelizes within a class by default).
- **CPM**: `src/Directory.Packages.props`. **Shared props**: `src/Directory.Build.props` (`Nullable` + `ImplicitUsings` + `LangVersion latest` + package metadata folded in from the removed `version.props`). `src/global.json` (SDK pin + MTP test runner).
- **Nullable** enabled repo-wide; all 138 resulting warnings resolved with annotations only (no behavior change).
- **Upgrades**: `Microsoft.Extensions.*` → 10.0.9, BenchmarkDotNet → 0.15.8, TUnit 1.58.0.
- **Solution/cleanup**: `.sln` → `.slnx`; removed the legacy `packages/` dir, Cake/AppVeyor scripts (`build.*`, both `appveyor.yml`), and the orphaned `ExistsForAll.SimpleConfig.Benchmark` project.

## Pending / next steps
1. **Apply the CI workflow** (deferred). Two options:
   - **GitHub web editor** (simplest — commits workflow files without a local `workflow`-scoped token): on the `modernize/tunit-cpm-upgrade` branch, edit `.github/workflows/dotnet.yml` and paste the YAML from the appendix.
   - **Locally**: `gh auth refresh -h github.com -u guy-lud -s workflow`, then commit the appendix YAML to `.github/workflows/dotnet.yml` and push (see push recipe below).
2. **PR #5** — review and merge.
3. Optional: add reviewers/labels to PR #5.

## Gotchas a new session MUST know

### Pushing to this repo
- The active `gh` account **`guy-frontegg` is read-only** on `existall/SimpleSettings`. **`guy-lud`** has push/admin.
- A **global git rewrite** turns HTTPS into SSH: `url."git@github.com:".insteadOf https://github.com/`, and the loaded SSH key authenticates as `guy-frontegg` (denied). So a normal `git push` fails.
- Neither `gh` token has the **`workflow`** scope (blocks pushing changes under `.github/workflows/`).
- Recipe that works — push as `guy-lud` over real HTTPS (bypassing the SSH rewrite), then restore:
  ```bash
  gh auth switch -u guy-lud
  GIT_CONFIG_GLOBAL=/dev/null git \
    -c "url.https://github.com/existall/SimpleSettings.git.insteadOf=https://github.com/existall/SimpleSettings.git" \
    -c credential.helper= -c credential.helper='!gh auth git-credential' \
    push https://github.com/existall/SimpleSettings.git <branch>:<branch>
  gh auth switch -u guy-frontegg   # restore original active account
  ```

### Building / testing
- **Run `dotnet` from `src/`** so `global.json`'s Microsoft.Testing.Platform opt-in is found. From the repo root, `dotnet test` silently falls back to the legacy VSTest path and **fails on .NET 10**.
- Build: `cd src && dotnet build ExistAll.SimpleConfig.slnx -c Release` → expect **0/0**.
- Test (local; net10 only): `cd src && dotnet test ExistAll.SimpleConfig.slnx -c Release -f net10.0` → expect **39/39**.
- net8.0 tests are build-only locally (no runtime); they run in CI once the workflow is applied.

## Appendix — CI workflow to apply (`.github/workflows/dotnet.yml`)
```yaml
name: .NET

on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET (8.0 + 10.0)
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            10.0.x

      # Run from src/ so global.json (which opts into Microsoft.Testing.Platform
      # for TUnit) is discovered — running from the repo root falls back to the
      # legacy VSTest path and fails on .NET 10.
      - name: Restore
        run: dotnet restore ExistAll.SimpleConfig.slnx
        working-directory: src

      - name: Build
        run: dotnet build ExistAll.SimpleConfig.slnx --configuration Release --no-restore
        working-directory: src

      - name: Test (net8.0 + net10.0)
        run: dotnet test ExistAll.SimpleConfig.slnx --configuration Release --no-restore
        working-directory: src
```
