# Phase 4: Collection & Validation Binding - Pattern Map

**Mapped:** 2026-07-15
**Files analyzed:** 17 (11 source new/modified + 6 test new/extend)
**Analogs found:** 17 / 17 (every file has an in-repo analog; this is a codebase-internal phase)

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| NEW `Conversion/ListTypeConverter.cs` | converter | transform | `Conversion/ArrayTypeConverter.cs` + `Conversion/EnumerableTypeConverter.cs` | exact (both extend `CollectionTypeConverter`) |
| MOD `Core/Reflection/TypeExtensions.cs` (add `IsListLike`) | utility | transform | `IsEnumerable` in same file (lines 21-26) | exact |
| MOD `Core/Reflection/TypeConverter.cs` `CreateNullResult` | service | transform | the method itself (lines 27-36) | self |
| MOD `Conversion/TypeConvertersCollections.cs` (register `ListTypeConverter`) | config | event-driven (registration) | existing `AddLast(...)` chain (lines 7-12) | self |
| MOD `Conversion/PropertyConversion.cs` `Convert` (VAL-02) | service | transform | the method itself (lines 29-40) | self |
| MOD `Extensions.Binders/ConfigurationBinder.cs` `BindPropertySettings` (COLL-03) | binder | request-response (config read) | the method itself (lines 26-36) | self |
| NEW `SettingsValidationException.cs` (VAL-01) | model (exception) | — | `SettingsPropertyValueException.cs` / `SettingsBindingException.cs` (aggregate-carrying variant) | role-match |
| NEW `SettingsValidatorAttribute.cs` (VAL-01, name = discretion) | model (attribute) | — | `SettingsPropertyAttribute.cs` | role-match (different `AttributeTargets`) |
| MOD `Validations/ISettingsValidator.cs` + `ISettingValidation.cs` (async→sync) | model (interface) | — | the files themselves | self |
| MOD `Validations/ValidationContext.cs` + `ValidationContextOfT.cs` (add ctor) | model | — | `ValidationError.cs` ctor shape (lines 8-12) | role-match |
| MOD `ValuesPopulator.cs` `PopulateInstanceWithValues` (VAL-01 hook) + `ConvertPropertyValue` filter (VAL-02) | service | event-driven (post-populate) | the method itself (lines 29-65, 114-128) | self |
| MOD `Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs` (API-02 + DI validators) | config (DI ext) | event-driven (registration) | the method itself (lines 7-44) | self |
| EXT `Conversion/CollectionConversionTests.cs` | test | — | itself (TUnit) | self |
| EXT `Core/TypeConverterTests.cs` | test | — | `CollectionConversionTests.cs` conventions | role-match |
| EXT `ConfigBuilderConfigurationBinderIntegrationTests.cs` | test | — | `AddSimpleSettingsIntegrationTests.cs` | role-match |
| EXT `Conversion/ExceptionRedactionTests.cs` | test | — | itself | self |
| EXT `DependencyInjection/AddSimpleSettingsIntegrationTests.cs` + NEW `SimpleSettings/SettingsValidationTests.cs` | test | — | `AddSimpleSettingsIntegrationTests.cs` | self / role-match |

**Cross-cutting conventions (all source files):** one public type per file; `I`-prefixed interfaces; block-scoped namespaces (`namespace X { ... }`, NOT file-scoped — verified across every source file read); internal types by default, `public` only on the intended surface; no `[Serializable]`/serialization ctors; comments explain the *why* of a non-obvious performance/security choice, sparingly. `net8.0;net10.0` — no `field` keyword / extension members (C# 14 / net10-only) in shared library code.

## Pattern Assignments

### `Conversion/ListTypeConverter.cs` (NEW — converter, transform)

**Analog:** `Conversion/ArrayTypeConverter.cs` (whole file) and `Conversion/EnumerableTypeConverter.cs` (whole file). Both are ~20-line `internal` subclasses of `CollectionTypeConverter` overriding only `CanConvert` + `GetElementType`.

**Shape to copy** — `ArrayTypeConverter.cs:3-19` (constructor forwarding + two overrides):
```csharp
internal class ArrayTypeConverter : CollectionTypeConverter
{
    public ArrayTypeConverter(SettingsOptions settingsOptions, TypeConvertersCollections converters)
        : base(settingsOptions, converters) { }

    public override bool CanConvert(Type settingsType) => settingsType.IsArray;
    protected override Type GetElementType(Type settingsType) => settingsType.GetElementType()!;
}
```

**Generic-arg element type** — copy from `EnumerableTypeConverter.cs:18-21`:
```csharp
protected override Type GetElementType(Type settingsType)
    => settingsType.GetTypeInfo().GetGenericArguments()[0];   // needs `using System.Reflection;`
```

**The wrap to `List<T>`** — the base `CollectionTypeConverter.Convert` (`CollectionTypeConverter.cs:25-38`) builds a typed `Array`. To wrap it, the planner must make that method `virtual` (or extract a `protected` array-build helper). `CanConvert` uses the NEW `IsListLike` predicate. Materialize via `Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType), builtArray)!`. Do NOT widen `IsEnumerable` (Pitfall 1).

---

### `Core/Reflection/TypeExtensions.cs` (MODIFY — add `IsListLike`)

**Analog:** the existing `IsEnumerable` (lines 21-26) — the exact predicate shape to mirror:
```csharp
public static bool IsEnumerable(this Type type)
{
    var info = type.GetTypeInfo();
    return info.IsGenericType && info.GetGenericTypeDefinition() == typeof(IEnumerable<>);
}
```
`IsListLike` must be **disjoint** from `IsEnumerable` and `IsArray`: match generic definitions `List<>`, `IList<>`, `ICollection<>`, `IReadOnlyList<>`, `IReadOnlyCollection<>`. Same `GetTypeInfo().IsGenericType && GetGenericTypeDefinition() == ...` idiom, checked against the set. COLL-03's binder also needs a "collection shape" check (array OR list OR enumerable) — either add a combined `IsCollectionShape` helper here or compose the three predicates.

---

### `Core/Reflection/TypeConverter.cs` — `CreateNullResult` (MODIFY, COLL-02)

**Analog:** the method itself (lines 27-36). Today it returns `null` for reference types that are not the open `IEnumerable<>`:
```csharp
private static object? CreateNullResult(Type propertyType)
{
    if (!propertyType.IsEnumerable())
        return propertyType.GetTypeInfo().IsValueType ? Activator.CreateInstance(propertyType) : null;
    var elementType = propertyType.GetTypeInfo().GetGenericArguments()[0];
    return Array.CreateInstance(elementType, 0);
}
```
Extend the shape branch: array target → `Array.CreateInstance(elementType, 0)` (element type via `GetElementType()` / `GetElementType()!`); list family → empty `List<T>` (`Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!`); open `IEnumerable<T>` → unchanged empty array; else → existing value-type/null branch. Reuse the same `Array.CreateInstance` idiom already in the file (do not reintroduce `Enumerable.Empty` reflection — see State of the Art).

---

### `Conversion/TypeConvertersCollections.cs` (MODIFY — register `ListTypeConverter`)

**Analog:** the ctor itself (lines 5-13):
```csharp
AddLast(new DateTimeTypeConverter(settingsOptions));
AddLast(new UriTypeConvertor());
AddLast(new ArrayTypeConverter(settingsOptions, this));
AddLast(new EnumerableTypeConverter(settingsOptions, this));
AddLast(new EnumTypeConverter());
AddLast(new DefaultTypeConverter());
```
Insert `AddLast(new ListTypeConverter(settingsOptions, this));` after `EnumerableTypeConverter` and before `EnumTypeConverter`. Predicate is disjoint, so order is safe. **Do NOT reorder** existing entries (load-bearing user-first / `DefaultTypeConverter`-last).

---

### `Conversion/PropertyConversion.cs` — `Convert` (MODIFY, VAL-02)

**Analog:** the method itself (lines 29-40):
```csharp
public object? Convert(object? value)
{
    if (value == null)
    {
        if (_throwOnNull)
            throw new SettingsPropertyNullException(_propertyName);
        return _nullResult;
    }
    return _converter.Convert(value, _strippedType);
}
```
Extend the guard so `_throwOnNull` also rejects null-or-whitespace strings (`value is string s && string.IsNullOrWhiteSpace(s)`). Throw a **value-free** `SimpleSettingsException` subtype — reuse `SettingsPropertyNullException` (already excluded from the redaction filter, simplest) OR a new sibling. If a NEW type is introduced, the `ValuesPopulator.ConvertPropertyValue` filter at line 122 MUST be widened (see below) — Pitfall 3. `_throwOnNull` resolved at `TypeConverter.cs:16` (`attribute is { AllowEmpty: false }`).

---

### `Extensions.Binders/ConfigurationBinder.cs` — `BindPropertySettings` (MODIFY, COLL-03)

**Analog:** the method itself (lines 26-36):
```csharp
public void BindPropertySettings(BindingContext context)
{
    var section = _sections.GetOrAdd(context.Section, static (name, self) => self.ResolveSection(name), this);
    var value = section[context.Key];
    if (value != null)
        context.SetNewValue(value);
}
```
Add a collection-shape branch BEFORE the scalar read: `section.GetSection(context.Key).GetChildren()` (`using Microsoft.Extensions.Configuration;` already present); if children exist, `context.SetNewValue(childValues.ToArray())` (a `string[]`) and return (children win). Fall through to the scalar otherwise; for collection targets, skip `SetNewValue` when the scalar is null/whitespace (empty→empty; COLL-02 default applies). Use `context.PropertyType` + the new `IsCollectionShape`/`IsListLike|IsArray|IsEnumerable` predicate. Preserve the `static` factory `GetOrAdd` (no per-call closure) and the `ResolveSection` root/`[SettingsSection]` prefixing (lines 38-41) — inherited for free by `GetSection(key)`. `CollectionTypeConverter.AsArray` (lines 43-51) passes an incoming `Array` through unchanged, so no converter change.

**Redaction (D-06 gate):** this method runs inside `ValuesPopulator`'s catch → `SettingsBindingException` (which retains only binder type/section/key primitives, never the value — see `SettingsBindingException.cs:8-16`). Do not put any element value into an exception message.

---

### `SettingsValidationException.cs` (NEW — VAL-01 aggregate exception)

**Analog:** `SettingsPropertyValueException.cs` (lines 13-33) for the property-carrying shape, and `SettingsBindingException.cs` for the "carries typed context via base ctor + exposes read-only props" pattern. All derive `SimpleSettingsException` (message-only or message+inner base ctors — `SimpleSettingsException.cs:10-18`; no serialization ctor).

**Shape to copy** (from `SettingsPropertyValueException.cs`):
```csharp
public class SettingsPropertyValueException : SimpleSettingsException
{
    public SettingsPropertyValueException(Type settingsType, PropertyInfo property, Type conversionErrorType)
        : base(Resources.PropertySetterExceptionMessage(settingsType, property, conversionErrorType.Name))
    { ... SettingsType = settingsType; ... }
    public Type SettingsType { get; }
}
```
For `SettingsValidationException`: ctor takes `IReadOnlyList<ValidationError>` (from `Validations/ValidationError.cs` — has `SettingsName`, `ErrorMessage`), exposes `Errors`, and builds its message via a NEW `Resources` factory. **Redaction (D-12):** message composed ONLY from author-supplied `ValidationError.ErrorMessage` — add no bound values. Follow the `Resources.cs` static-factory-method convention (lines 35-43) for the message text; mirror the S1 comment style of `SettingsPropertyValueException` explaining why no value is embedded.

---

### `SettingsValidatorAttribute.cs` (NEW — VAL-01 object-level, name = discretion)

**Analog:** `SettingsPropertyAttribute.cs` (whole file):
```csharp
[AttributeUsage(AttributeTargets.Property)]
public class SettingsPropertyAttribute : Attribute
{
    public Type? ConverterType { get; set; }
    public Type? ValidatorType { get; set; }
    public SettingsPropertyAttribute(string name) { Name = name; }
    public SettingsPropertyAttribute() { }
}
```
New attribute uses `[AttributeUsage(AttributeTargets.Interface)]`, carries `Type ValidatorType` (the `ISettingValidation<TSettings>` impl), ctor `SettingsValidatorAttribute(Type validatorType)`. Read at plan build via `GetTypeInfo().GetCustomAttribute<...>(true)` (same idiom as `TypeExtensions.GetSectionName` line 14 and `ValuesPopulator.cs:84`). Instantiate the validator via `Activator.CreateInstance` (parameterless ctor) — same pattern as `TypeConverter.GetConverter` line 43.

---

### `Validations/*` (MODIFY — sync signatures + ctor, VAL-01)

**Interfaces** (`ISettingsValidator.cs:3-6`, `ISettingValidation.cs:3-6`) — drop `Task<>`:
```csharp
public interface ISettingsValidator { ValidationResult Validate(ValidationContext context); }
public interface ISettingValidation<T> : ISettingsValidator { ValidationResult Validate(ValidationContext<T> context); }
```

**Context ctors** — analog is `ValidationError.cs:8-12` (plain ctor-assigns-getter pattern). `ValidationContext.cs:5` and `ValidationContextOfT.cs:5` are getter-only:
```csharp
public class ValidationContext { public object? Settings { get; } }
public class ValidationContext<T> : ValidationContext { public new T? Settings { get; } }
```
Add `ValidationContext(object settings)` and `ValidationContext<T>(T settings)` (the derived ctor calls `base(settings)` and sets the shadowed `T? Settings`). `ValidationResult` (`AddError`/`Errors`/`IsValid`) and `ValidationError` are already complete — do not change.

---

### `ValuesPopulator.cs` — validation hook + filter (MODIFY, VAL-01 + VAL-02)

**Analog:** the method itself. **VAL-01 hook (D-09):** after the `foreach (var propertyPlan in plan.Properties)` loop (`ValuesPopulator.cs:38-64`), once the instance is fully populated: gather core-path validators (object-level `[SettingsValidator]` on the type via `Activator`; property-level `[SettingsProperty(ValidatorType)]` threaded through the `PropertyPlan` built at lines 90-93), run all, aggregate `ValidationError`s, throw ONE `SettingsValidationException`.

**VAL-02 filter** — `ConvertPropertyValue` (lines 114-128), the exception filter at line 122:
```csharp
catch (Exception e) when (e is not SettingsPropertyNullException)
{
    throw new SettingsPropertyValueException(settingsType, propertyPlan.Property, e.GetType());
}
```
If VAL-02 uses a NEW empty-exception type, widen to `when e is not (SettingsPropertyNullException or SettingsPropertyEmptyException)`. Preserve the S1 comment intent (value never chained/embedded).

---

### `Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs` (MODIFY, API-02 + VAL-01 DI)

**Analog:** the method itself (lines 7-44). `IntegrateSimpleSettings` currently returns `void` and builds the collection at line 32:
```csharp
var settingsCollection = settingsBuilder.ScanAssemblies(options!.Assemblies);
foreach (var settings in settingsCollection)
    services.AddSingleton(settings.Key, settings.Value);
services.AddSingleton<ISettingsProvider>(new SettingsProvider(settingsCollection, settingsBuilder));
```
API-02: add `services.AddSingleton<ISettingsCollection>(settingsCollection)` (instance registration; collection is fully built — `ISettingsCollection` public, `SettingsCollection` internal), refactor `IntegrateSimpleSettings` to **return** the `ISettingsCollection`, add a new public entry point that surfaces it (cannot overload by return type — use an `out ISettingsCollection` overload or a distinctly-named method; keep existing `IServiceCollection`-returning overloads at lines 7-19). VAL-01 DI path: DI-resolved `ISettingValidation<T>` cannot run inline (container not built at registration — Pitfall 4); defer to a post-provider-build step, keeping the aggregate + `SettingsValidationException` contract identical to the core path (planner decision, gated by architect + security-auditor per Q3).

---

## Shared Patterns

### Exception family (S1 redaction)
**Source:** `SimpleSettingsException.cs:8-19` (abstract base, two forwarding ctors, no serialization ctor); `SettingsPropertyValueException.cs` (value-free — takes a failure `Type`, never the value/inner); `SettingsBindingException.cs` (primitives only, never `BindingContext`).
**Apply to:** `SettingsValidationException` and any VAL-02 exception — derive `SimpleSettingsException`, embed no bound values, message via a `Resources` factory.
```csharp
public abstract class SimpleSettingsException : Exception
{
    protected SimpleSettingsException(string message) : base(message) { }
    protected SimpleSettingsException(string message, Exception innerException) : base(message, innerException) { }
}
```

### Resource message factories
**Source:** `Resources.cs:35-43` (static methods returning interpolated strings; the value deliberately omitted for S1).
**Apply to:** every new exception message. Add a `SettingsValidationExceptionMessage(...)` (and, if a new VAL-02 type, its message) here rather than inlining strings.

### Attribute read at plan build (no per-populate cost)
**Source:** `ValuesPopulator.cs:84` (`GetCustomAttribute<SettingsPropertyAttribute>(inherit: true)` read once per property into the plan) and `TypeExtensions.cs:14` (`GetTypeInfo().GetCustomAttribute<SettingsSectionAttribute>(true)`).
**Apply to:** reading `[SettingsValidator]` (type-level) and `ValidatorType` (property-level) — resolve at plan build, thread into the `PropertyPlan`/plan, not on the hot populate path.

### Activator-based instantiation
**Source:** `TypeConverter.cs:43` (`(ISettingsTypeConverter)Activator.CreateInstance(attribute.ConverterType)!`).
**Apply to:** core-path validator instantiation (parameterless ctor).

### Perf-conscious iteration
**Source:** `CollectionTypeConverter.cs:53-67` and `TypeConverter.cs:45-56` — manual `foreach` over the concrete `LinkedList` (no LINQ `First`/`Where`, no boxed enumerator/closure) on populate-frequency paths; `static` factory + factory-argument `GetOrAdd` in `ConfigurationBinder.cs:30`.
**Apply to:** any new per-populate loop (validator gathering that runs per populate should prefer this; per-plan-build work may use LINQ).

### TUnit test conventions
**Source:** `Conversion/CollectionConversionTests.cs` (public class, `[Test]` methods `async Task`, `await Assert.That(...).IsTrue()/.IsEqualTo()/.Contains()`, private `Build<T>(...)` helper); `Conversion/ExceptionRedactionTests.cs` (sentinel-secret + `AssertRedacted` helper asserting absence from full `ToString()`); `DependencyInjection/AddSimpleSettingsIntegrationTests.cs` (`ServiceCollection` → `AddSimpleSettings(o => ...)` → `BuildServiceProvider().GetRequiredService<T>()`, in-memory `InMemoryCollection`/`InMemoryBinder` fakes).
**Apply to:** all Wave 0 test files. Method naming `Method_State_Expected`. `--treenode-filter` (NOT `--filter`); build/test from `src/`. Co-locate new fixture interfaces (`[SettingsValidator]` / `ValidatorType` / list-shaped + sequence-backed) per existing convention.
```csharp
[Test]
public async Task Convert_ToIntEnumerable_MaterializesAnArray()   // pinned — keep green (COLL-01 regression guard)
{
    var result = Build<IIntEnumerable>("IntEnumerable", nameof(IIntEnumerable.Values), "5,6,7");
    await Assert.That(result.Values is int[]).IsTrue();
}
```

## No Analog Found

None. Every file maps to an in-repo analog or is a self-modification. The only genuinely novel design decision — DI-resolved validator timing (Q3) — is an architecture choice, not a missing pattern; the aggregate-exception + redaction contract it must satisfy is fully modeled by the exception-family analogs above.

## Metadata

**Analog search scope:** `src/Core/ExistForAll.SimpleSettings/` (Conversion, Core/Reflection, Validations, root exceptions/attributes), `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/`, `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/`, `src/Tests/ExistForAll.SimpleSettings.UnitTests/`.
**Files scanned:** 21 source + 3 test files read at file:line.
**Project conventions source:** no `CLAUDE.md` / `.claude/skills` in-repo (research cites external dotnet-kit `modern-csharp` + `testing` skills); conventions inferred from read source + CONTEXT §Established Patterns.
**Pattern extraction date:** 2026-07-15
</content>
</invoke>
