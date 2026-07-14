# Phase 2: Binding Correctness & Engine Test Hardening - Research

**Researched:** 2026-07-14
**Domain:** .NET unit-test authoring (TUnit / Microsoft.Testing.Platform) against an existing runtime binding + conversion engine — brownfield test hardening, no production code changes
**Confidence:** HIGH (all findings verified against live source in this session)

## Summary

This is a **test-only** phase against a shipped engine. The open work is three test files — TEST-01 (`ValuesPopulator`), TEST-02 (`TypeConverter`), TEST-03 (Uri/DateTime scalar residual). ENG-01 is done (verify-only, #29) and COLL-01 is deferred by the owner; neither is implementation work here.

The single most important research finding: **much of what FIX-PLAN §T4/T5/T6 described as missing is already covered** by `ExceptionHierarchyTests.cs`, `ExceptionRedactionTests.cs`, `CollectionConversionTests.cs`, `SettingsPropertyTests.cs`, and `SettingsBuilderConversionsTests.cs`. The post-S1/C2 exception contract (`SettingsPropertyValueException` = value-free, `InnerException == null`; `SettingsBindingException` = primitives only; `SettingsPropertyNullException` = the required-missing path) is **already locked by live, passing tests** (verified below). Planning must dedupe hard against these — the genuine residual is small and specific.

**Primary recommendation:** Write three focused test files that add ONLY the uncovered cases — binder last-writer-wins precedence, `Nullable<int>` strip+convert, `null → value-type default`, `ConverterType` overriding the *collection* converter, and scalar `Uri`/`DateTime` positive conversion. Use the existing `SettingsBuilder.CreateBuilder(...)` + `InMemoryBinder`/`InMemoryCollection` integration pattern for most cases, and the internal `TypeConverter`/`PropertyConversion` seam (InternalsVisibleTo is already granted) for the pure converter-orchestration cases. Do NOT re-assert the exception contract that `ExceptionHierarchyTests` already owns.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**ENG-01 / T7 — DONE (verify-only).** Merged #29 (double-checked locking; one `_generationGate` over all generation; warm cache-hit path lock-free). Do NOT re-implement. If a plan references ENG-01 it is verify-only: confirm the fix in `SettingsClassGenerator.cs` and the 2 stress tests in `SettingsClassGeneratorTests.cs` satisfy success criterion #4 (concurrent same-interface generation → one `ReferenceEquals` impl, no duplicate-`DefineType`).

**TEST-01 / T4 — `ValuesPopulator` tests.**
- New file: `src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/ValuesPopulatorTests.cs` (uses the internal ctor + fake binders/converters).
- Cases: binder-throws ⇒ `SettingsBindingException`; a later binder overrides an earlier one (last-writer-wins precedence); no binder sets a value ⇒ the `[SettingsProperty]` attribute default survives; conversion-throws ⇒ `SettingsPropertyValueException`.
- ⚠ CONTRACT UPDATE (post-S1/C2) — FIX-PLAN's T4 wording is STALE. `SettingsPropertyValueException` now takes the failure `Type`, carries NO bound value, and chains NO inner (`InnerException == null`). Assert the current leak-safe contract: property name + target type + failure-type name present; the bound value ABSENT from the whole `ex.ToString()` chain. The value-free "required missing" path is `SettingsPropertyNullException`. `SettingsBindingException` chains the binder's inner but stores only primitives (`BinderType`/`Section`/`Key`), never the `BindingContext`.

**TEST-02 / T5 — `TypeConverter` tests.**
- New file: `src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs`.
- Cases: `null` → value-type default; `null` → `IEnumerable<int>` empty (not null); `Nullable<int>` strip-and-convert; `AllowEmpty = false` + no value ⇒ throw (`SettingsPropertyNullException`); attribute `ConverterType` bypasses the collection converter.

**TEST-03 / T6 — Converter residual (Uri/DateTime).**
- P4 (#25) already added `Conversion/CollectionConversionTests.cs`. Scope T6 to the RESIDUAL only — verify existing coverage first, then add `Uri`/`DateTime` scalar-conversion + edge tests where genuinely missing. Do NOT duplicate P4's coverage.
- The `List<int>` positive/doc test is DEFERRED with COLL-01 — not in this phase.

**COLL-01 / C1 — DEFERRED (open decision, do not plan).** The broaden-vs-document+throw decision for `List<T>`/`IList<T>`/`ICollection<T>` is deferred by the owner. Do NOT plan the implementation or the `List<T>` positive/doc test. Background: `TypeExtensions.IsEnumerable` matches only exact `IEnumerable<>`; other shapes fall through to `DefaultTypeConverter` and throw.

### Claude's Discretion

- Test file layout, fixture interface shapes, `[Arguments]`/`[NotInParallel]` usage per existing TUnit conventions; whether ENG-01 verify-only warrants its own thin plan or a note inside another plan.

### Deferred Ideas (OUT OF SCOPE)

- **COLL-01 / C1** — `List<T>`/`IList<T>`/`ICollection<T>` support decision and its `List<T>` positive/doc test. Deferred by owner.
- **D1 Validations**, **D2 EqualityCompererCreator** — HELD; out of scope.
- Any new binding features.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TEST-01 | `ValuesPopulator` tests — binder precedence + bind/convert exception-wrapper contracts (T4) | Source read (`ValuesPopulator.cs`); genuine gap = last-writer-wins precedence + default-survives; contract cases already partly locked by `ExceptionHierarchyTests` (see Don't-Duplicate table) |
| TEST-02 | `TypeConverter` tests — null/nullable/empty-enumerable/`AllowEmpty`/attribute-`ConverterType` paths (T5) | Source read (`TypeConverter.cs`, `PropertyConversion.cs`); gaps = null→value-type default, `Nullable<int>` strip+convert, `ConverterType`-over-collection; empty-enumerable + AllowEmpty already covered |
| TEST-03 | Converter residual — `Uri`/`DateTime` scalar (T6; `List<T>` doc test deferred) | Source read (`UriTypeConvertor.cs`, `DateTimeTypeConverter.cs`); array cases fully covered by P4; residual = scalar positive conversion + format-mismatch |
| ENG-01 | Generator concurrency race (T7) | **VERIFY-ONLY** — confirmed present in `SettingsClassGeneratorTests.cs` (2 stress tests, lines 103–166); no new work |
| COLL-01 | Collection support decision (C1) | **DEFERRED** — do not plan; documented for the requirements-coverage gate as intentionally deferred |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Binder precedence (last-writer-wins) | Engine core (`ValuesPopulator`) | — | Populate loop iterates binders in order, overwriting `tempValue` per `context.HasNewValue` |
| Bind-failure wrapping | Engine core (`ValuesPopulator` catch) | Exception types | `try/catch` around `binder.BindPropertySettings` → `SettingsBindingException` |
| Convert-failure wrapping (leak-safe) | Engine core (`ValuesPopulator.ConvertPropertyValue`) | `PropertyConversion` | Redacting `SettingsPropertyValueException` (Type only) |
| Conversion orchestration (null/nullable/converter-select) | `Core.Reflection.TypeConverter` → `PropertyConversion` | Converter chain | `CreateConversion` resolves converter + null-result once per type |
| Scalar type conversion (Uri/DateTime) | Individual `ISettingsTypeConverter`s | `SettingsOptions` (format/delimiter) | `UriTypeConvertor`, `DateTimeTypeConverter` |

All test targets live in one tier — the engine core assembly `ExistForAll.SimpleSettings`. No multi-tier concerns.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| TUnit | 1.58.0 (pinned in `src/Directory.Packages.props`) | Test framework + fluent assertions | Already the project's only test framework; runs on Microsoft.Testing.Platform per `src/global.json` |
| .NET SDK | 10.0.301 installed (global.json floor 10.0.100, `rollForward: latestFeature`) | Build/run | Local runs net10 only; net8 is build-only; CI runs both TFMs |

**No new packages are introduced by this phase.** All test doubles are hand-written (project convention: no Moq/NSubstitute). `## Package Legitimacy Audit` is therefore **N/A — no external packages installed.**

**Verification:** `dotnet --version` → `10.0.301` [VERIFIED: local shell]. TUnit 1.58.0 pinned [VERIFIED: TESTING.md + Directory.Packages.props reference]. InternalsVisibleTo to the test project confirmed in `src/Core/ExistForAll.SimpleSettings/Info.cs:3` [VERIFIED: grep].

## Architecture Patterns

### System Under Test — data flow

```
config value (string, from InMemoryBinder/ConfigurationBinder/etc.)
        │
        ▼
ValuesPopulator.PopulateInstanceWithValues(instance, settings, options, binders[])
        │  builds/caches SettingsPlan (one per interface type)
        │
        ├─ for each PropertyPlan:
        │     tempValue = propertyPlan.DefaultValue        ← [SettingsProperty] default
        │     for each binder (in order):                  ← LAST-WRITER-WINS
        │         try binder.BindPropertySettings(context)
        │             if context.HasNewValue → tempValue = context.NewValue
        │         catch → throw SettingsBindingException(binder, context, inner)   ← wraps, primitives only
        │
        │     propertyValue = ConvertPropertyValue(tempValue)
        │         └─ PropertyConversion.Convert(value):
        │               value == null && throwOnNull → throw SettingsPropertyNullException(name)
        │               value == null                → return nullResult (empty[] | default(T) | null)
        │               else                          → converter.Convert(value, strippedType)
        │            catch (not SettingsPropertyNullException) → throw SettingsPropertyValueException(Type only)  ← leak-safe
        │
        └─ property.SetValue(instance, propertyValue)
```

`TypeConverter.CreateConversion(propertyInfo, attribute, options)` (called once per property at plan build) decides, up front: `throwOnNull = attribute is { AllowEmpty: false }`; `nullResult` (empty `T[]` for `IEnumerable<T>`, `Activator.CreateInstance` for value types, else null); `strippedType` (unwraps `Nullable<>`); and the chosen converter (`attribute.ConverterType` wins outright, else first `CanConvert` in `options.Converters`).

### Component Responsibilities

| File | Responsibility | Test target |
|------|----------------|-------------|
| `src/Core/.../ValuesPopulator.cs` | Binder loop, precedence, bind/convert exception wrapping | TEST-01 |
| `src/Core/.../Core/Reflection/TypeConverter.cs` | Conversion resolution (null-result, nullable strip, converter select) | TEST-02 |
| `src/Core/.../Conversion/PropertyConversion.cs` | Runtime null-check + throwOnNull + converter dispatch | TEST-02 |
| `src/Core/.../Conversion/UriTypeConvertor.cs` | `new Uri((string)value)` | TEST-03 |
| `src/Core/.../Conversion/DateTimeTypeConverter.cs` | `DateTime.ParseExact(value, options.DateTimeFormat, Invariant)` | TEST-03 |

### Pattern 1: Integration-style build with in-memory binder (dominant existing pattern)
**What:** Drive the whole engine through the public builder, feed values via `InMemoryCollection`/`InMemoryBinder`.
**When to use:** Most TEST-01/02/03 cases — it exercises the real plan/convert path and matches every existing conversion test.
**Example:**
```csharp
// Source: src/Tests/.../Conversion/CollectionConversionTests.cs (verified live)
var collection = new InMemoryCollection();
collection.Add("SectionName", nameof(IThing.Value), "raw");
var builder = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));
var result = builder.GetSettings<IThing>();
await Assert.That(result.Value).IsEqualTo(expected);
```
Note: default `SectionNameFormatter` strips a leading `I` (`IThing` → section `"Thing"`). `InMemoryBinder`/`InMemoryCollection` are in namespace `ExistForAll.SimpleSettings.Binder` (visible via InternalsVisibleTo).

### Pattern 2: Direct converter-orchestration seam (for pure TypeConverter cases)
**What:** Construct `SettingsOptions` (public parameterless ctor; its `Converters` are auto-seeded with the default chain) and call the internal `TypeConverter.CreateConversion(...)` directly, then exercise the returned `PropertyConversion.Convert(...)`.
**When to use:** TEST-02 cases that are about resolution logic itself (null→default, nullable strip, ConverterType selection) without needing a generated instance.
**Example:**
```csharp
// Source: verified against TypeConverter.cs + SettingsOptions.cs + PropertyConversion.cs
var options = new SettingsOptions();                                  // Converters seeded: DateTime,Uri,Array,Enumerable,Enum,Default
var prop = typeof(ISample).GetProperty(nameof(ISample.Count))!;       // e.g. int?  or int
var attribute = prop.GetCustomAttribute<SettingsPropertyAttribute>(inherit: true);
var conversion = new TypeConverter().CreateConversion(prop, attribute, options);
await Assert.That(conversion.Convert(null)).IsEqualTo(0);             // null → value-type default
await Assert.That(conversion.Convert("42")).IsEqualTo(42);           // Nullable<int> strip+convert
```
`TypeConverter`, `ITypeConverter`, `PropertyConversion`, and `SettingsOptions.Converters` are all internal but reachable via `[InternalsVisibleTo("ExistForAll.SimpleSettings.UnitTests")]`.

### Pattern 3: `ValuesPopulator` internal ctor + fake binders (for TEST-01 unit isolation)
**What:** `ValuesPopulator` exposes `internal ValuesPopulator(ITypePropertiesExtractor, ITypeConverter)` plus public `PopulateInstanceWithValues(instance, settings, options, binders)`.
**When to use:** TEST-01 precedence + default-survives, where you want to feed hand-written fake `ISectionBinder`s in a known order and assert on the resulting instance. `instance` can be a hand-written concrete class implementing the test interface (property `SetValue` targets the interface `PropertyInfo`, so any implementer works); the simplest route is still `SettingsBuilder` with two ordered `InMemoryBinder`s. Discretion per CONTEXT.
**Example (fake binder shape, from the live `ThrowingBinder`):**
```csharp
// Source: src/Tests/.../SimpleSettings/ExceptionHierarchyTests.cs (verified live)
private class ThrowingBinder : ISectionBinder
{
    public void BindPropertySettings(BindingContext context)
        => throw new InvalidOperationException("binder failed");
}
// A setting binder: call context.SetNewValue(x) to contribute a value.
```

### Anti-Patterns to Avoid
- **Duplicating the exception contract.** `ExceptionHierarchyTests` + `ExceptionRedactionTests` already lock `SettingsPropertyValueException` (value-free, `InnerException == null`, safe metadata), `SettingsBindingException` (BinderType/Section/Key), and secret-absence in `ToString()` for int/enum/DateTime/Uri/custom. Do NOT re-assert these; add only the uncovered precedence/orchestration behaviors.
- **Trusting FIX-PLAN's exception wording.** FIX-PLAN §T4 says the value exception "carries value/inner." That is STALE — the live type takes a `Type`, never chains an inner. Trust the source.
- **Using a mock framework.** Project has none; hand-write in-memory fakes.
- **Culture-sensitive assertions without isolation.** Any test touching `CultureInfo.CurrentCulture` must use `[NotInParallel]` and restore in `finally` (see `DefaultTypeConverterTests`).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Feeding config values | A custom `ISectionBinder` per test | `InMemoryCollection` + `InMemoryBinder` (`ExistForAll.SimpleSettings.Binder`) | Already the established, visible test double |
| Env-var isolation | Manual set/unset | `DisposableEnvironmentVariable` (`using (...)`) | Existing RAII helper; not needed here but noted |
| Secret-leak assertions | New redaction helper | The sentinel + `ToString().Contains(Secret)` pattern | Already in `ExceptionRedactionTests` if any leak assertion is truly needed |
| Building a settings instance | Reflection.Emit by hand | `SettingsBuilder.CreateBuilder(...).GetSettings<T>()` | The public path already used everywhere |

**Key insight:** The test infrastructure for this engine is mature. Every needed primitive (in-memory binder, sentinel redaction assert, culture isolation, `[Arguments]` data-driving) already exists in the test project — the phase is about *coverage*, not tooling.

## Don't-Duplicate — Existing Coverage Map (dedupe target)

> This is the load-bearing research output. Verified by reading each test file live.

| CONTEXT case | Already covered by | New work needed? |
|--------------|--------------------|------------------|
| binder-throws ⇒ `SettingsBindingException` (BinderType/Section/Key) | `ExceptionHierarchyTests.BinderThrows_ExposesBinderContext` | Residual only — a **unit-level** version via `ValuesPopulator` + fake binder (integration contract already locked) |
| conversion-throws ⇒ `SettingsPropertyValueException` (value-free, `InnerException == null`, safe metadata) | `ExceptionHierarchyTests.ConversionFailure_ExposesSafeStructuredMetadata_AndNoChainedInner` + all of `ExceptionRedactionTests` | **NO — do not duplicate.** Contract fully locked |
| secret absent from `ToString()` (int/enum/DateTime/Uri/custom converter) | `ExceptionRedactionTests` (5 tests) | **NO** |
| last-writer-wins precedence (later binder overrides earlier) | **nothing** | **YES — genuine gap** |
| no binder sets value ⇒ `[SettingsProperty]` default survives | `CollectionConversionTests.Convert_DefaultArray_IsPassedThrough` (no-binder case only) | **YES (thin)** — the "binders present but none set" scalar case is uncovered |
| `null` → value-type default | **nothing** (only the enumerable null case exists) | **YES — genuine gap** |
| `null` → `IEnumerable<int>` empty (not null) | `CollectionConversionTests.Convert_UnboundEnumerable_NoDefault_YieldsEmptyArray` | **NO — do not duplicate** |
| `Nullable<int>` strip-and-convert | **nothing** (grep: no `int?`/`Nullable` in any test) | **YES — genuine gap** |
| `AllowEmpty = false` + no value ⇒ `SettingsPropertyNullException` | `SettingsPropertyTests.Build_WhenAllowEmptyIsFalse_ShouldThrowException` | Residual only — optional unit-level `PropertyConversion.Convert(null)` variant |
| attribute `ConverterType` wins (scalar) | `SettingsBuilderConversionsTests.Build_WhenAddLocalConverter_ShouldReturnConverterValue` (Guid) | Scalar covered; **the "bypasses the *collection* converter" case (ConverterType on an `IEnumerable<T>`/array property) is the residual — YES** |
| array of `Uri` / `DateTime` | `CollectionConversionTests.Convert_DelimitedString_ToUriArray_*` / `ToDateTimeArray_*` | **NO — do not duplicate** |
| scalar `Uri` positive parse | **nothing** (only the redaction failure case) | **YES — genuine gap** |
| scalar `DateTime` positive parse with configured `DateTimeFormat` | **nothing** (only array + redaction failure) | **YES — genuine gap** |
| ENG-01 concurrency (same + distinct interface) | `SettingsClassGeneratorTests` lines 103–166 (2 Barrier/Parallel stress tests) | **NO — verify-only, already present** |

## Runtime State Inventory

**Omitted — not a rename/refactor/migration phase.** This phase adds test files only; no stored data, service config, OS registration, secrets, or build artifacts are renamed or migrated. Verified: the work is net-new `[Test]` methods in the existing test project.

## Common Pitfalls

### Pitfall 1: Section-name formatting trips up in-memory binder keys
**What goes wrong:** Values never bind because the section key doesn't match.
**Why it happens:** The default `SectionNameFormatter` strips a leading `I` — `IIntSetting` → section `"IntSetting"`, and `[SettingsSection]`/interface-name rules apply. `InMemoryCollection.Add(section, key, value)` must use the *formatted* section name.
**How to avoid:** Mirror existing tests — `collection.Add("Thing", nameof(IThing.Value), ...)` for `IThing`, or `typeof(T).Name.Substring(1)`.
**Warning signs:** Property comes back as its default/empty instead of the bound value.

### Pitfall 2: Asserting the stale exception shape
**What goes wrong:** A test asserts `ex.InnerException` is non-null or that the value appears in the message — fails (or worse, would pass on old code).
**Why it happens:** FIX-PLAN §T4 predates S1/C2.
**How to avoid:** Assert `ex.InnerException` **IsNull**, `ex.ConversionErrorType == typeof(FormatException)` (etc.), and value ABSENT from `ex.ToString()`. See `ExceptionHierarchyTests.ConversionFailure_*` for the exact shape.

### Pitfall 3: Nullable target types
**What goes wrong:** Assuming `int?` flows through unchanged.
**Why it happens:** `TypeConverter.StripIfNullable` unwraps `Nullable<>` before converter selection, so an `int?` property converts via the `int` path; `null` yields `Activator.CreateInstance(int?)` = `null` (not `0`) because `CreateNullResult` runs on the *original* (nullable) type, which is a value type → `default(int?)` = null. Distinguish the non-nullable `int` case (null → `0`) from `int?` (null → `null`).
**How to avoid:** Test both `int` and `int?` explicitly; assert `int` null→`0` and `int?` "42"→`42`.
**Warning signs:** Unexpected `0` vs `null`.

### Pitfall 4: Culture / parallelism
**What goes wrong:** Flaky DateTime/decimal parses under non-invariant cultures when tests run in parallel.
**Why it happens:** `CultureInfo.CurrentCulture` is process-global. TUnit runs tests in parallel by default.
**How to avoid:** `[NotInParallel]` on any culture-mutating test and restore in `finally` (pattern in `DefaultTypeConverterTests`). Scalar `Uri`/`DateTime` positive tests using invariant format need no culture mutation but keep `[NotInParallel]` if they set culture.

### Pitfall 5: DateTime converter requires string input and exact format
**What goes wrong:** Passing a non-string or a differently-formatted date throws.
**Why it happens:** `DateTimeTypeConverter.Convert` does `DateTime.ParseExact((string)value, options.DateTimeFormat, InvariantCulture)`; default format is `"yyyy-MM-dd"`. A mismatch throws `FormatException` → wrapped in `SettingsPropertyValueException`.
**How to avoid:** Positive test uses `"2020-01-02"`; a negative/edge test can assert a mismatched format wraps as `SettingsPropertyValueException` (already implied by redaction test — don't over-duplicate).

## Code Examples

### TEST-02: null → value-type default vs `Nullable<int>` (direct seam)
```csharp
// Source: verified against TypeConverter.cs / PropertyConversion.cs / SettingsOptions.cs
var options = new SettingsOptions();
var conv = new TypeConverter();

var intProp   = typeof(ISample).GetProperty(nameof(ISample.Count))!;     // int
var intConv   = conv.CreateConversion(intProp, intProp.GetCustomAttribute<SettingsPropertyAttribute>(true), options);
await Assert.That(intConv.Convert(null)).IsEqualTo(0);                    // value-type default

var nullProp  = typeof(ISample).GetProperty(nameof(ISample.Maybe))!;     // int?
var nullConv  = conv.CreateConversion(nullProp, nullProp.GetCustomAttribute<SettingsPropertyAttribute>(true), options);
await Assert.That(nullConv.Convert(null)).IsNull();                      // nullable → null result
await Assert.That(nullConv.Convert("42")).IsEqualTo(42);                 // strip + convert
```

### TEST-01: last-writer-wins precedence (integration)
```csharp
// Two ordered binders; the SECOND must win. No existing test covers this.
var c1 = new InMemoryCollection(); c1.Add("Sample", nameof(ISample.Name), "first");
var c2 = new InMemoryCollection(); c2.Add("Sample", nameof(ISample.Name), "second");
var builder = SettingsBuilder.CreateBuilder(x =>
{
    x.AddSectionBinder(new InMemoryBinder(c1));
    x.AddSectionBinder(new InMemoryBinder(c2));   // later → wins
});
await Assert.That(builder.GetSettings<ISample>().Name).IsEqualTo("second");
```

### TEST-03: scalar Uri / DateTime positive
```csharp
var c = new InMemoryCollection();
c.Add("Endpoint", nameof(IEndpoint.Url), "https://a.example/");
c.Add("Endpoint", nameof(IEndpoint.When), "2020-01-02");   // default format yyyy-MM-dd
var b = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(c)));
var r = b.GetSettings<IEndpoint>();
await Assert.That(r.Url).IsEqualTo(new Uri("https://a.example/"));
await Assert.That(r.When).IsEqualTo(new DateTime(2020, 1, 2));
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `SettingsPropertyValueException` carries value + chained inner (FIX-PLAN §T4) | Value-free; takes failure `Type`; `InnerException == null` | S1 #27 / C2 #28 | Tests must assert the leak-safe shape; contract already locked by `ExceptionHierarchyTests` |
| VSTest runner | Microsoft.Testing.Platform (TUnit) | pre-GSD | Run from `src/`; `dotnet test` with the platform opt-in in `global.json` |
| `IEnumerable<T>` null → `Enumerable.Empty<T>()` | `Array.CreateInstance(elementType, 0)` (a real `T[]`) | P4 #25 | `null → IEnumerable<int>` yields `int[]`; already tested — do not duplicate |

**Deprecated/outdated:** FIX-PLAN.md is frozen historical; its T4/T5/T6 test specs predate S1/C2. Mine it for intent, trust the live source for the contract.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | (none) | — | All findings verified against live source or the local toolchain this session. |

**All claims in this research were verified against the codebase or the local environment — no user confirmation needed.**

## Open Questions

1. **Should TEST-01 use the internal `ValuesPopulator` ctor with fakes, or the `SettingsBuilder` integration path?**
   - What we know: CONTEXT names the internal ctor; the integration path is simpler and dominates the existing suite; both are viable and InternalsVisibleTo is granted.
   - What's unclear: nothing blocking — this is explicitly Claude's Discretion.
   - Recommendation: Use the integration path for precedence/default (clearest, matches suite); reserve the internal ctor + fakes only if a case genuinely needs binder-order isolation the builder can't express.

2. **Depth of TEST-03 negative/edge cases (DateTime format mismatch, malformed Uri).**
   - What we know: The failure→`SettingsPropertyValueException` wrapping is already locked by redaction tests for both types.
   - Recommendation: Add scalar POSITIVE tests (the real gap) plus at most one format-mismatch negative; do not re-prove redaction.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build + test | ✓ | 10.0.301 | — |
| net10.0 runtime | local test run | ✓ | 10.0 | — |
| net8.0 runtime | local test run | ✗ (build-only locally) | — | CI runs net8; locally target net10 with `--framework net10.0` |
| TUnit / Microsoft.Testing.Platform | test execution | ✓ | 1.58.0 (via restore) | — |

**Missing with no fallback:** none.
**Missing with fallback:** net8 runtime — run net10 locally (`dotnet test <proj> --framework net10.0 --no-build`, build first); CI covers net8 parity.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | TUnit 1.58.0 on Microsoft.Testing.Platform |
| Config file | `src/global.json` (`test.runner`), `src/Directory.Packages.props` (version pin) |
| Quick run command | `dotnet test src/Tests/ExistForAll.SimpleSettings.UnitTests --framework net10.0 --no-build` (build first from `src/`) |
| Full suite command | from `src/`: `dotnet build "$SOLUTION" -c Release --no-restore` then `dotnet test "$SOLUTION" -c Release --no-build` (both TFMs in CI) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| TEST-01 | Last-writer-wins precedence (later binder overrides) | unit/integration | `dotnet test ...UnitTests --framework net10.0 --no-build --filter "*ValuesPopulator*"` | ❌ Wave 0 (`Core/ValuesPopulatorTests.cs`) |
| TEST-01 | No binder sets value ⇒ `[SettingsProperty]` default survives | unit/integration | same filter | ❌ Wave 0 |
| TEST-01 | binder-throws ⇒ `SettingsBindingException` (unit-level, fake binder) | unit | same filter | ❌ Wave 0 (contract already covered at integration level) |
| TEST-02 | `null` → value-type default | unit | `--filter "*TypeConverter*"` | ❌ Wave 0 (`Core/TypeConverterTests.cs`) |
| TEST-02 | `Nullable<int>` strip + convert | unit | same filter | ❌ Wave 0 |
| TEST-02 | `ConverterType` bypasses the collection converter | unit/integration | same filter | ❌ Wave 0 |
| TEST-03 | scalar `Uri` positive parse | integration | `--filter "*Uri*"` / new class | ❌ Wave 0 (`Conversion/ScalarConversionTests.cs` or extend existing) |
| TEST-03 | scalar `DateTime` positive parse (configured format) | integration | same | ❌ Wave 0 |
| ENG-01 | Concurrent generation → one shared impl | stress (verify-only) | `--filter "*Concurrent*"` | ✅ `SettingsClassGeneratorTests.cs` (lines 103–166) |

### Sampling Rate
- **Per task commit:** `dotnet test src/Tests/ExistForAll.SimpleSettings.UnitTests --framework net10.0 --no-build` (net10, fast).
- **Per wave merge:** full net10 suite from `src/`.
- **Phase gate:** full suite green on **net8 + net10** (CI parity) before `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/ValuesPopulatorTests.cs` — TEST-01 (precedence, default-survives, unit-level bind-throw)
- [ ] `src/Tests/ExistForAll.SimpleSettings.UnitTests/Core/TypeConverterTests.cs` — TEST-02 (null→default, nullable strip, ConverterType-over-collection)
- [ ] TEST-03 scalar `Uri`/`DateTime` — new `Conversion/ScalarConversionTests.cs` OR add to an existing converter test class (discretion)
- [ ] No framework install needed — infrastructure exists; `Core/` test subfolder is new (mirrors source `Core/` namespace).

## Security Domain

`security_enforcement: true`, ASVS level 1. This phase adds **no new attack surface** — it is test code. Its security relevance is **reinforcing** the existing SEC-01 secret-redaction invariant.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V5 Input Validation | indirect | Conversion is validation; tests confirm typed conversion + null/AllowEmpty handling |
| V7/V8 Error Handling & Logging (secret leakage) | yes (verify) | Existing `SettingsPropertyValueException` value-free contract + `ExceptionRedactionTests` — this phase must NOT weaken it; any new failure-path test must keep the value-absent assertion if it touches `ToString()` |
| V6 Cryptography | no | — |
| V2/V3/V4 Auth/Session/Access | no | Library concern, not this phase |

### Known Threat Patterns for this stack
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Bound secret leaking via exception message / chained inner / logs | Information Disclosure | Value-free `SettingsPropertyValueException` (takes `Type`, no inner); locked by `ExceptionHierarchyTests` + `ExceptionRedactionTests`. New tests must not introduce assertions that print bound values. |

**Guidance for the planner:** Any new negative/failure test that inspects an exception must reuse the sentinel-absence pattern (`Assert.That(ex.ToString().Contains(Secret)).IsFalse()`) rather than asserting on the raw value. No security blocker for this phase.

## Sources

### Primary (HIGH confidence — live source read this session)
- `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs` — binder loop, precedence, exception wrapping
- `src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypeConverter.cs` + `Conversion/PropertyConversion.cs` — conversion resolution
- `src/Core/ExistForAll.SimpleSettings/SettingsPropertyValueException.cs`, `SettingsPropertyNullException.cs`, `SettingsBindingException.cs`, `SimpleSettingsException.cs` — post-S1/C2 contract
- `src/Core/ExistForAll.SimpleSettings/Conversion/{DateTimeTypeConverter,UriTypeConvertor,DefaultTypeConverter,TypeConvertersCollections}.cs`
- `src/Core/ExistForAll.SimpleSettings/{SettingsOptions,BindingContext,ISectionBinder,IValuesPopulator}.cs`, `Core/Reflection/{ITypeConverter,ITypePropertiesExtractor,TypeExtensions}.cs`, `Binder/{InMemoryBinder,InMemoryCollection}.cs`, `Info.cs` (InternalsVisibleTo)
- Test files: `Conversion/{CollectionConversionTests,DefaultTypeConverterTests,ExceptionRedactionTests}.cs`, `SimpleSettings/{ExceptionHierarchyTests,SettingsPropertyTests,SettingsBuilderConversionsTests,SettingsClassGeneratorTests}.cs`
- `.planning/codebase/TESTING.md`; `.planning/config.json`; `src/global.json`
- `dotnet --version` → 10.0.301

### Secondary / Tertiary
- None — no web research required; the phase is fully internal.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versions verified from installed SDK + pinned package + global.json.
- Architecture / data flow: HIGH — read every file in the target path.
- Existing-coverage dedupe map: HIGH — every referenced test read in full.
- Pitfalls: HIGH — derived directly from source semantics (nullable strip, section formatting, culture).

**Research date:** 2026-07-14
**Valid until:** 2026-08-13 (stable internal codebase; re-verify only if `ValuesPopulator`/`TypeConverter`/exception types change before planning)
