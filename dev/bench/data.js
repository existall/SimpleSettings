window.BENCHMARK_DATA = {
  "lastUpdate": 1783945485497,
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
      }
    ]
  }
}