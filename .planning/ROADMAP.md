# Roadmap: ExistForAll.SimpleSettings

## Overview

**Status (2026-07-14):** Phase 1 shipped (S1 #27, C2 #28); ENG-01/T7 also merged (#29). `master` @ `10f9275`. Active phase is **Phase 2** — ENG-01 done; COLL-01 (deferred) + engine tests (TEST-01/02/03) remain.

The binding engine already ships and works. This milestone is a hardening + pre-stable
cleanup pass that batches every remaining breaking change and safety fix before cutting the
first `v2.0.0-beta`. It starts by locking the secret-safe exception story and giving
consumers one catchable, structured exception base (Phase 1), proves binding correctness
across collection/nullable/converter shapes and closes the generator concurrency race with
tests (Phase 2), trims and corrects the public surface, packaging, and command-line binder
(Phase 3), tells consumers the truth about AOT/trim and refreshes the docs (Phase 4), and
finally publishes the batched result as the first beta (Phase 5). Everything serves the core
value: config → typed settings maps accurately, and never leaks a secret doing it.

## Phases

**Phase Numbering:**

- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

- [x] **Phase 1: Exception Safety & Public Hierarchy** — ✓ COMPLETE (S1 #27, C2 #28 merged 2026-07-14) - No secret leaks; one catchable, structured `SimpleSettingsException` base
- [x] **Phase 2: Binding Correctness & Engine Test Hardening** - Collections/nullable/converters verified; generator race closed by tests *(ENG-01/T7 done #29; COLL-01 + TEST-01/02/03 remain)* (completed 2026-07-14)
- [ ] **Phase 3: Public Surface, Packaging & Binder Cleanup** - Meaningful public surface; per-TFM deps; correct command-line parsing
- [ ] **Phase 4: AOT/Trim Honesty & Documentation** - Honest AOT/trim signals; canonically-named docs
- [ ] **Phase 5: First v2.0.0-beta Release** - Batched breaking changes ship as an installable pre-release

## Phase Details

### Phase 1: Exception Safety & Public Hierarchy

**Goal**: The library never leaks secret bound values through failures, and consumers can catch every library error as one public, structured category.
**Depends on**: Nothing (first phase)
**Requirements**: SEC-01, SEC-02, EXC-01
**Status**: ✓ COMPLETE — shipped outside GSD via the FIX-PLAN track (S1 #27 + C2 #28), merged to `master` @ `13b78dd` on 2026-07-14. All 5 success criteria met; secret-redaction is structural (`SettingsPropertyValueException` takes the failure `Type`, not the `Exception`); +11 tests (5 redaction + 6 hierarchy).
**Success Criteria** (what must be TRUE):

  1. A secret sentinel bound to an `int`/`enum`/`DateTime`/`Uri`/custom-converter property is absent from the entire `ex.ToString()` chain of the thrown exception.
  2. A "required value missing" failure still surfaces its full diagnostic message via `SettingsPropertyNullException` (not redacted).
  3. A consumer can `catch (SimpleSettingsException)` and handle every boundary failure the library raises.
  4. Boundary exceptions expose structured context (property name, target type, failure type name, binder/section/key) without the bound value.
  5. No exception wrapper (binding, extraction, generation, value-conversion) embeds a bound value or chains a value-bearing inner.

**Plans**: n/a — delivered pre-GSD (S1 #27 + C2 #28)

### Phase 2: Binding Correctness & Engine Test Hardening

**Goal**: Binding maps config to typed settings accurately across every supported collection, nullable, and converter shape, with the engine's concurrency and precedence behavior locked by tests.
**Depends on**: Phase 1 (engine tests assert the S1/C2 exception contract)
**Requirements**: COLL-01, TEST-01, TEST-02, TEST-03, ENG-01
**Status**: In progress — ENG-01/T7 delivered pre-GSD (merged #29: generator concurrency race closed via double-checked locking + same/distinct-interface stress tests). COLL-01 (C1 — decision deferred) and TEST-01/02/03 (T4/T5/T6) remain.
**Success Criteria** (what must be TRUE):

  1. A settings interface exposing `List<T>`/`IList<T>`/`ICollection<T>` either binds correctly or fails with a clear, documented error (per the C1 decision), covered by a test.
  2. Binder precedence (last binder wins; attribute default applies when none set) is verified by `ValuesPopulator` tests.
  3. `TypeConverter` null / nullable / empty-enumerable / `AllowEmpty` / attribute-`ConverterType` paths are verified by tests.
  4. Concurrent first-touch generation of the same interface returns one `ReferenceEquals` implementation with no duplicate-`DefineType` race. ✓ Met by #29 (T7).
  5. `Uri`/`DateTime` and collection converters have parity tests passing on net8 and net10.

**Plans**: 2/2 plans complete

- [x] 02-01-PLAN.md — Engine-core correctness tests (TEST-01 ValuesPopulator precedence/default; TEST-02 TypeConverter null/nullable/ConverterType-over-collection) + ENG-01 concurrency verify
- [x] 02-02-PLAN.md — Converter scalar residual (TEST-03 scalar Uri/DateTime positive + one format-mismatch negative); COLL-01 documented as owner-deferred

### Phase 3: Public Surface, Packaging & Binder Cleanup

**Goal**: The public API and packages carry only meaningful, correctly-scoped surface, and the command-line binder parses real-world arguments correctly — the remaining breaking changes batched before beta.
**Depends on**: Phase 2
**Requirements**: API-01, PKG-01, PKG-02, SRC-02
**Success Criteria** (what must be TRUE):

  1. `SettingsHolder`/`ISettingsHolder` are internal and no longer appear on the public surface; build and suite stay green.
  2. `Core.AspNet` either exposes a consumable public type or is removed from the solution.
  3. A net8 consumer is no longer transitively forced onto `Microsoft.Extensions.* 10.x` (per-TFM floor), or the pin is documented with justification.
  4. A quoted command-line value containing spaces binds correctly and the executable path (`arg[0]`) is skipped.

**Plans**: 2 plans
**Wave 1**

- [ ] 03-01-PLAN.md — Public surface & packaging cleanup: SettingsHolder → internal sealed (API-01), drop dead Core.AspNet package (PKG-01), per-TFM Microsoft.Extensions.* floor (PKG-02) [Wave 1]

**Wave 2** *(blocked on Wave 1 completion)*

- [ ] 03-02-PLAN.md — Command-line binder cleanup: SkipFirstArgument option + space-separated `--k v` lookahead + arg[0] skip + AddCommandLine tokenization (SRC-02) [Wave 2, depends on 03-01]

### Phase 4: AOT/Trim Honesty & Documentation

**Goal**: Consumers get honest signals about AOT/trim support and accurate, canonically-named documentation.
**Depends on**: Phase 3
**Requirements**: AOT-01, DOC-01
**Success Criteria** (what must be TRUE):

  1. Public reflection-based entry points carry `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` annotations and/or the AOT/trim limitation is documented before stable.
  2. Building an AOT/trimmed consumer surfaces a warning (or finds a clearly documented limitation) rather than failing silently.
  3. README uses the canonical `ExistForAll.SimpleSettings` name and links to current repo/package paths (no legacy `existall`/`SimpleConfig` references).

**Plans**: TBD

### Phase 5: First v2.0.0-beta Release

**Goal**: All batched breaking changes and hardening ship as the first pre-release beta consumers can install.
**Depends on**: Phases 1–4 (release gate — all breaking + hardening work complete)
**Requirements**: REL-01
**Success Criteria** (what must be TRUE):

  1. A `v2.0.0-beta` tag exists and the packages are published to NuGet.org via the release workflow.
  2. A net8 consumer and a net10 consumer can install and use the beta package.
  3. All sub-packages (Core, Binders, Extensions.GenericHost, and Core.AspNet if retained) ship under the canonical `ExistForAll.SimpleSettings` identity.
  4. The full test suite passes on net8 and net10 at the tagged commit.

**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Exception Safety & Public Hierarchy | n/a (shipped) | ✓ Complete | 2026-07-14 (#27/#28) |
| 2. Binding Correctness & Engine Test Hardening | 2/2 | Complete    | 2026-07-14 |
| 3. Public Surface, Packaging & Binder Cleanup | 0/2 | Not started | - |
| 4. AOT/Trim Honesty & Documentation | 0/TBD | Not started | - |
| 5. First v2.0.0-beta Release | 0/TBD | Not started | - |
