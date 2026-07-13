# SimpleSettings — Fix Plan

_Derived from the 2026-07-10 three-part review (architecture · tests · performance). Every finding below was verified against source with file:line. Work items are self-contained and ordered so they can be implemented one at a time._

## Progress (2026-07-13)
- **Done & merged:** B1, B2, B4, B5, B9 + T1, T2 (PR #8) · BindingContext test (#10) · D3 namespace typo (#11) · T3 DI integration tests (#12) · solution rename (#13) · **A2 naming → ExistForAll (#15)** · **P0 benchmark harness (#16)** · **P1 provider cache + C3 decided/implemented (#17)** · **P2 memoize `ExtractTypeProperties` + `HashSet` dedup (#18)** · **docs tutorials refresh (#20)** · **Q1–Q4 perf quick wins + M1 collision fix + micro-benchmarks (#21)**.
- **Q1–Q4 proven** via isolated micro-benchmarks (macro `ScanBenchmark` can't resolve them): Q1 2.7× / 64 KB→88 B · Q3 2.65× / 152 B→0 · Q4 32× / 224 B→0. **Q5 was already resolved by B4.** **M1** (code-review finding): namespace-qualify the generated impl name in the generator only — `GetNormalizeInterfaceName` also backs the section name. Suite → **56 per TFM**.
- **Merged since:** **benchmark-tracking CI (#22)** — BDN on push/PR, gates PRs on **allocation** regressions (>10%) via github-action-benchmark on `gh-pages`; time informational. · **session-wrap docs (#23)** · **P3 — cached "settings plan" (#24)** — `SettingsPlan` per type (section name once+lazy, key/default/converter precomputed, `[SettingsProperty]` read once). Warm re-populate **−55–61%** (50 props 15,681→6,816 B); gated `ScanBenchmark` **≈flat (−0.4%)**. Reviewed by code+perf agents; emitted/compiled setter reverted (regressed the gated cold scan for **no** warm gain — net10 `SetValue` is already alloc-free). New gated `PlanPopulateBenchmark` tracks the warm path.
- **P4 merged (#25)** — de-reflect + DRY the array/enumerable converters: shared `CollectionTypeConverter` (`Array.CreateInstance` + indexed fill, manual `LinkedList` walk); `CreateNullResult` de-reflected. Gated `ConvertArrayBenchmark`: **1.33 KB→688 B (−49%), 5.7×**. Suite 68/TFM. Included a user modernization pass (collection expressions across ~19 files).
- **P5 merged (#26)** — resolve config section once per type: `ConfigurationBinder` caches the `IConfigurationSection` per section name (`ConcurrentDictionary`, zero-capture `GetOrAdd`). Plan reviewed by architect+perf+security (chose internal cache over a contract change — layering); code reviewed via `/code-review`. Gated `ConfigBinderBenchmark`: **BindNoRoot 80→40 B (−50%), BindWithRoot 144→56 B (−61%)**. Suite **71/TFM**. Also gated P4's `ConvertArrayBenchmark` (was never in the CI filter). **Security review surfaced a pre-existing leak → new item S1.**
- **Perf track P0–P5 = COMPLETE + merged.** `master` @ `498fc81`.
- **S1 shipped + merged (#27).** Conversion-failure exceptions no longer carry the bound value **or** chain the value-bearing framework inner; the value-free "required value missing" case split into its own `SettingsPropertyNullException`. `master` @ `5277c60`. Detail in §S1 below.
- **C2 shipped** (branch `refactor/c2-exception-hierarchy`, PR open) — **public exception hierarchy**: `SimpleSettingsException` base, reparent all 10, promote the 4 escapees to public, flatten 3 to root namespace, leak-safe structured properties, retype the `TypeIsNotInterface` throw. Plan reviewed by `security-auditor` + `dotnet-architect` (both ENDORSE-WITH-CHANGES, both fired cleanly) + perf in-context; code by `/code-review`. Suite **82 net10** (was 76; +6). Detail in §C2 below.
- **In flight:** C2 PR open (branch `refactor/c2-exception-hierarchy`) — carries this fix-plan + handoff refresh.
- **Next:** engine tests (T4 `ValuesPopulator` / T5 `TypeConverter` / T7 generator concurrency race) **or** continue the pre-stable breaking cleanups (A5 make `SettingsHolder` internal / C1 `List<T>` support / A6 command-line quoting / A3 `Core.AspNet` / A4 dependency floor) · **A1 (HIGH)** AOT/trim story · optional P3b.
- **C3 — DECIDED (option 2):** cache in the provider only; Core `SettingsBuilder.GetSettings` unchanged; no reload. See #17.
- **Held — do NOT delete (feature work coming):** D1 Validations (reconcile with the `validate-settings` branch) · D2 EqualityCompererCreator.
- Running status lives in `SESSION-HANDOFF.md`.

## How to work this plan
- **Build/test from `src/`** (`global.json` opts into Microsoft.Testing.Platform for TUnit). `dotnet test` from `src/`. Only the net10 runtime is installed locally → net8 is build-only locally; CI runs both.
- **Baseline:** 78 tests green on `master` @ `febaaba`.
- **Breaking changes are FREE right now.** Only `2.0.0-alpha.0.110` is published; there is **no stable `v*` tag** yet (the `version-*` tags belong to the dead legacy `ExistAll.SimpleConfig` package). Do all breaking cleanup (dead API removal, namespace rename) **before** cutting the first `v2.0.0-beta`.
- **Pairing:** where a test is marked _“pairs with Bx”_, commit the test **with** the fix so the fix ships with its regression guard. Tests marked _“fails first”_ are expected to be red against current `master` — write them, watch them fail, then apply the fix.
- Suggested commit granularity: one item = one commit (or one small PR per phase).

## Legend
`Sev` = High / Med / Low · `Eff` = S (≤30 min) / M (hours) / L (day+) · `Break` = public API/behavior break?

---

## Summary checklist

**Phase 1 — Correctness bugfixes (one-liners, non-breaking, highest ROI)**
- [x] B1 · Register `EnumTypeConverter` — enum binding is broken · Sev High · Eff S
- [x] B2 · Invariant culture in `DefaultTypeConverter` — locale data corruption · Sev High · Eff S
- [x] B4 · Fix `BindingContext.PropertyType` (returns the interface, not the property type) · Sev Med · Eff S
- [x] B5 · Fix validator contradiction (can’t null `AttributeType`) · Sev Med · Eff S
- [x] B9 · Fix broken error-message interpolation in `Resources` · Sev Low · Eff S

**Phase 2 — Dead code & naming (do while pre-stable; breaking)**
- [ ] D1 · Delete (or wire) the dead `Validations` API + `ValidatorType` · Sev Med · Eff M · **Break** · **HELD — feature work coming**
- [ ] D2 · Delete (or fix+wire) `EqualityCompererCreator` (dead + invalid IL) · Sev Med · Eff S · **HELD**
- [x] D3 · Fix the `ExistsForAll` namespace typo in the Binders package · Sev Med · Eff S · **Break**

**Phase 3 — Correctness/feature (needs design)**
- [ ] C1 · Decide: support `List<T>`/`IList<T>`/`ICollection<T>` or document the `IEnumerable<T>`-only limit · Sev Med · Eff M
- [x] C2 · Introduce a public `SimpleSettingsException` base; reparent all 10 exceptions, promote the 4 build-path escapees to public, flatten 3 to the root namespace, add leak-safe structured properties, retype the `TypeIsNotInterface` throw · Sev Med · Eff M · **Break**
- [x] C3 · Decide reload / provider-vs-singleton semantics (see also P1) · Sev High · Eff M–L · **DECIDED: option 2, provider-level cache (#17)**

**Phase 4 — Tests (write-first shortlist, then broaden)**
- [x] T1 · Culture parse test (fails first · pairs with B2)
- [x] T2 · Enum-from-string test (fails first · pairs with B1)
- [x] T3 · DI / Generic-Host integration tests (headline feature, 0 coverage today)
- [ ] T4 · `ValuesPopulator` unit tests (precedence + exception wrappers)
- [ ] T5 · `TypeConverter` unit tests (null / nullable / empty-enumerable / attribute)
- [ ] T6 · Converter unit tests (array / enumerable / Uri / DateTime + `List<T>` doc test)
- [ ] T7 · `SettingsClassGenerator` caching + concurrency stress; collection not-found; binder edge cases · *(caching now covered; concurrency race still open — see P/Q4 note)*

**Phase 5 — Performance**
- [x] P0 · Upgrade the benchmark harness (MemoryDiagnoser + phase-split + fixtures) — do first, to measure P1–P3
- [x] P1 · Cache built instance on the `ISettingsProvider` resolve path · Sev High · Eff S
- [x] P2 · Memoize `ExtractTypeProperties` + fix O(n²) dedup · Sev High · Eff S
- [x] P3 · Cached “settings plan” — hoist section (lazy) + keys, precompute/cache converters, plan per type. Reflective `SetValue` kept; compiled setter deferred (regressed the gated cold scan for no warm gain) · Sev High · Eff L
- [x] Q1–Q5 · Quick wins (GetEnumerator, OrdinalIgnoreCase, env-binder, type-cache; Q5 dead ctor checks already done by B4)
- [x] P4 · De-reflect + DRY array/enumerable converters (shared `CollectionTypeConverter`; `Array.CreateInstance` + manual converter walk; `CreateNullResult` de-reflected) — **1.33 KB→688 B, 5.7×**; merged #25 · Sev Med · Eff M
- [x] P5 · Resolve config section once per type — `ConfigurationBinder` caches the `IConfigurationSection` per section name (`ConcurrentDictionary`, zero-capture `GetOrAdd`) — **BindNoRoot 80→40 B (−50%), BindWithRoot 144→56 B (−61%)**; merged #26 · Sev Med · Eff M
- [x] S1 · Redact secret values from exception messages — **full fix (Rec 1)**: redacted message + value-bearing inner no longer chained + null-not-allowed split into `SettingsPropertyNullException` · Sev Med · Eff S · **found in P5 security review** *(detail below)*

**Phase 6 — Architecture strategy**
- [ ] A1 · Decide AOT/trim story; annotate `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` and/or plan a source generator · Sev High · Eff M–L
- [x] A2 · Consolidate naming: `ExistAll` / `ExistsForAll` → `ExistForAll` (folder, `.slnx`, `Company`, benchmark) · Sev Low · Eff M
- [ ] A3 · `Core.AspNet` ships no public type — make `Environments` public or drop the package · Sev Med · Eff S
- [ ] A4 · Float `Microsoft.Extensions.*` floor per-TFM (don’t force net8 consumers to 10.x) · Sev Med · Eff S
- [ ] A5 · Make `SettingsHolder`/`ISettingsHolder` internal (leaked detail) · Sev Low · Eff S · **Break**
- [ ] A6 · Fix command-line parsing of quoted values / exe path · Sev Med · Eff M

---

# Phase 1 — Correctness bugfixes

### B1 · Register `EnumTypeConverter`  — Sev High · Eff S · non-breaking
**Problem:** `EnumTypeConverter` exists (`src/Core/ExistForAll.SimpleSettings/Conversion/EnumTypeConverter.cs`) but is **never registered**. `TypeConvertersCollections` adds only DateTime/Uri/Array/Enumerable/Default (`src/Core/ExistForAll.SimpleSettings/Conversion/TypeConvertersCollections.cs:9-13`). An enum property bound from a string therefore falls through to `DefaultTypeConverter` → `Convert.ChangeType("Monday", typeof(DayOfWeek))` → `InvalidCastException`.
**Fix:** register it immediately before `DefaultTypeConverter` (order matters — `DefaultTypeConverter.CanConvert` always returns `true`):
```csharp
AddLast(new EnumerableTypeConverter(settingsOptions, this));
AddLast(new EnumTypeConverter());        // <-- add (parameterless ctor)
AddLast(new DefaultTypeConverter());
```
**Verify:** T2 goes green. Consider (separately) whether `Enum.Parse` should be case-insensitive and/or `Enum.IsDefined`-checked (`EnumTypeConverter.cs:15` is case-sensitive with no defined-value check) — capture as a follow-up decision, not part of this fix.

### B2 · Invariant culture in `DefaultTypeConverter`  — Sev High · Eff S · non-breaking
**Problem:** `src/Core/ExistForAll.SimpleSettings/Conversion/DefaultTypeConverter.cs:14` calls `System.Convert.ChangeType(value, settingsType)` with no `IFormatProvider`, so `double`/`decimal`/`float` parse under `CultureInfo.CurrentCulture`. On a `de-DE` host, `"1.5"` → `15` or throws — silent data corruption.
**Fix:**
```csharp
using System.Globalization;
...
return System.Convert.ChangeType(value, settingsType, CultureInfo.InvariantCulture);
```
**Verify:** T1 goes green.

### B4 · Fix `BindingContext.PropertyType`  — Sev Med · Eff S · behavior fix (public)
**Problem:** `src/Core/ExistForAll.SimpleSettings/BindingContext.cs:38` sets `PropertyType = propertyInfo.DeclaringType!` — the **declaring settings interface**, not the property’s type. This is public surface consumed by custom `ISectionBinder` authors. (Confirmed: no internal code reads `context.PropertyType`, so the fix is safe.)
**Fix:** `PropertyType = propertyInfo.PropertyType;`  Also delete the dead null-checks at `BindingContext.cs:31-32` (`string.Equals(x, null, …)` is always false after the `x == null` guards above).
**Verify:** add an assertion `context.PropertyType == typeof(TheProperty)` (fails first).

### B5 · Fix validator contradiction  — Sev Med · Eff S · non-breaking
**Problem:** `src/Core/ExistForAll.SimpleSettings/Core/SettingsOptionsValidator.cs:10-18` allows any one of Attribute/Interface/Suffix indication, but line 17 then **unconditionally** requires `AttributeType` to be an `Attribute`. Setting `AttributeType = null` (to use interface/suffix only) makes `IsAssignableFrom(null)` false → throws `SettingsOptionNonAttributeException(null!)`, which NREs building its own message.
**Fix:** guard the check:
```csharp
if (settingsOptions.AttributeType != null &&
    !typeof(Attribute).GetTypeInfo().IsAssignableFrom(settingsOptions.AttributeType))
    throw new SettingsOptionNonAttributeException(settingsOptions.AttributeType);
```
**Verify:** T-row asserting `AttributeType=null` + non-empty suffix validates without throwing.

### B9 · Fix broken error-message interpolation  — Sev Low · Eff S · non-breaking
**Problem:** `src/Core/ExistForAll.SimpleSettings/Resources.cs:46` is a verbatim (`@`) string with no `$`, so users see the literal `[{typeName}]`. Also `:49` has a stray `$` inside an interpolated string (`[${type.FullName}]`).
**Fix:** add `$` on line 45/46 (`$@"[{typeName}] is not an interface…"`); remove the stray `$` on line 49 (`[{type.FullName}]`).
**Verify:** existing suite stays green; optional message assertion.

---

# Phase 2 — Dead code & naming (breaking — do while pre-stable)

### D1 · Delete or wire the dead `Validations` API  — Sev Med · Eff M · **Break**
**Problem:** the entire `src/Core/ExistForAll.SimpleSettings/Validations/*` (`ISettingsValidator`, `ISettingValidation<T>`, `ValidationContext`, `ValidationContextOfT`, `ValidationResult`, `ValidationError`) plus `SettingsPropertyAttribute.ValidatorType` (`SettingsPropertyAttribute.cs:16`) are **public but referenced nowhere** — no `.Validate(` call exists in the pipeline (grep-confirmed). Ships a contract that silently does nothing.
**Fix — pick one:**
- **(Recommended, fast)** Delete the `Validations/` folder and `SettingsPropertyAttribute.ValidatorType`.
- **(Feature)** Wire validation into `ValuesPopulator.PopulateInstanceWithValues` after conversion: resolve `ValidatorType`/registered validators, run them, aggregate `ValidationError`s, throw a typed exception on failure. Larger; treat as its own feature spec.
**Verify:** solution builds; suite green. If wiring, add validation tests.

### D2 · Delete or fix+wire `EqualityCompererCreator`  — Sev Med · Eff S
**Problem:** `src/Core/ExistForAll.SimpleSettings/Core/Reflection/EqualityCompererCreator.cs` has **0 references** (grep-confirmed) and `SettingsClassGenerator.cs:48` discards the field list (`out _`). It’s also **buggy**: `Emit(OpCodes.Ldc_I4)` at `:38` uses the no-operand overload (invalid IL — proof it never ran), and `DeclareLocal(typeBuilder.DeclaringType!)` at `:18` is null for a top-level generated type.
**Fix — pick one:**
- **(Recommended)** Delete `EqualityCompererCreator.cs` + `IEqualityCompererCreator.cs` (both `internal`, non-breaking).
- **(Feature)** Fix the IL (`Emit(OpCodes.Ldc_I4_0)` / correct operand; local type = the `TypeBuilder`), capture the fields from `PropertyCreator.CreateAnonymousProperties(..., out var fields)`, and call `CreateEqualsMethod`/`CreateGetHashCodeMethod` in `SettingsClassGenerator.GenerateType`. Add value-equality tests.
**Verify:** builds; suite green.

### D3 · Fix the `ExistsForAll` namespace typo  — Sev Med · Eff S · **Break**
**Problem:** one file in the Binders package declares `namespace ExistsForAll.SimpleSettings.Binders` (`src/Core/ExistAll.SimpleConfig.Extensions.Binders/CommandLineSettingsBinder.cs:7`) while its 6 siblings use `ExistForAll.SimpleSettings.Binders`. The csproj’s `<RootNamespace>` is also the typo (`ExistForAll.SimpleSettings.Binders.csproj:5` = `ExistsForAll…`). Consumers must import two namespaces for one package.
**Fix:** change `CommandLineSettingsBinder.cs:7` to `ExistForAll.SimpleSettings.Binders`; fix `<RootNamespace>` to `ExistForAll.SimpleSettings.Binders`. Remove the now-redundant `using ExistsForAll.SimpleSettings.Binders;` in `SettingsBuilderFactoryExtensions.cs` if present.
**Verify:** builds; suite green.

---

# Phase 3 — Correctness / feature (design first)

### C1 · `List<T>`/`IList<T>`/`ICollection<T>` support  — Sev Med · Eff M
**Problem:** `TypeExtensions.IsEnumerable` (`src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypeExtensions.cs:32-37`) matches **only** the exact `IEnumerable<>`. So `List<int>`, `IList<int>`, `ICollection<int>` properties fall through to `DefaultTypeConverter` and throw.
**Fix — decide:** (a) document the limitation and throw a clear error, or (b) broaden `EnumerableTypeConverter.CanConvert` to handle assignable collection interfaces/`List<T>` and return a compatible instance. Coordinate with P4.
**Verify:** T6 doc test, upgraded to a positive test if (b).

### C2 · Public exception base + structured data  — Sev Med · Eff M · **Break** · **DONE (branch `refactor/c2-exception-hierarchy`)**
**Problem:** 10 exception types derived straight from `Exception` (no common base — couldn’t `catch (SimpleSettingsException)`); 4 were `internal` yet escaped the public build path (uncatchable by type); context (binder/section/key/type) lived only in message strings; the reachable not-an-interface guard threw an untyped `InvalidOperationException` (`SettingsCollection.cs:21,31`, `SettingsBuilder.cs:71`).

**Resolved (this PR).** Added `public abstract class SimpleSettingsException : Exception` (protected `(message)` + `(message, inner)` ctors; no parameterless/`[Serializable]` ctor — BinaryFormatter is obsolete on net8/10). Reparented all 10; promoted the 4 escapees (`SettingsPropertyValueException`, `SettingsPropertyNullException`, `TypeGenerationException`, `SettingsPropertyExtractionException`) to public; **flattened 3 mis-namespaced types** (`SettingsExtractionException` from `.Core`; `TypeGenerationException` + `SettingsPropertyExtractionException` from `.Core.Reflection`) to the root namespace so the public surface is coherent. Exposed leak-safe structured properties (`SettingsBindingException.{BinderType,Section,Key}`, `SettingsPropertyValueException.{SettingsType,PropertyName,TargetType,ConversionErrorType}`, `SettingsType`/`OptionType`/`ArgumentName` on the rest). New `SettingsTypeNotInterfaceException : SimpleSettingsException` replaces the 3 `InvalidOperationException(TypeIsNotInterface)` throws (the one real behavior break — flag in release notes). The two documented-**unreachable** "No converter found" `InvalidOperationException`s (`TypeConverter.cs:57`, `CollectionTypeConverter.cs:68`) were **left** as internal invariant guards.

**S1 preserved structurally:** `SettingsPropertyValueException`'s ctor now takes the failure's **`Type`** (not the `Exception`), so no value-bearing object crosses its boundary; `SettingsBindingException` stores primitives and **does not retain the `BindingContext`** (which holds the bound value). `InnerException == null` is asserted by test.

**Reviewed:** plan by `security-auditor` (ENDORSE-WITH-CHANGES — structural S1 hardening) + `dotnet-architect` (ENDORSE-WITH-CHANGES — namespace flattening, `Section`/`ConversionErrorType` naming, "now public" test must use reflection since IVT masks it); code by `/code-review` (high) + Roslyn `detect_antipatterns` (0). **Tests +6** (`SimpleSettings/ExceptionHierarchyTests.cs`): base is public+abstract; a reflection invariant that every library exception derives from the base; the 4 promotions are public (reflection); not-interface→typed+catchable; conversion→structured metadata + `InnerException == null`; binder-throws→context. Suite **82 net10** (was 76).

### C3 · Reload / provider-vs-singleton semantics  — Sev High · Eff M–L
**Problem:** DI registers startup-built **singletons** (`ServicesSettingsBuilderExtensions.cs:37-42`), but `ISettingsProvider.GetSettings` **re-binds a fresh instance every call** (`SettingsBuilder.cs:99-108`) — so `GetService<IFoo>()` and `provider.GetSettings<IFoo>()` can return different objects/values. No `IOptionsMonitor`-style reload exists (acknowledged by the `// replace this…` TODO at `ServicesSettingsBuilderExtensions.cs:35`).
**Fix — decide the contract:** either (a) cache in the provider so both paths agree (implemented by **P1**), or (b) add a snapshot/change-token reload path. Document whichever you choose. This is the defining feature vs `IOptions` — resolve before the “replacement” claim ships.
**Verify:** T3 pins the chosen semantics.

---

# Phase 4 — Tests

> Match existing TUnit conventions: `[Test] async Task`, `[Arguments(...)]`, `[NotInParallel("…")]` for process-global state, `await Assert.That(...).IsEqualTo/.Throws<T>()`, nested `public interface` fixtures per class, build via `SettingsBuilder.CreateBuilder(x => …)`. New files under `src/Tests/ExistForAll.SimpleSettings.UnitTests/`.

### T1 · Culture parse (fails first · pairs with B2)
`Conversion/DefaultTypeConverterTests.cs` — `[NotInParallel]`, set `CultureInfo.CurrentCulture = new("de-DE")` in a try/finally; bind a `double` from `"1.5"`, assert `1.5`; `decimal` from `"1234.56"`; int baseline `"42"`→`42`.

### T2 · Enum-from-string (fails first · pairs with B1)
`Conversion/EnumConversionTests.cs` — in-memory `"Monday"` → `DayOfWeek.Monday` (fails today); `DefaultValue = DayOfWeek.Monday` path (works today) for contrast; converter-level case-sensitivity + undefined-value behavior (documents current `Enum.Parse`).

### T3 · DI / Generic-Host integration (headline feature — 0 coverage)
**Prep (one-time):** add to `src/Tests/ExistForAll.SimpleSettings.UnitTests/ExistForAll.SimpleSettings.UnitTests.csproj`:
```xml
<ProjectReference Include="..\..\Core\ExistForAll.SimpleSettings.Extensions.GenericHost\ExistForAll.SimpleSettings.Extensions.GenericHost.csproj" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" />
```
`DependencyInjection/AddSimpleSettingsIntegrationTests.cs`:
- resolves a settings interface with bound values (in-memory source);
- registered as **singleton** (two resolves ⇒ `ReferenceEquals`);
- `ISettingsProvider.GetSettings<T>()` behavior per the C3 decision;
- `AddSimpleSettings(null)` → `ArgumentNullException`.

### T4 · `ValuesPopulator` unit (internal ctor + fakes)
`Core/ValuesPopulatorTests.cs` — binder-throws ⇒ `SettingsBindingException` (inner preserved); conversion-throws ⇒ `SettingsPropertyValueException` (carries type/value/property); later binder overrides earlier; no binder ⇒ attribute default survives.

### T5 · `TypeConverter` unit
`Core/TypeConverterTests.cs` — null→value-type default; null→`IEnumerable<int>` empty (not null); `Nullable<int>` strip+convert; allow-empty-false throw; attribute `ConverterType` bypasses the collection.

### T6 · Converters
`Conversion/{Array,Enumerable,Uri,DateTime}…Tests.cs` — split delimiter (default + custom + empty-entry removal); single-value→collection; result is concrete `List<T>`; Uri from string + non-string throws; DateTime custom format + invariant culture; **`List<int>` doc test** (see C1).

### T7 · Generator / collection / binders
`SimpleSettings/SettingsClassGeneratorConcurrencyTests.cs` — same interface twice ⇒ `ReferenceEquals`; extraction-fails ⇒ `TypeGenerationException`; `Parallel.For` stress on `GenerateType(typeof(IRoot))` (the check-then-`DefineType` at `SettingsClassGenerator.cs:37-44` is unsynchronized — expected flaky, documents the hazard, see P-note). Plus `SettingsCollection` not-found/try-get/duplicate-add; command-line + env-var edge cases; 3-binder precedence extension.

---

# Phase 5 — Performance

> Two cost phases: **startup** (once per discovered type — IL emit + reflective populate; dominates wall-clock) and **per-resolve** (DI-singleton path is already free; only `ISettingsProvider.GetSettings` re-pays). Fixing the populate/convert path helps both.

### P0 · Benchmark harness upgrade (do first)  — Eff M
`src/performance/ExistsForAll.SimpleSettings.Benchmark/` currently measures only cold `ScanAssemblies` and has no allocation view. Add: `[MemoryDiagnoser]`; split into `ColdScan` / `WarmResolve_Provider` / `WarmResolve_DiSingleton`; `[Params]` property count 1/10/50; array-heavy, enum/DateTime/Uri, and deep-hierarchy fixtures; a converter-selection microbenchmark. Capture a baseline before P1–P5; gate on **mean** and **Allocated**.

### P1 · Cache built instance on the provider path  — Sev High · Eff S
`SettingsProvider.GetSettings` → `SettingsBuilder.GetSettings` → `InnerBuild(Type)` (`src/Core/ExistForAll.SimpleSettings/SettingsBuilder.cs:99-108`) runs `Activator.CreateInstance` + full populate on **every** call. Memoize by `Type` (`ConcurrentDictionary<Type, object>`), or serve from the startup-built `SettingsCollection`. ~99% cut on repeat provider resolves. **Also implements C3(a)** — decide C3 first.

### P2 · Memoize property extraction + fix O(n²) dedup  — Sev High · Eff S
`TypePropertiesExtractor.ExtractTypeProperties` (`src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypePropertiesExtractor.cs:16-27`) does LINQ + an O(n²) `Where(p => properties.All(...))` dedup, and is called twice per type (`SettingsClassGenerator.cs:42` + `ValuesPopulator.cs:31`) with no shared cache. Add `ConcurrentDictionary<Type, PropertyInfo[]>`; replace the dedup with a `HashSet<string>`; share one cache across generator + populator.

### P3 · Cached compiled “settings plan”  — Sev High · Eff L (biggest ceiling)
The populate loop (`src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs:36-55`) is reflection-saturated: reflective `property.SetValue` (`:55`, boxes value types); `GetSectionName` recomputed inside both loops though it’s constant per type; `GetPropertyName` recomputed per binder; `SettingsPropertyAttribute` read ~3×/property (`:36-40` + `TypeConverter.cs:36`). Build a per-interface `SettingsPlan` once (`ConcurrentDictionary<Type, SettingsPlan>`) holding: section name (once), and per property — resolved key, default value, chosen converter, and a **compiled setter** (emit the populate into the generated class in `PropertyCreator`, or a compiled `Action<object,object?>`). Folds in converter caching (below).

**Resolved (this PR):** `SettingsPlan` cached per type on the `ValuesPopulator` instance — section name resolved once and **lazily** (a no-binder scan never pays for it), per-property `PropertyPlan`/`PropertyConversion` as `readonly struct`s (one array alloc, no per-property object), converter chosen once (manual walk, not LINQ `First`, so the `LinkedList` enumerator isn't boxed), and `[SettingsProperty]` read **once** per property then threaded into key/default/conversion (was 3× via `GetPropertyName`/`GetDefaultValue`/`CreateConversion`). **Warm re-populate −55–61%** (50 props 15,681→6,816 B); gated `ScanBenchmark` **≈flat (−0.4%)**. The **compiled setter was dropped**: both variants (emit `__Set` into the generated type, and a compiled `Action`) regressed the *gated* cold `ScanBenchmark` (+25% for the emitted `__Set`) because the extra per-type codegen isn't amortized on a populate-once scan — and it bought **nothing** on the warm path, since net10's reflective `PropertyInfo.SetValue` no longer allocates an args array. A **code + perf review pass** (two agents) confirmed behavior/exception parity and drove the attribute-read consolidation + binder-array materialization; it also flagged **exception wrapping** (converter-setup failures at plan build are re-wrapped as `SettingsPropertyValueException`) and two follow-ups below.

**Follow-ups from the review:** (a) **P3b** — tiered/lazy setter compilation (compile on the 2nd+ populate) would de-reflect the hot path without regressing the cold scan; only worth it if a profile shows `SetValue` *time* (not allocation) matters. (b) **Binder key alloc** — `InMemoryCollection.CreateKey` (`Binder/InMemoryCollection.cs:27`) concatenates `section + ":" + key` per lookup (~⅓ of the warm populate number); a `ValueTuple<string,string>` dictionary key would make lookups allocation-free and remove the `"a:b"` collision ambiguity (same class of fix as Q3). Not a P3 regression — pre-existing binder cost.

### Quick wins  — Eff S each
- **Q1** `SettingsCollection.GetEnumerator` (`SettingsCollection.cs:52`) rebuilds a whole `Dictionary` per enumeration → `yield return` over the existing dictionary.
- **Q2** `SettingsTypesExtractor.cs:32` `Name.ToLower().EndsWith(suffix.ToLower())` → `EndsWith(suffix, StringComparison.OrdinalIgnoreCase)` (also fixes an ordinal-correctness smell); hoist the trimmed suffix. (Startup allocs across all scanned types.)
- **Q3** `EnvironmentVariableBinder` (`EnvironmentVariableBinder.cs:27-39`) news a `StringBuilder` per property + double dictionary lookup → fast-path `context.Key` when no prefix/formatter; single lookup.
- **Q4** `SettingsClassGenerator.cs:37` string-keyed assembly `GetType(...Replace("+"…))` per generate → `Dictionary<Type, Type>` cache.
- **Q5** `BindingContext.cs:31-32` dead null-checks per allocation (also covered by B4).

### P4 · De-reflect + DRY array/enumerable converters  — Sev Med · Eff M · **DONE (branch `perf/p4-dereflect-converters`)**
Was: `TypeConverter.cs` (empty enumerable via `Enumerable.Empty` `MakeGenericMethod().Invoke()`), `ArrayTypeConverter` (`Activator.CreateInstance(List<>)` + reflected `Enumerable.ToArray` `Invoke`), `EnumerableTypeConverter` (`Activator.CreateInstance(List<>)`), both selecting the element converter with LINQ `First` (boxes the `LinkedList` enumerator + a closure).
Now: a shared `CollectionTypeConverter` base implements `Convert` once — normalize the value to an array (split delimited string / passthrough / wrap scalar), select the element converter by walking the concrete `LinkedList` (struct enumerator, no boxing/closure), then fill an `Array.CreateInstance(elementType, n)` by index. `ArrayTypeConverter`/`EnumerableTypeConverter` are now thin subclasses differing only in `CanConvert` + element-type extraction; both return `T[]` (safe — `IsEnumerable()` matches only `IEnumerable<T>`, which a `T[]` satisfies). `CreateNullResult` uses `Array.CreateInstance(t,0)` instead of the `Enumerable.Empty<T>()` reflection. **Proof (`ConvertArrayBenchmark`, gated): 1.33 KB→688 B (−49%), 1,247→219 ns (5.7×).** 12 parity tests in `Conversion/CollectionConversionTests.cs` (int/string/enum/DateTime/Uri elements, empty-entry removal, custom delimiter, default passthrough, unbound→empty `T[]`, `T[]`-not-`List` guard, bad-element negative). Residual 688 B = irreducible split-substrings + element boxing + result array (shared by old & new). **Code-reviewed clean** (dotnet code-reviewer verified all parity claims; the enum/DateTime/Uri + null-path + negative tests were its suggestions — partially closes T6).

### P5 · Resolve config section once per type  — Sev Med · Eff M · **DONE (merged #26)**
Was: `ConfigurationBinder.BindPropertySettings` called `_configuration.GetSection(...)` per property (a fresh `ConfigurationSection` alloc each time) + a `$"{RootSection}:{Section}"` interpolation per property when a root is set — though the section is constant per type.
Now: the binder caches the resolved `IConfigurationSection` per section name in a `private readonly ConcurrentDictionary<string, IConfigurationSection>`, via a **zero-capture** `GetOrAdd(context.Section, static (name, self) => self.ResolveSection(name), this)` (a capturing lambda would allocate a 64 B delegate per call — measured). Kept the internal-cache approach (Option 2), **not** a contract change: the three-specialist plan review found threading `IConfigurationSection` through the Core `ISectionBinder`/`BindingContext` contract to be a layering violation (Core must not reference `Microsoft.Extensions.Configuration`), and the optimization is single-implementer (env/cmdline/in-memory binders are flat lookups). Reload-safe: `GetSection` returns a live view, so the cached section re-reads providers on each access (locked by a test). Dropped the dead `?.` (`GetSection` never returns null); stripped a stray BOM. **Proof (`ConfigBinderBenchmark`, gated): BindNoRoot 80→40 B (−50%), BindWithRoot 144→56 B (−61%)** — matched the perf review's predicted −40/−88 B deltas. 3 parity/live-view tests; also wired P4's `ConvertArrayBenchmark` into the CI filter (it was never gated). Reviewed clean (`/code-review`, after the `dotnet-claude-kit:code-reviewer` agent misfired 3×).

### S1 · Redact secret values from exception messages  — Sev Med · Eff S · found in P5 security review
`Resources.PropertySetterExceptionMessage` (`Resources.cs:34-36`) interpolates the raw bound value: `failed to to set the value [{value}] within the property [{property.Name}] for interface [{interfaceType.Name}]`. It backs `SettingsPropertyValueException`, thrown at **`ValuesPopulator.cs:124`** (per-populate convert failure — carries the real value) and `:103` (plan-build failure — passes `null`, so no leak there). A secret-bearing value that fails conversion lands in `Exception.Message` → logs. **Bigger than the one-liner:** the failing converter's **inner exception** *also* embeds the value — `Convert.ChangeType`/`Enum.Parse`/`DateTime.ParseExact`/`new Uri` all put the raw input in their own messages, and it's chained in (`: base(msg, exception)`) so loggers print it. **Realistic trigger:** a secret on a *typed* property (e.g. a credentialed URL failing `Uri` parse, or a secret mis-bound to `int`/`enum`); secret→`string` doesn't fail, so it won't hit. **Fix fork (decide in the plan review):** (a) redact our message only — quick, partial (inner exception still leaks); (b) also suppress/sanitize the value-bearing inner exception — full fix, costs diagnostic detail. Log type + length instead of the value. Both the exception and helper are `internal` → **non-breaking**. Fix the `failed to to set` double-word typo while here. Clean path for reference: `SettingsBindingException` (`Resources.cs:20-23`) already logs only binder + section + key, no value.

**Resolved (this PR) — full fix, Rec 1.** The three-specialist plan review chose **Rec 1** (redact + drop the inner, no config knob). Rec 2 (an opt-in `SettingsOptions` flag to restore the value/inner for debugging) was **rejected** by the security review as insecure-by-configuration (a bool that re-enables secret logging gets flipped on and left on); Rec 3 (redact our message only) fails the goal because the inner still leaks. Shipped:
- **`Resources.PropertySetterExceptionMessage`** rewritten — no `{value}`; reports property name, **target type** (`property.PropertyType.Name`), interface, and the failing converter's **exception type name** (a compile-time identifier, can't carry a secret). Typo fixed.
- **`SettingsPropertyValueException`** — ctor drops the `object? value` param; **does not chain** the framework inner (its message embeds the raw value on modern .NET: `FormatException`/`ArgumentException` for int/enum/DateTime). Uniform invariant: *this type never carries a value and never chains an inner* → auditably leak-proof. Both `ValuesPopulator` throw sites (`:104` plan-build, `:133` convert) updated.
- **New `SettingsPropertyNullException`** (internal) — the "AllowEmpty = false, no value" path (`PropertyConversion.cs:36`) is value-free, so it keeps its full message; `ConvertPropertyValue` rethrows it unredacted rather than folding it into the value exception (security-review required change — blanket redaction would have collapsed a useful message to `[Exception]`).
- **`ISectionBinder`** doc note — `SettingsBindingException` chains the binder's inner (it's `public`), so custom binders must not throw value-bearing messages; built-in binders don't (all four verified).
- **Tests (+5, suite 76 net10):** `Conversion/ExceptionRedactionTests.cs` binds a sentinel secret to `int`/`enum`/`DateTime`/`Uri`/a hostile custom converter and asserts the sentinel is absent from the whole `ex.ToString()` chain while property + target type remain; the existing null-path test updated to `SettingsPropertyNullException` + a message assertion. **Note (modern .NET):** `UriFormatException` is *generic* (no URI), so the Uri leak was via our message, not the inner — the Uri test targets our message accordingly. **Accepted tradeoff:** a non-secret misconfiguration now loses the framework message/stack (gets property + target + failure type instead) — flag in release notes.

---

# Phase 6 — Architecture strategy

### A1 · AOT / trimming story  — Sev High · Eff M–L
The engine is `Reflection.Emit` (`SettingsClassGenerator.cs:26`) + `MakeGenericType/Method` + `GetExportedTypes`, with **zero** `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` annotations on net8/net10 TFMs → silently breaks in Native AOT / trimmed apps. **Decide:** annotate the public entry points (honest consumer warnings) and/or plan a source-generator path replacing runtime emit. At minimum, document the limitation in the README before the stable release. Consider `AssemblyBuilderAccess.Run` instead of `RunAndCollect` (`:26`) unless unloading is required.

### A2 · Consolidate naming  — Sev Low · Eff M
Three spellings repo-wide: `ExistAll` (folder `ExistAll.SimpleConfig.Extensions.Binders`, `src/ExistAll.SimpleConfig.slnx`), `ExistsForAll` (`Company` in `Directory.Build.props`, benchmark project), `ExistForAll` (everything else). Pick `ExistForAll`; rename folder + `.slnx` + `Company` + benchmark namespace. Batch with D3.

### A3 · `Core.AspNet` exports nothing  — Sev Med · Eff S
`Environments` is `internal` (`src/Core/ExistForAll.SimpleSettings.Core.AspNet/Environments.cs:3`) and `Info.cs` only exposes to tests, so the published `ExistForAll.SimpleSettings.Core.AspNet` package has no consumable public type. Make `Environments` public, or drop the package.

### A4 · Dependency floor per-TFM  — Sev Med · Eff S
`src/Directory.Packages.props` pins `Microsoft.Extensions.*` to `10.0.9` for **both** TFMs, pulling net8 consumers of the binders/DI packages up to the 10.x assemblies. Float the floor to the lowest supported (`8.0.x`) via per-TFM `PackageVersion`, or justify the pin.

### A5 · Hide `SettingsHolder`  — Sev Low · Eff S · **Break**
`SettingsHolder`/`ISettingsHolder` are `public` but never appear in a public signature (only built internally in `SettingsCollection.cs:15`). Make internal.

### A6 · Command-line quoted-value parsing  — Sev Med · Eff M
The command-line binder splits `Environment.CommandLine.Trim().Split(' ')` (in `src/Core/ExistAll.SimpleConfig.Extensions.Binders/SettingsBuilderFactoryExtensions.cs`), which breaks quoted values with spaces and includes the executable path. Parse args properly (respect quotes; skip arg[0]).

---

## Recommended first PR
**Phase 1 (B1, B2, B4, B5, B9) + their tests (T1, T2)** — small, non-breaking, and each fix ships with a regression guard. Then Phase 2 (breaking cleanup) while still pre-stable, then Phase 4 tests, then Phase 5 perf (P0 → P1 → P2 → quick wins → P3).
