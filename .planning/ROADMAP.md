# Roadmap: ExistForAll.SimpleSettings

## Overview

**Status (2026-07-19):** Phases 1–4 complete (Phase 1 #27/#28, Phase 2 #30, Phase 3 #31, Phase 4 #33/#34/#35; ENG-01/T7 #29). `master` @ `0c858fa`. Active phase is **Phase 5 — Documentation** (docs-only; AOT-01 deferred to a future v2.1 milestone); first beta is Phase 6.

The binding engine already ships and works. This milestone is a hardening + pre-stable
cleanup pass that batches every remaining breaking change and safety fix before cutting the
first `v2.0.0-beta`. It starts by locking the secret-safe exception story and giving
consumers one catchable, structured exception base (Phase 1), proves binding correctness
across collection/nullable/converter shapes and closes the generator concurrency race with
tests (Phase 2), trims and corrects the public surface, packaging, and command-line binder
(Phase 3), binds collections and settings validation correctly across empty/sequence/validator
shapes (Phase 4), refreshes the docs to canonical naming and accurate, current content (Phase 5), and
finally publishes the batched result as the first beta (Phase 6). AOT/trim honesty (AOT-01) is
deferred to a future v2.1 milestone — its `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]`
annotations are additive and non-breaking, so they need not batch pre-beta. Everything serves the core
value: config → typed settings maps accurately, and never leaks a secret doing it.

## Phases

**Phase Numbering:**

- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

- [x] **Phase 1: Exception Safety & Public Hierarchy** — ✓ COMPLETE (S1 #27, C2 #28 merged 2026-07-14) - No secret leaks; one catchable, structured `SimpleSettingsException` base
- [x] **Phase 2: Binding Correctness & Engine Test Hardening** - Collections/nullable/converters verified; generator race closed by tests *(ENG-01/T7 done #29; COLL-01 + TEST-01/02/03 remain)* (completed 2026-07-14)
- [x] **Phase 3: Public Surface, Packaging & Binder Cleanup** - Meaningful public surface; per-TFM deps; correct command-line parsing (completed 2026-07-14)
- [x] **Phase 4: Collection & Validation Binding** — ✓ COMPLETE (Waves 1–3 merged #33/#34/#35, 2026-07-19) - Empty/sequence collection binding, working settings validation, and DI collection surface (client pre-beta engine requirements)
- [x] **Phase 5: Documentation** - Canonically-named, accurate, current docs (README + docs/); AOT-01 deferred to a future v2.1 milestone (completed 2026-07-20)
- [ ] **Phase 6: First v2.0.0-beta Release** - Batched breaking changes ship as an installable pre-release

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

**Plans**: 2/2 plans complete
**Wave 1**

- [x] 03-01-PLAN.md — Public surface & packaging cleanup: SettingsHolder → internal sealed (API-01), drop dead Core.AspNet package (PKG-01), per-TFM Microsoft.Extensions.* floor (PKG-02) [Wave 1]

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 03-02-PLAN.md — Command-line binder cleanup: SkipFirstArgument option + space-separated `--k v` lookahead + arg[0] skip + AddCommandLine tokenization (SRC-02) [Wave 2, depends on 03-01]

### Phase 4: Collection & Validation Binding

**Goal**: Collections bind correctly across empty, comma-scalar, and YAML-sequence shapes; declared settings validation actually runs; and the DI extension exposes the settings collection — the client-requested engine features batched before beta.
**Depends on**: Phase 3
**Requirements**: COLL-02, COLL-03, VAL-01, VAL-02, API-02
**Status**: ✓ COMPLETE — Waves 1–3 merged (#33 Waves 1–2, #34 planning, #35 Wave 3) to `master` @ `0c858fa` on 2026-07-19. All 6 success criteria MET (VERIFICATION.md — 6/6, non-vacuous tests); security gate cleared (SECURITY.md — 15/15 threats closed, D-06 secret-redaction gate signed off); suite 153/153 on net8 + net10.
**Success Criteria** (what must be TRUE):

  1. An unset `T[]` / `List<T>` / `IEnumerable<T>` binds to an empty collection, never `null` (COLL-02).
  2. A collection binds from a YAML/child-section sequence (`- a` / `- b`); the comma-scalar form still binds (prod `MultiHost__CommonHosts` unaffected); children win when both exist; each element still flows the inner converter chain (COLL-03).
  3. A settings object's declared `ISettingValidation<T>` / `[SettingsProperty(ValidatorType=...)]` is invoked in the bind pipeline, including cross-property rules (VAL-01).
  4. `[SettingsProperty(AllowEmpty=false)]` rejects empty / whitespace values at bind, not just `null` (VAL-02). *(Unsubstituted `${ENV:-}` placeholder detection deferred per CONTEXT D-13 — revisit post-beta.)*
  5. `AddSimpleSettings(...)` exposes the `ISettingsCollection` (return value or resolvable service) (API-02).
  6. After COLL-03 edits `ConfigurationBinder.BindPropertySettings`, the S1/SEC-01 secret-redaction invariant is re-verified; suite green on net8 + net10.

**Plans**: 5/5 plans complete

**Wave 1**

- [x] 04-01-PLAN.md — List<T> family conversion (COLL-01/D-01) + empty-not-null default for array/List/IEnumerable (COLL-02/D-02) [Wave 1]

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 04-02-PLAN.md — Config child-sequence binding via GetChildren() (COLL-03/D-04, D-05) + S1 sequence-element redaction regression (D-06 security gate) [Wave 2, depends on 04-01]
- [x] 04-03-PLAN.md — VAL-01 core path: sync validator contracts + SettingsValidationException + [SettingsValidator]/ValidatorType wired into the populate pipeline incl. cross-property (VAL-01) [Wave 2, depends on 04-01]
- [x] 04-05-PLAN.md — VAL-02: AllowEmpty=false rejects empty/whitespace at bind, value-free (VAL-02/D-13, D-14) [Wave 2, depends on 04-01]

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 04-04-PLAN.md — API-02 ISettingsCollection exposure (DI singleton + out-overload, D-15) + VAL-01 deferred DI-resolved validator runner (D-11/Q3, DIM-bridge dispatch) [Wave 3, depends on 04-03]

### Phase 5: Documentation

**Goal**: Consumers get accurate, canonically-named documentation — the consumer-facing README and the docs/ folder tell the truth about the current API and carry the Phase 1–4 security/behavior guidance.
**Depends on**: Phase 4
**Requirements**: DOC-01
**Success Criteria** (what must be TRUE):

  1. README uses the canonical `ExistForAll.SimpleSettings` name and links to current repo/package paths (no legacy `SimpleConfig` repo/product references; repo stays `existall/SimpleSettings`).
  2. The README code example reflects the real API (`[SettingsSection]` / `[SettingsProperty(DefaultValue=…)]`), not the stale `[DefaultValue]` form, and the install command + logo resolve.
  3. The mandated Phase 1–4 guidance is documented (secret-redaction; validators must not echo secrets, incl. constructors; opt-in/deferred DI `ValidateSimpleSettings()`; validate⇒discoverable coupling; spaced-secrets bind via `AddCommandLine`; Phase-3 breaking-change list) — concise in README, detailed in docs/.
  4. The docs/ folder carries no legacy `SimpleConfig` naming or dead `existall/SimpleConfig` links, and package metadata (`<Description>`, repo/package URLs, logo) is canonical.

**Note**: AOT-01 (annotate reflection entry points / document the AOT-trim limitation) was **deferred to a future v2.1 milestone** during Phase-5 discussion (2026-07-19). Rationale: `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` are additive, non-breaking attributes, so they need not batch into the pre-beta window; adding them post-stable is safe. See REQUIREMENTS.md (AOT-01 → Deferred).

**Plans**: 4/4 plans complete

**Wave 1** *(parallel — no file overlap)*

- [x] 05-01-PLAN.md — docs/ canonicalization: rename the extension guide to a canonical filename + repoint its 5 in-docs inbound links; drop the residual legacy parenthetical in getting_started.md (D-05) [Wave 1]
- [x] 05-02-PLAN.md — package metadata: fix `<Description>` typo + legacy `<PackageTags>` token in src/Directory.Build.props; build sanity (D-06) [Wave 1]
- [x] 05-03-PLAN.md — new deep-guidance page docs/Security.md: the 6 mandated Phase 1–4 items (secret-redaction + 2 caveats, validator secret-safety incl. constructor, opt-in/deferred `ValidateSimpleSettings()` on `IServiceProvider`, validate⇒discoverable, `AddCommandLine` spaced values, v1→v2 breaking-change list) (D-04 deep) [Wave 1]

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 05-04-PLAN.md — README full rewrite: canonical logo/title, `dotnet add package` ×3, canonical ToC (incl. renamed page + Security), correct `[SettingsProperty(DefaultValue=…)]` quickstart, trimmed positioning, concise Security + migration sections; phase-final 13-gate grep sweep + `dotnet build`/`pack` (D-01/D-02/D-03/D-04) [Wave 2, depends on 05-01/05-02/05-03]

### Phase 6: First v2.0.0-beta Release

**Goal**: All batched breaking changes and hardening ship as the first pre-release beta consumers can install.
**Depends on**: Phases 1–5 (release gate — all breaking + hardening work complete)
**Requirements**: REL-01
**Success Criteria** (what must be TRUE):

  1. A `v2.0.0-beta` tag exists and the packages are published to NuGet.org via the release workflow.
  2. A net8 consumer and a net10 consumer can install and use the beta package.
  3. All sub-packages (Core, Binders, Extensions.GenericHost, and Core.AspNet if retained) ship under the canonical `ExistForAll.SimpleSettings` identity.
  4. The full test suite passes on net8 and net10 at the tagged commit.

**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Exception Safety & Public Hierarchy | n/a (shipped) | ✓ Complete | 2026-07-14 (#27/#28) |
| 2. Binding Correctness & Engine Test Hardening | 2/2 | Complete    | 2026-07-14 |
| 3. Public Surface, Packaging & Binder Cleanup | 2/2 | Complete    | 2026-07-14 |
| 4. Collection & Validation Binding | 5/5 | ✓ Complete | 2026-07-19 (#35) |
| 5. Documentation | 4/4 | Complete   | 2026-07-20 |
| 6. First v2.0.0-beta Release | 0/TBD | Not started | - |
