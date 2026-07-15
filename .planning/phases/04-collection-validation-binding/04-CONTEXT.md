# Phase 4: Collection & Validation Binding - Context

**Gathered:** 2026-07-15
**Status:** Ready for planning

<domain>
## Phase Boundary

Make collection binding correct across every shape (empty, comma-scalar, YAML/child-section
sequence), make declared settings-validation actually **run** in the bind pipeline, and expose the
settings collection from the DI extension — the client's pre-beta engine batch. Requirements
**COLL-02, COLL-03, VAL-01, VAL-02, API-02** are locked by ROADMAP; this discussion settled **HOW**.

**Explicitly pulled into scope:** full `List<T>`/`IList<T>`/`ICollection<T>` support (the long-deferred
**COLL-01**) — COLL-02/COLL-03's `List<T>` behavior is incoherent without it.

**Not in scope:** AOT-01 / DOC-01 (Phase 5); REL-01 (Phase 6); held D2 `EqualityCompererCreator`;
`${ENV:-}` placeholder detection (deferred — see Deferred Ideas); `IOptionsMonitor`-style reload.
</domain>

<decisions>
## Implementation Decisions

### Collection scope — absorb COLL-01 (full `List<T>` family)
- **D-01:** Land **full `List<T>` family support** this phase. One converter materializes a `List<T>`
  and satisfies `List<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`;
  arrays stay on `ArrayTypeConverter`. Broaden `TypeExtensions.IsEnumerable`
  (`Core/Reflection/TypeExtensions.cs:21-26`, matches only the open `IEnumerable<>` today) and the
  element-type detection so these shapes are recognized for both conversion and the null/empty default.
  Reuse the existing `CollectionTypeConverter` array-build path (`Conversion/CollectionTypeConverter.cs`),
  then wrap into a `List<T>` when the target is a list shape. Rationale: only `ArrayTypeConverter`
  (`IsArray`) and `EnumerableTypeConverter` (open `IEnumerable<>`) exist today, so a populated `List<T>`
  currently falls to `DefaultTypeConverter` and throws — the ROADMAP success criteria and client doc
  both name `List<T>`, so the empty/sequence fixes require real conversion support.

### COLL-02 — empty-collection default
- **D-02:** Extend `TypeConverter.CreateNullResult` (`Core/Reflection/TypeConverter.cs:27-36`, the null
  branch) so an unset `T[]`, `List<T>` family, and `IEnumerable<T>` all bind an **empty collection,
  never `null`**. Today only `IEnumerable<T>` does (via `IsEnumerable()`); arrays and lists fall to the
  value-type/`null` branch. Materialize `Array.CreateInstance(elementType, 0)` for arrays and a new
  empty `List<T>` for the list shapes.
- **D-03:** **Sequencing — COLL-02 before COLL-03.** COLL-03's "empty / whitespace / empty-sequence →
  empty" behavior leans on COLL-02's empty-not-null default already being in place.

### COLL-03 — YAML / child-section sequence binding (redaction-critical)
- **D-04:** `ConfigurationBinder.BindPropertySettings`
  (`Extensions.Binders/ConfigurationBinder.cs:26-36`) currently reads a single scalar `section[key]`.
  For **collection-typed targets**, enumerate `GetChildren()` and set the sequence (as an array /
  `string[]`) onto `context`. The existing `CollectionTypeConverter.AsArray`
  (`Conversion/CollectionTypeConverter.cs:43-51`) already passes an incoming `Array` straight through,
  so a binder that sets an array `NewValue` flows through the inner element-converter chain unchanged.
- **D-05:** Locked sub-rules (from client doc, non-negotiable): **children win** when both a scalar and
  children exist; **comma-scalar MUST still bind** (prod env override `MultiHost__CommonHosts` depends
  on it); empty / whitespace / empty-sequence → **empty, never `[""]`**; each element still flows the
  inner converter chain (so `int[]` / enum arrays keep working); honor `[SettingsSection]` / root
  prefixing exactly as the scalar path does today.
- **D-06 (SECURITY GATE, locked):** COLL-03 edits `BindPropertySettings`, which runs inside
  `ValuesPopulator`'s wrapping catch (`ValuesPopulator.cs:50-60` → `SettingsBindingException`). The
  **S1/SEC-01 secret-redaction invariant MUST be re-verified**: add a regression test proving a secret
  value carried in a YAML sequence element is absent from the whole `ex.ToString()` chain on a
  bind/convert failure. `security-auditor` owns this in the plan-review panel.

### VAL-01 — settings validation wired into the pipeline
- **D-07:** **Redesign the held validator interfaces to synchronous.** Change
  `ISettingsValidator.Validate` / `ISettingValidation<T>.Validate` to return `ValidationResult` (drop
  `Task<>`). The populate/build path is synchronous `void` and runs at startup; a sync validator hooks in
  with no sync-over-async hazard. The interfaces are dead scaffolding (never invoked), so the signature
  change is free pre-beta.
- **D-08:** **Complete the held scaffolding.** `ValidationContext` (`Validations/ValidationContext.cs`)
  and `ValidationContext<T>` are getter-only with no constructor — they can't currently carry a
  `Settings` value. Add construction so the populated instance is passed in.
- **D-09:** **Timing — validation runs at build/populate (startup).** Hook after the property-set loop
  in `ValuesPopulator.PopulateInstanceWithValues` (`ValuesPopulator.cs:29-65`), i.e. once the instance is
  fully populated (cross-property rules need all properties set).
- **D-10:** **Failure — aggregate, throw once.** Run all applicable validators for the type, collect
  every `ValidationError`, and throw ONE new **`SettingsValidationException : SimpleSettingsException`**
  carrying the full error list. Fail-fast at startup; operator sees every problem at once; stays in the
  one catchable exception family.
- **D-11:** **Discovery — BOTH mechanisms.**
  - *Core path (always):* property-level `[SettingsProperty(ValidatorType=typeof(X))]`
    (`SettingsPropertyAttribute.cs:14`, runs on that property's value) **and** an object-level
    interface-attribute (e.g. `[SettingsValidator(typeof(X))]`) on the settings interface, instantiated
    via `Activator` (parameterless ctor). Works for both `SettingsBuilder` and DI paths.
  - *DI path (when a container is present, `AddSimpleSettings`):* also resolve registered
    `ISettingValidation<T>` from the container — supports validators needing injected services (closest
    to replacing the gateway's interim FluentValidation boot-hook).
- **D-12:** **Redaction.** The thrown `SettingsValidationException` must add no bound values itself.
  `ValidationError` messages are validator-author-supplied → the author owns not echoing secrets;
  document this. Re-verify no value leak (ties to S1/SEC-01).

### VAL-02 — tighter `AllowEmpty`
- **D-13:** `[SettingsProperty(AllowEmpty=false)]` rejects **null + `""` + whitespace**
  (`string.IsNullOrWhiteSpace`). `${ENV:-}` placeholder detection is **deferred** (client marked it
  lowest-urgency; pattern-matching `${...}` risks false positives, and the library only sees whatever the
  config provider left — it does no substitution itself).
- **D-14:** **Inline at the conversion layer.** Extend `PropertyConversion.Convert`'s existing
  `_throwOnNull` branch (`Conversion/PropertyConversion.cs:29-40`; `throwOnNull` is resolved at plan-build
  in `TypeConverter.CreateConversion:16`) to also reject empty/whitespace strings. Keep it a binding-layer
  concern (not a VAL-01 validator). Throw a **value-free** exception in the `SimpleSettingsException`
  family; exact type is the planner's call (reuse `SettingsPropertyNullException` or a dedicated
  empty-specific sibling). NOTE: the client doc's pointer `TypeConverter.ValidateNullAcceptance` is
  **stale** — that method no longer exists; `PropertyConversion` is the real hook.

### API-02 — expose `ISettingsCollection`
- **D-15:** **Implement both** exposure mechanisms, then decide the final shape at review on the branch
  (reversal is free pre-beta):
  1. Register `ISettingsCollection` as a resolvable DI singleton
     (`services.AddSingleton<ISettingsCollection>(settingsCollection)` in
     `ServicesSettingsBuilderExtensions.IntegrateSimpleSettings`; the collection already exists at
     `:32`).
  2. Add an overload of `AddSimpleSettings` that returns the `ISettingsCollection`.
  Keep the existing `IServiceCollection`-returning overloads intact so the fluent chain still works.
  `ISettingsCollection` is public; `SettingsCollection` impl stays internal.

### Claude's Discretion
- Exact new type/method names (`SettingsValidationException`, `[SettingsValidator]`, the VAL-02 exception
  type, the API-02 return overload signature), test file placement under
  `src/Tests/ExistForAll.SimpleSettings.UnitTests/`, and the precise `List<T>`-materialization mechanism
  (wrap the built array vs. build a `List<T>` directly). All breaking/behavior changes are expected and
  batched pre-beta.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirement / roadmap / client source
- `.planning/ROADMAP.md` — Phase 4 goal + 6 success criteria
- `.planning/REQUIREMENTS.md` — COLL-02 / COLL-03 / VAL-01 / VAL-02 / API-02 + traceability
- `.planning/backlog/client-requirements-pre-beta.md` — the client's original per-item asks and rationale
  (⚠ its `TypeConverter.ValidateNullAcceptance` pointer is stale; trust current source)

### COLL-01 scope + COLL-02 (empty default)
- `src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypeConverter.cs:27-36` — `CreateNullResult` null branch
- `src/Core/ExistForAll.SimpleSettings/Core/Reflection/TypeExtensions.cs:21-26` — `IsEnumerable` (broaden here)
- `src/Core/ExistForAll.SimpleSettings/Conversion/CollectionTypeConverter.cs` — shared array-build path (`AsArray:43-51`)
- `src/Core/ExistForAll.SimpleSettings/Conversion/ArrayTypeConverter.cs` — `IsArray` target
- `src/Core/ExistForAll.SimpleSettings/Conversion/EnumerableTypeConverter.cs` — open `IEnumerable<>` target
- `src/Core/ExistForAll.SimpleSettings/Conversion/TypeConvertersCollections.cs` — the converter `LinkedList`

### COLL-03 (sequence binding + redaction gate)
- `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/ConfigurationBinder.cs:26-36` — `BindPropertySettings` (add `GetChildren()`)
- `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs:50-60` — the `SettingsBindingException` wrapping catch (redaction context)
- Exception contract (S1/SEC-01): `SettingsBindingException`, `SettingsPropertyValueException`, `SettingsPropertyNullException`, `SimpleSettingsException`

### VAL-01 / VAL-02 (validation + empty)
- `src/Core/ExistForAll.SimpleSettings/Validations/` — held scaffolding: `ISettingValidation.cs`, `ISettingsValidator.cs`, `ValidationContext.cs` (needs ctor), `ValidationContextOfT.cs`, `ValidationResult.cs`, `ValidationError.cs`
- `src/Core/ExistForAll.SimpleSettings/SettingsPropertyAttribute.cs:14` — `ValidatorType` (VAL-01), `AllowEmpty:12` (VAL-02)
- `src/Core/ExistForAll.SimpleSettings/ValuesPopulator.cs:29-65` — populate path (VAL-01 hook point, after the property loop)
- `src/Core/ExistForAll.SimpleSettings/Conversion/PropertyConversion.cs:29-40` — `_throwOnNull` branch (VAL-02 hook)
- `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs:21-44` — DI-path validator resolution site

### API-02
- `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs:7-44` — `AddSimpleSettings` / `IntegrateSimpleSettings` (collection built at `:32`)
- `src/Core/ExistForAll.SimpleSettings/ISettingsCollection.cs` — the public interface to expose
- `src/Core/ExistForAll.SimpleSettings/SettingsCollection.cs` — internal impl; `SettingsCollectionExtensions.cs` — `GetSettings<T>` helper

### Test conventions
- `.planning/codebase/TESTING.md` — TUnit conventions (⚠ `--filter` returns 0 tests / exit 5; use `--treenode-filter` or run unfiltered; build/test from `src/`)
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/Conversion/CollectionConversionTests.cs`, `Core/TypeConverterTests.cs`, `Core/ValuesPopulatorTests.cs` — existing coverage to extend/dedupe against
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`CollectionTypeConverter` array-build path** — `Array.CreateInstance` + indexed element-convert; the
  `List<T>` shapes can reuse this and wrap the result. `AsArray` already passes an `Array` through, so a
  COLL-03 binder that sets an array `NewValue` needs no converter change.
- **Per-property `PropertyConversion` struct + plan cache** — `AllowEmpty`/`throwOnNull` and the null-result
  are resolved once at plan-build; VAL-02 extends the existing branch, no hot-path scan added.
- **Held `Validations/*` types** — `ValidationResult`/`ValidationError` are complete; the interfaces +
  `ValidationContext` need the sync + ctor changes only.
- **`InternalsVisibleTo`** (`Info.cs`) — keeps internal types reachable from UnitTests/Benchmark.

### Established Patterns
- One public type per file; `I`-prefixed interfaces; block-scoped namespaces; `net8.0;net10.0` only;
  central package management. Modern C# (skill `modern-csharp`), TUnit tests (skill `testing`) — cite in the plan.
- Ordered converter chain (user first, `DefaultTypeConverter` last) and last-writer-wins binder precedence
  are load-bearing — new collection/validation code must not reorder them.
- Breaking changes are free pre-stable and batched before `v2.0.0-beta`.

### Integration Points
- `TypeConverter.CreateNullResult` / `TypeExtensions.IsEnumerable` — collection shape detection (COLL-01/02).
- `ConfigurationBinder.BindPropertySettings` — sequence reading (COLL-03).
- `ValuesPopulator.PopulateInstanceWithValues` — post-populate validation hook (VAL-01).
- `PropertyConversion.Convert` — empty rejection (VAL-02).
- `ServicesSettingsBuilderExtensions` — DI validator resolution + `ISettingsCollection` exposure (VAL-01/API-02).

### Research flags (for gsd-phase-researcher / plan-review)
- **DI-path validator timing/ordering** — `AddSimpleSettings` builds instances at registration; DI-resolved
  validators may depend on services not yet registered. Resolve the ordering and keep the aggregate+redaction
  contract identical across the core path and the DI path.
- **`List<T>` materialization** — confirm the cheapest path (wrap the existing built array vs. build the list).
</code_context>

<specifics>
## Specific Ideas
- **`origin/validate-settings` is a pre-modernization fossil** (all old `SimpleConfig`→`SimpleSettings`
  rename commits, ~188k deletions, dead `build.cake`/`appveyor.yml`). It carries an old
  `Core/SettingsOptionsValidator` not on `master`, but nothing reconcilable. **VAL-01 is greenfield** —
  ignore the REQUIREMENTS "reconcile with the validate-settings branch" note as effectively moot (glance at
  the old `SettingsOptionsValidator` only for original design intent, do not port).
- **dotnet-kit layering:** plan-review panel up front (`dotnet-architect` + `performance-analyst` +
  `security-auditor`, the latter owns the D-06 redaction gate); executor uses `modern-csharp` + `testing`
  skills; `code-reviewer` on the finished diff.
- Ship on branch `gsd/phase-4-collection-validation-binding` → PR via `guy-lud`. Never commit to `master`
  (every push publishes a throwaway alpha via `release.yml`).
</specifics>

<deferred>
## Deferred Ideas
- **`${ENV:-}` unsubstituted-placeholder detection** (VAL-02) — deferred; revisit if a real case appears
  (needs a precise pattern to avoid false positives; library does no env-substitution itself).
- **AOT-01 / DOC-01** — Phase 5. **REL-01** — Phase 6.
- **D2 `EqualityCompererCreator`** — HELD; out of scope.

None of the above block Phase 4.
</deferred>

---

*Phase: 04-collection-validation-binding*
*Context gathered: 2026-07-15*
</content>
</invoke>
