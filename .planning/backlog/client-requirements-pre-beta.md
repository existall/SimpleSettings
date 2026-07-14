# Client requirements — pre-beta engine features (BACKLOG)

**Source:** client (via owner), captured 2026-07-14
**Status:** BACKLOG — to be routed into ROADMAP/REQUIREMENTS after Phase 3 completes.
**Intended window:** before the first `v2.0.0-beta` (this milestone batches breaking changes pre-beta).

> Relationship to existing deferrals: **#1 and #2 pull `COLL-01` (List<T> support) forward and expand it**; **#3 is the held `D1 Validations`**. Recommend inserting a dedicated engine phase before the beta and renumbering the beta phase last. Provisional requirement IDs proposed below — finalize during roadmap integration.

---

## COLL-02 — Empty-collection default *(cheapest; pure win; no back-compat risk)*
- **What:** an unset `T[]` / `List<T>` currently binds `null`; only `IEnumerable<T>` binds empty. Make all three default to an **empty collection, never null**.
- **Where:** `TypeConverter.ConvertValue` null branch. Root cause: `IsEnumerable()` matches only the open `IEnumerable<>` typedef, so arrays / `List<T>` fall through to `return null`. Return `Array.CreateInstance(elementType, 0)` / a new `List<T>` for those.
- **Done when:** unset `T[]`, `List<T>`, `IEnumerable<T>` all bind empty. ~1–2 lines.
- **Unblocks:** gateway drops every `?? []` guard.

## COLL-03 — YAML-sequence / indexed-child binding *(headline readability fix)*
- **What:** collections bind only from a comma-scalar (`"a,b,c"`) today; a readable YAML `- a` / `- b` sequence is silently dropped. Make the binder read **sequence children**.
- **Where:** `ConfigurationBinder.BindPropertySettings` reads a single scalar `section[key]` before conversion runs, so child sections (`key:0`, `key:1`, …) never reach a converter — a converter can't fix this, the **binder** must. Enumerate `GetChildren()` for collection targets.
- **Load-bearing (must keep working):** the comma-scalar form MUST still bind — prod env-var overrides (`MultiHost__CommonHosts`) depend on it.
- **Sub-rules:** children win if both a scalar and children exist; empty / whitespace / empty-sequence → empty (never `[""]`); each element still flows the inner converter chain (so `int[]` / enum arrays keep working); honor `[SettingsSection]` / root prefixing exactly as the scalar path.
- **Unblocks:** revert comma-scalar hacks back to readable sequences in the gateway (`PublicRoute:Routes`, `MultiHost:CommonHosts`, CORS).
- **⚠ Security note:** this changes `BindPropertySettings`, the redaction-critical method Phase 3 left untouched (runs inside the `SettingsBindingException` chaining catch). The S1/SEC-01 secret-redaction invariant MUST be re-verified when this lands.

## VAL-01 — Working class-level / settings-object validation *(= held D1)*
- **What:** `SettingsPropertyAttribute.ValidatorType` and `ISettingValidation<T>` are declared but **never invoked** anywhere in 1.0.0 — dead scaffolding. Wire them into the bind pipeline so a settings object can validate itself (incl. cross-property rules).
- **Where:** invoke the declared validator after conversion/population in the bind flow (today nothing calls it).
- **Unblocks:** gateway Story 8.8 — replace the interim FluentValidation `SpiceDbSettingsValidator` (the PR #103 boot-hook workaround) with the native mechanism.

## VAL-02 — Tighter `AllowEmpty` *(lowest urgency — consumers already fail closed)*
- **What:** `[SettingsProperty(AllowEmpty=false)]` rejects only `null` at bind; an explicit `""`, whitespace, or an unsubstituted `${ENV:-}` placeholder binds clean. Reject those too.
- **Where:** `TypeConverter.ValidateNullAcceptance` (the current null-only check). Naturally rides on VAL-01 if expressed as an `ISettingValidation`.
- **Value:** converts a late runtime failure into a clean startup Fatal. Low urgency because every gateway consumer already denies-all on a blank secret.

## API-02 — Expose `ISettingsCollection` from the DI extension
- **What:** the `AddSimpleSettings` DI extension method should, in the end, expose the `ISettingsCollection` somehow.
- **Where:** DI registration extension (`AddSimpleSettings`).
- **Note:** bundled with the client's item #4; scope/shape (return value vs. resolvable service) to be decided during planning.

---

## Recommended integration (after Phase 3)
1. `/gsd-new-milestone` OR extend the current milestone: add these as REQUIREMENTS.md entries (finalize IDs).
2. Insert a **new engine phase** (collections + validation) before the beta; renumber: current Phase 4 (AOT/docs) and Phase 5 (beta) shift out so the beta stays last.
3. Discuss-phase for the new phase (COLL-02/03 sequencing, VAL-01 pipeline hook point, the S1 re-verification gate for COLL-03).
