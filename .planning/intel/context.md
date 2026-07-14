# Context (Intel)

_Running notes from DOC-type sources, keyed by topic, with source attribution. These describe what
the library already does (feature/usage material) plus current project state._

## Topic: What SimpleSettings is / why it exists
- Framework-independent alternative to ASP.NET Core `IOptions<>`. Lets services depend on their own
  **interfaces** (e.g. `IEmailServiceConfig`) rather than a framework abstraction or concrete option
  classes. Motivation: `IOptions<>` couples app code to a framework (DIP violation), forces concrete
  classes over interfaces, requires manual `services.Configure<>` per type (doesn't scale), and is
  awkward outside Microsoft DI.
- Previously named "SimpleConfig"; now `ExistForAll.SimpleSettings`.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/README.md

## Topic: Getting started / core usage
- Entry point `SettingsBuilder.CreateBuilder()`; `ScanAssemblies(...)` returns an
  `ISettingsCollection` mapping `Type` -> generated implementation. At scan time SimpleSettings finds
  every settings-interface indication and uses `Emit` to create a concrete class at runtime (Roslyn
  was benchmarked and found too slow).
- Retrieve via `settingsCollection.GetSettings<T>()`, iterate the collection, or ask the builder
  directly (`SettingsBuilder.CreateBuilder().GetSettings<T>()`).
- `SettingsProperty(DefaultValue = ...)` seeds property values.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/docs/getting_started.md

## Topic: Dependency injection / Generic Host
- `ExistForAll.SimpleSettings.Extensions.GenericHost` provides `AddSimpleSettings(o => o.AddAssemblies(...))`.
- Every scanned interface is registered as a **singleton** and can be injected directly; also
  resolvable via the registered `ISettingsProvider.GetSettings<T>()`.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/docs/getting_started.md

## Topic: Building the collection
- `ScanAssemblies` accepts an `IEnumerable<Assembly>` or one required assembly + more. Only **public
  (exported)** interfaces are scanned.
- `CreateBuilder(factory => ...)` exposes `SetupOptions` + `Set*` helpers to mutate `SettingsOptions`
  (e.g. `SetDateTimeFormat`, `SetArraySplitDelimiter`).
- Binders added via `factory.AddSectionBinder(...)`; order matters (last binder wins; else default).
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/docs/building_the_collection.md

## Topic: Declaring a settings interface (three ways)
- (1) base interface `ISettingsSection`, (2) `[SettingsSection]` attribute, or (3) name suffix
  (`...Settings` by default). Any one suffices — KISS. All three indications are configurable via
  `SettingsOptions`.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/docs/Build Config Interface.md

## Topic: Default values & SettingsProperty options
- If no binder set a value, a `[SettingsProperty(DefaultValue = ...)]` is applied. `DefaultValue`
  accepts any value convertible to the property type, including `Enum`, `DateTime`, `Uri`, arrays,
  and `IEnumerable<T>`. DateTime default parse format is `yyyy-MM-dd` (configurable via
  `SettingsOptions.DateTimeFormat`).
- `SettingsProperty` members beyond `DefaultValue`: `Name` (override lookup key),
  `ConverterType` (per-property `ISettingsTypeConverter`), `AllowEmpty` (default true; false =>
  binding throws if no value resolved). App can inherit `SettingsPropertyAttribute` to decouple.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/docs/Default Values.md

## Topic: Section binders
- A binder implements `ISectionBinder.BindPropertySettings(BindingContext context)`. Context provides
  `Section`, `Key`, and `CurrentValue` (running value starting from the default). Call
  `context.SetNewValue(value)` to contribute; skip to preserve prior value.
- Section name = interface name with leading `I` trimmed (`ISomeInterface` -> `SomeInterface`),
  configurable via `SettingsOptions` or per-interface `[SettingsSection("name")]`. Key = property
  name, overridable via `[SettingsProperty(Name = "...")]`.
- Built-in binders: `InMemoryCollection` (core, via `AddInMemoryCollection`); and in
  `ExistForAll.SimpleSettings.Binders`: `ConfigurationBinder` (`AddConfiguration`, reads .NET
  `IConfiguration`), `EnvironmentVariableBinder` (`AddEnvironmentVariable`), command-line binder
  (`AddCommandLine`/`AddArguments`).
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/docs/Build a SectionBinder.md

## Topic: Extending SimpleSettings (SettingsOptions)
- `SettingsOptions`: `AttributeType` (default `SettingsSectionAttribute`), `InterfaceBase` (default
  `ISettingsSection`), `SettingsSuffix` (default `"Settings"`), `ArraySplitDelimiter` (default `","`),
  `DateTimeFormat` (default `"yyyy-MM-dd"`), `SectionNameFormatter` (default trims leading `I`).
  Mutated via factory `Set*` helpers, not directly.
- Custom converters via `AddTypeConverter` (added first => wins over built-ins).
- Documented future-feature ideas (author's wishlist, not committed): diagnostics/reporting, Roslyn
  support, AOP.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/docs/Extend Simple Config.md

## Topic: Current project state (session handoff, 2026-07-13)
- On branch `security/s1-redact-exception-value` with the S1 commit (code + tests + docs refresh),
  PR open. `master` @ `498fc81`. Perf track P0–P5 complete + merged. Build clean (0 warnings, both
  TFMs). Suite 76 net10 (+5 S1 redaction tests).
- `gh-pages` branch holds benchmark baseline (`dev/bench/`) — do not delete. Remote also has merged
  `perf/p4-*`/`perf/p5-*` branches and legacy/held branches (`validate-settings`, `version-7.x`).
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md

## Topic: Ranked next priorities (from handoff)
1. Merge the S1 PR once CI green. 2. Engine tests T4/T5/T7 (generator concurrency race still open).
3. Breaking cleanups batched pre-stable: C2, A5, C1, A6, A3, A4. 4. A1 AOT/trim (HIGH). 5. README
stale-link refresh. 6. D1 validations feature (owner-driven) + optional P3b compiled setter.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md

## Topic: Working gotchas (durable)
- Run `dotnet` from `src/` (do not `cd` repo-root first); net8 build-only locally, CI runs both.
- Push/PR via the `guy-lud` account (SSH alias `github-guy-lud`); `guy-frontegg` is read-only.
  `git push` already uses guy-lud; for `gh` writes switch user then switch back.
- Benchmarks run from `src/` via `dotnet run -c Release --project performance/...Benchmark`.
- Wrap ritual: refresh the handoff so it rides the session's real work-branch PR; never commit docs
  to `master` (every master push burns an alpha) and never create a dedicated docs branch.
- Commits/PRs omit the Co-Authored-By / Generated-with trailer (project preference).
- dotnet-claude-kit sub-agents intermittently misfire (0 tool calls); use `/code-review` skill and
  in-context lenses as fallback; `security-auditor` still worth spawning.
- source: /Users/guyludvig/frontegg/development/open-source/SimpleSettings/SESSION-HANDOFF.md
