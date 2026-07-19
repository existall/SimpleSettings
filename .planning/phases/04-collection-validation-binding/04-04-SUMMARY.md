---
phase: 04-collection-validation-binding
plan: 04
subsystem: dependency-injection
tags: [dependency-injection, validation, di-scope, exception-redaction, dotnet, tunit, api-surface]

# Dependency graph
requires:
  - phase: 04-collection-validation-binding
    provides: "Plan 03 sync validation contracts (ISettingValidation<T> DIM bridge, ValidationContext, SettingsValidationException.ThrowIfAny, SettingsValidatorInvocationException)"
provides:
  - "ISettingsCollection exposed from AddSimpleSettings: registered as a DI singleton AND surfaced via an AddSimpleSettings(out ISettingsCollection, Action?) overload (API-02/D-15)"
  - "Deferred, opt-in DI-resolved validator path: IServiceProvider.ValidateSimpleSettings() runs DI-registered ISettingValidation<T> from a fresh scope and throws the same aggregated SettingsValidationException as the core path (VAL-01 DI path / D-11)"
affects: [05-aot-docs, DOC-01]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "DI validator dispatch via the ISettingValidation<T> default-interface bridge (cast to ISettingsValidator) — no reflection over Validate; mirrors core ValuesPopulator.InvokeValidator"
    - "Fresh-scope resolution via IServiceScopeFactory.CreateScope() so scoped-dependency validators resolve under validateScopes:true (review S-1)"
    - "Shared SettingsValidationException.ThrowIfAny so the DI-path thrown contract is identical to the core path (review S-3)"
    - "Deferred opt-in runner (resolvable + IServiceProvider extension) — Abstractions-only, no IHostedService/Options dependency (Q3)"

key-files:
  created:
    - src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ISettingsValidationRunner.cs
    - src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/SettingsValidationRunner.cs
    - src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServiceProviderValidationExtensions.cs
  modified:
    - src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs

key-decisions:
  - "Dispatch uses the DIM bridge, NOT reflection (supersedes the plan's S-2 text): the runner casts each resolved validator to ISettingsValidator and calls Validate(new ValidationContext(instance)); GetServices resolves via MakeGenericType(ISettingValidation<>). Byte-identical to the shipped core InvokeValidator."
  - "The DI runner reads NO attribute: attribute-declared object/property validators ([SettingsSection].ValidatorType, [SettingsProperty].ValidatorType) are the core path and already run inline during AddSimpleSettings/ScanAssemblies. The DI path is additive (D-11): it only resolves DI-registered ISettingValidation<T>."
  - "ISettingsValidationRunner + impl are internal (architect nice-to-have): the only consumer entry point is the public ValidateSimpleSettings(); trims public surface pre-beta. Runner registered/resolved within GenericHost, no InternalsVisibleTo needed."
  - "Runner injects IServiceScopeFactory (not IServiceProvider) — states the create-scopes intent, avoids the service-locator smell, still Abstractions-only (architect nice-to-have)."
  - "Error accumulator is eagerly allocated (not the core path's lazy-null) and passed non-null to ThrowIfAny, which ArgumentNullExceptions on null; the deferred path is not hot so the lazy optimization is unneeded (architect required)."
  - "ValidateSimpleSettings resolves the runner via GetService + a caller-facing InvalidOperationException when AddSimpleSettings was not called (dotnet-kit review S1) — the internal runner type no longer leaks into the misuse message."

patterns-established:
  - "Deferred opt-in validation entry point on IServiceProvider (post-BuildServiceProvider); attribute validators stay inline, DI validators run on the explicit call"
  - "Per-test DI validators over plain suffix-discovered fixtures in the UnitTests assembly (never attribute-discovered, so they never run during a sibling test's ScanAssemblies)"

requirements-completed: [VAL-01, API-02]

coverage:
  - id: A1
    description: "ISettingsCollection is resolvable via GetRequiredService and serves the same instance the container resolves for the interface (API-02/D-15 mechanism 1)"
    requirement: "API-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs#AddSimpleSettings_RegistersSettingsCollection_ServingTheContainerInstance"
        status: pass
    human_judgment: false
  - id: A2
    description: "AddSimpleSettings(out ISettingsCollection, Action?) surfaces the built collection with bound values and preserves the IServiceCollection fluent chain (API-02/D-15 mechanism 2)"
    requirement: "API-02"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs#AddSimpleSettings_OutOverload_SurfacesBoundCollection_AndPreservesChain"
        status: pass
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs#AddSimpleSettings_OutOverload_SurfacesSameInstanceAsResolvedSingleton"
        status: pass
    human_judgment: false
  - id: A3
    description: "A DI-registered failing ISettingValidation<T> (constructor-injected) throws an aggregated SettingsValidationException carrying the error; a passing one does not throw"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs#ValidateSimpleSettings_WhenDiValidatorFails_ThrowsAggregatedException"
        status: pass
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs#ValidateSimpleSettings_WhenDiValidatorPasses_DoesNotThrow_AndReturnsProvider"
        status: pass
    human_judgment: false
  - id: A4
    description: "DI validators do NOT run during AddSimpleSettings/BuildServiceProvider (counter == 0) and run exactly once on the explicit ValidateSimpleSettings() call (counter == 1) — deferred timing (Pitfall 4)"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs#AddSimpleSettings_DoesNotRunDiValidators_UntilValidateIsCalled"
        status: pass
    human_judgment: false
  - id: A5
    description: "A validator whose constructor injects a SCOPED dependency resolves and executes under BuildServiceProvider(validateScopes:true) — proving fresh-scope resolution (review S-1)"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs#ValidateSimpleSettings_ResolvesValidatorFromFreshScope_ForScopedDependencies"
        status: pass
    human_judgment: false
  - id: A6
    description: "A throwing DI validator whose message embeds a bound secret surfaces as value-free SettingsValidatorInvocationException whose full ToString() omits the secret (D-12/T-04-VAL); the secret is asserted bound first so the test is non-vacuous"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs#ValidateSimpleSettings_WhenValidatorThrows_RedactsBoundValue"
        status: pass
    human_judgment: false
  - id: A7
    description: "ValidateSimpleSettings is a no-op returning the provider when no DI validators are registered; throws an informative error when AddSimpleSettings was never called"
    requirement: "VAL-01"
    verification:
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs#ValidateSimpleSettings_WithNoDiValidators_IsNoOp_AndReturnsProvider"
        status: pass
      - kind: unit
        ref: "src/Tests/ExistForAll.SimpleSettings.UnitTests/DependencyInjection/AddSimpleSettingsIntegrationTests.cs#ValidateSimpleSettings_WithoutAddSimpleSettings_Throws"
        status: pass
    human_judgment: false

# Metrics
duration: ~35min (incl. plan-review trio + code/test review)
completed: 2026-07-15
status: complete
---

# Phase 04 Plan 04: DI-Resolved Validator Path + ISettingsCollection Exposure

**`AddSimpleSettings` now hands back the built `ISettingsCollection` (resolvable singleton + `out`-overload), and a deferred, opt-in `IServiceProvider.ValidateSimpleSettings()` runs DI-registered `ISettingValidation<T>` from a fresh scope, throwing the same value-free aggregated `SettingsValidationException` as the core path — completing VAL-01 (DI path) and API-02.**

## Accomplishments
- **API-02 (D-15):** `IntegrateSimpleSettings` returns the built `ISettingsCollection`; it is registered as `AddSingleton<ISettingsCollection>(collection)` and surfaced by a new `AddSimpleSettings(out ISettingsCollection, Action? = null)` overload that preserves the `IServiceCollection` fluent chain. The out param and the DI singleton are the one instance the collection built.
- **VAL-01 DI path (D-11):** a new internal `ISettingsValidationRunner`/`SettingsValidationRunner` and the public `IServiceProvider.ValidateSimpleSettings()` extension run DI-registered `ISettingValidation<T>` in a deferred, opt-in post-provider-build step (the container is not built during `AddSimpleSettings` — Pitfall 4).
- **Fresh-scope resolution (S-1):** the runner resolves validators from `IServiceScopeFactory.CreateScope()`, so a validator with a scoped injected dependency resolves and executes under `BuildServiceProvider(validateScopes: true)`.
- **Contract-identical failure (S-3/D-10/D-12):** the runner dispatches via the `ISettingValidation<T>` default-interface bridge (no reflection), wraps a throwing validator value-free as `SettingsValidatorInvocationException(type, type)` (no bound value, no chained inner), and aggregates through the shared `SettingsValidationException.ThrowIfAny` — identical to the core `ValuesPopulator.InvokeValidator` by construction.

## Task Commits
1. **Task 1 — expose ISettingsCollection (API-02/D-15)** — `08a9c33` (feat)
2. **Task 2 — DI-resolved validator path (VAL-01 DI path)** — `3eb67f5` (feat)
3. **Review fixes (GSD + dotnet-kit + test-engineer findings)** — `c2b4f2c` (refactor)

## Files Created/Modified
- `ISettingsValidationRunner.cs` (created) — internal interface, `void Validate()`.
- `SettingsValidationRunner.cs` (created) — internal sealed impl; fresh scope, DIM-bridge dispatch, value-free invocation guard, shared `ThrowIfAny`.
- `ServiceProviderValidationExtensions.cs` (created) — public `ValidateSimpleSettings(this IServiceProvider)` with XML-doc documenting the opt-in/deferred contract; caller-facing error on misuse.
- `ServicesSettingsBuilderExtensions.cs` (modified) — `IntegrateSimpleSettings` returns `ISettingsCollection`; registers `AddSingleton<ISettingsCollection>` + `AddSingleton<ISettingsValidationRunner, SettingsValidationRunner>`; new `out`-overload.
- `AddSimpleSettingsIntegrationTests.cs` (modified) — 10 new tests + fixtures (3 API-02, 7 VAL-01 DI path incl. redaction + timing-counter + fresh-scope-execution + no-op + misuse).

## Decisions Made / Deviations from Plan
The written 04-04-PLAN.md was **superseded in two ways** (documented in the handoff and confirmed by the plan-review trio); the plan text was patched to match what shipped:
1. **Dispatch = DIM bridge, not reflection.** The plan's S-2 text called for `GetMethod("Validate", ...)` + `MakeGenericType` on the context. Instead, the runner casts to `ISettingsValidator` and calls `Validate(new ValidationContext(instance))` — `ISettingValidation<T>` default-implements the base `Validate`, so no reflection over `Validate` is needed (only `MakeGenericType(ISettingValidation<>)` for `GetServices`). This mirrors the post-comment-#3 core path and structurally removes the `TargetInvocationException` inner-chaining leak vector (former REVIEW MED-3).
2. **Object validator lives on `[SettingsSection].ValidatorType` (comment #3).** The DI runner reads no attribute; attribute-declared validators are the core path and already run inline during `ScanAssemblies`. D-11: the DI path is additive.

Additional refinements adopted from the plan-review trio (all architect/security nice-to-haves or required items):
- `ISettingsValidationRunner` + impl are **internal** (public surface trimmed pre-beta; only `ValidateSimpleSettings()` is public).
- Runner injects **`IServiceScopeFactory`**, not `IServiceProvider`.
- Error accumulator **eagerly allocated** and passed non-null to `ThrowIfAny` (it `ArgumentNullException`s on null).
- `ValidateSimpleSettings` throws a **caller-facing** `InvalidOperationException` (not the raw `GetRequiredService` message naming the internal runner) when `AddSimpleSettings` was not called.

## Reviews
- **Plan-review trio (up front, on the corrected design):** dotnet-architect — APPROVE-WITH-CHANGES (scope factory, internal, eager list, doc the opt-in, patch the plan); security-auditor — APPROVE-WITH-CHANGES, **T-04-VAL satisfied by construction**, confirmed the exact guard boundary (resolve outside the try; build context + Validate inside; capture `e.GetType()` only, never chain `e`), required a gating DI-path redaction test; performance-analyst — APPROVE, no benchmark-gate risk (warm populate path untouched).
- **Post-implementation review (on the Wave 3 diff):** gsd-code-reviewer — no BLOCKER/HIGH/MEDIUM, 3 LOW (fixed the internal-ID comment + trimmed comments; multi-call last-wins is pre-existing); dotnet-kit code-reviewer — no critical, adopted S1 (caller-facing error) + S2 (pin redaction precondition); dotnet-kit test-engineer — coverage adequate, adopted the invocation-counter timing probe, the scope-execution assertion, the redaction precondition, and the no-op test. Report: `04-04-REVIEW.md`.

## Carry-forward for Phase 5 / DOC-01
- **README/API docs MUST document the opt-in caveat:** DI-registered validators run **only** on the explicit `ValidateSimpleSettings()` call after `BuildServiceProvider()`; attribute-declared validators run automatically during binding. (XML-doc is already on `ValidateSimpleSettings`.)
- **Advisory to document (security):** validator **constructors** must not echo injected secrets — the D-12 echo-responsibility extends to the DI ctor surface (resolution runs outside the value-free guard).
- **Validate ⇒ discoverable coupling** (comment #3) still applies to the DI path: a type is validated by the runner only if it is scan-discovered into the `ISettingsCollection`.

## Deviations from Plan
Dispatch mechanism, runner visibility, scope-factory injection, and eager-list allocation deviate from the literal plan text as described above (superseded; plan text patched). No scope changes; both requirements (VAL-01 DI path, API-02) delivered.

## Issues Encountered
- Solution-wide `dotnet build -c Release -f net8.0` still reports NETSDK1005 on the net10-only Benchmark (pre-existing, out of scope). Authoritative `dotnet build SimpleSettings.slnx -c Release` (no `-f`) succeeds for net8.0 + net10.0 with 0 warnings.

## Verification
- `cd src && dotnet build SimpleSettings.slnx -c Release` → 0 warnings, 0 errors (net8.0 + net10.0).
- Full UnitTests suite (net10) → **153/153 pass** (was 143 on master; +10 this plan). net8 runs in CI.
- `AddSimpleSettingsIntegrationTests` (net10) → 15/15 (5 pre-existing + 10 new).
- Redaction (T-04-VAL): `ValidateSimpleSettings_WhenValidatorThrows_RedactsBoundValue` binds a sentinel secret, asserts it is bound, then asserts the thrown `SettingsValidatorInvocationException.ToString()` omits it.
- Fresh scope (S-1): the scoped-dependency validator resolves and runs (counter == 1) under `validateScopes: true`.
- No new PackageReference in GenericHost (Abstractions-only preserved).

## Next Phase Readiness
- Phase 4 code is complete (all 5 plans landed). Remaining Phase 4 steps: phase verify (gsd-verifier), security gate (/gsd-secure-phase 4, D-06 sign-off), mark complete.
- DOC-01 (Phase 5) carry-forward items listed above.
- No blockers.

## Self-Check: PASSED
- FOUND: src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ISettingsValidationRunner.cs
- FOUND: src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/SettingsValidationRunner.cs
- FOUND: src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServiceProviderValidationExtensions.cs
- FOUND: .planning/phases/04-collection-validation-binding/04-04-SUMMARY.md
- FOUND commits: 08a9c33, 3eb67f5, c2b4f2c

---
*Phase: 04-collection-validation-binding*
*Completed: 2026-07-15*
