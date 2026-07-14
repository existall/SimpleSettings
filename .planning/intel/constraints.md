# Constraints (Intel)

_Synthesized from SPEC-type sources. The single SPEC in the ingest set is `FIX-PLAN.md`
(precedence 0, highest). These are the durable technical contracts, invariants, and NFRs the
roadmapper must respect._

## CON-secret-redaction-invariant — Exception redaction (nfr / security)
- type: nfr
- content: `SettingsPropertyValueException` must never carry the bound value and must never chain a
  framework inner exception (both can embed secrets). Only property name, target type, and the
  failing converter's exception **type name** may be surfaced. The value-free "required value
  missing" case must use `SettingsPropertyNullException` (keeps its full message). Custom
  `ISectionBinder` authors must not throw value-bearing messages, because the public wrapper
  `SettingsBindingException` chains the binder's inner. Do not reintroduce a value param or chained
  inner, and do not add an opt-in flag to restore them (rejected as insecure-by-configuration).
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (§S1)

## CON-allocation-gate — Benchmark CI gates on allocations (nfr)
- type: nfr
- content: `benchmark.yml` runs BenchmarkDotNet on push to `master` + PRs and gates PRs on
  **allocated-byte** regressions (>10%) via github-action-benchmark; wall-clock time is
  informational only. Baseline lives on the `gh-pages` branch (`dev/bench/`) — do not delete it.
  New/changed hot paths must add a gated benchmark. Proven wins to preserve: P3 warm re-populate
  −55/−61%, P4 ConvertArrayBenchmark 1.33 KB→688 B (−49%, 5.7×), P5 ConfigBinder BindNoRoot
  80→40 B (−50%) / BindWithRoot 144→56 B (−61%).
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (P0, P3, P4, P5, progress)

## CON-provider-cache-semantics — Provider caches built instances (protocol)
- type: protocol
- content: `ISettingsProvider` caches the built instance per type (C3 option 2 / P1), so
  `GetService<IFoo>()` (DI singleton) and `provider.GetSettings<IFoo>()` return consistent objects.
  Core `SettingsBuilder.GetSettings` is unchanged; there is no `IOptionsMonitor`-style reload
  (deferred as future "option 3"). Binder order matters: the last binder to call `SetNewValue` wins;
  if none set a value, the `SettingsProperty` default applies.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (C3, P1)

## CON-target-frameworks — Modern .NET only (nfr)
- type: nfr
- content: Multi-targets `net8.0` + `net10.0` only (classic .NET Framework dropped). Only the net10
  runtime is installed locally => net8 is build-only locally; CI runs both. Build/test from `src/`
  (`global.json` opts into Microsoft.Testing.Platform for TUnit). Test suite baseline: 76 tests on
  net10.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (How to work this plan)
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (Current state, Gotchas)

## CON-aot-trim-unsupported — AOT / trimming currently unsafe (nfr)
- type: nfr
- content: The engine uses `Reflection.Emit` + `MakeGenericType/Method` + `GetExportedTypes` with
  zero `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` annotations, so it silently breaks under
  Native AOT / trimmed apps. Until A1 is resolved, AOT/trim is unsupported and must be documented as
  a limitation before the stable release.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (A1)

## CON-pre-stable-breaking-window — Breaking changes free pre-beta (protocol)
- type: protocol
- content: No stable `v*` tag exists (only auto-alphas `2.0.0-alpha.*`; legacy `version-*` tags
  belong to the dead `ExistAll.SimpleConfig` package). Breaking changes are free and should be
  batched before the first `v2.0.0-beta`. Any push to `master` publishes an alpha (no `paths`
  filter), so all changes go through PRs; do not commit doc-only changes to `master`.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (How to work this plan)
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (How releasing works, Gotchas)

## CON-canonical-naming — ExistForAll.SimpleSettings namespace/package (schema)
- type: schema
- content: Canonical spelling is `ExistForAll` (not `ExistAll`/`ExistsForAll`). Package is
  `ExistForAll.SimpleSettings` (+ `.Extensions.GenericHost`, `.Binders`, `.Core.AspNet`). Legacy
  `SimpleConfig` naming is retired. New code and docs must use the canonical name.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (A2, D3)

## CON-versioning-release — MinVer / Trusted Publishing (protocol)
- type: protocol
- content: Versioning = MinVer, tag prefix `v`, baseline `2.0.0`, keyless publish via NuGet Trusted
  Publishing (OIDC). `ci.yml` builds+tests net8/net10 on PRs to `master`. `release.yml`: push to
  `master` auto-publishes a height-based `-alpha`; manual Release (`workflow_dispatch`,
  channel beta/rc/stable + bump) tags `v*` and publishes (`dry_run` previews). Solution =
  `SimpleSettings.slnx` via the `SOLUTION` env var.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md (How releasing works)

## CON-converter-chain — Type converter chain contract (api-contract)
- type: api-contract
- content: Values are converted to the property type through a chain of `ISettingsTypeConverter`
  (`CanConvert(Type)` + `Convert(object, Type)`). Order matters: user converters (via
  `AddTypeConverter`) are added first and win over built-ins; `DefaultTypeConverter.CanConvert`
  always returns true and must remain last; `EnumTypeConverter` must be registered before it
  (fixed in B1). `DefaultTypeConverter` must use `CultureInfo.InvariantCulture` (fixed in B2).
  Array/enumerable conversion is unified in `CollectionTypeConverter` (P4); currently only exact
  `IEnumerable<T>` is matched (`List<T>`/`IList<T>` unsupported pending C1).
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/FIX-PLAN.md (B1, B2, P4, C1)
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/docs/Extend Simple Config.md
