# Phase 2: Binding Correctness & Engine Test Hardening - Pattern Map

**Mapped:** 2026-07-14
**Files analyzed:** 3 new test files (TEST-01, TEST-02, TEST-03)
**Analogs found:** 3 / 3 (all exact-role matches — existing TUnit test classes in the same project)

This is a **test-only** phase. Every new file is a TUnit test class under
`src/Tests/ExistForAll.SimpleSettings.UnitTests/`. There is no production-code analog to copy;
the analogs are existing **test classes** whose TUnit shape, fixture layout, and engine-wiring
the new files mirror. Do NOT re-assert contracts already locked by `ExceptionHierarchyTests` /
`ExceptionRedactionTests` (see the Don't-Duplicate map in `02-RESEARCH.md`).

## File Classification

| New File | Role | Data Flow | Closest Analog | Match Quality |
|----------|------|-----------|----------------|---------------|
| `Core/ValuesPopulatorTests.cs` (TEST-01) | test | request-response (bind→convert→set) | `SimpleSettings/ExceptionHierarchyTests.cs` (fake binder + `CatchBase`) + `Conversion/CollectionConversionTests.cs` (two-binder integration) | exact |
| `Core/TypeConverterTests.cs` (TEST-02) | test | transform (null/nullable/converter-select) | `Conversion/CollectionConversionTests.cs` (integration build) + `SimpleSettings/SettingsBuilderConversionsTests.cs` (`ConverterType`) | exact |
| `Conversion/ScalarConversionTests.cs` (TEST-03) | test | transform (scalar parse) | `Conversion/CollectionConversionTests.cs` (`Build<T>` helper, Uri/DateTime array cases) + `Conversion/DefaultTypeConverterTests.cs` (`[NotInParallel]` culture) | exact |

Note: `Core/` is a new test subfolder mirroring the source `ExistForAll.SimpleSettings.Core` namespace.
TEST-03 may extend an existing class instead of a new `ScalarConversionTests.cs` (Claude's discretion).

## Shared TUnit Conventions (apply to ALL three files)

**Source of truth:** `Conversion/CollectionConversionTests.cs`, `Conversion/DefaultTypeConverterTests.cs`.

- File is a plain `public class XxxTests` in namespace `ExistForAll.SimpleSettings.UnitTests.<Folder>`
  wrapped in a block-scoped namespace (project convention — see every analog).
- Test methods: `[Test] public async Task Name_State_Expected()`.
- Assertions: `await Assert.That(actual).IsEqualTo(expected)` / `.IsTrue()` / `.IsNull()` /
  `.IsNotEqualTo(...)`; throwing: `await Assert.That(() => act()).Throws<TException>()`.
- Fixtures are **nested `public interface`** declarations at the bottom of the class
  (`CollectionConversionTests.cs:148-182`), decorated with `[SettingsProperty(...)]` where a default
  or `ConverterType` is needed.
- Build path: `SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)))`
  then `builder.GetSettings<T>()`.
- Section-name gotcha: default `SectionNameFormatter` strips the leading `I` — `IThing` → `"Thing"`.
  Use the formatted name in `collection.Add(section, key, value)` (see `DefaultTypeConverterTests.cs:9`
  const `Section = "NumericSettings"` for `INumericSettings`).
- `InMemoryBinder`/`InMemoryCollection` live in namespace `ExistForAll.SimpleSettings.Binder`
  (`using ExistForAll.SimpleSettings.Binder;` — visible via InternalsVisibleTo, `Info.cs:3`).
- Culture-mutating tests: `[NotInParallel]` + save/restore in `finally`
  (`DefaultTypeConverterTests.cs:11-30`).

---

## Pattern Assignments

### `Core/ValuesPopulatorTests.cs` (TEST-01)

**Analogs:** `SimpleSettings/ExceptionHierarchyTests.cs` (fake binder + catch helper),
`Conversion/CollectionConversionTests.cs` (integration `Build<T>` helper).

**Source under test:** `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs` — binder loop
(`ValuesPopulator.cs:45-63`, last-writer-wins via `context.HasNewValue`), bind-catch wrapping
(`:59-61` → `SettingsBindingException`), convert wrapping (`:117-131`).

**Genuine gaps to cover (per RESEARCH Don't-Duplicate map):** last-writer-wins precedence;
"binders present but none set value ⇒ `[SettingsProperty]` default survives" (scalar); optional
unit-level bind-throw ⇒ `SettingsBindingException`.
**Do NOT re-assert** the value-free `SettingsPropertyValueException` contract — owned by
`ExceptionHierarchyTests.ConversionFailure_ExposesSafeStructuredMetadata_AndNoChainedInner`.

**Last-writer-wins pattern** (build two ordered binders; recommended integration route per
RESEARCH Open Question #1 — `CollectionConversionTests.cs:131-146` helper shape):
```csharp
var c1 = new InMemoryCollection(); c1.Add("Sample", nameof(ISample.Name), "first");
var c2 = new InMemoryCollection(); c2.Add("Sample", nameof(ISample.Name), "second");
var builder = SettingsBuilder.CreateBuilder(x =>
{
    x.AddSectionBinder(new InMemoryBinder(c1));
    x.AddSectionBinder(new InMemoryBinder(c2));   // later binder wins
});
await Assert.That(builder.GetSettings<ISample>().Name).IsEqualTo("second");
```

**Fake binder pattern** (mirror `ExceptionHierarchyTests.cs:116-120`; a *setting* fake calls
`context.SetNewValue(...)`):
```csharp
private class ThrowingBinder : ISectionBinder
{
    public void BindPropertySettings(BindingContext context)
        => throw new InvalidOperationException("binder failed");
}
```

**Bind-throw assertion** (mirror `ExceptionHierarchyTests.cs:79-90`, using the `CatchBase` helper
at `:92-104` — copy it verbatim if a unit-level bind-throw case is written):
```csharp
var ex = (SettingsBindingException)CatchBase(() => builder.GetSettings<IIntSettings>())!;
await Assert.That(ex.BinderType).IsEqualTo(typeof(ThrowingBinder));
await Assert.That(ex.Section).IsEqualTo("IntSettings");
await Assert.That(ex.Key).IsEqualTo(nameof(IIntSettings.Value));
```

**Internal-ctor option** (only if binder-order isolation is needed the builder can't express —
`ValuesPopulator.cs:26-35`): `new ValuesPopulator(new TypePropertiesExtractor(), new TypeConverter())`
then `PopulateInstanceWithValues(instance, typeof(T), options, binders)`. RESEARCH recommends the
integration path first.

---

### `Core/TypeConverterTests.cs` (TEST-02)

**Analogs:** `Conversion/CollectionConversionTests.cs` (integration + fixture interfaces),
`SimpleSettings/SettingsBuilderConversionsTests.cs` (`ConverterType` attribute).

**Source under test:** `Core/Reflection/TypeConverter.cs:13-24` (`CreateConversion`),
`:28-37` (`CreateNullResult` — enumerable→empty `T[]`, value-type→`Activator.CreateInstance`,
else null), `:39-58` (`GetConverter` — `attribute.ConverterType` wins at `:43-44`),
`:60-65` (`StripIfNullable`); runtime null-check in `Conversion/PropertyConversion.cs:31-42`
(`throwOnNull` ⇒ `SettingsPropertyNullException`).

**Genuine gaps:** `null` → value-type default (`int` → `0`); `Nullable<int>` strip+convert
(`int?` null → `null`, `"42"` → `42`); `ConverterType` bypasses the **collection** converter
(attribute on an `IEnumerable<T>`/array property — the residual; scalar `ConverterType` already
covered by `SettingsBuilderConversionsTests`). Empty-enumerable + `AllowEmpty` are already covered —
do NOT duplicate.

**Direct-seam pattern** (RESEARCH Pattern 2 — for pure resolution logic, no generated instance):
```csharp
var options = new SettingsOptions();            // Converters auto-seeded: DateTime,Uri,Array,Enumerable,Enum,Default
var conv    = new TypeConverter();
var intProp = typeof(ISample).GetProperty(nameof(ISample.Count))!;   // int
var c       = conv.CreateConversion(intProp, intProp.GetCustomAttribute<SettingsPropertyAttribute>(true), options);
await Assert.That(c.Convert(null)).IsEqualTo(0);                     // value-type default
// int? property: null -> null, "42" -> 42 (StripIfNullable, TypeConverter.cs:60-65)
```
Nullable pitfall (RESEARCH Pitfall 3): `int?` null → `null` (nullResult built on the original
nullable type), NOT `0`. Test `int` and `int?` separately.

**`ConverterType` fixture pattern** (mirror `SettingsBuilderConversionsTests.cs:32-58` — a
`SettingsPropertyAttribute` subclass with `ConverterType` set, plus an `ISettingsTypeConverter`;
here point it at a collection-typed property to prove it bypasses `CollectionTypeConverter`):
```csharp
public interface IThing
{
    [MyConv(ConverterType = typeof(FakeCollectionConverter))]
    IEnumerable<int> Values { get; set; }
}
```

---

### `Conversion/ScalarConversionTests.cs` (TEST-03)

**Analog:** `Conversion/CollectionConversionTests.cs` (the `Build<T>` helper at `:131-146` and the
Uri/DateTime **array** cases at `:101-116` — mirror shape, scope to **scalar**).

**Source under test:** `Conversion/UriTypeConvertor.cs` (`new Uri((string)value)`),
`Conversion/DateTimeTypeConverter.cs` (`DateTime.ParseExact(value, options.DateTimeFormat,
InvariantCulture)`, default format `"yyyy-MM-dd"`).

**Genuine gaps:** scalar `Uri` positive parse; scalar `DateTime` positive parse with configured
format. Array-of-Uri/DateTime are fully covered by P4 — do NOT duplicate. At most one DateTime
format-mismatch negative (redaction already locked — don't re-prove).

**Scalar positive pattern** (integration `Build<T>` shape from `CollectionConversionTests.cs:131-146`):
```csharp
var c = new InMemoryCollection();
c.Add("Endpoint", nameof(IEndpoint.Url), "https://a.example/");
c.Add("Endpoint", nameof(IEndpoint.When), "2020-01-02");     // default yyyy-MM-dd
var b = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(c)));
var r = b.GetSettings<IEndpoint>();
await Assert.That(r.Url).IsEqualTo(new Uri("https://a.example/"));
await Assert.That(r.When).IsEqualTo(new DateTime(2020, 1, 2));
```
Fixture interface at class bottom (mirror `CollectionConversionTests.cs:148-182`):
`public interface IEndpoint { Uri Url { get; set; } DateTime When { get; set; } }`.

---

## ENG-01 — Verify-Only (no new file)

Not mapped as new work. The 2 stress tests already exist and satisfy success criterion #4:
- `SettingsClassGeneratorTests.cs:103-116` `GenerateType_ConcurrentSameInterface_ReturnsSingleSharedType`
  (`Parallel.For(0,128)` → `results.Distinct().Count() == 1`).
- `SettingsClassGeneratorTests.cs:118-166` `GenerateType_ConcurrentAcrossSameAndDistinctInterfaces_IsRaceFree`
  (32 threads + `Barrier`, 8 interfaces, zero failures, one shared impl each).
Verify present + green; do NOT re-implement.

## No Analog Found

None. Every new file has a strong same-project test analog. COLL-01/C1 and D1/D2 are deferred/held
and intentionally unmapped.

## Metadata

**Analog search scope:** `src/Tests/ExistForAll.SimpleSettings.UnitTests/{Conversion,SimpleSettings}`,
`src/Core/ExistForAll.SimpleSettings/{,Core/Reflection,Conversion}`.
**Files scanned:** 7 (4 test analogs, 3 source-under-test).
**Pattern extraction date:** 2026-07-14
</content>
</invoke>
