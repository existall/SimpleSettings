---
phase: 04-collection-validation-binding
type: security
asvs_level: 1
block_on: high
threats_total: 15
threats_closed: 15
threats_open: 0
status: secured
verified: 2026-07-19
diff_range: master@0c858fa (Phase 4 Waves 1-3 merged)
---

# Phase 4 — Collection / Validation / Binding: Security Gate

Retroactive verification that every threat mitigation declared in the Phase 4 plan
threat models (`04-01`..`04-05-PLAN.md`) is present in the merged implementation. Each
`mitigate` threat is confirmed by locating the mitigation in code (file:line) AND a
green proving test; each `accept` threat is confirmed against the accepted-risk basis.

Config (from `04-RESEARCH.md:481`): `security_enforcement: true`,
`security_asvs_level: 1`, `security_block_on: high`.

Load-bearing invariant under test — **S1 / SEC-01:** no bound configuration value (which
may be a secret) may appear in any library exception's `ToString()` chain that reaches
logs. Every library exception is value-free (Type/property metadata only) and never chains
an inner exception that saw a value.

## Verdict

**SECURED.** 15/15 threats CLOSED. 0 blocking-open, 0 non-blocking-open. Every security
suite is green on net10; the B-1 InternalsVisibleTo gate compiles clean on net8 and net10.
The prior in-flight "by construction" sign-off on T-04-VAL (`04-04-REVIEW.md`) is confirmed
to hold in the merged tree.

## Threat Verification

| Threat ID | Wave/Plan | Category | Severity | Disposition | Status | Evidence (code + test) |
|-----------|-----------|----------|----------|-------------|--------|------------------------|
| T-04-01 | W1 / 04-01 | Info Disclosure | medium | mitigate | CLOSED | Element-convert failure wrapped value-free at `ValuesPopulator.cs:187-191` (`SettingsPropertyValueException(type, property, e.GetType())`, no inner chained); `ListTypeConverter`/`CollectionTypeConverter` add no value to any message. Test: `ExceptionRedactionTests.Convert_SecretInListSequenceElement_DoesNotLeakValue` (8/8 green). |
| T-04-02 (W1) | W1 / 04-01 | Tampering | low | mitigate | CLOSED | `TypeConvertersCollections.cs:11` registers `ListTypeConverter` after `EnumerableTypeConverter`, before `EnumTypeConverter`; disjoint `IsListLike` predicate (`ListTypeConverter.cs:23-26`); user-first/Default-last order preserved. Test: `CollectionConversionTests` (green). |
| T-04-S1 (D-06) | W2 / 04-02 | Info Disclosure | **high** | mitigate | CLOSED | **SECURITY GATE.** Sequence walk `ConfigurationBinder.TrySetChildSequence` (`ConfigurationBinder.cs:58-75`) only collects child strings + `SetNewValue` — no conversion inside the binder, so the element-convert failure never lands in the inner-chaining `SettingsBindingException`; it surfaces later via `ValuesPopulator.ConvertPropertyValue` (`:179-193`) as value-free `SettingsPropertyValueException`. Tests: `Convert_SecretInSequenceElement_DoesNotLeakValue`, `Convert_SecretInFirstSequenceElement_DoesNotLeakValue`, `Convert_SecretInListSequenceElement_DoesNotLeakValue` — first + later element, int[] + List<int> (8/8 green). |
| T-04-02 (W2) | W2 / 04-02 | Tampering | medium | mitigate | CLOSED | Children-win precedence: `ConfigurationBinder.cs:35` calls `TrySetChildSequence` first and returns early when children exist; comma-scalar path preserved (`:38-49`). Test: `SettingsBuilderConfigurationBinderIntegrationTests` (14/14 green). |
| T-04-03 | W2 / 04-02 | Denial of Service | low | accept | CLOSED | Sequence length is operator-controlled config, no untrusted remote input; consistent with the pre-existing scalar-binding exposure. No code required. |
| T-04-VAL (core) | W2 / 04-03 | Info Disclosure | **high** | mitigate | CLOSED | Aggregate message composed only from author `ValidationError` text (`Resources.cs:56-68`), no bound value; `SettingsValidationException` retains only `Errors` (`SettingsValidationException.cs`). Throwing-validator vector wrapped value-free as `SettingsValidatorInvocationException(validatorType, e.GetType())` — no inner chained (`ValuesPopulator.cs:110-115`). Tests: `Exception_CarriesAuthorMessage_AndNoBoundValue`, `ThrowingValidator_IsWrappedValueFree_NotLeaked` (12/12 green). |
| T-04-04 | W2 / 04-03 | Tampering | low | mitigate | CLOSED | Validator type comes from a compile-time attribute; parameterless `Activator.CreateInstance` (`ValuesPopulator.cs:107`). No untrusted-type input. |
| T-04-05 | W2 / 04-03 | Denial of Service | low | accept | CLOSED | Validators are operator-authored startup code; failures fail-fast at populate. Acceptable startup behavior. |
| T-04-VAL (DI) | W3 / 04-04 | Info Disclosure | **high** | mitigate | CLOSED | DI runner mirrors the core contract: aggregates via the SHARED `SettingsValidationException.ThrowIfAny` (`SettingsValidationRunner.cs:52`); throwing validator wrapped value-free `SettingsValidatorInvocationException(validator.GetType(), e.GetType())`, no inner chained (`:40-44`). Dispatch is the DIM bridge `((ISettingsValidator)validator).Validate(...)` (`:38`) — no `MethodInfo.Invoke`, so no `TargetInvocationException` inner-chaining vector. Test: `ValidateSimpleSettings_WhenValidatorThrows_RedactsBoundValue` (guards non-vacuous: asserts secret IS bound first), `ValidateSimpleSettings_WhenDiValidatorFails_ThrowsAggregatedException` (15/15 green). |
| T-04-06 | W3 / 04-04 | Elevation of Privilege | medium | mitigate | CLOSED | DI validators run only via explicit `IServiceProvider.ValidateSimpleSettings()` (`ServiceProviderValidationExtensions.cs:20-30`) after `BuildServiceProvider()`; registration only adds the runner singleton (`ServicesSettingsBuilderExtensions.cs:53`), never invokes it. Test: `AddSimpleSettings_DoesNotRunDiValidators_UntilValidateIsCalled` (counter 0 → 1). |
| T-04-07 | W3 / 04-04 | Spoofing | low | accept | CLOSED | Validators resolve from the consumer's own DI container via `GetServices(typeof(ISettingValidation<>).MakeGenericType(pair.Key))` (`SettingsValidationRunner.cs:27-29`); no untrusted type input crosses the boundary. |
| T-04-SC | W1-W3 (all) | Supply Chain | n/a | accept | CLOSED | No package installs this phase. GenericHost csproj carries only `Microsoft.Extensions.DependencyInjection.Abstractions` + the Core `ProjectReference` (`...GenericHost.csproj:7-13`) — no hosting dependency added. Binders reuses Core via IVT, not a new package (`Info.cs:5`). |
| T-04-08 | W2 / 04-05 | Info Disclosure | low | mitigate | CLOSED | AllowEmpty=false rejection reuses value-free `SettingsPropertyNullException(_propertyName)` (`PropertyConversion.cs:34-35, 43-44`; message = property name only, `Resources.cs:44-45`). Test: `SettingsPropertyTests` (6/6 green). |
| T-04-09 | W2 / 04-05 | Tampering | medium | mitigate | CLOSED | `SettingsPropertyNullException` stays excluded from the redaction filter by construction (`ValuesPopulator.cs:187` `when (e is not SettingsPropertyNullException)`); every other exception is wrapped into value-free `SettingsPropertyValueException`, so no value-bearing type bypasses redaction and the empty diagnostic is never over-redacted. |

## New Attack Surface Introduced During Implementation (unregistered flags — non-blocking)

The SUMMARY files carry no `## Threat Flags` section. Two items of new surface were detected
by direct code inspection:

- **`SettingsValidatorInvocationException` (new public type, `SettingsValidatorInvocationException.cs`)** —
  not named by ID in any plan threat register (the registers named only `SettingsValidationException`).
  It is in fact the *primary* leak vector for both validator paths: a validator that reads a secret and
  then throws with it in its own message. The implementation handles it correctly and value-free —
  type-only ctor, `Resources.cs:70-73` message names only the validator + failure types, and neither
  call site chains the inner (`ValuesPopulator.cs:114`, `SettingsValidationRunner.cs:43`). It is covered
  by the T-04-VAL mitigation intent and pinned by two dedicated redaction tests (core + DI). Informational
  only — mitigated and tested; no action required.
- **Object-level validator declaration moved to the existing `[SettingsSection(ValidatorType = ...)]`**
  (`ValuesPopulator.cs:167-168`) rather than the new `SettingsValidatorAttribute` that plan 04-03 Task 1
  described. Implementation deviation from the plan text; no security impact — the value-free exception
  contract is independent of which attribute declares the validator. Informational.

## Accepted / Residual Risks

- **Author-supplied `ValidationError.ErrorMessage` reaches `SettingsValidationException.ToString()`** — by
  design (author text, not a bound value; `Resources.cs:56-68`). If a validator author echoes a secret
  into their own error message it will surface. This is the documented validator-author responsibility
  stated in the T-04-VAL mitigation plan. Accepted residual (author boundary, not a library defect).
- **A validator returning a `null` `ValidationResult` would NRE at `result.Errors`** (outside the try in
  both paths). Identical behavior core vs DI; an NRE carries no bound settings value. Accepted (parity
  preserved intentionally — `04-04-REVIEW.md` non-findings).
- **`provider.GetServices(...)` activation failures propagate raw** (`SettingsValidationRunner.cs:29`,
  outside the try). Not a `SimpleSettingsException` and cannot carry a bound settings value. Accepted.
- **Repeated `AddSimpleSettings` validates only the last collection** (`04-04-REVIEW.md` LOW-3). DI
  last-wins edge case, pre-existing pattern, multi-call likely unsupported. Not a security regression.

## D-06 Sign-off (mandatory security gate, T-04-S1, severity high)

**SIGNED OFF — CLOSED.** A secret carried in a YAML / child-section sequence element is provably absent
from the entire exception `ToString()` chain on a bind/convert failure. The child-sequence walk
(`ConfigurationBinder.TrySetChildSequence`) never converts inside the binder, so the value-bearing
converter exception never reaches the inner-chaining `SettingsBindingException`; it is caught and rewrapped
value-free as `SettingsPropertyValueException(settingsType, property, e.GetType())` at
`ValuesPopulator.cs:187-191` (no message value, no chained inner). This is proven across both element
positions (first + later) and both converter shapes (int[] + List<int>) by three green regression tests in
`ExceptionRedactionTests`, and the whole redaction suite (5 pre-existing + 3 new) is green on net10.

## Verification Runs (merged tree, net10.0 Release)

- `ExceptionRedactionTests` — 8/8 pass (D-06 gate: 5 pre-existing S1 + 3 new sequence).
- `SettingsValidationTests` — 12/12 pass (core-path VAL, aggregate, cross-property, throwing-validator redaction).
- `AddSimpleSettingsIntegrationTests` — 15/15 pass (DI VAL, deferred timing, fresh-scope, DI throwing-validator redaction).
- `SettingsPropertyTests` — 6/6 pass (VAL-02 empty/whitespace rejection, value-free).
- `SettingsBuilderConfigurationBinderIntegrationTests` — 14/14 pass (children-win, comma-scalar, empty→empty, prefix).
- B-1 IVT gate: `ExistForAll.SimpleSettings.Binders` + `...Extensions.GenericHost` build clean on net8.0 AND net10.0 (no CS0122 — Core's `internal IsCollectionShape` is visible via the `Info.cs:5` grant).

Implementation files were treated as READ-ONLY; only this SECURITY.md was written.
