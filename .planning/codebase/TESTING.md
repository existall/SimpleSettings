# Testing Patterns

**Analysis Date:** 2026-07-13

## Test Framework

**Runner:**
- TUnit 1.58.0 (version pinned in `src/Directory.Packages.props`).
- Runs on **Microsoft.Testing.Platform** (not VSTest). Opt-in is declared in `src/global.json`:
  ```json
  "test": { "runner": "Microsoft.Testing.Platform" }
  ```
- Test project `src/Tests/ExistForAll.SimpleSettings.UnitTests/ExistForAll.SimpleSettings.UnitTests.csproj` sets `TestingPlatformDotnetTestSupport=true`, `IsTestProject=true`, `IsPackable=false`, `UseAppHost=false`.
- Targets `net8.0;net10.0` — tests must pass on both TFMs.

**Assertion Library:**
- TUnit's fluent assertions. Global usings in `src/Tests/ExistForAll.SimpleSettings.UnitTests/GlobalUsings.cs`:
  `TUnit.Core`, `TUnit.Assertions`, `TUnit.Assertions.Extensions`.

**Run Commands:**
```bash
# Run from src/ (global.json with the Microsoft.Testing.Platform opt-in lives there)
cd src
dotnet restore "$SOLUTION"
dotnet build "$SOLUTION" -c Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test  "$SOLUTION" -c Release --no-build       # run all tests
```
CI runs exactly this sequence with `working-directory: src` (`.github/workflows/ci.yml`).

## Test File Organization

**Location:**
- Separate test project, not co-located with source: `src/Tests/ExistForAll.SimpleSettings.UnitTests/`.
- Organized into feature subfolders mirroring concerns: `Conversion/`, `Core/AspNet/`, `DependencyInjection/`, `Binders/CommandLine/`, `SimpleSettings/`.

**Naming:**
- Test classes suffixed `Tests`: `ConfigurationBinderCacheTests`, `ExceptionRedactionTests`, `SettingsCollectionTests`.
- Test methods use `Scenario_Condition_ExpectedOutcome` underscore style:
  `Bind_MultipleProperties_AllResolveFromCachedSection`, `Convert_SecretToInt_DoesNotLeakValue`.
- Test-only interfaces/fixtures kept alongside their tests, `I`-prefixed: `IMultiProp`, `ICacheProbe`, `IEmailSenderSettings.cs`, `ITestInterface.cs`.

**Structure:**
```
src/Tests/ExistForAll.SimpleSettings.UnitTests/
├── Conversion/         # converter + exception-redaction tests
├── Core/AspNet/        # ASP.NET environment tests
├── DependencyInjection/ # AddSimpleSettings integration tests
├── Binders/CommandLine/ # command-line binder tests
├── SimpleSettings/     # core builder / collection / attribute tests
├── GlobalUsings.cs
├── appSettings.json    # config fixture for JSON-backed tests
└── I*.cs               # shared test settings interfaces
```

## Test Structure

**Suite Organization:**
```csharp
namespace ExistForAll.SimpleSettings.UnitTests
{
    public class ConfigurationBinderCacheTests
    {
        [Test]
        public async Task Bind_MultipleProperties_AllResolveFromCachedSection()
        {
            var config = BuildConfig(new Dictionary<string, string?> { ["MultiProp:A"] = "a" });
            var result = Build<IMultiProp>(new ConfigurationBinder(config));
            await Assert.That(result.A).IsEqualTo("a");
        }

        // private static helpers at the bottom of the class
        private static IConfiguration BuildConfig(Dictionary<string, string?> data)
            => new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }
}
```

**Patterns:**
- Every test is `[Test]` and `async Task` — TUnit assertions are awaited: `await Assert.That(x).IsEqualTo(y)`.
- Arrange/Act/Assert layout, no comment headers.
- Shared arrange logic factored into `private static` helper methods at the bottom of the class (`BuildConfig`, `Build<T>`).
- Small nested test interfaces declared inside the test class when only that class needs them (`public interface IMultiProp { ... }`).

## Mocking

**Framework:** None. No Moq/NSubstitute dependency.

**Patterns:**
- Test doubles are hand-written in-memory implementations rather than mocks — e.g. `InMemoryCollection` / `InMemoryBinder` (used in `Conversion/ExceptionRedactionTests.cs`), custom leaky converters to exercise redaction.
- Real `Microsoft.Extensions.Configuration` with `AddInMemoryCollection` is used instead of mocking `IConfiguration`.

**What to Mock:**
- Prefer real in-memory `IConfiguration` and small concrete fakes over mock frameworks.

**What NOT to Mock:**
- Do not mock the configuration stack; build a real `ConfigurationBuilder().AddInMemoryCollection(...)`.

## Fixtures and Factories

**Test Data:**
- `appSettings.json` in the test project root is packed as a JSON config fixture.
- Environment-variable tests use a disposable RAII helper that sets and reliably clears the variable:
  ```csharp
  internal class DisposableEnvironmentVariable : IDisposable  // sets on ctor, unsets on Dispose
  ```
  Files: `src/Tests/ExistForAll.SimpleSettings.UnitTests/DisposableEnvironmentVariable.cs`,
  `AspCoreDisposableEnvironmentVariable.cs`. Use `using (new DisposableEnvironmentVariable(name, value))` to isolate env-dependent tests.
- Sentinel-based secret tests: a distinctive constant (`const string Secret = "S3CR3T-sentinel-do-not-log"`) is bound and asserted absent from exception `ToString()` (`Conversion/ExceptionRedactionTests.cs`).

**Location:**
- Shared settings interfaces live at the test-project root (`IRoot.cs`, `IArraysInterface.cs`, etc.); scenario-specific ones nest inside their test class.

## Coverage

**Requirements:** None enforced. No coverage gate or threshold in CI. ~70 `[Test]` methods across the unit-test project.

**View Coverage:**
```bash
# Not configured; Microsoft.Testing.Platform coverage would be enabled per its extension if needed
```

## Test Types

**Unit Tests:**
- The bulk. Exercise the builder, converters, collections, attributes in isolation.

**Integration Tests:**
- Present in-project (naming: `*IntegrationTests`): `ConfigBuilderConfigurationBinderIntegrationTests.cs`, `DependencyInjection/AddSimpleSettingsIntegrationTests.cs`. These wire the real DI container / configuration end-to-end.

**E2E Tests:** Not used.

**Benchmarks (not tests):** `src/performance/ExistForAll.SimpleSettings.Benchmark/` uses BenchmarkDotNet 0.15.8 (net10.0 only). CI gates allocation regressions via `.github/workflows/benchmark.yml`. Treat allocation regressions as failures when changing hot paths (`ValuesPopulator`, converters).

## Common Patterns

**Async Testing:**
```csharp
[Test]
public async Task Name()
{
    await Assert.That(actual).IsEqualTo(expected);
}
```

**Error / Exception Testing:**
```csharp
// Capture the typed exception, then assert on its content
var ex = CaptureConversionFailure<IIntSetting>(nameof(IIntSetting.Value), Secret);
await AssertRedacted(ex, nameof(IIntSetting.Value), nameof(Int32));
```
Exceptions are captured via a `try { ... } catch (SettingsPropertyValueException e)` helper that returns the caught exception, rather than an assertion-throws wrapper. Redaction tests assert the secret appears **nowhere** in `ex.ToString()` while safe diagnostics (property name, target type) are present.

---

*Testing analysis: 2026-07-13*
