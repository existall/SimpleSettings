# Phase 3: Public Surface, Packaging & Binder Cleanup - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-14
**Phase:** 3-public-surface-packaging-binder-cleanup
**Areas discussed:** Core.AspNet (PKG-01), Microsoft.Extensions floor (PKG-02), CLI binder (SRC-02), SettingsHolder (API-01)

---

## Core.AspNet (PKG-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Drop the package | Remove Core.AspNet from the .slnx/solution; nothing references it publicly; ASP.NET consumers already get these constants from Microsoft.Extensions.Hosting. | ✓ |
| Make Environments public | Keep the package; flip the class to `public static` so the 3 constants become its consumable surface. | |
| Fold into GenericHost, drop pkg | Move Environments (public) into the existing GenericHost package, then remove the standalone Core.AspNet package. | |

**User's choice:** Drop the package.
**Notes:** Core.AspNet holds one `internal static class Environments` (3 const strings) → zero public surface shipped, duplicating ASP.NET Core's own `Environments` constants. Removal touches `SimpleSettings.slnx:13` and `UnitTests.csproj:19` (ProjectReference).

---

## Microsoft.Extensions floor (PKG-02)

| Option | Description | Selected |
|--------|-------------|----------|
| Float per-TFM, 8.0.0 net8 floor | Conditional PackageVersion: net8 → 8.0.0 (lowest), net10 → 10.0.x. Widest net8 compatibility. | |
| Float per-TFM, latest 8.0.x | Same split but require a recent 8.0.x floor (picks up security patches). | ✓ |
| Keep 10.0.9 pin + document | Leave both TFMs on 10.0.9 with a justification note; net8 consumers stay forced onto 10.x. | |

**User's choice:** Float per-TFM, latest 8.0.x floor for net8 (10.0.x for net10).
**Notes:** All four `Microsoft.Extensions.*` currently pinned `10.0.9` in `Directory.Packages.props`. Exact latest 8.0.x patch to be resolved by research/planning.

---

## CLI binder (SRC-02)

| Option | Description | Selected |
|--------|-------------|----------|
| Add --key value + skip arg[0] | Lookahead-pair a prefixed key with the next token as its value; keep inline `=`/`:`; skip arg[0] by default. | ✓ |
| Add --key value, arg[0]-skip opt-in | Same space-separated support, but arg[0]-skip off by default. | |
| Minimal: arg[0] skip only | Skip arg[0], keep inline-delimiter-only parsing; document that `--key value` isn't supported. | |

**User's choice:** Add space-separated `--key value` + skip arg[0] by default.
**Notes:** Today the binder only parses inline `--key=value`/`--key:value`; a space-separated `--key value` (how a quoted value with spaces arrives, as its own token) is silently dropped. A following token starting with a prefix is treated as a new key, not a value. `arg[0]` skip exposed as a default-on option toggle.

---

## SettingsHolder (API-01)

| Option | Description | Selected |
|--------|-------------|----------|
| internal sealed class | Flip to `internal sealed`; tests reach it via InternalsVisibleTo. Breaking, but free pre-stable. | ✓ |
| internal (not sealed) | Flip to internal but leave inheritable. | |
| Keep public | Only if an external consumer references it directly (unlikely pre-stable). | |

**User's choice:** `internal sealed class`.
**Notes:** `ISettingsHolder` is already internal; only `SettingsCollection` uses `SettingsHolder`. `InternalsVisibleTo` covers UnitTests + Benchmark.

## Claude's Discretion

- Exact CPM conditional syntax and the precise latest 8.0.x version (PKG-02).
- Option naming for the arg[0] toggle (e.g. `SkipFirstArgument`) and whether the skip is unconditional or exe-path-heuristic (SRC-02).
- New binder test placement (`Tests/.../Binders/`).

## Deferred Ideas

- AOT-01 (reflection/AOT-trim annotations) + DOC-01 (README) — Phase 4.
- REL-01 (cut v2.0.0-beta) — Phase 5.
- COLL-01 (`List<T>` support) — still owner-deferred.
- D1 Validations / D2 EqualityCompererCreator — HELD.
- CLI binder getopt-style short/boolean flags — not adopted (string-value binder doesn't model booleans).
