# Phase 4: Collection & Validation Binding - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-15
**Phase:** 04-collection-validation-binding
**Areas discussed:** Collection scope (List<T>), VAL-01 validation model, VAL-02 empty semantics, API-02 exposure shape

---

## Collection scope — is `List<T>` in?

| Option | Description | Selected |
|--------|-------------|----------|
| Full: add List<T> family | Absorb COLL-01: one converter materializes List<T> and satisfies List<T>/IList<T>/ICollection<T>/IReadOnlyList<T>/IReadOnlyCollection<T>; broaden IsEnumerable/element detection. Meets ROADMAP + client doc which both name List<T>. | ✓ |
| Arrays + IEnumerable<T> only | Cheapest; List<T> stays deferred; requires walking back the List<T> wording and leaves an incoherent "empty List<T>" half-state. | |

**User's choice:** Full: add List<T> family
**Notes:** Confirmed no converter matches List<T> today (only ArrayTypeConverter/EnumerableTypeConverter), so COLL-02/COLL-03's List<T> behavior requires real conversion support. COLL-01 (owner-deferred since Phase 2) is pulled into Phase 4.

---

## VAL-01 — validation model

### Q1: async vs sync signature

| Option | Description | Selected |
|--------|-------------|----------|
| Redesign to sync | `ValidationResult Validate(...)`; hooks cleanly into the sync startup populate path, no sync-over-async hazard; free pre-beta (interfaces are dead). | ✓ |
| Keep async | Preserve Task<ValidationResult>; supports I/O validators but forces sync-over-async or an async pipeline ripple. | |

**User's choice:** Redesign to sync

### Q2: failure behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Aggregate, throw one exception | Collect every ValidationError, throw one SettingsValidationException : SimpleSettingsException; operator sees all problems at once. | ✓ |
| Fail-fast on first error | Throw on first invalid; simplest but slower multi-field debug loop. | |

**User's choice:** Aggregate, throw one exception

### Q3: object-level validator discovery

| Option | Description | Selected |
|--------|-------------|----------|
| Attribute + Activator | `[SettingsValidator(typeof(X))]` on the interface, Activator-instantiated; works in both paths; no injected deps. | |
| DI-resolved | Container-registered ISettingValidation<T>; supports injected deps but DI-path only. | |
| Both: attribute + optional DI | Attribute validators always run in the core path; DI-registered validators also resolve when a container is present. Most flexible. | ✓ |

**User's choice:** Both: attribute + optional DI
**Notes:** Research flag captured — DI-path validator timing/ordering (validators with service deps) and keeping aggregate+redaction identical across both sites.

---

## VAL-02 — what counts as "empty"

### Q1: scope

| Option | Description | Selected |
|--------|-------------|----------|
| "" + whitespace only | Reject null + empty + whitespace; skip the ${ENV:-} placeholder heuristic (lowest-urgency, false-positive risk). | ✓ |
| + unsubstituted ${ENV:-} | Also reject unresolved ${VAR}/${VAR:-}; meets full written ask but needs a precise heuristic. | |

**User's choice:** "" + whitespace only

### Q2: placement

| Option | Description | Selected |
|--------|-------------|----------|
| Inline at conversion layer | Extend PropertyConversion.Convert's _throwOnNull branch; local, trivial, value-free exception at conversion. | ✓ |
| Built-in validator (rides VAL-01) | Express as an ISettingValidation flowing through the aggregate exception; more indirection for a one-line check. | |

**User's choice:** Inline at conversion layer

---

## API-02 — exposing `ISettingsCollection`

| Option | Description | Selected |
|--------|-------------|----------|
| Resolvable DI singleton | AddSingleton<ISettingsCollection>; non-breaking, keeps fluent chain. | (part of choice) |
| Return value from the method | Hand back the collection directly; breaks IServiceCollection chaining or needs an overload. | (part of choice) |
| Both DI + provider surface | Register DI singleton + expose on ISettingsProvider; widest reach, largest surface. | |

**User's choice:** Free-text — "we need to try them both and decide" → resolved to: implement both the DI singleton and a return-value overload, keep existing IServiceCollection overloads, decide the final shape at review on the branch (path A over a separate spike).
**Notes:** Reversal is free pre-beta, so building both and choosing empirically is low-risk.

## Claude's Discretion

- Exact new type/method names (SettingsValidationException, [SettingsValidator], VAL-02 exception type, API-02 return overload signature).
- Test file placement; the precise List<T>-materialization mechanism (wrap array vs. build list directly).

## Deferred Ideas

- `${ENV:-}` unsubstituted-placeholder detection (VAL-02) — deferred.
- AOT-01 / DOC-01 — Phase 5; REL-01 — Phase 6; D2 EqualityCompererCreator — HELD.
</content>
