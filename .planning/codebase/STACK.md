# Technology Stack

**Analysis Date:** 2026-07-13

## Languages

**Primary:**
- C# - All source code across `src/Core`, `src/Tests`, `src/performance`. Language version is intentionally unset in `src/Directory.Build.props`; each target framework uses its default (net8.0 → C# 12, net10.0 → C# 14).

**Secondary:**
- MSBuild XML - Build/packaging configuration (`src/Directory.Build.props`, `src/Directory.Packages.props`, `*.csproj`).
- YAML - CI/CD workflow definitions (`.github/workflows/*.yml`).

## Runtime

**Environment:**
- .NET (modern). Libraries multi-target `net8.0;net10.0` (see each `*.csproj`). Classic .NET Framework support has been dropped.
- Benchmark project targets `net10.0` only (`src/performance/ExistForAll.SimpleSettings.Benchmark/ExistForAll.SimpleSettings.Benchmark.csproj`).

**SDK:**
- .NET SDK `10.0.100` pinned via `src/global.json` with `rollForward: latestFeature`, `allowPrerelease: false`.
- CI installs both `8.0.x` and `10.0.x` runtimes (`.github/workflows/ci.yml`).

**Package Manager:**
- NuGet with Central Package Management enabled (`ManagePackageVersionsCentrally=true` in `src/Directory.Packages.props`).
- Single package source: nuget.org only, with `<clear />` (`src/nuget.config`).
- Lockfile: not present (no `packages.lock.json`); versions are pinned centrally instead.

## Frameworks

**Core:**
- `Microsoft.Extensions.Configuration` 10.0.9 - Configuration abstraction consumed by the binders.
- `Microsoft.Extensions.Configuration.Json` 10.0.9 - JSON configuration source (tests).
- `Microsoft.Extensions.DependencyInjection` / `.Abstractions` 10.0.9 - DI integration (GenericHost extensions, tests).

**Testing:**
- `TUnit` 1.58.0 - Test framework running on Microsoft.Testing.Platform (opted in via `src/global.json` `test.runner`).

**Build/Dev:**
- `MinVer` 7.0.0 - Git-tag-driven versioning (`MinVerTagPrefix=v`, `MinVerMinimumMajorMinor=2.0` in `src/Directory.Build.props`).
- `BenchmarkDotNet` 0.15.8 - Allocation/performance benchmarking (`src/performance/...Benchmark`).

## Key Dependencies

**Critical:**
- `Microsoft.Extensions.Configuration` 10.0.9 - Backbone for binding configuration into settings interfaces.
- `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.9 - Enables registering settings into the service collection.

**Infrastructure:**
- `MinVer` 7.0.0 (PrivateAssets=all) - Derives package version from git tags.
- `BenchmarkDotNet` 0.15.8 - Regression gating in CI (allocation-based).

## Configuration

**Environment:**
- No `.env` files. The library itself *reads* environment variables at runtime via `EnvironmentVariableBinder` (`src/Core/ExistForAll.SimpleSettings.Extensions.Binders/EnvironmentVariableBinder.cs`).
- CI sets `DOTNET_NOLOGO`, `DOTNET_CLI_TELEMETRY_OPTOUT`, `DOTNET_SKIP_FIRST_TIME_EXPERIENCE`, `NUGET_PACKAGES` (`.github/workflows/*.yml`).

**Build:**
- `src/Directory.Build.props` - Shared build settings: `Nullable=enable`, `ImplicitUsings=enable`, deterministic/SourceLink-enabled reproducible packaging, shared NuGet metadata (Authors, license MIT, icon, README), MinVer config.
- `src/Directory.Packages.props` - Central package versions.
- `src/global.json` - SDK pin + test runner selection.
- `src/nuget.config` - Restricts restore to nuget.org.
- Solution file: `src/SimpleSettings.slnx` (XML solution format; referenced by CI as `$SOLUTION`).

## Platform Requirements

**Development:**
- .NET SDK 10.0.100+ (rolls forward within latest feature band).
- Run `dotnet test` / `dotnet build` from the `src/` directory (where `global.json` lives).

**Production:**
- Distributed as NuGet packages (libraries), consumed by any net8.0 or net10.0 application. No standalone deployment target.

---

*Stack analysis: 2026-07-13*
