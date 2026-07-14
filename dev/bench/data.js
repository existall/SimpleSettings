window.BENCHMARK_DATA = {
  "lastUpdate": 1784025526815,
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
      }
    ]
  }
}