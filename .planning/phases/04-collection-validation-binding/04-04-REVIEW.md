---
phase: 04-collection-validation-binding (Wave 3)
reviewed: 2026-07-15T00:00:00Z
depth: deep
diff_range: 4bae833..HEAD
files_reviewed: 4
files_reviewed_list:
  - src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ISettingsValidationRunner.cs
  - src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServiceProviderValidationExtensions.cs
  - src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs
  - src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/SettingsValidationRunner.cs
findings:
  blocker: 0
  high: 0
  medium: 0
  low: 3
  total: 3
status: issues_found
---

# Phase 04 Wave 3: Code Review Report

**Depth:** deep (cross-file: verified DI runner against core `ValuesPopulator.InvokeValidator`, `SettingsValidationException.ThrowIfAny`, `ISettingValidation<T>` default-interface bridge, `SettingsValidatorInvocationException`)
**Status:** issues_found (LOW only — nothing material)

## Summary

The DI-resolved validator path is a faithful mirror of the core populate path. All security-sensitive
invariants hold:

- **Redaction (LOCKED):** on a validator throw the runner rethrows value-free as
  `new SettingsValidatorInvocationException(validator.GetType(), e.GetType())` — no chained inner, no
  settings value. The bound `pair.Value` never reaches any library exception's message/ToString. Verified
  against the dedicated redaction test (`ThrowingDiValidator` embeds a sentinel secret; the surfaced
  exception is asserted clean).
- **No reflection:** dispatch is the `((ISettingsValidator)validator).Validate(new ValidationContext(pair.Value))`
  cast; the `ISettingValidation<T>` default-interface bridge forwards to the author overload. Cast is
  provably safe (services resolved for `ISettingValidation<pair.Key>` are `ISettingsValidator`).
- **Non-null aggregate:** `errors` is eagerly `new List<ValidationError>()`, so `ThrowIfAny(errors)` is
  never handed null. Empty list → no-op, matching core.
- **Fresh scope:** `IServiceScopeFactory.CreateScope()` with `using`; disposed even when `ThrowIfAny`
  throws. Singleton runner injecting singleton `ISettingsCollection` + `IServiceScopeFactory` — no captive
  dependency, no async/lifetime issue.
- **out-overload + registration:** `AddSimpleSettings(out ISettingsCollection, Action?)` returns the built
  collection and `services`; no overload ambiguity with the existing 2-arg `Action` overload (differs by
  the `out` parameter). `ISettingsCollection` + `ISettingsValidationRunner` registered as singletons.

No BLOCKER / HIGH / MEDIUM findings. Three LOW items below.

## Low

### LOW-1: Internal planning-ID reference ("See S1") in a comment — reintroduces a prior review finding

**File:** `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/SettingsValidationRunner.cs:41`
**Issue:** The catch-block comment ends with `See S1.` — an internal planning-ID reference. This is
exactly the class of comment a prior review flagged (LOW-1) and the phase convention explicitly prohibits.
(The core files carry the same references, but this diff adds a new occurrence.)
**Fix:** Drop the planning-ID tail:
```csharp
// A validator threw: surface value-free (the inner may embed a secret it read) — only the
// validator and failure types, never the instance and never a chained inner.
```

### LOW-2: Multi-line rationale comments exceed the org "one line max" guideline

**File:** `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/SettingsValidationRunner.cs:19-20, 34-35, 40-42, 53-54`
**Issue:** Four two-line comments. Borderline: this matches the established heavily-commented style of the
core files (`ValuesPopulator`, the exception family), so it is arguably an accepted project convention that
overrides the org default. Noted for consistency only; not actionable if the project style is intentional.
**Fix:** Optionally collapse each to a single line, or leave as-is to match core style.

### LOW-3: Repeated `AddSimpleSettings` calls silently validate only the last collection

**File:** `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs:51-53`
**Issue:** Each call adds another `ISettingsCollection` and `ISettingsValidationRunner` singleton. DI
last-wins, so a second `AddSimpleSettings` (different assembly set) means the runner's injected
`ISettingsCollection` is the last one — the first collection's settings are never checked by
`ValidateSimpleSettings()`. Pre-existing last-wins pattern (same as `ISettingsProvider`), and multi-call is
likely unsupported, so this is an edge-case gap, not a regression.
**Fix:** If multi-call is out of scope, no change. If it should be supported, have the runner enumerate all
registered `ISettingsCollection` instances (inject `IEnumerable<ISettingsCollection>`).

## Non-findings verified (parity preserved, intentionally not flagged)

- A validator returning `null` `ValidationResult` would NRE at `result.Errors` (outside the try). This is
  **identical** to core `InvokeValidator` (`ValuesPopulator.cs:117`); fixing only the DI side would break
  the "identical contract" guarantee. Left as-is intentionally.
- `provider.GetServices(...)` activation failures propagate raw (outside the try). Not a library exception
  and cannot carry a bound settings value — no redaction concern.
- Author-supplied `ValidationError.ErrorMessage` may reach `SettingsValidationException`'s message — by
  design (author text, not a bound value); matches core.
- `SettingsValidatorInvocationException` uses `validator.GetType()` (runtime) vs core's `validatorType`
  (declared) — per the approved constraint; both surface the concrete validator type. Not a divergence.

---

_Reviewer: Claude (adversarial code review)_
_Depth: deep_
