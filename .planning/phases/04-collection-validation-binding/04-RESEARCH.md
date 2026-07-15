# Phase 4: Collection & Validation Binding - Research

**Researched:** 2026-07-15
**Domain:** SimpleSettings binding engine (internal C# library) — collection conversion, config-sequence binding, settings validation, DI surface
**Confidence:** HIGH (codebase-internal; every claim below is grounded in read source with file:line)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Land **full `List<T>` family support** this phase. One converter materializes a `List<T>` and satisfies `List<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`; arrays stay on `ArrayTypeConverter`. Broaden `TypeExtensions.IsEnumerable` (`Core/Reflection/TypeExtensions.cs:21-26`) and element-type detection. Reuse the existing `CollectionTypeConverter` array-build path (`Conversion/CollectionTypeConverter.cs`), then wrap into a `List<T>`.
- **D-02:** Extend `TypeConverter.CreateNullResult` (`Core/Reflection/TypeConverter.cs:27-36`) so an unset `T[]`, `List<T>` family, and `IEnumerable<T>` all bind an **empty collection, never `null`**. Materialize `Array.CreateInstance(elementType, 0)` for arrays and a new empty `List<T>` for the list shapes.
- **D-03:** Sequencing — **COLL-02 before COLL-03** (COLL-03's empty behavior leans on COLL-02's empty-not-null default).
- **D-04:** `ConfigurationBinder.BindPropertySettings` (`Extensions.Binders/ConfigurationBinder.cs:26-36`) reads a single scalar today. For collection-typed targets, enumerate `GetChildren()` and set the sequence (as an array / `string[]`) onto `context`. `CollectionTypeConverter.AsArray` (`:43-51`) passes an incoming `Array` straight through.
- **D-05:** Locked sub-rules: **children win** over scalar; **comma-scalar MUST still bind** (prod `MultiHost__CommonHosts`); empty / whitespace / empty-sequence → **empty, never `[""]`**; each element flows the inner converter chain; honor `[SettingsSection]` / root prefixing.
- **D-06 (SECURITY GATE):** COLL-03 edits run inside `ValuesPopulator`'s wrapping catch (`ValuesPopulator.cs:50-60`). **S1/SEC-01 secret-redaction MUST be re-verified** with a regression test proving a secret in a YAML sequence element is absent from the whole `ex.ToString()` chain on a bind/convert failure. `security-auditor` owns this.
- **D-07:** Redesign the held validator interfaces to **synchronous** — `ISettingsValidator.Validate` / `ISettingValidation<T>.Validate` return `ValidationResult` (drop `Task<>`).
- **D-08:** Complete the held scaffolding — `ValidationContext` / `ValidationContext<T>` need a constructor so the populated `Settings` can be carried.
- **D-09:** Timing — validation runs at build/populate (startup), hooked after the property-set loop in `ValuesPopulator.PopulateInstanceWithValues` (`ValuesPopulator.cs:29-65`).
- **D-10:** Failure — aggregate, throw once. New `SettingsValidationException : SimpleSettingsException` carrying the full `ValidationError` list.
- **D-11:** Discovery — **BOTH** mechanisms. *Core path (always):* property-level `[SettingsProperty(ValidatorType=typeof(X))]` **and** an object-level interface attribute (e.g. `[SettingsValidator(typeof(X))]`), instantiated via `Activator` (parameterless ctor). *DI path (`AddSimpleSettings`):* also resolve registered `ISettingValidation<T>` from the container.
- **D-12:** Redaction — `SettingsValidationException` adds no bound values itself; `ValidationError` messages are validator-author-supplied (author owns not echoing secrets; document this). Re-verify no leak.
- **D-13:** `[SettingsProperty(AllowEmpty=false)]` rejects **null + `""` + whitespace** (`string.IsNullOrWhiteSpace`). `${ENV:-}` detection **deferred**.
- **D-14:** Inline at the conversion layer — extend `PropertyConversion.Convert`'s `_throwOnNull` branch (`Conversion/PropertyConversion.cs:29-40`). Throw a **value-free** exception in the `SimpleSettingsException` family; exact type is the planner's call. NOTE: client doc's `TypeConverter.ValidateNullAcceptance` pointer is **stale** — `PropertyConversion` is the real hook.
- **D-15:** Implement **both** API-02 exposure mechanisms: (1) `services.AddSingleton<ISettingsCollection>(settingsCollection)`; (2) an `AddSimpleSettings` variant that returns `ISettingsCollection`. Keep the existing `IServiceCollection`-returning overloads. `ISettingsCollection` public; `SettingsCollection` internal.

### Claude's Discretion
- Exact new type/method names (`SettingsValidationException`, `[SettingsValidator]`, the VAL-02 exception type, the API-02 return-overload signature), test file placement under `src/Tests/ExistForAll.SimpleSettings.UnitTests/`, and the precise `List<T>`-materialization mechanism (wrap the built array vs. build the list directly). All breaking/behavior changes are expected and batched pre-beta.

### Deferred Ideas (OUT OF SCOPE)
- `${ENV:-}` unsubstituted-placeholder detection (VAL-02) — deferred.
- AOT-01 / DOC-01 — Phase 5. REL-01 — Phase 6.
- D2 `EqualityCompererCreator` (EQ-01) — HELD, out of scope.
- `IOptionsMonitor`-style reload.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| COLL-02 | Unset `T[]`/`List<T>` bind an empty collection, never `null` | `CreateNullResult` shape detection + empty-materialization; §Standard Stack / Pattern 1 |
| COLL-03 | Bind collections from config child-section sequences via `GetChildren()`; comma-scalar still binds; children win; empty→empty; elements flow the converter chain; re-verify redaction | `ConfigurationBinder.BindPropertySettings` sequence read + `AsArray` passthrough; §Pattern 2; §Security Domain |
| VAL-01 | Wire `ISettingValidation<T>` + `[SettingsProperty(ValidatorType)]` into the populate path incl. cross-property rules; BOTH discovery mechanisms; aggregate-throw | Sync interface redesign + `ValidationContext` ctor + post-populate hook + `SettingsValidationException`; §Pattern 3; §Open Questions (DI timing) |
| VAL-02 | `[SettingsProperty(AllowEmpty=false)]` rejects `""`/whitespace at bind, value-free | `PropertyConversion.Convert` `_throwOnNull` branch extension + exception-filter update; §Pattern 4 |
| API-02 | `AddSimpleSettings(...)` exposes `ISettingsCollection` (DI singleton + return-value) | `IntegrateSimpleSettings` refactor; §Pattern 5; §Open Questions (overload shape) |
</phase_requirements>

## Summary

This is an entirely codebase-internal phase: no new external dependencies, no new NuGet packages. Every capability slots into an existing, well-factored seam — the converter chain (`TypeConvertersCollections`), the per-type `SettingsPlan` cache, the `ISectionBinder` chain, and the `AddSimpleSettings` DI extension. The engineering risk is not "how does technology X work" but "how do these five edits interact with the load-bearing invariants already in place": converter ordering, last-writer-wins binder precedence, the S1 secret-redaction contract, and the per-type plan cache.

The five changes decompose cleanly. **COLL-01/02** broaden collection-shape detection in one reflection helper and add a `List<T>`-materializing converter that reuses the shared array-build path. **COLL-03** adds a `GetChildren()` branch to one binder method; because `CollectionTypeConverter.AsArray` already passes an `Array` straight through the element-converter chain, the binder only has to produce a `string[]` — no converter change. **VAL-01** completes dead scaffolding (sync signature + a constructor) and adds one post-populate hook plus a new aggregate exception; its only genuine unknown is DI-resolved-validator timing (settings are built at *registration*, before the container exists). **VAL-02** extends one `if` branch and — critically — must be reflected in the redaction exception-filter. **API-02** is a small refactor of `IntegrateSimpleSettings` plus new overloads, with one real C# constraint (you cannot overload by return type alone).

**Primary recommendation:** Implement in the locked order (COLL-02 → COLL-03 → VAL-01 → VAL-02 → API-02). Keep `IsEnumerable` narrow and introduce a *separate* list-shape predicate + `ListTypeConverter` (broadening `IsEnumerable` in place would make `EnumerableTypeConverter` claim `List<T>` and return an array, which cannot be assigned to a `List<T>` property). Treat DI-path validator timing as an explicit planner decision gated by the architect + security-auditor.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| `List<T>` family conversion (COLL-01) | Core library — `Conversion/*TypeConverter` + `TypeConvertersCollections` | Core — `Reflection/TypeExtensions` (shape detection) | Conversion is owned by the converter chain; shape detection is a reflection concern shared with the null-result path |
| Empty-not-null default (COLL-02) | Core library — `Reflection/TypeConverter.CreateNullResult` | — | The null/empty outcome is resolved once at plan build; it is a conversion-plan concern, not a binder concern |
| Config child-sequence read (COLL-03) | Binders extension — `Extensions.Binders/ConfigurationBinder` | Core — `CollectionTypeConverter.AsArray` (passthrough) | Reading `IConfiguration.GetChildren()` is a config-provider concern owned by the binder; the converter just consumes the produced `string[]` |
| Settings validation execution (VAL-01, core path) | Core library — `ValuesPopulator.PopulateInstanceWithValues` | Core — `Validations/*`, `Activator` | Validation runs once the instance is fully populated; the populate loop is the only place with the finished instance |
| Settings validation (VAL-01, DI-resolved) | GenericHost extension — `ServicesSettingsBuilderExtensions` | Core — shared aggregate/exception contract | Only the DI extension has a container; service-dependent validators must resolve there |
| Empty-string rejection (VAL-02) | Core library — `Conversion/PropertyConversion.Convert` | Core — exception-filter in `ValuesPopulator.ConvertPropertyValue` | Empty rejection is a per-property binding-layer concern resolved at plan build (`throwOnNull`), not a VAL-01 validator |
| `ISettingsCollection` exposure (API-02) | GenericHost extension — `ServicesSettingsBuilderExtensions` | Core — `ISettingsCollection` (already public) | The collection is built in the DI integration path; exposure is a DI-surface concern |

## Standard Stack

No new libraries. This phase uses only what is already referenced and pinned.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET BCL (`System.*`) | net8.0 / net10.0 | `List<T>`, `Array.CreateInstance`, reflection, `Activator` | Target runtimes; no dependency added `[VERIFIED: dotnet --list-sdks → 10.0.301]` |
| `Microsoft.Extensions.Configuration` | 8.0.0 (net8) / 10.0.9 (net10) | `IConfigurationSection.GetChildren()` for COLL-03 | Already referenced by the Binders package `[VERIFIED: src/Directory.Packages.props]` |
| `Microsoft.Extensions.DependencyInjection(.Abstractions)` | 8.0.x / 10.0.9 | `AddSingleton<ISettingsCollection>`, DI-resolved validators (API-02 / VAL-01 DI path) | Already referenced by the GenericHost package `[VERIFIED: src/Directory.Packages.props]` |

### Supporting (test only)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| TUnit | 1.58.0 | Test framework on Microsoft.Testing.Platform | All new tests `[VERIFIED: src/Directory.Packages.props + global.json runner]` |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Reusing `CollectionTypeConverter` array path + wrap to `List<T>` | Build `List<T>` directly element-by-element in a bespoke converter | Direct build avoids one array→list copy but duplicates the split/convert/passthrough logic already de-reflected in P4; wrap reuses the shared path (D-01 favors reuse). See Open Questions Q1 |
| `AddSingleton<ISettingsCollection>` instance registration | Factory registration `AddSingleton<ISettingsCollection>(sp => ...)` | Instance registration is correct here — the collection is fully built at registration time; a factory adds nothing |

**Installation:** None. `dotnet restore` uses the existing central package versions.

## Package Legitimacy Audit

Not applicable — this phase installs **no external packages**. All dependencies (`Microsoft.Extensions.*`, TUnit) are already present and pinned via central package management in `src/Directory.Packages.props`. No `npm`/`pip`/`cargo` and no new `PackageVersion` entries are introduced.

## Architecture Patterns

### System Architecture Diagram

```
                        SettingsBuilder.CreateBuilder / AddSimpleSettings
                                        │
                                        ▼
                          ScanAssemblies / GetSettings(type)
                                        │
                                        ▼
                     ValuesPopulator.PopulateInstanceWithValues
                                        │
             ┌──────────────────────────┼───────────────────────────┐
             ▼                          ▼                            ▼
   GetOrBuildPlan (cached)   per-property binder loop        [VAL-01 NEW hook]
   • key / default           (last-writer-wins)              run validators AFTER
   • PropertyConversion         │                            the property loop
     - throwOnNull  ◄─ VAL-02    ▼                              │
     - nullResult   ◄─ COLL-02  ISectionBinder chain            ▼
                                 • ConfigurationBinder      aggregate ValidationError[]
                                   ├─ scalar section[key]   throw SettingsValidationException
                                   └─ [COLL-03 NEW]              (core: Activator;
                                      GetSection(key)             DI: resolved ISettingValidation<T>)
                                      .GetChildren() → string[]
                                        │
                                        ▼
                                 tempValue (string | string[])
                                        │
                                        ▼
                          PropertyConversion.Convert(value)
                            • null/empty → nullResult OR throw (VAL-02)
                            • else → converter.Convert
                                        │
              ┌─────────────────────────┼──────────────────────────┐
              ▼                          ▼                          ▼
        ArrayTypeConverter      [ListTypeConverter NEW]     EnumerableTypeConverter
        (IsArray)               (List<T>/IList<T>/...)      (open IEnumerable<>)
              └──────────── CollectionTypeConverter.AsArray + element chain ───────┘
                                        │
                                        ▼
                    redaction boundary: convert failure →
                    SettingsPropertyValueException (value-free)  [S1 / COLL-03 gate]

  API-02: IntegrateSimpleSettings also does AddSingleton<ISettingsCollection>(collection)
          and returns the collection through a new AddSimpleSettings overload shape.
```

### Recommended Project Structure

No new folders. Files land in existing locations:
```
src/Core/ExistForAll.SimpleSettings/
├── Core/Reflection/TypeExtensions.cs        # broaden shape detection (COLL-01/02)
├── Core/Reflection/TypeConverter.cs          # CreateNullResult empty-not-null (COLL-02)
├── Conversion/ListTypeConverter.cs           # NEW converter (COLL-01)
├── Conversion/TypeConvertersCollections.cs   # register ListTypeConverter (order matters)
├── Conversion/PropertyConversion.cs          # empty rejection (VAL-02)
├── ValuesPopulator.cs                         # validation hook + exception-filter update
├── Validations/*.cs                           # sync signatures + ctor (VAL-01)
├── SettingsValidationException.cs            # NEW aggregate exception (VAL-01)
├── SettingsValidatorAttribute.cs             # NEW object-level attribute (VAL-01) [name = discretion]
└── (VAL-02 exception: reuse SettingsPropertyNullException OR new sibling)
src/Core/ExistForAll.SimpleSettings.Extensions.Binders/
└── ConfigurationBinder.cs                     # GetChildren() sequence read (COLL-03)
src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/
└── ServicesSettingsBuilderExtensions.cs       # ISettingsCollection exposure + DI validators (API-02/VAL-01)
src/Tests/ExistForAll.SimpleSettings.UnitTests/  # extend Conversion/, Core/, DependencyInjection/, SimpleSettings/
```

### Pattern 1: `List<T>` family — separate predicate + wrap the built array (COLL-01/02)

**What:** A new `ListTypeConverter : CollectionTypeConverter` that reuses the base array-build path (`CollectionTypeConverter.Convert` → `Array.CreateInstance` + element convert) and wraps the result into a `List<T>`. Shape detection lives in a *new* predicate, not by widening `IsEnumerable`.

**When to use:** Target property is `List<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, or `IReadOnlyCollection<T>`.

**Why a separate predicate (critical):** `IsEnumerable` (`TypeExtensions.cs:21-26`) is consumed in **two** places — `EnumerableTypeConverter.CanConvert` (`EnumerableTypeConverter.cs:13`) and `TypeConverter.CreateNullResult` (`TypeConverter.cs:29`). If you widen `IsEnumerable` to also match the list shapes, `EnumerableTypeConverter` will claim `List<T>` and return a `T[]`; `T[]` is **not** assignable to a `List<T>` property, so `PropertyInfo.SetValue` throws. The pinned test `Convert_ToIntEnumerable_MaterializesAnArray` (`CollectionConversionTests.cs:38-46`) also requires `IEnumerable<T>` to keep returning an array. `[VERIFIED: codebase grep — IsEnumerable used at TypeExtensions.cs:21, EnumerableTypeConverter.cs:13, TypeConverter.cs:29]`

**Converter ordering:** current chain (`TypeConvertersCollections.cs:7-12`) is `DateTime, Uri, Array, Enumerable, Enum, Default`. Insert `ListTypeConverter` so its predicate is **disjoint** from `ArrayTypeConverter` (`IsArray`) and `EnumerableTypeConverter` (`IsEnumerable`, i.e. exactly the open `IEnumerable<>`). A `List<int>` is neither `IsArray` nor the `IEnumerable<>` definition, so it currently falls through to `DefaultTypeConverter` and throws via `Convert.ChangeType`. Placement after `EnumerableTypeConverter` and before `EnumTypeConverter`/`DefaultTypeConverter` is safe because the predicates do not overlap. **Do not reorder** the existing converters (load-bearing per CONVENTIONS / CONTEXT). `[VERIFIED: TypeConvertersCollections.cs:7-12; ArrayTypeConverter.cs:10-13; EnumerableTypeConverter.cs:13-15]`

**Cheapest materialization:** `new List<T>(array)` where `array` is the base-built `Array`. **Correction (review S-4/A1):** `new List<T>(array)` does NOT alias the array — because the built `T[]` is an `ICollection<T>`, the `List<T>(ICollection<T>)` constructor allocates a fresh right-sized backing buffer and `CopyTo`s the elements into it, so the wrap transiently holds two n-sized buffers (an element copy, not a cheap "reference-copy" as previously stated). This satisfies all five shapes because `List<T>` implements `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`. **Do NOT materialize via `Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType), builtArray)` on the per-populate path** — that reintroduces the reflection idiom P4 deleted plus transient `Type[1]`/`object[1]`/`Invoke`. Use a cached per-element-type `Func<Array, object>` factory (built once via a generic helper doing `new List<TElement>(array)`), so per-populate materialization is a plain delegate call. `CreateNullResult`'s cold plan-build reflection stays as-is (the empty-`List<T>` null-result must additionally be produced fresh per bind — a shared mutable empty list is a correctness bug, review B-3). The array build is unchanged, so the array/enumerable hot paths (benchmark-gated) are untouched; the list wrap only runs for previously-throwing list targets. `[ASSUMED — .NET BCL semantics; verify no benchmark regression per PERF-01/PERF-02 gate]`

**Example (illustrative shape only — planner owns final form):**
```csharp
// Source: derived from CollectionTypeConverter.cs:25-38 (existing array-build path)
internal class ListTypeConverter : CollectionTypeConverter
{
    public ListTypeConverter(SettingsOptions settingsOptions, TypeConvertersCollections converters)
        : base(settingsOptions, converters) { }

    public override bool CanConvert(Type settingsType) => settingsType.IsListLike();

    protected override Type GetElementType(Type settingsType)
        => settingsType.GetTypeInfo().GetGenericArguments()[0];

    // wrap the base-built typed array into a List<T>
    public override object Convert(object value, Type settingsType)
    {
        var built = (Array)base.Convert(value, settingsType);
        var elementType = GetElementType(settingsType);
        var listType = typeof(List<>).MakeGenericType(elementType);
        return Activator.CreateInstance(listType, built)!;
    }
}
```
Note: `CollectionTypeConverter.Convert` is currently a non-virtual public method (`CollectionTypeConverter.cs:25`). To override it, the planner makes it `virtual` (or extracts the array-build into a protected helper the list converter calls). Either is a mechanical, non-breaking internal change.

**COLL-02 empty-not-null:** `CreateNullResult` (`TypeConverter.cs:27-36`) must branch on shape:
- array target → `Array.CreateInstance(elementType, 0)` (element type via `GetElementType()`),
- list family → empty `List<T>`,
- open `IEnumerable<T>` → `Array.CreateInstance(elementType, 0)` (unchanged),
- else → value-type default / null (unchanged).

Today arrays hit the `!IsEnumerable()` branch and return `null` because an array is a reference type — so unbound `int[]` currently binds `null`, which COLL-02 fixes. `[VERIFIED: TypeConverter.cs:27-36 — array is reference type, IsValueType false → returns null]`

### Pattern 2: Config child-sequence binding (COLL-03)

**What:** In `ConfigurationBinder.BindPropertySettings` (`ConfigurationBinder.cs:26-36`), when the target property is collection-typed, read `section.GetSection(context.Key).GetChildren()` and, if any children exist, set a `string[]` of their `.Value` onto the context (children win over the scalar). Otherwise fall back to the existing scalar `section[context.Key]`.

**When to use:** `context.PropertyType` is array / list-family / `IEnumerable<T>` (same shape predicate as Pattern 1). Reuse the shape helper so scalars are untouched.

**Why it flows unchanged:** `CollectionTypeConverter.AsArray` (`CollectionTypeConverter.cs:43-51`) already does `value as Array ?? new[]{value}` — an incoming `string[]` passes straight through, then each element goes through `GetElementConverter` (int/enum/DateTime/Uri all keep working). So the binder only needs to produce a `string[]`; **no converter change** for COLL-03. `[VERIFIED: CollectionTypeConverter.cs:43-51]`

**Locked sub-rules mapped to mechanism (D-05):**
- **Children win:** check `GetChildren()` first; only fall back to `section[key]` when there are no children.
- **Comma-scalar still binds:** when no children, `section[context.Key]` returns `"a,b,c"` exactly as today → `AsArray` splits on `ArraySplitDelimiter` (`SettingsOptions.cs:13`, default `","`). Prod `MultiHost__CommonHosts` env override unaffected. `[VERIFIED: SettingsOptions.cs:13; CollectionTypeConverter.cs:45-48]`
- **Empty / whitespace / empty-sequence → empty, never `[""]`:** if `GetChildren()` is empty AND the scalar is `null`/whitespace, **do not** call `SetNewValue`; `tempValue` stays at the default and COLL-02's empty-not-null `nullResult` produces the empty collection. This avoids the `AsArray` split of a whitespace string yielding a single non-empty `[" "]` element (`Split(..., RemoveEmptyEntries)` does not drop a whitespace-only token). Guard with `string.IsNullOrWhiteSpace`. `[VERIFIED: CollectionTypeConverter.cs:47 uses RemoveEmptyEntries but whitespace token survives]`
- **Root / `[SettingsSection]` prefixing honored:** the section is resolved through the existing `_sections.GetOrAdd(...ResolveSection...)` path (`ConfigurationBinder.cs:30,38-41`), which already applies `RootSection`. `GetSection(key).GetChildren()` is a child of that resolved section, so prefixing is inherited for free. `[VERIFIED: ConfigurationBinder.cs:38-41]`

**Example (illustrative):**
```csharp
// Source: derived from ConfigurationBinder.cs:26-36 (existing scalar read)
public void BindPropertySettings(BindingContext context)
{
    var section = _sections.GetOrAdd(context.Section,
        static (name, self) => self.ResolveSection(name), this);

    if (context.PropertyType.IsCollectionShape())
    {
        var children = section.GetSection(context.Key).GetChildren();
        var values = children.Select(c => c.Value).Where(v => v != null).ToArray();
        if (values.Length > 0) { context.SetNewValue(values); return; }
        // fall through to scalar; if scalar is null/whitespace, do NOT set (COLL-02 empty result)
    }

    var value = section[context.Key];
    if (context.PropertyType.IsCollectionShape() && string.IsNullOrWhiteSpace(value)) return;
    if (value != null) context.SetNewValue(value);
}
```
`context.PropertyType` is available on `BindingContext` (`BindingContext.cs:13,35`). `[VERIFIED: BindingContext.cs:13,35]`

### Pattern 3: Synchronous validation pipeline (VAL-01)

**What:** Complete the held `Validations/*` scaffolding and run validators after the populate loop.

**Interface redesign (D-07):**
```csharp
// ISettingsValidator.cs / ISettingValidation.cs — drop Task<>
public interface ISettingsValidator { ValidationResult Validate(ValidationContext context); }
public interface ISettingValidation<T> : ISettingsValidator { ValidationResult Validate(ValidationContext<T> context); }
```
Current signatures return `Task<ValidationResult>` and are never invoked (`ISettingValidation.cs:5`, `ISettingsValidator.cs:5`) — dead scaffolding, so the signature change is free pre-beta. `[VERIFIED: ISettingValidation.cs:3-6, ISettingsValidator.cs:3-6]`

**Context ctor (D-08):** `ValidationContext.Settings` is getter-only with no ctor (`ValidationContext.cs:5`); `ValidationContext<T>` shadows it with `new T? Settings` (`ValidationContextOfT.cs:5`) — neither can carry a value. Add constructors so the populated instance is passed in (e.g. `ValidationContext(object settings)` and `ValidationContext<T>(T settings)`). `ValidationResult` / `ValidationError` are already complete (`ValidationResult.cs`, `ValidationError.cs`). `[VERIFIED: ValidationContext.cs:1-7, ValidationContextOfT.cs:1-6, ValidationResult.cs, ValidationError.cs]`

**Hook point (D-09):** after the `foreach (var propertyPlan in plan.Properties)` loop in `ValuesPopulator.PopulateInstanceWithValues` (`ValuesPopulator.cs:38-64`), i.e. once every property is set (cross-property rules need the full instance). Collect validators, run all, aggregate errors, throw once. `[VERIFIED: ValuesPopulator.cs:29-65]`

**Discovery — core path (always, via `Activator`, D-11):**
- **Object-level:** a NEW attribute (e.g. `[SettingsValidator(typeof(X))]`, `AttributeTargets.Interface`) on the settings interface; `X` implements `ISettingValidation<TSettings>`; instantiate via `Activator.CreateInstance` (parameterless ctor); pass `new ValidationContext<TSettings>(instance)`.
- **Property-level:** `[SettingsProperty(ValidatorType=typeof(X))]` (`SettingsPropertyAttribute.cs:14`) — runs on **that property's value**. This is a real contract ambiguity: the held interfaces are typed to the *settings* type, not the property value. See Open Questions Q2 — the planner must define whether a property-level validator implements `ISettingValidation<TProperty>` (receiving the property value) or validates the settings object. Both attribute hooks already have a natural read point: the object-level attribute at the type, the property-level attribute is already read per property at plan build (`ValuesPopulator.cs:84`) and can be threaded into the `PropertyPlan`.

**Discovery — DI path (`AddSimpleSettings`, D-11):** additionally resolve registered `ISettingValidation<T>` from the container. **This is the phase's one genuine unknown — see Open Questions Q3.**

**Aggregate exception (D-10):** new `SettingsValidationException : SimpleSettingsException` carrying the full `IReadOnlyList<ValidationError>`. Follow the existing base-ctor pattern (`SimpleSettingsException.cs:10-18` — message + optional inner; no serialization ctor). Message built via a `Resources` factory (`Resources.cs` convention). `[VERIFIED: SimpleSettingsException.cs:8-19; Resources.cs pattern]`

**Redaction (D-12):** `SettingsValidationException` composes its message only from author-supplied `ValidationError.ErrorMessage`; it adds no bound values. Document that validator authors own not echoing secrets. A regression test asserts the aggregate `ToString()` contains only the author messages (no injected values).

### Pattern 4: Empty-string rejection at conversion (VAL-02)

**What:** Extend `PropertyConversion.Convert` (`PropertyConversion.cs:29-40`). Today it throws `SettingsPropertyNullException` only when `value == null` and `_throwOnNull`. Add: when `_throwOnNull` and the value is a null-or-whitespace string, throw the same value-free exception before converting.

```csharp
// Source: PropertyConversion.cs:29-40 (extended)
public object? Convert(object? value)
{
    if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
    {
        if (_throwOnNull) throw new SettingsPropertyNullException(_propertyName); // or new empty-specific sibling
        if (value is null) return _nullResult;
        // whitespace string with AllowEmpty=true: fall through to normal conversion
    }
    return _converter.Convert(value, _strippedType);
}
```
`_throwOnNull` is resolved at plan build from `AllowEmpty` (`TypeConverter.cs:16` — `attribute is { AllowEmpty: false }`). `[VERIFIED: PropertyConversion.cs:29-40; TypeConverter.cs:16]`

**CRITICAL integration detail — the redaction exception-filter:** `ValuesPopulator.ConvertPropertyValue` wraps conversion failures in the redacting `SettingsPropertyValueException`, **except** `SettingsPropertyNullException`, via the filter `catch (Exception e) when (e is not SettingsPropertyNullException)` (`ValuesPopulator.cs:122`). If VAL-02 reuses `SettingsPropertyNullException`, it propagates cleanly. **If the planner introduces a new `SettingsPropertyEmptyException`, the filter at `ValuesPopulator.cs:122` MUST be widened** to `when e is not (SettingsPropertyNullException or SettingsPropertyEmptyException)` — otherwise the clear empty-value message gets redacted into a generic conversion error. An empty/whitespace value is not itself sensitive, but keep the exception value-free regardless. `[VERIFIED: ValuesPopulator.cs:114-128]`

**Naming note:** `_throwOnNull` becomes a slight misnomer (now also throws-on-empty). Renaming to e.g. `_rejectEmpty` is optional cleanup (internal struct field; no public surface). `[VERIFIED: PropertyConversion.cs:12]`

### Pattern 5: Expose `ISettingsCollection` (API-02)

**What:** In `IntegrateSimpleSettings` (`ServicesSettingsBuilderExtensions.cs:21-44`), add `services.AddSingleton<ISettingsCollection>(settingsCollection)` (the collection already exists at `:32`), and add a public entry point that returns the collection.

**Real C# constraint:** you **cannot** overload `AddSimpleSettings` by return type alone. The two existing overloads differ by parameters (`(services)` vs `(services, action)`) and both return `IServiceCollection` (`:7-19`). A return-`ISettingsCollection` variant needs a distinct signature. Options (planner picks, D-15 says decide on-branch):
1. **`out` parameter overload** — `AddSimpleSettings(this IServiceCollection services, out ISettingsCollection settings, Action<...>? action = null)` returning `IServiceCollection` (keeps chaining).
2. **Distinctly-named method** — e.g. `AddSimpleSettingsCollection(...)` returning `ISettingsCollection`.
Refactor `IntegrateSimpleSettings` to **return** the built `ISettingsCollection` (currently `void`, `:21`) so both public shapes can choose what to surface. Keep the existing `IServiceCollection`-returning overloads intact for the fluent chain. `[VERIFIED: ServicesSettingsBuilderExtensions.cs:7-44]`

`ISettingsCollection` is already `public` (`ISettingsCollection.cs:3`); `SettingsCollection` is `internal` (`SettingsCollection.cs:6`) and stays internal — DI registration uses the interface, so no visibility change needed. `[VERIFIED: ISettingsCollection.cs:3, SettingsCollection.cs:6]`

### Anti-Patterns to Avoid
- **Widening `IsEnumerable` in place** to cover `List<T>` — breaks `EnumerableTypeConverter` (returns array, unassignable to `List<T>`) and the pinned enumerable-returns-array test. Use a separate predicate.
- **Reordering the converter chain** — user-first / `DefaultTypeConverter`-last ordering is load-bearing (CONTEXT §Established Patterns). Insert the new converter with a disjoint predicate; never move existing entries.
- **Running DI-resolved validators inside `PopulateInstanceWithValues` during `ScanAssemblies`** — the container does not exist yet at registration time (see Q3).
- **Letting a new VAL-02 exception fall through the redaction filter unlisted** — silently redacts the empty-value diagnostic.
- **Putting any bound value into `SettingsValidationException` or a COLL-03 exception message** — violates S1.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Split delimited scalar → typed array | A new split/convert loop | Existing `CollectionTypeConverter` array-build path (`:25-38`) | Already de-reflected + benchmark-tuned in P4; reuse via the `List<T>` wrap |
| Read config sequence | Manual index probing (`section[key:0]`, `key:1`...) | `IConfigurationSection.GetChildren()` | Standard config API; handles arbitrary length + provider layering |
| Empty collection instance | `new T[0]` literals scattered | `Array.CreateInstance(elementType, 0)` (already the P4 pattern in `CreateNullResult`) | One materialization site, element-type driven |
| Aggregate multiple validation failures | Ad-hoc `List<Exception>` | `ValidationResult` (already has `AddError`/`Errors`/`IsValid`) + one `SettingsValidationException` | Scaffolding is complete; only interface async→sync + a ctor remain |
| Catchable error family | `throw new Exception(...)` | `SimpleSettingsException` subtype | Enforced by the existing hierarchy invariant test (`ExceptionHierarchyTests.cs`) |

**Key insight:** Nearly every "new" capability is a small edit at an existing seam plus reuse of a P4-optimized path. The failure mode in this phase is not missing infrastructure but breaking one of the four invariants (converter order, binder precedence, S1 redaction, plan cache) while wiring the edits.

## Runtime State Inventory

Not a rename/refactor/migration phase — greenfield feature edits to a library with no stored/registered runtime state. Omitted per the section trigger.

## Common Pitfalls

### Pitfall 1: `List<T>` claimed by the wrong converter
**What goes wrong:** After adding list support, `List<int>` binds to a `T[]` and `SetValue` throws, or `IEnumerable<int>` starts returning a `List<T>` and breaks the pinned test.
**Why it happens:** Widening the shared `IsEnumerable` predicate instead of adding a disjoint one; or placing `ListTypeConverter` where its predicate overlaps `EnumerableTypeConverter`.
**How to avoid:** Separate `IsListLike` predicate; keep `IsEnumerable` matching only the open `IEnumerable<>`; verify the three collection tests plus a new `IEnumerable`-still-returns-array assertion.
**Warning signs:** `ArgumentException: object of type 'T[]' cannot be converted to type 'List`1'` at `PropertyInfo.SetValue`.

### Pitfall 2: Whitespace scalar produces `[" "]` instead of empty
**What goes wrong:** A collection property bound to a whitespace-only scalar yields a one-element collection, violating D-05 "whitespace → empty".
**Why it happens:** `AsArray` splits with `RemoveEmptyEntries`, which drops empty tokens but keeps whitespace tokens.
**How to avoid:** Guard `string.IsNullOrWhiteSpace(value)` in the binder for collection targets and skip `SetNewValue`, letting COLL-02's empty `nullResult` apply.
**Warning signs:** A collection with `Count == 1` and a blank element in the empty-input test.

### Pitfall 3: VAL-02 exception silently redacted
**What goes wrong:** A dedicated empty-value exception is caught by the redaction wrapper and re-thrown as a generic `SettingsPropertyValueException`, losing the "empty not allowed" diagnostic.
**Why it happens:** The exception-filter at `ValuesPopulator.cs:122` only excludes `SettingsPropertyNullException`.
**How to avoid:** Either reuse `SettingsPropertyNullException` (already excluded) or widen the filter to include the new type.
**Warning signs:** Test expecting the empty-specific exception type catches `SettingsPropertyValueException` instead.

### Pitfall 4: DI validators run before their dependencies are registered
**What goes wrong:** A DI-resolved `ISettingValidation<T>` that injects an app service throws or resolves a half-built graph, because `AddSimpleSettings` builds and validates settings at *registration* time.
**Why it happens:** `IntegrateSimpleSettings` calls `ScanAssemblies` → `PopulateInstanceWithValues` eagerly at `:32`, before `BuildServiceProvider` and before the consumer finishes registering services.
**How to avoid:** Defer DI-resolved validators to a post-provider-build step (see Q3). Core-path (`Activator`, parameterless) validators are safe to run inline.
**Warning signs:** `InvalidOperationException: no service for type ...` during `AddSimpleSettings`, or validators that never fire.

### Pitfall 5: Benchmark regression on the collection hot path
**What goes wrong:** The `List<T>` wrap adds an allocation that trips the BenchmarkDotNet allocation gate (PERF-02).
**Why it happens:** `new List<T>(array)` copies the backing store.
**How to avoid:** Keep array/`IEnumerable<T>` paths byte-for-byte unchanged (the wrap only runs for list-shaped targets, which previously *threw*). Confirm the benchmark job stays green; a net-new allocation for a previously-failing path is acceptable, a regression on existing paths is not.
**Warning signs:** `benchmark.yml` flags allocated-bytes increase on `ValuesPopulator`/array converter benchmarks.

## Code Examples

All illustrative examples are inlined in Patterns 1–5 above, each cited to the exact source lines they derive from. No external documentation examples are needed for this phase.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `Enumerable.Empty<T>()` via reflected `GetMethod("Empty")` | `Array.CreateInstance(elementType, 0)` | P4 | COLL-02 follows the same pattern for arrays/lists |
| Per-converter `List<T>` + reflected `ToArray` | Shared `CollectionTypeConverter` array build | P4 | COLL-01 wraps the shared array; no per-converter duplication |
| Value-bearing conversion exceptions | Value-free `SettingsPropertyValueException` (takes failure `Type`, no inner) | S1/C2 (#27/#28) | COLL-03 + VAL-01/02 must preserve this; do not chain value-bearing inners |
| Held async validator scaffolding (`Task<ValidationResult>`) | Synchronous `ValidationResult` (this phase) | Phase 4 (D-07) | Free change — interfaces never invoked |

**Deprecated/outdated:**
- `origin/validate-settings` branch and its `Core/SettingsOptionsValidator` — pre-modernization fossil; **do not port** (CONTEXT §specifics). VAL-01 is greenfield.
- Client doc pointer `TypeConverter.ValidateNullAcceptance` — **stale**, method no longer exists; VAL-02's hook is `PropertyConversion.Convert`.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `new List<T>(builtArray)` is the cheapest correct materialization satisfying all five list shapes | Pattern 1 | If a benchmark regresses, planner switches to direct list build; behavior identical either way |
| A2 | Making `CollectionTypeConverter.Convert` virtual (or extracting a protected helper) is a clean internal change | Pattern 1 | Low — internal type, `InternalsVisibleTo` covers tests |
| A3 | Property-level `[SettingsProperty(ValidatorType)]` validates the property value (not the whole settings object) | Pattern 3 / Q2 | Medium — wrong contract shape means rework of the property-validator path; needs planner decision |
| A4 | Deferring DI-resolved validators to a post-build step preserves the aggregate + redaction contract | Pattern 3 / Q3 | High — this is the phase's critical unknown; architect + security-auditor must sign off |
| A5 | An empty/whitespace config value is not itself a secret, so VAL-02's exception may name the property | Pattern 4 | Low — exception is kept value-free regardless |

## Open Questions (RESOLVED)

> All four resolved during planning (plan set `6db217a`): Q1 → 04-01 (wrap the array-build path), Q2 → 04-03 (`ISettingValidation<TProperty>` for property-level), Q3 → 04-04 (deferred post-provider-build DI-validator runner), Q4 → 04-04/04-05 (`out ISettingsCollection` overload). The recommendations below are now locked by the cited plans.

1. **`List<T>` materialization: wrap vs. direct build (CONTEXT research flag).** — **RESOLVED → 04-01 (wrap).**
   - What we know: reuse-the-array wrap is simplest and honors D-01's "reuse the array-build path"; direct build saves one copy.
   - What's unclear: whether the wrap trips PERF-02 for list-shaped targets.
   - Recommendation: implement the wrap; run `benchmark.yml` locally/CI; only switch to direct build if a *regression on existing paths* appears (a new allocation on a previously-throwing path is expected, not a regression).

2. **Property-level validator contract (VAL-01, D-11).**
   - What we know: `[SettingsProperty(ValidatorType=...)]` "runs on that property's value"; the held interfaces are typed to the settings `T`.
   - What's unclear: does a property-level validator implement `ISettingValidation<TProperty>` (context carries the property value) or validate the whole settings object post-populate?
   - Recommendation: property-level `ValidatorType` implements `ISettingValidation<TProperty>` and receives `ValidationContext<TProperty>` carrying the property value; object-level `[SettingsValidator]` implements `ISettingValidation<TSettings>`. Planner to lock this shape.

3. **DI-resolved validator timing/ordering (CONTEXT CRITICAL UNKNOWN, D-11).**
   - What we know: `IntegrateSimpleSettings` builds + would-validate settings at registration (`:32`), before `BuildServiceProvider` and before consumer services are all registered. Core-path (`Activator`, parameterless) validators are safe inline; DI-resolved validators are not.
   - What's unclear: the mechanism to run service-dependent validators after the container exists while keeping the aggregate + redaction contract byte-identical to the core path.
   - Recommendation (for planner + architect + security-auditor): run **core-path validators inline** in `ValuesPopulator` (covers `SettingsBuilder` and DI). For **DI-resolved** validators, defer to a post-provider-build step registered by `AddSimpleSettings` — e.g. a small `IStartupValidator`/`IHostedService` that resolves `ISettingValidation<T>` per settings type and throws the **same** `SettingsValidationException`. Verify the GenericHost package may reference the needed hosting abstraction; if not, fall back to a resolvable `ISimpleSettingsValidationRunner` the host invokes, or restrict DI-resolved validators to dependency-free ones. Gate at plan-review.

4. **API-02 return-overload shape (D-15).**
   - What we know: cannot overload by return type; two candidate shapes (`out` param vs. distinct method name).
   - Recommendation: implement the DI-singleton registration unconditionally; add an `out ISettingsCollection` overload (keeps chaining) as the default return-value mechanism; final shape decided on-branch (reversal free pre-beta).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (net10) | build + test (net10 runtime locally) | ✓ | 10.0.301 | — |
| .NET 8 runtime | net8 test execution | ✗ locally (build-only) | — | CI runs net8; build both TFMs locally, run net8 in CI |
| TUnit / Microsoft.Testing.Platform | all tests | ✓ | 1.58.0 | — |
| `Microsoft.Extensions.Configuration` | COLL-03 `GetChildren()` | ✓ | 8.0.0 / 10.0.9 | — |
| `Microsoft.Extensions.DependencyInjection` | API-02 / VAL-01 DI path | ✓ | 8.0.x / 10.0.9 | — |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** net8 runtime not installed locally — build targets both TFMs (`net8.0;net10.0`), tests run on net10 locally and net8 in CI. `[VERIFIED: dotnet --list-sdks → only 10.0.301; TESTING.md CI matrix]`

**TUnit invocation gotcha (VERIFIED via STATE.md + TESTING.md):** `dotnet test --filter` returns 0 tests / exit 5 on Microsoft.Testing.Platform. Use `--treenode-filter "/*/*/ClassNameTests/*"` or run unfiltered. Build + test from `src/`.

## Validation Architecture

Nyquist validation is **enabled** (`config.json → workflow.nyquist_validation: true`). Test framework is established; every requirement maps to automatable TUnit tests extending existing files.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | TUnit 1.58.0 on Microsoft.Testing.Platform |
| Config file | `src/global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`) |
| Quick run command | `cd src && dotnet test "$SOLUTION" -c Release --no-build --treenode-filter "/*/*/CollectionConversionTests/*"` |
| Full suite command | `cd src && dotnet restore && dotnet build -c Release --no-restore && dotnet test -c Release --no-build` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| COLL-01 | Delimited scalar → `List<int>`/`IList<int>`/`ICollection<int>`/`IReadOnlyList<int>`/`IReadOnlyCollection<int>` all materialize a `List<T>` | unit | `--treenode-filter "/*/*/CollectionConversionTests/*"` | ✅ extend `Conversion/CollectionConversionTests.cs` |
| COLL-01 | `IEnumerable<T>` still returns a `T[]` (no regression) | unit | same | ✅ already pinned (`Convert_ToIntEnumerable_MaterializesAnArray`) — keep green |
| COLL-02 | Unbound `int[]` → empty array, not null | unit | `--treenode-filter "/*/*/TypeConverterTests/*"` | ✅ extend `Core/TypeConverterTests.cs` |
| COLL-02 | Unbound `List<int>` → empty list, not null | unit | same | ❌ Wave 0 (add cases) |
| COLL-03 | Child-section sequence binds to `string[]`/`int[]`/`List<T>` | integration | `--treenode-filter "/*/*/SettingsBuilderConfigurationBinderIntegrationTests/*"` | ✅ extend `ConfigBuilderConfigurationBinderIntegrationTests.cs` |
| COLL-03 | Children win when scalar + children both present | integration | same | ❌ Wave 0 |
| COLL-03 | Comma-scalar still binds (regression guard) | integration | same | ✅ existing comma path — add explicit assertion |
| COLL-03 | Empty / whitespace / empty-sequence → empty, never `[""]` | integration | same | ❌ Wave 0 |
| COLL-03 | `[SettingsSection]` / root prefix honored on sequence | integration | same | ❌ Wave 0 |
| **COLL-03 / S1** | **Secret in a sequence element absent from full `ex.ToString()` on convert failure** | unit | `--treenode-filter "/*/*/ExceptionRedactionTests/*"` | ✅ extend `Conversion/ExceptionRedactionTests.cs` |
| VAL-01 | Object-level `[SettingsValidator]` runs; failing validator throws `SettingsValidationException` | unit | `--treenode-filter "/*/*/*Validation*/*"` | ❌ Wave 0 (new test class) |
| VAL-01 | Property-level `ValidatorType` runs on the property value | unit | same | ❌ Wave 0 |
| VAL-01 | Multiple failures aggregate into one exception's error list | unit | same | ❌ Wave 0 |
| VAL-01 | Cross-property rule validates against the fully-populated instance | unit | same | ❌ Wave 0 |
| VAL-01 | DI-resolved `ISettingValidation<T>` runs (per Q3 mechanism) | integration | `--treenode-filter "/*/*/AddSimpleSettingsIntegrationTests/*"` | ❌ Wave 0 (extend DI tests) |
| VAL-01 / D-12 | `SettingsValidationException.ToString()` contains only author messages, no injected values | unit | same as VAL-01 | ❌ Wave 0 |
| VAL-02 | `AllowEmpty=false` rejects `""` and whitespace (not just null); value-free exception | unit | `--treenode-filter "/*/*/TypeConverterTests/*"` or `SettingsPropertyTests` | ✅ extend `Core/TypeConverterTests.cs` / `SimpleSettings/SettingsPropertyTests.cs` |
| VAL-02 | `AllowEmpty=true` still accepts empty/whitespace | unit | same | ❌ Wave 0 |
| API-02 | `ISettingsCollection` resolvable via `GetRequiredService` after `AddSimpleSettings` | integration | `--treenode-filter "/*/*/AddSimpleSettingsIntegrationTests/*"` | ✅ extend `DependencyInjection/AddSimpleSettingsIntegrationTests.cs` |
| API-02 | Return-value overload yields the same collection; `IServiceCollection` chaining preserved | integration | same | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** the `--treenode-filter` quick run for the touched area.
- **Per wave merge:** full suite on net10 locally (`dotnet test -c Release --no-build`).
- **Phase gate:** full suite green on net8 + net10 (CI) plus `benchmark.yml` allocation gate green before `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `Conversion/CollectionConversionTests.cs` — add `List<T>` family + `IEnumerable`-still-array cases (COLL-01)
- [ ] `Core/TypeConverterTests.cs` — empty-not-null for array + `List<T>` (COLL-02); empty/whitespace rejection (VAL-02)
- [ ] `ConfigBuilderConfigurationBinderIntegrationTests.cs` — sequence bind, children-win, comma-scalar guard, empty→empty, prefix (COLL-03)
- [ ] `Conversion/ExceptionRedactionTests.cs` — **S1 sequence-element redaction** regression (COLL-03 gate)
- [ ] New `SimpleSettings/SettingsValidationTests.cs` (or similar) — object-level, property-level, aggregate, cross-property, redaction (VAL-01)
- [ ] `DependencyInjection/AddSimpleSettingsIntegrationTests.cs` — DI-resolved validator (Q3), `ISettingsCollection` resolution + return overload (VAL-01/API-02)
- [ ] Test fixtures: new settings interfaces with `[SettingsValidator]` / `ValidatorType` / list-shaped + sequence-backed properties (co-locate per TESTING.md conventions)

## Security Domain

`security_enforcement: true`, `security_asvs_level: 1`, `security_block_on: high`.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Library binds config; no auth surface |
| V3 Session Management | no | — |
| V4 Access Control | no | — |
| V5 Input Validation | yes | VAL-01 (settings validation) + VAL-02 (empty rejection); config values validated at bind via the converter chain and validators |
| V6 Cryptography | no | No crypto; never hand-rolled |
| V7 Error Handling & Logging | **yes (primary)** | S1 secret-redaction invariant — exceptions carry no bound values, chain no value-bearing inners; `SettingsPropertyValueException` takes a failure `Type`, not the exception |
| V8 Data Protection | yes | Secret values in config must not leak into logs via `Exception.ToString()`/`ILogger` (the reason the library logs nothing itself) |

### Known Threat Patterns for this stack
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Secret in a YAML/child-sequence element leaks through a bind/convert failure (COLL-03) | Information Disclosure | Convert failures surface as value-free `SettingsPropertyValueException` (`ValuesPopulator.cs:114-128`); `SettingsBindingException` retains only primitives (binder type/section/key), never `BindingContext` (`SettingsBindingException.cs:5-16`). New regression test proves the sentinel is absent from the whole `ex.ToString()` chain. |
| Validator error message echoes a secret value (VAL-01) | Information Disclosure | `SettingsValidationException` adds no bound values; document validator-author responsibility (D-12); test asserts only author messages appear. |
| VAL-02 empty-value exception embeds the value | Information Disclosure | Exception is value-free (property name only), reusing `SettingsPropertyNullException`'s value-free message contract. |
| Custom `ISectionBinder`/`ISettingsTypeConverter` leaks a value in its own message | Information Disclosure | Wrapper drops the inner and keeps only its `Type` name; already covered by `ExceptionRedactionTests` (`Convert_CustomConverterLeakingValue_IsStillRedacted`) — extend to the sequence path. |

**Security gate (D-06):** the S1 sequence-element redaction regression test is mandatory and owned by `security-auditor` in the plan-review panel. Blocking severity is `high`.

## Sources

### Primary (HIGH confidence)
- Codebase source (read this session, cited inline with file:line): `TypeConverter.cs`, `TypeExtensions.cs`, `CollectionTypeConverter.cs`, `ArrayTypeConverter.cs`, `EnumerableTypeConverter.cs`, `TypeConvertersCollections.cs`, `DefaultTypeConverter.cs`, `ConfigurationBinder.cs`, `ValuesPopulator.cs`, `PropertyConversion.cs`, `SettingsPropertyAttribute.cs`, `Validations/*`, `ServicesSettingsBuilderExtensions.cs`, `ISettingsCollection.cs`, `SettingsCollection.cs`, `BindingContext.cs`, `SettingsBuilder.cs`, `SettingsPlan.cs`, `SettingsOptions.cs`, `SimpleSettingsException.cs`, `SettingsPropertyValueException.cs`, `SettingsPropertyNullException.cs`, `SettingsBindingException.cs`, `Resources.cs`, `Info.cs`.
- Existing tests (patterns to extend): `Conversion/CollectionConversionTests.cs`, `Conversion/ExceptionRedactionTests.cs`, `DependencyInjection/AddSimpleSettingsIntegrationTests.cs`, `ConfigBuilderConfigurationBinderIntegrationTests.cs`.
- Planning artifacts: `04-CONTEXT.md` (D-01..D-15), `REQUIREMENTS.md`, `STATE.md`, `.planning/codebase/CONVENTIONS.md`, `.planning/codebase/TESTING.md`, `.planning/config.json`.
- Environment: `dotnet --list-sdks` → 10.0.301; `src/Directory.Packages.props` (pinned versions).

### Secondary (MEDIUM confidence)
- dotnet-claude-kit `modern-csharp` skill — C# 14 idioms (primary ctors, collection expressions, pattern matching). **Constraint applied:** `field` keyword and extension members are C# 14 (net10) only; `LangVersion` is unset per-TFM (net8 → C# 12), so those two features MUST NOT appear in shared library code. Collection expressions `[]` and primary constructors are C# 12 and safe on net8.
- dotnet-claude-kit `testing` skill — AAA, test-behavior-not-implementation, `Method_State_Expected` naming, test-data builders. Adapted to this project's TUnit + hand-written in-memory fakes (no xUnit/WebApplicationFactory/Testcontainers here).

### Tertiary (LOW confidence)
- None. This phase required no web research; all findings are codebase-verified.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; all versions verified against the pinned central props.
- Architecture / integration points: HIGH — every seam read at file:line; invariants confirmed.
- Pitfalls: HIGH — derived directly from the read code (converter predicates, exception filter, DI timing).
- DI-validator timing (Q3): MEDIUM — mechanism is a planner decision; the constraint (build-at-registration) is verified, the resolution is recommended not locked.

**Research date:** 2026-07-15
**Valid until:** 2026-08-14 (stable internal domain; re-check only if the converter chain, `ValuesPopulator`, or the DI extension is refactored before planning)
</content>
</invoke>
