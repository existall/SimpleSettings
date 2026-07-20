# Phase 5: Documentation - Context

**Gathered:** 2026-07-19
**Status:** Ready for planning

<domain>
## Phase Boundary

Make the consumer-facing documentation **accurate and canonically-named**: refresh the root
`README.md` (which ships to nuget.org via `PackageReadmeFile`) and the `docs/` folder so they tell
the truth about the current API, use the canonical `ExistForAll.SimpleSettings` naming and current
`existall/SimpleSettings` links, and carry the Phase 1–4 security/behavior guidance. Requirement
**DOC-01** is locked by ROADMAP; this discussion settled **HOW**.

**Scope change (this discussion):** **AOT-01 was removed from Phase 5 and deferred to a future v2.1
milestone.** The `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` annotations are additive and
non-breaking, so they need not batch into the pre-beta window; adding them post-stable is safe.
Recorded in `.planning/ROADMAP.md` (Phase 5 note) and `.planning/REQUIREMENTS.md` (AOT-01 → Deferred).
Phase 5 was renamed **"AOT/Trim Honesty & Documentation" → "Documentation"** (slug `05-documentation`).

**Not in scope:** AOT-01 (deferred to v2.1); REL-01 / cutting the beta (Phase 6); validator-dispatch
caching (deferred perf, code-review M2 — a code change, not docs); any runtime/behavior change.
</domain>

<decisions>
## Implementation Decisions

### README shape — modernize, self-contained
- **D-01:** Rewrite `README.md` to a modern, self-contained OSS shape: (1) canonical title (drop the
  "(previously SimpleConfig)" tag); (2) **install** via `dotnet add package ExistForAll.SimpleSettings`
  (plus `.Binders` / `.Extensions.GenericHost` where relevant), replacing the legacy `Install-Package`
  PMC command; (3) a ~30-second **quickstart** with a CORRECT minimal example; (4) a short **feature
  overview**; (5) **links into `docs/`** for depth. Trim the long "why `IOptions<>` is bad" polemic to a
  tight blurb — keep the positioning, drop the wall of text.
- **D-02 (correctness, not just naming):** The README code example MUST reflect the **real API** — a
  settings **interface** (discovered via `ISettingsSection` base / `[SettingsSection]` / `Settings`
  suffix) with **`[SettingsProperty(DefaultValue = "…")]`**, NOT the stale `[DefaultValue("…")]` shown
  today (that attribute form is factually wrong). Verify every API token against current source before
  writing (see research flags).
- **D-03:** Fix all consumer-facing surface in the README: the broken `existall/Shepherd` logo, every
  dead `existall/SimpleConfig/blob/master/docs/*` ToC link (repoint to `existall/SimpleSettings`), the
  install command, and prose typos.

### Guidance placement — concise in README, deep in docs/
- **D-04:** The README carries a **concise** "Security notes" + "Breaking changes / migration" section
  (visible to every nuget.org reader); the **deep detail lives in `docs/`**. The mandated content (LOCKED
  by the Phase-4 security sign-off — `[[04-CONTEXT]]` D-06/D-12, Phase 4 `SECURITY.md`) that MUST be
  documented:
  - **Secret-redaction invariant** — conversion/bind failures never surface the bound value or chain a
    value-bearing inner (S1/SEC-01).
  - **Validator authors must not echo secrets** — neither in `ValidationError` message text NOR in
    **validator constructors** (DI resolution runs *outside* the value-free guard, so a ctor that logs an
    injected secret leaks it).
  - **DI-path `ValidateSimpleSettings()` is opt-in / deferred** — must be called *after*
    `BuildServiceProvider()`; attribute / `ValidatorType` validators run **inline** in the bind pipeline.
  - **validate ⇒ discoverable coupling** — `[SettingsSection(ValidatorType=…)]` (object-level validator)
    also makes the type scan-discovered.
  - **Spaced secrets bind via `AddCommandLine`** — a quoted value containing spaces arrives as its own
    token; document the `--key value` lookahead + `arg[0]` skip behavior (SRC-02).
  - **Phase-3 breaking-change list** — `SettingsHolder`/`ISettingsHolder` internal (API-01); `Core.AspNet`
    package dropped (PKG-01); per-TFM `Microsoft.Extensions.*` floor (PKG-02); public exception hierarchy
    (`SimpleSettingsException` base, EXC-01).

### docs/ — refresh all six in place
- **D-05:** Update **all six** `docs/` files in place: purge `SimpleConfig` product naming + dead
  `existall/SimpleConfig` links (repoint to `existall/SimpleSettings`), **rename `docs/Extend Simple
  Config.md`** to a canonical name (e.g. `Extending SimpleSettings.md`) and fix inbound links, correct
  stale API references (e.g. `[DefaultValue]`), and add the deep security/behavior + migration content the
  README summarizes. Preserve existing page structure/history (in-place, not a restructure).

### Metadata — fix alongside docs
- **D-06:** Fix `src/Directory.Build.props` `<Description>` typos ("appliaction" → "application"; the
  "decouples the frameworks from your appliaction" prose), canonicalize/replace the broken
  `existall/Shepherd` logo reference, and verify `<RepositoryUrl>`/`<PackageProjectUrl>` (already
  `existall/SimpleSettings`) + the three `ExistForAll.*` NuGet package links resolve.

### Claude's Discretion
- Exact README section ordering/wording and how far to trim the IOptions polemic; the precise canonical
  name for the renamed `Extend Simple Config.md`; whether "migration" is a README subsection vs a dedicated
  `docs/` page; which `docs/` page holds the deep security guidance; whether to add a `CHANGELOG.md`;
  whether to add a CI legacy-reference / dead-link check (nice-to-have — not required by the criteria).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirement / roadmap / decision source
- `.planning/ROADMAP.md` — Phase 5 "Documentation" goal + 4 success criteria + the AOT-01 deferral note
- `.planning/REQUIREMENTS.md` — DOC-01 (active) + AOT-01 (v2 → Deferred) + traceability
- `SESSION-HANDOFF.md` (repo root) — the authoritative **DOC-01 must-include list** and cadence rules
- `.planning/phases/04-collection-validation-binding/SECURITY.md` — the redaction invariant + T-04-VAL DI
  path sign-off (authoritative for the security guidance the docs must carry)
- `.planning/phases/04-collection-validation-binding/04-CONTEXT.md` — D-06/D-12 (redaction), D-11 (discovery
  mechanisms / validate⇒discoverable), D-15 (API-02 `ISettingsCollection`)
- `.planning/phases/03-public-surface-packaging-binder-cleanup/03-CONTEXT.md` — Phase-3 breaking-change
  detail (API-01, PKG-01, PKG-02, SRC-02) for the migration section

### Files to edit
- `README.md` — the refresh target (ships to NuGet via `PackageReadmeFile`)
- `docs/getting_started.md`, `docs/building_the_collection.md`, `docs/Build Config Interface.md`,
  `docs/Default Values.md`, `docs/Build a SectionBinder.md`, `docs/Extend Simple Config.md` (rename) — all six
- `src/Directory.Build.props:16-18,23` — `<Description>` (typos), `<PackageProjectUrl>`, `<RepositoryUrl>`,
  `<PackageReadmeFile>` (metadata + logo)

### Current-API ground truth (so every example/claim is accurate — VERIFY against these, do not trust the old docs)
- `src/Core/ExistForAll.SimpleSettings/SettingsBuilder.cs` — `CreateBuilder` / `ScanAssemblies` / `GetSettings` (direct API)
- `src/Core/ExistForAll.SimpleSettings.Extensions.GenericHost/ServicesSettingsBuilderExtensions.cs` — `AddSimpleSettings(...)`, the API-02 `ISettingsCollection` exposure, and the opt-in `ValidateSimpleSettings()` (DI validation path)
- `src/Core/ExistForAll.SimpleSettings/SettingsPropertyAttribute.cs` — `DefaultValue`/`Name`/`ConverterType`/`AllowEmpty`/`ValidatorType` (grounds the corrected example)
- `src/Core/ExistForAll.SimpleSettings/SettingsSectionAttribute.cs` — `[SettingsSection]` incl. object-level `ValidatorType` (validate⇒discoverable)
- `src/Core/ExistForAll.SimpleSettings/ISettingsCollection.cs` — API-02 public surface
- `src/Core/ExistForAll.SimpleSettings/Validations/` — `ISettingValidation.cs`, `ISettingsValidator.cs`, `ValidationError.cs`, `ValidationResult.cs` (validator authoring; the secret-safety guidance)
- `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/CommandLineSettingsBinder.cs` + `CommandLineSettingsBinderOptions.cs` — spaced-value binding / `AddCommandLine` / `SkipFirstArgument`
- Exception hierarchy (secret-redaction): `SimpleSettingsException`, `SettingsPropertyValueException`, `SettingsPropertyNullException`, `SettingsValidationException`, `SettingsValidatorInvocationException` (Core project; the invariant is authoritative in Phase 4 `SECURITY.md`)
- `.planning/codebase/CONVENTIONS.md`, `.planning/codebase/STRUCTURE.md` — naming/structure so doc examples match house style
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`docs/` folder (6 pages)** already exists — refresh in place rather than starting from scratch.
- **README is packaged** (`PackageReadmeFile` in `Directory.Build.props`) — so the README refresh is a
  real *package-content* change that ships in the `.nupkg`, not throwaway planning docs.

### Established Patterns
- Canonical identity: namespace/packages `ExistForAll.SimpleSettings` (+`.Binders`, `.Extensions.GenericHost`);
  repo `existall/SimpleSettings`. The **`existall` org stays** — only the legacy `SimpleConfig` repo/product
  name is purged.
- Settings types are **interfaces** (proxy generator can't implement internal interfaces — keep example
  interfaces `public`). Modern C#, block-scoped namespaces, `net8.0;net10.0`.

### Integration Points
- `README.md` → nuget.org package page (`PackageReadmeFile`) and the GitHub repo landing page.
- README ToC / "learn more" links → `docs/` pages (must resolve to `existall/SimpleSettings` paths).
- `Directory.Build.props` metadata → the published package (`<Description>`, URLs, logo, README).

### Research flags (for gsd-phase-researcher / gsd-planner)
- **Verify every API token against current source** before writing examples — the *current* README example
  (`[DefaultValue]`) is already wrong. Confirm the exact public entry-point signatures, `[SettingsProperty]`
  members, and especially the **`ValidateSimpleSettings()`** API shape/usage (Phase 4 Wave 3) — name/receiver
  must match source, not this doc's paraphrase.
- **Acceptance sweep:** a legacy-reference grep (`existall/SimpleConfig`, standalone `SimpleConfig`,
  `Install-Package`, `[DefaultValue]`) across README + docs/ + `Directory.Build.props` should come back clean;
  links should resolve. Planner may wire this as the DOC-01 verification.
</code_context>

<specifics>
## Specific Ideas
- The current README's `[DefaultValue("SomeUrl")]` example is a **factual bug**, not just a naming issue —
  the real API is `[SettingsProperty(DefaultValue = "…")]`. Treat doc accuracy as a first-class goal.
- **Phase 5 earns a real master alpha (not a throwaway doc-only push):** because the README is packaged
  (`PackageReadmeFile`) and `<Description>` ships in the `.nupkg`, this phase changes *package content* —
  so its PR is a legitimate release-worthy master merge. The **held Phase-4 closeout docs** on branch
  `gsd/phase-4-closeout` (VERIFICATION.md, SECURITY.md, ROADMAP/STATE) should **ride to master on Phase 5's
  PR** — branch Phase 5 off `gsd/phase-4-closeout` (or cherry-pick those docs onto the Phase-5 branch). See
  `[[simplesettings-handoff-workflow]]`.
- Ship on a feature branch → PR via the **`guy-lud`** account (never commit to `master`; `origin` uses the
  `github-guy-lud` SSH alias). See `[[simplesettings-push-access]]`.
- Standing review rule still applies but be proportional for a docs phase: plan-review is light; the payoff
  is a careful accuracy/security-content read of the finished docs (a `security-auditor`/`code-reviewer`
  pass confirming no secret-echoing examples and that the mandated guidance is present and correct).
</specifics>

<deferred>
## Deferred Ideas
- **AOT-01** — annotate reflection entry points (`[RequiresDynamicCode]`/`[RequiresUnreferencedCode]`)
  and/or document the AOT/trim limitation. **Deferred to a future v2.1 milestone** (this discussion).
  Non-breaking to add later. Recorded in ROADMAP + REQUIREMENTS.
- **Validator-dispatch caching** — deferred perf follow-up (code-review M2). A code change, not docs; not
  Phase 5.
- **REL-01** — cut the first `v2.0.0-beta`. Phase 6.
- **EQ-01** (HELD), **PERF-03** (deferred), **`${ENV:-}` placeholder detection** (VAL-02/D-13, deferred) —
  unchanged; out of scope.

None of the above block Phase 5.
</deferred>

---

*Phase: 05-documentation*
*Context gathered: 2026-07-19*
