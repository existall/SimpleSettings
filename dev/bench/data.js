window.BENCHMARK_DATA = {
  "lastUpdate": 1783883987637,
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
      }
    ]
  }
}