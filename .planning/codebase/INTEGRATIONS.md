# External Integrations

**Analysis Date:** 2026-07-13

## APIs & External Services

This is a self-contained .NET configuration-binding library. It has no runtime dependencies on external HTTP APIs or SaaS services. Its "integrations" are the configuration sources it binds from and the build/publish infrastructure.

**Configuration source binders** (`src/Core/ExistForAll.SimpleSettings.Extensions.Binders/`):
- Environment variables - `EnvironmentVariableBinder.cs` (reads `Environment.GetEnvironmentVariables()`, optional prefix + name formatter).
- Command line arguments - `CommandLineSettingsBinder.cs` / `CommandLineSettingsBinderOptions.cs`.
- `Microsoft.Extensions.Configuration` (`IConfiguration`) - `ConfigurationBinder.cs`, bridging any registered config provider (JSON, etc.).

## Data Storage

**Databases:**
- None. The library binds in-memory/config-provider values into typed settings interfaces.

**File Storage:**
- Local filesystem only, indirectly via `Microsoft.Extensions.Configuration.Json` config sources (used in tests, not a hard dependency of the core library).

**Caching:**
- Internal per-type settings plan cache only (see `SettingsPlan.cs` in the core project); no external cache service.

## Authentication & Identity

**Auth Provider:**
- None. No authentication or identity handling in the library.

## Monitoring & Observability

**Error Tracking:**
- None. Errors surface as typed exceptions (`SettingsBindingException`, `SettingsPropertyValueException`, etc. in `src/Core/ExistForAll.SimpleSettings/`).

**Logs:**
- No logging framework. BenchmarkDotNet writes run logs under `src/BenchmarkDotNet.Artifacts/` during benchmarking.

## CI/CD & Deployment

**Hosting:**
- GitHub (`github.com/existall/SimpleSettings`). Distribution target is NuGet.org.

**CI Pipeline (GitHub Actions, `.github/workflows/`):**
- `ci.yml` - PR validation on `master`: builds and tests against .NET 8 + 10 from `src/`. Read-only permissions.
- `release.yml` - On push to `master` produces alpha prereleases; `workflow_dispatch` supports beta/rc/stable channels with SemVer bump and dry-run. Packs and pushes to NuGet (`NUGET_SOURCE=https://api.nuget.org/v3/index.json`). Tag prefix `v`, baseline version `2.0.0`.
- `benchmark.yml` - Runs BenchmarkDotNet on push/PR to `master`; gates on allocated bytes (deterministic) rather than wall-clock time. Records baselines to the `gh-pages` data branch; comments on regressing PRs.

**Actions used:** `actions/checkout@v4` (fetch-depth 0 for MinVer), `actions/setup-dotnet@v4`.

## Environment Configuration

**Required env vars (runtime library):**
- None mandatory. Consumers may expose settings through environment variables that `EnvironmentVariableBinder` reads (optional prefix supported).

**CI env vars:**
- `DOTNET_NOLOGO`, `DOTNET_CLI_TELEMETRY_OPTOUT`, `DOTNET_SKIP_FIRST_TIME_EXPERIENCE`, `NUGET_PACKAGES`, `SOLUTION`, and release-specific `NUGET_SOURCE`, `TAG_PREFIX`, `BASELINE_VERSION`, `ARTIFACTS`.

**Secrets location:**
- GitHub Actions secrets (used by `release.yml` for the NuGet publish token). Not stored in the repo. No `.env` files present.

## Webhooks & Callbacks

**Incoming:**
- None (library, not a service).

**Outgoing:**
- None.

---

*Integration audit: 2026-07-13*
