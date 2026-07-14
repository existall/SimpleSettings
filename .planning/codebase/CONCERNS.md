# Codebase Concerns

**Analysis Date:** 2026-07-13

> Context: This repo carries a detailed, source-verified `FIX-PLAN.md` (37 KB) and
> `SESSION-HANDOFF.md` tracking an active hardening effort (architecture · tests ·
> performance · security). The performance track (P0–P5) is COMPLETE and merged;
> security item S1 is in flight on the current branch. The concerns below reflect
> what remains OPEN in source as of `master` @ `498fc81`.

## Tech Debt

**Dead `Validations` API (never wired into the pipeline):**
- Issue: A whole `Validations/` namespace ships (`ISettingsValidator`, `ISettingValidation`, `ValidationContext`, `ValidationContextOfT`, `ValidationError`, `ValidationResult`) plus `ValidatorType` on `SettingsPropertyAttribute`, but nothing in the populate/bind path consumes it. Public surface with no behavior.
- Files: `src/Core/ExistForAll.SimpleSettings/Validations/*.cs`, `src/Core/ExistForAll.SimpleSettings/SettingsPropertyAttribute.cs`
- Impact: Misleads consumers (looks like validation is supported), enlarges the public API that must be kept stable post-`v2.0.0`.
- Fix approach: FIX-PLAN item **D1** — either wire it into `ValuesPopulator` or delete it. **HELD** deliberately: a `validate-settings` feature branch is expected to reconcile with it. Decide before cutting the first stable tag (breaking changes are free while only `2.0.0-alpha` is published).

**Dead `EqualityCompererCreator` with invalid IL:**
- Issue: `IEqualityCompererCreator` / `EqualityCompererCreator` are never invoked by `SettingsClassGenerator` (which uses `PropertyCreator` only). The emitted IL is also wrong — `CreateEqualsMethod` emits `OpCodes.Ldc_I4` with no operand (line 40) where it means to push a false/return value, so if it were ever wired in it would produce broken types.
- Files: `src/Core/ExistForAll.SimpleSettings/Core/Reflection/EqualityCompererCreator.cs`, `.../IEqualityCompererCreator.cs`
- Impact: Dead, latently-broken reflection-emit code that looks load-bearing.
- Fix approach: FIX-PLAN item **D2** — delete, or fix the IL and wire it. **HELD** pending feature decision.

**`Core.AspNet` package ships no public type:**
- Issue: The entire `ExistForAll.SimpleSettings.Core.AspNet` project contains only `internal static class Environments`. A published NuGet package with zero public surface.
- Files: `src/Core/ExistForAll.SimpleSettings.Core.AspNet/Environments.cs`
- Impact: A meaningless package on the feed; consumers can reference it and get nothing.
- Fix approach: FIX-PLAN item **A3** — make `Environments` public or drop the package entirely.

**Naming still not fully consolidated:**
- Issue: The core namespace was renamed to `ExistForAll` (A2, done), but a legacy folder `src/Core/ExistAll.SimpleConfig.Extensions.Binders` and mixed `ExistsForAll`/`ExistAll` remnants have existed. The Binders assembly name is `ExistForAll.SimpleSettings.Binders` while its folder is `...Extensions.Binders` — folder/assembly/namespace drift.
- Files: `src/Core/ExistAll.SimpleConfig.Extensions.Binders/`, `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/`
- Impact: Confusing layout; risk of shipping under inconsistent identities.
- Fix approach: Continue the A2 consolidation; verify one canonical name across folder, `.csproj` `AssemblyName`, and root namespace.

## Known Bugs

**Command-line parser mishandles quoted values and `=`-bearing paths:**
- Symptoms: `SplitByDelimiter` splits on the *first* delimiter occurrence with no quote handling. An argument like `--path="C:\a=b\c"` splits at the `=` inside the quoted value, and surrounding quotes are never stripped. The exe path / positional args are also not distinguished.
- Files: `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/CommandLineSettingsBinder.cs` (`SplitByDelimiter`, lines 56–90; `Parse`, 37–53)
- Trigger: Any quoted CLI value containing a delimiter char.
- Workaround: Avoid delimiter characters inside values; pass values without quotes.
- Fix approach: FIX-PLAN item **A6** — proper tokenization (quote-aware split, strip surrounding quotes, ignore/skip a leading exe path).

## Security Considerations

**Secret bound values leaking through conversion-failure exceptions (S1 — in flight):**
- Risk: A failed type conversion of a bound setting value (e.g. a malformed connection string or token) could surface the raw value in the exception message or via a chained framework `InnerException`, then land in logs / crash reports.
- Files: `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs` (`ConvertPropertyValue`, lines 117–131)
- Current mitigation: Full fix shipped on branch `security/s1-redact-exception-value` (PR open): `SettingsPropertyValueException` no longer carries the bound value **nor** chains the value-bearing inner (only the failure's type name is surfaced); the value-free "required value missing" case was split into `SettingsPropertyNullException` so its useful message survives. 5 redaction tests added (`ExceptionRedactionTests.cs`).
- Recommendations: Merge S1. Audit the other exception wrappers (`SettingsBindingException`, `SettingsExtractionException`, `TypeGenerationException`) for the same value-leak shape and confirm none embed bound values.

**Reflection.Emit + `GetExportedTypes()` scanning trust boundary:**
- Risk: `SettingsTypesExtractor` reflects over every exported type of every scanned assembly and generates dynamic types for matches. Scanning untrusted/plugin assemblies runs their type initializers and widens attack surface.
- Files: `src/Core/ExistForAll.SimpleSettings/Core/SettingsTypesExtractor.cs`, `src/Core/ExistForAll.SimpleSettings/Core/Reflection/SettingsClassGenerator.cs`
- Current mitigation: Failures are wrapped (`SettingsExtractionException`, `TypeGenerationException`); scan is opt-in by assembly.
- Recommendations: Document that only trusted assemblies should be scanned.

## Performance Bottlenecks

**Status: the identified hot paths are already fixed and gated.** The perf track (P0–P5, plus quick wins Q1–Q5) is complete and merged, with BenchmarkDotNet CI gating PRs on >10% allocation regressions (`.github/workflows`, `src/performance/ExistForAll.SimpleSettings.Benchmark`). Documented wins: provider instance cache (P1), memoized `ExtractTypeProperties` + O(n²)→dedup fix (P2), cached per-type `SettingsPlan` (P3, warm re-populate −55–61%), de-reflected collection converters (P4, 5.7×), per-type config-section cache (P5, −50–61%).

**Remaining (deferred, low priority):**
- Problem: Property writes still use reflective `PropertyInfo.SetValue`.
- Files: `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs` (line 66)
- Cause: A compiled/emitted setter was tried and reverted — it regressed the cold scan for no warm gain (net10 `SetValue` is already alloc-free).
- Improvement path: FIX-PLAN item **P3b** — tiered/lazy compiled setter, ONLY if set *time* shows up in a real profile.

## Fragile Areas

**Dynamic type generation (`SettingsClassGenerator`):**
- Files: `src/Core/ExistForAll.SimpleSettings/Core/Reflection/SettingsClassGenerator.cs`, `.../PropertyCreator.cs`
- Why fragile: Runtime IL emission via `Reflection.Emit`. Generated impl-type names are namespace-mangled to avoid collisions (M1 fix, line 47), but there is a known open concern about **concurrent** `GenerateType` calls racing to `DefineType` the same name before the `ConcurrentDictionary` cache is populated (the `TryGetValue`→`DefineType`→assign window is not atomic).
- Safe modification: Do not change the mangling without checking it doesn't also alter the config section name (`SettingsOptions.SectionNameFormatter` intentionally uses a *different*, simple-name helper). Preserve the exception-wrapping contract.
- Test coverage: Caching is covered (`SettingsClassGeneratorTests.cs`); the **concurrency race is NOT** (FIX-PLAN T7).

**`ValuesPopulator` precedence + exception contract:**
- Files: `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs`
- Why fragile: Binder application order defines value precedence, and three distinct exception paths (bind failure, converter-setup failure at plan build, value-conversion failure) each have a carefully-worded contract about what they may/may not surface (see S1). Easy to regress the redaction guarantee.
- Test coverage: Redaction is now covered; precedence + wrapper behavior (FIX-PLAN **T4**) is not.

## Scaling Limits

**Microsoft.Extensions.* version floor forces net8 consumers onto 10.x:**
- Current capacity: All `Microsoft.Extensions.*` packages are pinned to `10.0.9` in `src/Directory.Packages.props`, applied uniformly across both `net8.0` and `net10.0` targets.
- Limit: A net8 consumer referencing SimpleSettings is transitively dragged to `Microsoft.Extensions.* 10.x`, which they may not want.
- Scaling path: FIX-PLAN item **A4** — float the `Microsoft.Extensions.*` floor per-TFM (lower floor for net8, 10.x for net10).

## Dependencies at Risk

**No AOT / trimming story:**
- Risk: The library is built entirely on runtime reflection + `Reflection.Emit`. It is fundamentally incompatible with NativeAOT and aggressive trimming, but carries no `[RequiresDynamicCode]` / `[RequiresUnreferencedCode]` annotations — consumers get no warning.
- Impact: Silent runtime failures for AOT/trimmed apps; a future .NET pushing harder toward AOT raises the stakes.
- Migration plan: FIX-PLAN item **A1** — decide the AOT/trim story; annotate the reflection entry points and/or plan a compile-time source generator as an alternative to `Reflection.Emit`.

## Missing Critical Features

**Only `IEnumerable<T>` collections are supported:**
- Problem: `TypeConverter.CreateNullResult` / the collection converter path materialize `T[]` for `IEnumerable<T>`; `List<T>`, `IList<T>`, `ICollection<T>` are not first-class.
- Files: `src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypeConverter.cs`, `src/Core/ExistForAll.SimpleSettings/Conversion/CollectionTypeConverter.cs`
- Blocks: Settings interfaces exposing `List<T>`/`IList<T>` properties.
- Fix approach: FIX-PLAN item **C1** — either support the common collection interfaces or explicitly document the `IEnumerable<T>`-only limit.

**No public exception base:**
- Problem: Boundary-crossing exceptions (`SettingsBindingException`, `SettingsPropertyValueException`, etc.) lack a common public `SimpleSettingsException` base, so consumers can't catch the library's failures as one category.
- Files: exception types across `src/Core/ExistForAll.SimpleSettings/`
- Fix approach: FIX-PLAN item **C2** (breaking) — introduce a public base and make boundary exceptions public + structured. Do before the first stable tag.

## Test Coverage Gaps

**`ValuesPopulator` (precedence + exception wrappers):**
- What's not tested: Binder precedence ordering and the bind/setup/convert exception wrapper contracts (beyond the S1 redaction tests).
- Files: `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs`
- Risk: A refactor could silently reorder precedence or re-introduce a value leak.
- Priority: High (FIX-PLAN **T4**).

**`TypeConverter` null / nullable / empty-enumerable / attribute paths:**
- What's not tested: `CreateNullResult`, `StripIfNullable`, attribute-`ConverterType` selection, the "no converter found" path.
- Files: `src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypeConverter.cs`
- Risk: Nullable/enumerable edge regressions.
- Priority: Medium (FIX-PLAN **T5**).

**`SettingsClassGenerator` concurrency stress:**
- What's not tested: Concurrent `GenerateType` for the same interface (the `TryGetValue`→`DefineType` race).
- Files: `src/Core/ExistForAll.SimpleSettings/Core/Reflection/SettingsClassGenerator.cs`
- Risk: Duplicate-type-name / race failures under parallel first-touch.
- Priority: Medium (FIX-PLAN **T7**).

**Converter unit tests (array / enumerable / Uri / DateTime + `List<T>` doc test):**
- What's not tested: FIX-PLAN **T6** — some coverage exists (`CollectionConversionTests.cs`, `EnumConversionTests.cs`, `DefaultTypeConverterTests.cs`), but Uri/DateTime and the `List<T>` limitation are gaps.
- Priority: Medium.

---

*Concerns audit: 2026-07-13*
