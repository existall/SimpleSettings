# Requirements: ExistForAll.SimpleSettings

**Defined:** 2026-07-13
**Core Value:** Correctness of binding — config → strongly-typed settings maps accurately across every supported shape (sections, arrays/enumerables, defaults, nullable, custom converters)

## Validated (Shipped)

Delivered capabilities in the current codebase (brownfield baseline). Not mapped to phases.

### Binding & Type Generation

- [x] **BIND-01**: Consumer declares settings as interfaces (`ISettingsSection` base, `[SettingsSection]` attribute, or `Settings` suffix); the library emits a concrete impl at runtime
- [x] **BIND-02**: `SettingsBuilder.CreateBuilder` / `ScanAssemblies` map interface types to generated implementations (only exported interfaces scanned)
- [x] **BIND-03**: Values resolve through an ordered `ISectionBinder` chain (last-writer-wins), falling back to the property default when no binder sets a value
- [x] **BIND-04**: `[SettingsProperty]` options (`DefaultValue`, `Name`, `ConverterType`, `AllowEmpty`) control per-property binding

### Conversion

- [x] **CONV-01**: Values convert through an ordered `ISettingsTypeConverter` chain (user converters win; `DefaultTypeConverter` last; `InvariantCulture`)
- [x] **CONV-02**: Sections, defaults, enums, `DateTime`, `Uri`, arrays and `IEnumerable<T>` bind correctly
- [x] **CONV-03**: Array/enumerable conversion unified and de-reflected in `CollectionTypeConverter` (P4)

### Value Sources & Host Integration

- [x] **SRC-01**: In-memory, `IConfiguration`, environment-variable, and command-line binders contribute values
- [x] **HOST-01**: `AddSimpleSettings(...)` registers each scanned interface as a DI singleton plus an `ISettingsProvider`
- [x] **HOST-02**: `ISettingsProvider` caches the built instance per type (C3 option 2) so DI and provider return consistent objects

### Performance & Naming

- [x] **PERF-01**: Per-type caching (generated types, extracted properties, settings plans) keeps warm resolves cheap (P1–P5)
- [x] **PERF-02**: BenchmarkDotNet CI gates PRs on allocated bytes, not wall-clock time
- [x] **NAME-01**: Canonical `ExistForAll.SimpleSettings` naming across namespace and package identity (A2/D3)

## v1 Requirements

Remaining open work (from `FIX-PLAN.md`), batched toward the first `v2.0.0-beta`. Each maps to a roadmap phase.

### Exception Safety

- [x] **SEC-01**: Conversion-failure exceptions never leak the bound value or chain a value-bearing inner; only property name, target type, and failure type name surface; value-free "required missing" is `SettingsPropertyNullException` (S1, merged #27; structural via C2)
- [x] **SEC-02**: Sibling exception wrappers (`SettingsBindingException`, `SettingsExtractionException`, `TypeGenerationException`) audited to confirm none embed bound values (merged #27/#28)
- [x] **EXC-01**: Public `abstract SimpleSettingsException` base; boundary exceptions made public + structured (property/target/failure/binder/section/key); bare `Exception` at `TypeConverter.cs:62` replaced (C2, merged #28, breaking)

### Binding Correctness & Engine Tests

- [ ] **COLL-01**: `List<T>`/`IList<T>`/`ICollection<T>` support decision — broaden the converter or document + throw a clear error, with a positive test (C1)
- [ ] **TEST-01**: `ValuesPopulator` tests — binder precedence + bind/convert exception-wrapper contracts (T4)
- [ ] **TEST-02**: `TypeConverter` tests — null/nullable/empty-enumerable/`AllowEmpty`/attribute-`ConverterType` paths (T5)
- [ ] **TEST-03**: Converter tests residual — `Uri`/`DateTime` + `List<T>` doc test tied to C1 (T6)
- [x] **ENG-01**: Fix the unsynchronized check-then-`DefineType` race in `SettingsClassGenerator` + concurrency stress tests (T7 — shipped pre-GSD via the FIX-PLAN track, merged #29: double-checked locking, one gate over all generation; same- + distinct-interface `Barrier` stress tests)

### Public Surface, Packaging & Binder Cleanup

- [ ] **API-01**: Make `SettingsHolder`/`ISettingsHolder` internal (A5, breaking)
- [ ] **PKG-01**: `Core.AspNet` exposes a public type or the package is dropped (A3)
- [ ] **PKG-02**: Float `Microsoft.Extensions.*` floor per-TFM (`8.0.x` for net8) or justify the pin (A4)
- [ ] **SRC-02**: Command-line binder parses quoted values with spaces correctly and skips `arg[0]` (A6)

### AOT/Trim & Documentation

- [ ] **AOT-01**: Annotate reflection entry points (`[RequiresDynamicCode]`/`[RequiresUnreferencedCode]`) and/or document the AOT/trim limitation before stable (A1)
- [ ] **DOC-01**: Refresh README to canonical naming and current repo/package links

### Release

- [ ] **REL-01**: Cut the first `v2.0.0-beta` with all batched breaking changes; consistent package identity across sub-packages; suite green on net8 + net10

## v2 Requirements

Deferred / held. Tracked but not in the current roadmap.

### Held (do NOT delete)

- **VAL-01**: Wire the `Validations/*` API + `SettingsPropertyAttribute.ValidatorType` into the populate path (reconcile with the `validate-settings` branch) — D1, HELD
- **EQ-01**: Delete or fix+wire `EqualityCompererCreator` (has a latent invalid-IL bug) for value-equality on generated types — D2, HELD

### Deferred

- **PERF-03**: Tiered/lazy compiled property setter — only if set-*time* shows up in a real profile (P3b); a compiled setter was tried and reverted (regressed cold scan, no warm gain)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Classic .NET Framework support | Dropped; modern .NET only (`net8.0;net10.0`) |
| `IOptionsMonitor`-style reload of built instances | Instances are snapshots by design; deferred as C3 "option 3" |
| Opt-in "full diagnostics" exception knob (restore value/inner) | Rejected as insecure-by-configuration (S1) |
| Full Roslyn source generator replacing `Reflection.Emit` | Future only; may surface as an A1 option, not committed this milestone |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SEC-01 | Phase 1 | ✓ Complete (#27) |
| SEC-02 | Phase 1 | ✓ Complete |
| EXC-01 | Phase 1 | ✓ Complete (#28) |
| COLL-01 | Phase 2 | Pending |
| TEST-01 | Phase 2 | Pending |
| TEST-02 | Phase 2 | Pending |
| TEST-03 | Phase 2 | Pending |
| ENG-01 | Phase 2 | ✓ Complete (#29) |
| API-01 | Phase 3 | Pending |
| PKG-01 | Phase 3 | Pending |
| PKG-02 | Phase 3 | Pending |
| SRC-02 | Phase 3 | Pending |
| AOT-01 | Phase 4 | Pending |
| DOC-01 | Phase 4 | Pending |
| REL-01 | Phase 5 | Pending |

**Coverage:**
- v1 requirements: 15 total
- Mapped to phases: 15
- Unmapped: 0 ✓
- Complete: 4 (Phase 1 SEC-01/SEC-02/EXC-01 #27/#28 + ENG-01/T7 #29); Pending: 11
- Validated (shipped, no phase): 13

---
*Requirements defined: 2026-07-13*
*Last updated: 2026-07-14 — ENG-01/T7 marked complete (#29). GSD is now the source of truth; FIX-PLAN.md frozen as a historical reference. Reconciled from session handoff + git.*
