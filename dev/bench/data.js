window.BENCHMARK_DATA = {
  "lastUpdate": 1783883216863,
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
      }
    ]
  }
}