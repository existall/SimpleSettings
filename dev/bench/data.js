window.BENCHMARK_DATA = {
  "lastUpdate": 1784541058596,
  "repoUrl": "https://github.com/existall/SimpleSettings",
  "entries": {
    "Allocations (bytes/op)": [
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "a20f2f1a870d8d65f406e6d9e1d627e9a3399c8d",
          "message": "Add benchmark tracking workflow (gate PRs on allocation regressions) (#22)\n\nRuns BenchmarkDotNet on every push to master and on PRs, then feeds per-benchmark\nallocated-bytes into benchmark-action/github-action-benchmark:\n- push to master records the new baseline on the gh-pages data branch (dev/bench);\n- PRs compare against that baseline, comment on a >10% allocation jump, and fail the check.\n\nGates on allocated bytes rather than time: allocation counts are deterministic and\nstable on shared runners, so they can safely fail a build; time stays informational\n(in the run logs). JSON export -> jq reshape -> customSmallerIsBetter was validated\nlocally against ScanBenchmark. Mirrors ci.yml's SDK/cache setup. The micro-benchmark\nfilters activate automatically once PR #21 lands them on master.",
          "timestamp": "2026-07-12T22:05:29+03:00",
          "tree_id": "1d024afe7a4f5480f8f822b6766394f0034aca99",
          "url": "https://github.com/existall/SimpleSettings/commit/a20f2f1a870d8d65f406e6d9e1d627e9a3399c8d"
        },
        "date": 1783883216632,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17528616,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "6bd5b0e22f67bf0a148a33cbf564e1605ce09426",
          "message": "Session wrap: refresh handoff + fix-plan; gitignore benchmark output (#23)\n\n* Session wrap: refresh handoff + fix-plan; gitignore BenchmarkDotNet output\n\n- Handoff/fix-plan now reflect #21 merged (Q1–Q4 + M1 collision fix +\n  micro-benchmarks, proven 2.7x–32x on repeated paths) and #22 open\n  (per-push benchmark tracking, gate on allocation regressions).\n- Records the durable facts: gate on allocations not time, gh-pages holds the\n  baseline, and the M1 rule (generated impl name is separate from the section name).\n- Next priority remains P3.\n- gitignore BenchmarkDotNet.Artifacts/ so local benchmark runs don't leave\n  untracked output.\n\n* gitignore .claude/settings.local.json (personal SessionStart hook)",
          "timestamp": "2026-07-12T22:18:29+03:00",
          "tree_id": "0b8865c30f9f19ad623373ee0761b8b023901187",
          "url": "https://github.com/existall/SimpleSettings/commit/6bd5b0e22f67bf0a148a33cbf564e1605ce09426"
        },
        "date": 1783883987442,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17560104,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "faa48d9e9cf123b44782c1957a863efd22137e37",
          "message": "Cache a per-type settings plan (P3) (#24)\n\n* Cache a per-type settings plan (P3)\n\nValuesPopulator now builds a SettingsPlan once per settings interface (cached on the populator instance, scoped to the builder's Options) instead of re-doing reflection on every populate. The plan resolves the section name once and lazily (a no-binder scan never pays for it), and holds per property a readonly-struct PropertyPlan/PropertyConversion carrying the resolved key, default value, and the converter chosen once up front. Converter selection walks the LinkedList manually rather than via LINQ First, so its struct enumerator is not boxed.\n\nWarm re-populate drops 52-56% in allocations (50 props: 15,681 -> 6,848 B). The gated ScanBenchmark rises +4.66% (under the 10% gate) - pure plan-build overhead on the populate-once cold scan. A new gated PlanPopulateBenchmark tracks the warm path.\n\nReflective PropertyInfo.SetValue is retained: an emitted __Set method / compiled Action<object,object?> setter was implemented and measured but reverted - it regressed the gated cold ScanBenchmark (+25% for the emitted __Set) for zero warm gain, since net10's SetValue no longer allocates an args array. A tiered/lazy compiled setter (P3b) is noted as a follow-up only if set time (not allocation) ever matters.\n\n56 tests pass on net8.0 + net10.0.\n\n* Apply code + perf review fixes to P3\n\nFrom a two-agent (code-review + perf) pass over the P3 diff:\n\n- Read [SettingsProperty] once per property at plan build instead of 3x (via GetPropertyName / GetDefaultValue / CreateConversion). GetCustomAttribute re-materializes the attribute each call, so at ~2000-type cold-scan scale this dominated: it drops the gated ScanBenchmark from +4.66% to ~flat (-0.40% vs master). The now-inlined GetPropertyName/GetDefaultValue helpers are retired (PropertyInfoExtensions deleted).\n\n- Re-wrap converter-setup failures at plan build as SettingsPropertyValueException, restoring the pre-P3 exception contract (a custom ConverterType that throws in its ctor / isn't an ISettingsTypeConverter used to surface wrapped, inside the per-populate convert try).\n\n- Materialize the section binders once in SettingsBuilder so the populator's 'as ISectionBinder[]' fast-path hits (the factory hands a SortedList.Values view); removes a per-populate ToArray (~32 B/call). Warm re-populate is now -55 to -61% vs master (50 props 15,681 -> 6,816 B).\n\n- Document ISettingsTypeConverter as stateless/thread-safe (custom converters are now selected once per type and shared). Minor: fix stale __Set benchmark docstring, drop a redundant string interpolation in GetNormalizeInterfaceName, pass PropertyPlan by 'in'.\n\n56 tests pass on net8.0 + net10.0.",
          "timestamp": "2026-07-13T10:57:06+03:00",
          "tree_id": "7c3487ec7b6ae726f1444ff7600af9ec4faf28b2",
          "url": "https://github.com/existall/SimpleSettings/commit/faa48d9e9cf123b44782c1957a863efd22137e37"
        },
        "date": 1783929526137,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17573336,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "f9a3061aea1a5ae6a3e1f547e0860a4b0dc4b7e5",
          "message": "De-reflect + DRY the array/enumerable converters (P4) (#25)\n\n* De-reflect + DRY the array/enumerable converters (P4)\n\nIntroduce a shared CollectionTypeConverter base that builds collection\nresults with Array.CreateInstance + indexed fill and selects the element\nconverter by walking the concrete LinkedList (struct enumerator) rather\nthan LINQ First. ArrayTypeConverter/EnumerableTypeConverter collapse to\nthin subclasses that differ only in CanConvert + element-type extraction;\nboth now return T[] (safe: IsEnumerable() matches only IEnumerable<T>,\nwhich a T[] satisfies). This drops the per-convert List<T> + its backing\narray, the reflected Enumerable.ToArray (MakeGenericMethod + Invoke +\nargs array), and the First predicate closure.\n\nTypeConverter.CreateNullResult swaps the Enumerable.Empty<T>() reflection\nfor Array.CreateInstance(t, 0), and GetConverter is now a true manual walk\n(matching its own comment). Also strips a stray UTF-8 BOM from the file.\n\nProof via the new gated ConvertArrayBenchmark (isolates the hot path like\nQ1/Q3/Q4): 1.33 KB -> 688 B (-49%), 1,247 -> 219 ns (5.7x). The residual\n688 B is the split-substrings + per-element boxing + result array shared\nby both the old and new code.\n\nAdds 7 collection-converter parity tests (delimited string -> int[] /\nstring[] / IEnumerable<int>, empty-entry removal, custom delimiter,\ndefault-array passthrough, and that the enumerable path materializes a\nT[]). Suite: 63 tests per TFM (was 56).\n\nAlso refreshes SESSION-HANDOFF.md + FIX-PLAN.md and carries the pre-P4\npost-P3 style tweaks to TypeConverter.cs / TypeExtensions.cs.\n\n* Harden P4 converter tests per code review\n\nAdds 5 collection-converter tests the dotnet code-reviewer suggested:\n- CreateNullResult null path: an unbound IEnumerable<T> with no default now\n  yields an empty T[] -- the one line P4 changed in TypeConverter.cs that no\n  existing test exercised (all others bind a value or supply a default).\n- Element-converter parity: DayOfWeek[], DateTime[], Uri[] (the shipped tests\n  only covered int/string, both routed to DefaultTypeConverter).\n- Negative: a non-numeric element for an int[] surfaces as the expected\n  SettingsPropertyValueException, pinning the exception-wrapping contract.\n\nSuite: 68 per TFM (was 63). No production changes. Refreshes\nSESSION-HANDOFF.md + FIX-PLAN.md (counts, PR #25, code-review outcome).\n\n* making it nicer a bit\n\n* bring it back",
          "timestamp": "2026-07-13T13:46:38+03:00",
          "tree_id": "3c45745a96cc0c0a525cc4f77a04aeba216e88fb",
          "url": "https://github.com/existall/SimpleSettings/commit/f9a3061aea1a5ae6a3e1f547e0860a4b0dc4b7e5"
        },
        "date": 1783939706354,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17573336,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "498fc8161ce91bbcf5304d5d09aec4d7e1ce877e",
          "message": "Resolve the config section once per type (P5) (#26)\n\nConfigurationBinder called _configuration.GetSection(...) on every property,\nthough the section is constant per settings type. Cache the resolved\nIConfigurationSection per section name in a ConcurrentDictionary, using a\nzero-capture GetOrAdd (static factory + factoryArgument) so no per-call\ndelegate is allocated. Reload-safe: GetSection returns a live view over the\nconfiguration root, so the cached section re-reads providers on each access.\nDrop the dead ?. (GetSection never returns null) and strip a stray BOM.\n\nChosen over threading the section through the ISectionBinder contract: that\nwould be a layering violation (Core must not reference\nMicrosoft.Extensions.Configuration) and the optimization is single-implementer.\nPlan reviewed by the architect/perf/security agents; code reviewed via\n/code-review.\n\nProof (new gated ConfigBinderBenchmark): BindNoRoot 80->40 B (-50%),\nBindWithRoot 144->56 B (-61%). Adds 3 tests (multi-property with/without\nRootSection, plus a cached-section-reflects-later-change live-view test). Also\nwires P4's ConvertArrayBenchmark into the CI filter (it was never gated) and\nadds Microsoft.Extensions.Configuration to the benchmark project.\n\nRefreshes SESSION-HANDOFF.md + FIX-PLAN.md and logs a pre-existing secret-leak\nfinding surfaced by the security review (S1: Resources.cs interpolates the raw\nbound value into the SettingsPropertyValueException message).\n\nSuite: 71 tests per TFM.",
          "timestamp": "2026-07-13T15:22:43+03:00",
          "tree_id": "3338cc21f23038b58e7a8f713741f6fa1d989698",
          "url": "https://github.com/existall/SimpleSettings/commit/498fc8161ce91bbcf5304d5d09aec4d7e1ce877e"
        },
        "date": 1783945485109,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17572784,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "5277c6075ebfb17ab94a6ad636073a1035f4bb31",
          "message": "Redact secret values from conversion-failure exceptions (S1) (#27)\n\n* Redact secret values from conversion-failure exceptions (S1)\n\nA bound configuration value that failed type conversion could reach logs\ntwo ways: our own SettingsPropertyValueException message interpolated the\nraw value, and the failing converter's framework inner exception\n(FormatException/ArgumentException for int/enum/DateTime) embeds the raw\ninput and was chained in. A secret on a typed property (e.g. a credentialed\nUri, or a secret mis-bound to int/enum) therefore leaked into logs.\n\nFix (full — plan review chose this over redact-message-only and over an\nopt-in \"restore diagnostics\" flag, which was rejected as insecure-by-config):\n\n- Resources.PropertySetterExceptionMessage: no longer includes the value;\n  reports property name, target type, and the failing converter's exception\n  type name (a compile-time identifier that cannot carry a secret). Fixes the\n  \"to to\" double-word typo.\n- SettingsPropertyValueException: ctor drops the value parameter and no longer\n  chains the framework inner exception. Invariant: this type never carries a\n  value and never chains an inner, so it is auditably leak-proof.\n- New SettingsPropertyNullException (internal): the \"AllowEmpty = false with no\n  value\" path is value-free, so it keeps its full, useful message instead of\n  being redacted into a value-conversion exception; ConvertPropertyValue\n  rethrows it unredacted.\n- ISectionBinder: doc note that custom binders must not throw exceptions whose\n  message embeds a fetched value (the public SettingsBindingException chains\n  the binder's inner). The built-in binders don't.\n\nBoth changed exceptions are internal, so this is non-breaking.\n\nTests: new Conversion/ExceptionRedactionTests.cs binds a sentinel secret to\nint/enum/DateTime/Uri and to a hostile custom converter, asserting the secret\nis absent from the entire exception ToString() chain while the property name\nand target type remain; the existing null-not-allowed test now expects\nSettingsPropertyNullException and asserts its message. Suite 76 net10 (was 71).\n\nAlso refreshes FIX-PLAN.md (S1 marked done) and SESSION-HANDOFF.md.\n\n* Use an exception filter for the null-exception passthrough\n\nAddress PR #27 review: replace the separate catch(SettingsPropertyNullException)\n{ throw; } with a filter on the general catch — catch (Exception e) when\n(e is not SettingsPropertyNullException). Same behavior, more concise, and the\nfilter never unwinds into the catch so the null exception's throw context is\nuntouched.",
          "timestamp": "2026-07-13T17:08:01+03:00",
          "tree_id": "74127d53e6cbae7e3c99ed81dc3d77f2440c9d0b",
          "url": "https://github.com/existall/SimpleSettings/commit/5277c6075ebfb17ab94a6ad636073a1035f4bb31"
        },
        "date": 1783951816657,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17573344,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "13b78dd4b0b3b229abe7f3f9e046aff6d645717c",
          "message": "Add a public exception hierarchy (C2) (#28)\n\nThe library's exceptions derived straight from Exception with no common base,\nso consumers couldn't catch (SimpleSettingsException); four were internal yet\nescaped the public build path (uncatchable by type); context lived only in\nmessage strings; and the reachable not-an-interface guard threw an untyped\nInvalidOperationException.\n\n- New public abstract SimpleSettingsException : Exception (protected (message)\n  and (message, inner) ctors; no parameterless or [Serializable] ctor -\n  BinaryFormatter is obsolete on the net8/net10 targets). All 10 library\n  exceptions now derive from it.\n- Promote the four build-path escapees to public: SettingsPropertyValueException,\n  SettingsPropertyNullException, TypeGenerationException,\n  SettingsPropertyExtractionException.\n- Flatten three mis-namespaced types to the root namespace so the public\n  surface is coherent: SettingsExtractionException (was .Core),\n  TypeGenerationException and SettingsPropertyExtractionException\n  (were .Core.Reflection).\n- Expose leak-safe structured properties instead of forcing consumers to parse\n  messages: SettingsBindingException.{BinderType,Section,Key};\n  SettingsPropertyValueException.{SettingsType,PropertyName,TargetType,\n  ConversionErrorType}; SettingsType / OptionType / ArgumentName on the rest.\n- Replace the untyped InvalidOperationException(TypeIsNotInterface) throws with\n  a typed SettingsTypeNotInterfaceException (the one runtime-behavior break -\n  see release notes). The two documented-unreachable \"No converter found\"\n  InvalidOperationExceptions are left as internal invariant guards.\n\nS1 secret-redaction is now structural, not conventional:\nSettingsPropertyValueException's ctor takes the failure's Type (not the\nException), so a value-bearing object cannot cross its boundary; and\nSettingsBindingException stores primitives instead of retaining the\nBindingContext, which holds the bound value.\n\nReviewed: plan by security-auditor + dotnet-architect (both\nENDORSE-WITH-CHANGES; adopted the Type-not-Exception hardening, namespace\nflattening, Section/ConversionErrorType naming, and reflection-based\naccessibility tests since InternalsVisibleTo masks it); code by /code-review\nplus Roslyn detect_antipatterns (0).\n\nTests: SimpleSettings/ExceptionHierarchyTests.cs (+6) - base is public+abstract,\na reflection invariant that every library exception derives from the base, the\nfour promotions are public, non-interface -> typed + catchable as base,\nconversion failure -> structured metadata + InnerException == null, and\nbinder-throws -> binder context. The existing not-interface test updated to the\nnew type. Suite 82 net10 (was 76).\n\nAlso refreshes FIX-PLAN.md (C2 marked done, stale \"bare Exception\" line fixed)\nand SESSION-HANDOFF.md.",
          "timestamp": "2026-07-13T17:58:27+03:00",
          "tree_id": "b815a0d2c8efc5adc395280a86daaec0df5f501d",
          "url": "https://github.com/existall/SimpleSettings/commit/13b78dd4b0b3b229abe7f3f9e046aff6d645717c"
        },
        "date": 1783954841501,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17573344,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "guy-lud",
            "username": "guy-lud"
          },
          "committer": {
            "email": "guy.lud@gmail.com",
            "name": "guy-lud",
            "username": "guy-lud"
          },
          "distinct": false,
          "id": "6ee61d464ef517dbd7e5f7862e041de1fd3d814c",
          "message": "chore: bootstrap GSD planning (.planning/)",
          "timestamp": "2026-07-14T10:38:08+03:00",
          "tree_id": "51170d90140e23d33d6a8714db7943b6b70ea0d0",
          "url": "https://github.com/existall/SimpleSettings/commit/6ee61d464ef517dbd7e5f7862e041de1fd3d814c"
        },
        "date": 1784015293950,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17573344,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "10f92754f8b3a5a838665bec4f6016cf7df2c9ca",
          "message": "Close the SettingsClassGenerator concurrency race (T7) (#29)\n\nGenerateType did a lock-free TryGetValue and, on a miss, called\n_moduleBuilder.DefineType(name) then wrote the cache. Two threads racing the\nsame interface both missed the cache and both defined the same type name (the\nsecond throws \"Duplicate type name\" -> the resolve/scan aborts). And because\nSystem.Reflection.Emit is not thread-safe, two threads generating different\ninterfaces also race the shared ModuleBuilder's metadata. Q4's\nConcurrentDictionary made the cache thread-safe but never serialized the\ncheck-then-define.\n\nReachable in production: GenericHost registers ISettingsProvider as a\nprocess-wide singleton over one SettingsBuilder, and cache-miss resolves fall\nthrough to lazy generation on the shared ModuleBuilder from concurrent DI\nresolutions.\n\nFix: double-checked locking. The warm cache-hit path stays lock-free\n(TryGetValue before the lock); on a miss, take a single generation gate,\nre-check, run the whole emit sequence (extracted to DefineImplementationType)\nplus the cache write inside the lock, and still wrap failures as\nTypeGenerationException (uncached). One gate over all generation, matching the\nshared _moduleBuilder's scope - not a per-type Lazy, which would only serialize\nsame-interface generation and leave the distinct-interface module race open.\n\nTests (+2 in SettingsClassGeneratorTests.cs): a same-interface Parallel.For\nstress (asserts one shared impl) and a distinct+same Barrier/32-thread stress\nacross 8 interfaces (asserts no failures and exactly one impl per interface;\nregression-guards the lock-all decision against a future switch to Lazy). Suite\n84 net10 (was 82); ran 5x green.\n\nReviewed: plan by dotnet-architect (ENDORSE-WITH-CHANGES - lock-all required,\nLazy rejected) + perf/security in-context; code by /code-review plus Roslyn\ndetect_antipatterns (0).\n\nAlso refreshes FIX-PLAN.md (T7 marked done) and SESSION-HANDOFF.md.",
          "timestamp": "2026-07-14T11:32:20+03:00",
          "tree_id": "d9dad5e70c2739956c440f4c23637932e79d7a42",
          "url": "https://github.com/existall/SimpleSettings/commit/10f92754f8b3a5a838665bec4f6016cf7df2c9ca"
        },
        "date": 1784018077900,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17573376,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "67aa72f901d66defc6903cf24ad06c101315e5ac",
          "message": "GSD planning cutover + Phase 2: binding-correctness engine test hardening (#30)\n\n* docs(gsd): reconcile .planning with merged T7 (#29); freeze FIX-PLAN.md\n\nGSD is now the source of truth for project tracking. Reconcile .planning to\nthe real git state and retire FIX-PLAN.md as the working doc.\n\n- Mark ENG-01/T7 complete across REQUIREMENTS/ROADMAP/STATE/PROJECT (shipped\n  pre-GSD via #29 — double-checked locking + same/distinct-interface stress tests)\n- Clear the stale \"T7 race open\" concern in STATE; log the generator-serialization\n  decision (one gate over all generation; not Lazy-per-type)\n- Phase 2 now: ENG-01 done; COLL-01 (C1, deferred) + TEST-01/02/03 remain\n- Freeze FIX-PLAN.md with a banner pointing at .planning/ (kept for its per-item\n  file:line detail, mined by each phase's CONTEXT/PLAN)\n\nLocal branch only (no push) — rides the next work branch to avoid a doc-only\nmaster alpha. SESSION-HANDOFF.md left uncommitted (living handoff).\n\n* docs(02): synthesize phase context from FIX-PLAN (COLL-01 deferred, ENG-01 verify-only)\n\n* docs(02): research binding-correctness engine test hardening\n\n* docs(02): add validation strategy\n\n* docs(02): create Phase 2 binding-correctness test-hardening plans\n\n* docs(02): add phase artifacts inventory to plan 01\n\n* docs(02): add pattern map; mark phase planned (2 plans, ready to execute)\n\n* test(02-01): add ValuesPopulator precedence + default-survives tests\n\n- last-writer-wins across two ordered binders\n- later silent binder does not clobber earlier set value\n- [SettingsProperty] DefaultValue survives when no binder sets the property\n\n* test(02-01): add TypeConverter null/nullable/ConverterType tests\n\n- null for non-nullable int resolves to 0\n- Nullable<int> null resolves to null; \"42\" strips and converts to 42\n- ConverterType on IEnumerable<int> bypasses the collection converter (sentinel wins)\n\n* docs(02-01): complete engine-core correctness test-hardening plan\n\n* test(02-02): add scalar Uri/DateTime conversion coverage (TEST-03)\n\n- Scalar Uri positive: bound URL string resolves to new Uri(value)\n- Scalar DateTime positive: yyyy-MM-dd string resolves via ParseExact\n- One DateTime format-mismatch negative asserts SettingsPropertyValueException type only\n- No array-of-* duplication (owned by CollectionConversionTests); no redaction re-proof (ExceptionRedactionTests)\n\n* docs(02-02): complete scalar converter-coverage (TEST-03) plan\n\n* docs(phase-02): complete phase execution\n\n* docs(phase-02): evolve PROJECT.md after phase completion",
          "timestamp": "2026-07-14T13:30:37+03:00",
          "tree_id": "8fea055c29ad07440635b812f46d88367f596268",
          "url": "https://github.com/existall/SimpleSettings/commit/67aa72f901d66defc6903cf24ad06c101315e5ac"
        },
        "date": 1784025195670,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17572816,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "guy-lud",
            "username": "guy-lud"
          },
          "committer": {
            "email": "guy.lud@gmail.com",
            "name": "guy-lud",
            "username": "guy-lud"
          },
          "distinct": true,
          "id": "87503594d7975f3ba520df9fc59105b5878fbb6e",
          "message": "pushing this crap now so i could remove them later",
          "timestamp": "2026-07-14T13:36:37+03:00",
          "tree_id": "ac1b5f873bdbe7999e292bbc3b4b0683b131963a",
          "url": "https://github.com/existall/SimpleSettings/commit/87503594d7975f3ba520df9fc59105b5878fbb6e"
        },
        "date": 1784025526566,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17573376,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "630319992ca42459396916505cb2a8b26d8581a2",
          "message": "Phase 3: Public Surface, Packaging & Binder Cleanup (#31)\n\n* docs(03): capture phase context\n\n* docs(state): record phase 3 context session\n\n* docs(03): research public-surface/packaging/binder cleanup phase\n\n* docs: refresh session handoff (Phase 3 — discuss+research done, planner next)\n\n* docs(phase-3): add validation strategy\n\n* docs(03): create phase plan (public surface, packaging & binder cleanup)\n\n* docs(03): refine D-05 (split exe-skip by entry point) + resolve open questions\n\n* docs(03): finalize phase 3 plan (2 plans) after plan-check + architect/security/perf review\n\n* refactor(03-01): make SettingsHolder internal sealed (API-01/D-01)\n\n- Flip public class SettingsHolder to internal sealed\n- Removes an internal DTO from the public API surface\n- Reachable from tests/benchmark via existing InternalsVisibleTo\n\n* chore(03-01): remove dead Core.AspNet package (PKG-01/D-02)\n\n- Drop Core.AspNet Project entry from SimpleSettings.slnx\n- Drop Core.AspNet ProjectReference from UnitTests.csproj\n- Delete src/Core/ExistForAll.SimpleSettings.Core.AspNet/ directory\n- Test-local Core/AspNet/Environments.cs duplicate untouched\n- Published 2.0.0-alpha.0.* alphas: unlist is owner-optional follow-up\n\n* chore(03-01): float Microsoft.Extensions.* floor per-TFM (PKG-02/D-03)\n\n- Split four Microsoft.Extensions.* pins into per-TFM conditional ItemGroups\n- net8 floors: Configuration 8.0.0, Configuration.Json 8.0.1, DI 8.0.1, DI.Abstractions 8.0.2\n- net10 stays 10.0.9 for all four\n- Frees net8 consumers of the packable Binders + GenericHost packages from 10.x\n\n* docs(03-01): complete public-surface/packaging/binder-cleanup plan\n\n* test(03-02): add failing CLI binder cases for lookahead + SkipFirstArgument\n\n- space-separated --k v, quoted-with-spaces single token\n- prefixed-next-token = new key, prefixed-value non-binding\n- SkipFirstArgument true/false, empty-token no-crash\n- CLI-path secret-redaction regression (S2)\n\n* feat(03-02): add SkipFirstArgument option + lookahead CLI parse (SRC-02/D-04,D-05)\n\n- SkipFirstArgument (default false, explicit override; no silent arg[0] drop)\n- Parse rewritten to index/lookahead loop; space-separated --k v binds\n- empty-safe zero-alloc prefix detection (length-delta + Array.IndexOf, cached prefix array)\n- prefixed next-token treated as new key; ArgumentPrefixes XML-doc note\n\n* feat(03-02): AddCommandLine sources GetCommandLineArgs + owns exe skip (SRC-02/Open Q #1)\n\n- replace Environment.CommandLine.Split(' ') with Environment.GetCommandLineArgs()\n- wrap caller action to enable SkipFirstArgument=true (exe skip owned by this entry point)\n- quoted-value-with-spaces criterion now holds end-to-end unconditionally\n\n* docs(03-02): complete command-line binder cleanup plan\n\n* fix(03-02): null-guard CLI lookahead value token (post-review hardening)\n\n* docs(phase-03): complete phase execution (verified 4/4, review clean)\n\n* docs(backlog): capture client pre-beta engine requirements (COLL/VAL/API)",
          "timestamp": "2026-07-14T16:47:58+03:00",
          "tree_id": "9345fe4ec63e1c68f37596870b7533580691bd76",
          "url": "https://github.com/existall/SimpleSettings/commit/630319992ca42459396916505cb2a8b26d8581a2"
        },
        "date": 1784037001621,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17573376,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "7f9e17caae2467cf03e0c22f7f6e5a9b2a42a524",
          "message": "chore: post-Phase-3 source cleanup + modernization (pre-Phase-4 prep) (#32)\n\nBroad cleanup/modernization across ~90 files. Known deferred (tracked): CommandLineSettingsBinder null-lookahead guard dropped (H-1, reachable NRE via AddArguments with a null element) + no covering test (M-1).",
          "timestamp": "2026-07-14T20:20:56+03:00",
          "tree_id": "86ae21f530edd610fcc258cba66313c1f70b3ee7",
          "url": "https://github.com/existall/SimpleSettings/commit/7f9e17caae2467cf03e0c22f7f6e5a9b2a42a524"
        },
        "date": 1784049802855,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17572816,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "52a2c6f06901384fdd35fe8e9882bdde19337bc9",
          "message": "Phase 4 (Waves 1–2): collection binding + validation engine (#33)\n\n* test(04-01): add failing tests for List<T>-family conversion\n\n- Add fixture interfaces for List/IList/ICollection/IReadOnlyList/IReadOnlyCollection<int>\n- Assert each materializes a List<int> from a delimited scalar\n- Add per-element DayOfWeek list test (element converter chain)\n\n* feat(04-01): add List<T>-family converter with disjoint shape predicates\n\n- Add IsListLike/IsCollectionShape predicates disjoint from IsEnumerable/IsArray\n- Add ListTypeConverter subclassing CollectionTypeConverter; materialize via cached per-element factory delegate (no warm-path reflection, S-4/A1)\n- Promote CollectionTypeConverter.Convert to virtual\n- Register ListTypeConverter after EnumerableTypeConverter, before EnumTypeConverter (disjoint, order preserved)\n\n* test(04-01): add failing tests for empty-not-null collection defaults\n\n- Unbound int[] -> empty int[], unbound List<int> -> empty List<int>\n- Regression: unbound IEnumerable<int> -> empty int[]\n- B-3: two null-binds of List<int> return reference-distinct, mutation-isolated instances\n\n* feat(04-01): empty-not-null default for every collection shape\n\n- CreateNullResult branches IsArray -> IsEnumerable -> IsListLike -> value-type/null\n- Array/IEnumerable keep the shared cached empty array\n- List family binds a fresh empty List<T> per bind via a baked factory delegate (B-3), reusing the existing _nullResult slot so PropertyPlan[] layout is unchanged\n- PropertyConversion.Convert invokes the factory when _nullResult is Func<object>\n\n* test(04-02): add failing child-sequence bind tests for ConfigurationBinder\n\n- child-section sequence binds string[]/int[]/List<string> elements\n- children win over coexisting scalar\n- comma-scalar regression guard (prod override)\n- whitespace scalar and empty sequence bind empty (no [\" \"] token)\n- RootSection prefix honored on the sequence path\n\n* feat(04-02): bind collections from IConfiguration child-section sequences\n\n- grant InternalsVisibleTo to the Binders assembly so ConfigurationBinder\n  reuses Core's internal IsCollectionShape predicate (single source of truth)\n- add GetChildren() branch: indexed sub-sections materialize a right-sized\n  string[] via a manual two-pass walk (no LINQ on the bind path)\n- children win over a coexisting scalar; empty sequence falls through\n- whitespace/empty scalar on a collection target skips SetNewValue so COLL-02's\n  empty-not-null default applies (avoids the [\" \"] token pitfall)\n- element chain and RootSection prefix inherited unchanged\n\n* test(04-02): D-06 secret redaction on the child-sequence bind path\n\n- Convert_SecretInSequenceElement_DoesNotLeakValue: int[] secret in a later\n  element is absent from the whole SettingsPropertyValueException.ToString()\n- first-element case (S-6): element position must not matter\n- List<int> case (S-6): redaction holds across the array and list converter shapes\n- drives the new ConfigurationBinder GetChildren() sequence path, not the scalar\n  InMemoryBinder path\n\n* feat(04-03): sync validation contracts, context ctors, validator attribute, aggregate exception\n\n- ISettingsValidator/ISettingValidation<T>.Validate return ValidationResult (drop Task<>) (D-07)\n- ValidationContext(object)/ValidationContext<T>(T) constructors carry the settings instance (D-08)\n- SettingsValidatorAttribute (AttributeTargets.Interface, Type ValidatorType) (D-11)\n- SettingsValidationException : SimpleSettingsException + static ThrowIfAny shared aggregate-and-throw (D-10/S-3)\n- Resources.SettingsValidationExceptionMessage composed only from author ValidationError text (D-12)\n\n* test(04-03): add failing tests for core-path settings validation\n\n- object-level [SettingsValidator] and property-level [SettingsProperty(ValidatorType)] invocation\n- multi-error aggregation into one SettingsValidationException\n- cross-property rule observing the fully-populated instance\n- D-12 value-free exception message\n- B-2 validator-free short-circuit fast path\n\n* feat(04-03): run declared validators in the core populate path\n\n- thread object-level [SettingsValidator] and property-level [SettingsProperty(ValidatorType)] into the cached plan at build (D-11)\n- SettingsPlan.HasValidators computed once at build; post-populate hook short-circuits before any allocation, error list allocated lazily (review B-2)\n- validators run after the property-set loop so cross-property rules see the full instance (D-09)\n- reflective dispatch selects the Validate overload via GetMethod(name, new[]{closedContextType}) + MakeGenericType context, avoiding AmbiguousMatchException (review S-2)\n- aggregate-and-throw routed through shared SettingsValidationException.ThrowIfAny (review S-3/D-10)\n\n* test(04-05): add failing AllowEmpty=false empty/whitespace rejection tests\n\n- AllowEmpty=false rejects \"\" and whitespace via SettingsPropertyNullException (value-free)\n- AllowEmpty=true still binds \"\" and whitespace as-is\n\n* feat(04-05): reject empty/whitespace strings for AllowEmpty=false (VAL-02)\n\n- Convert now throws value-free SettingsPropertyNullException for null or null-or-whitespace strings when AllowEmpty=false\n- AllowEmpty=true falls through to normal conversion (empty/whitespace bound as-is)\n- 04-01 list null-result factory dispatch preserved\n\n* fix(04-02): single-pass child-sequence bind — reload-safe, drops empty elements\n\n- Enumerate the live config view once (a second GetChildren() pass could race a\n  reload and overrun the count-sized array or inject null holes) — review MED-1.\n- Drop empty entries for parity with the comma-scalar split (RemoveEmptyEntries),\n  so [\"1\",\"\",\"3\"] no longer diverges from \"1,,3\" and crashes int[]/List<int> — review MED-2.\n- PR #33 review comment 1.\n\n* refactor(04-05): check value==null once in PropertyConversion.Convert\n\n- Consolidate the two null branches into one block (throw if required, else the\n  null-result/factory), keeping the whitespace-string reject and the AllowEmpty=true\n  fall-through intact. PR #33 review comment 2.\n\n* refactor(04-03): dispatch validators via ISettingValidation<T> DIM bridge; guard invocation value-free\n\n- ISettingValidation<T> default-implements the base Validate, forwarding to the generic overload, so\n  ValuesPopulator dispatches through a plain ISettingsValidator cast — no MakeGenericType/GetMethod/Invoke\n  reflection and no AmbiguousMatchException workaround (PR #33 comment 4).\n- Wrap validator invocation: a validator that throws is surfaced as the value-free\n  SettingsValidatorInvocationException (never chains the inner, which may hold a secret) — review MED-3.\n- ValidationContext ctor accepts object? (drops the !); fixtures implement only the generic Validate; add\n  property-validator positive/discriminating, object+property aggregation, and throwing-validator coverage.\n\n* test(04-02): lock child-sequence element order + cover empty-element drop\n\n- Assert order-sensitive SequenceEqual on indexed-sequence cases (IsEquivalentTo was order-insensitive\n  and would not catch an order regression) — review test-gap T1.\n- Add an interspersed-empty-element case proving parity with the comma-scalar — review MED-2.\n\n* chore(04): strip internal review-ID tokens from code comments\n\n* refactor(04): fold object validator onto [SettingsSection].ValidatorType (PR #33 comment #3)\n\nReuse the existing section attribute for the object-level validator instead of a separate\nSettingsValidatorAttribute (now deleted; it was unreleased, so not a breaking change).\nValuesPopulator reads the validator Type from SettingsSectionAttribute.ValidatorType; the\ndispatch and value-free exception wrapping are unchanged.\n\nBecause [SettingsSection] is the scan-discovery marker, the deliberately-failing validation\nfixtures would now be eagerly built by the whole-assembly ScanAssemblies tests. Making them\ninternal is not viable: the Reflection.Emit proxy generator emits into a random-GUID dynamic\nassembly that cannot implement an internal interface (verified: TypeLoadException). So the\nfixtures move to a new, never-scanned ExistForAll.SimpleSettings.UnitTests.Fixtures library,\nreferenced by UnitTests but never passed to ScanAssemblies.\n\nAdds a regression test that the merged validator also runs under the assembly-scan path.\nFull suite 143/143 on net10; build clean on net8 + net10.",
          "timestamp": "2026-07-15T21:11:16+03:00",
          "tree_id": "b77a79e6d778d95177f57c2e14a9abd33a9e7456",
          "url": "https://github.com/existall/SimpleSettings/commit/52a2c6f06901384fdd35fe8e9882bdde19337bc9"
        },
        "date": 1784139218390,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17909680,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1388c5c03bd2d9d67da90f1a57980a0fd6304f71",
          "message": "Phase 4: land planning/execution records + child-sequence collection-expression tweak (#34)\n\n* just using [] instead of new\n\n* docs(04): land Phase 4 planning + execution records on master",
          "timestamp": "2026-07-15T21:26:49+03:00",
          "tree_id": "0af39e18f9e2c35b5f0ecfafc64c6f6238e51d48",
          "url": "https://github.com/existall/SimpleSettings/commit/1388c5c03bd2d9d67da90f1a57980a0fd6304f71"
        },
        "date": 1784140156480,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17909680,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "0c858faa53420d95da1fe412bd151d54017e7c49",
          "message": "Phase 4 Wave 3: DI-resolved validator path + ISettingsCollection exposure (VAL-01 DI path, API-02) (#35)\n\n* docs: refresh session handoff (Phase 4 Waves 1-2 + comment #3 merged; Wave 3 next)\n\n* feat: expose ISettingsCollection from AddSimpleSettings (API-02/D-15)\n\nRegister the built ISettingsCollection as a DI singleton and add an\nAddSimpleSettings(out ISettingsCollection, Action?) overload that surfaces\nthe same instance while preserving the IServiceCollection fluent chain.\n\n* feat: DI-resolved settings validator path (VAL-01 DI path)\n\nAdd a deferred, opt-in ISettingsValidationRunner resolved via\nIServiceProvider.ValidateSimpleSettings(). It resolves DI-registered\nISettingValidation<T> from a fresh scope (so scoped-dependency validators\nwork), dispatches through the ISettingsValidator cast (the default-interface\nbridge, no reflection), and aggregates via the shared\nSettingsValidationException.ThrowIfAny so the thrown contract is identical to\nthe core populate path. A throwing validator surfaces value-free as\nSettingsValidatorInvocationException (type-only, no bound value, no inner).\n\n* refactor: address Wave 3 review findings\n\n- ValidateSimpleSettings surfaces a caller-facing error when AddSimpleSettings\n  was not called (internal runner type no longer leaks into the message).\n- Trim runner comments to one line each; drop internal planning-ID reference.\n- Harden tests: deferral test is now an invocation-counter timing probe\n  (0 runs at build, 1 on the explicit call); scope test asserts the scoped\n  validator actually executed; redaction test pins that the secret is bound;\n  add no-op (no DI validators) and misuse coverage.\n\n* docs(planning): Phase 4 Wave 3 (04-04) summary, review, plan patch, state\n\nRecord the VAL-01 DI path + API-02 execution: 04-04-SUMMARY.md, the\ngsd-code-reviewer 04-04-REVIEW.md, STATE.md (all 5 Phase 4 plans landed;\nverify+secure pending), and patch the plan's superseded reflective-dispatch\ntext to the shipped default-interface-bridge dispatch.",
          "timestamp": "2026-07-19T15:18:44+03:00",
          "tree_id": "46ae4cd9e1c5952f4eedc59c8e0c96e898a90a94",
          "url": "https://github.com/existall/SimpleSettings/commit/0c858faa53420d95da1fe412bd151d54017e7c49"
        },
        "date": 1784463656711,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17909680,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "guy.lud@gmail.com",
            "name": "GuyL",
            "username": "guy-lud"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4a8f82ed67b403ceb15b7c125dfafbc007d67287",
          "message": "Phase 5: Documentation (DOC-01) + Phase 4 closeout (#36)\n\n* docs(planning): Phase 4 closeout — verify + security gate + mark complete\n\n- VERIFICATION.md: 6/6 ROADMAP success criteria MET (non-vacuous tests, net8+net10 153/153).\n- SECURITY.md: 15/15 threats closed, 0 open; D-06 secret-redaction gate signed off; T-04-VAL DI path holds by construction in merged code.\n- ROADMAP.md + STATE.md: Phase 4 marked complete; advance to Phase 5 (AOT-01/DOC-01).\n\n* docs: refresh session handoff (Phase 4 complete + closeout held; Phase 5 next)\n\n* docs(05): defer AOT-01 to v2.1, scope phase 5 to Documentation (DOC-01)\n\n* docs(05): capture phase context\n\n* docs(state): record phase 5 context session (Documentation; AOT-01 deferred to v2.1)\n\n* docs(05): research phase documentation domain\n\n* docs(phase-5): add validation strategy\n\n* docs(05): create phase plan (4 plans — docs canonicalization, metadata, deep security page, README rewrite)\n\n* docs(05): harden build/pack verify gates (pipefail) + mark research open-questions resolved\n\n* docs(05): record planning completion in state\n\n* docs(05-01): rename extension guide to canonical filename\n\n- git mv \"Extend Simple Config.md\" -> \"Extending SimpleSettings.md\" (history preserved)\n- establishes canonical link target for inbound repoints and README\n\n* docs(05-01): repoint inbound links + drop residual legacy parenthetical\n\n- repoint 5 in-docs links to Extending%20SimpleSettings.md (2x SectionBinder, 2x building_the_collection, 1x Build Config Interface)\n- remove \"(previously SimpleConfig)\" parenthetical in getting_started.md\n- docs/ now free of legacy product name and dead legacy-repo links\n\n* docs(05-01): complete docs-canonicalization plan\n\n* fix(05-02): correct Description typo and legacy PackageTags token\n\n- Fix 'appliaction' -> 'application' in packaged <Description>\n- Replace legacy 'SimpleConfig' PackageTags token with 'SimpleSettings'\n\n* docs(05-02): complete package-metadata token fix plan\n\n* docs(05-03): author Security & Behavior guarantees page\n\n- Document secret-redaction value-free invariant with both carve-outs (author ValidationError text + DI validator constructors)\n- Add secret-safe validator example (echoes no bound value)\n- Document opt-in/deferred ValidateSimpleSettings() on IServiceProvider after BuildServiceProvider()\n- State validate => discoverable coupling via [SettingsSection(ValidatorType)]\n- Document AddCommandLine spaced-value binding with prefix-lookahead caveat\n\n* docs(05-03): append v1 -> v2 migration section\n\n- List four Phase-3 breaking changes: SettingsHolder internal (API-01), Core.AspNet dropped (PKG-01), per-TFM Microsoft.Extensions floor (PKG-02), public SimpleSettingsException base (EXC-01)\n- Name versions, not the legacy product token\n\n* docs(05-03): complete Security & Behavior guidance plan\n\n* docs(05-04): rewrite README structure — canonical logo, dotnet-add install, ToC, quickstart, DI\n\n- Replace broken existall/Shepherd logo with canonical absolute raw icon URL\n- Drop legacy product tag from title; three dotnet add package install lines\n- Repoint all ToC entries to existall/SimpleSettings docs + add Security & Behavior entry\n- Replace stale [DefaultValue] example with verified [SettingsProperty(DefaultValue = ...)] quickstart\n- Trim IOptions polemic to a tight blurb; add feature overview + DI snippet with ValidateSimpleSettings\n\n* docs(05-04): add concise Security notes + v1->v2 migration sections\n\n- Security notes: state the value-free exception invariant with both carve-outs (author ValidationError text + DI validator constructors); link to docs/Security.md\n- Breaking changes / migration: list API-01/PKG-01/PKG-02/EXC-01 by concept; deep-link docs/Security.md#migration\n- Phase-final gate: 13 DOC-VERIFICATION grep gates + dotnet build/pack -c Release from src/ all green\n\n* docs(05-04): complete README rewrite plan (phase-final gate green)\n\n* docs(phase-05): complete phase execution\n\n* docs: refresh session handoff (Phase 5 complete; PR + Phase 6 next)",
          "timestamp": "2026-07-20T12:48:37+03:00",
          "tree_id": "44743e44b025b15d118beeda51cb35e3a9b39919",
          "url": "https://github.com/existall/SimpleSettings/commit/4a8f82ed67b403ceb15b7c125dfafbc007d67287"
        },
        "date": 1784541057888,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindNoRoot",
            "value": 40,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConfigBinderBenchmark.BindWithRoot",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ConvertArrayBenchmark.ConvertArray",
            "value": 688,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnumerateBenchmark.Enumerate",
            "value": 88,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.EnvBinderBenchmark.BindFastPath",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.GenerateTypeBenchmark.GenerateWarm",
            "value": 0,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 1)",
            "value": 144,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 10)",
            "value": 1376,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.PlanPopulateBenchmark.Populate(PropertyCount: 50)",
            "value": 6816,
            "unit": "bytes"
          },
          {
            "name": "ExistForAll.SimpleSettings.Benchmark.ScanBenchmark.ColdScan",
            "value": 17909680,
            "unit": "bytes"
          }
        ]
      }
    ]
  }
}