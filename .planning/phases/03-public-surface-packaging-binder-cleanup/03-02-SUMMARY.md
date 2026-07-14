---
phase: 03-public-surface-packaging-binder-cleanup
plan: 02
subsystem: binders
tags: [command-line, cli, argument-parsing, lookahead, secret-redaction]

# Dependency graph
requires:
  - phase: 03-01
    provides: finalized package floors (PKG-02) and Core.AspNet-free graph (PKG-01) — stable restore/build graph
provides:
  - CommandLineSettingsBinder space-separated (--k v) parsing via single-token lookahead
  - SkipFirstArgument option (default false) split-by-entry-point exe-skip (D-05 refined)
  - AddCommandLine sourcing Environment.GetCommandLineArgs() so quoted-with-spaces values bind end-to-end
  - empty-safe zero-alloc CLI prefix detection (no IndexOutOfRangeException)
  - CLI-path secret-redaction regression coverage (S2)
affects: [ship, verify-work, public-surface docs]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Split-by-entry-point exe skip: AddArguments binds exactly, AddCommandLine owns the exe drop"
    - "Single-token lookahead where a prefixed next-token is a new key (diverges from Microsoft's unconditional consume)"

key-files:
  created: []
  modified:
    - src/Core/ExistForAll.SimpleSettings.Extensions.Binders/CommandLineSettingsBinderOptions.cs
    - src/Core/ExistForAll.SimpleSettings.Extensions.Binders/CommandLineSettingsBinder.cs
    - src/Core/ExistForAll.SimpleSettings.Extensions.Binders/SettingsBuilderFactoryExtensions.cs
    - src/Tests/ExistForAll.SimpleSettings.UnitTests/Binders/CommandLine/ArgumentsTests.cs

key-decisions:
  - "SkipFirstArgument defaults false; AddArguments binds arg[0], AddCommandLine sets it true internally (D-05 refined, owner-approved)"
  - "Prefix detection uses length-delta after TrimStart + Array.IndexOf on a cached char[] — empty-safe and zero-alloc, never bare token[0]"
  - "A space-separated value beginning with a prefix char (-, /) is treated as a new key and does not bind; use the inline delimiter form (documented on ArgumentPrefixes)"

patterns-established:
  - "Lookahead consumes the next token as value only when the current token was prefixed and the next token is non-prefixed; a prefixed next token leaves the current key valueless (unstored)"
  - "AddCommandLine wraps the caller action so SkipFirstArgument=true is applied first, then caller override runs"

requirements-completed: [SRC-02]

coverage:
  - id: D1
    description: "Space-separated --k v and quoted-with-spaces single token bind to the full value (Phase SC #4)"
    requirement: SRC-02
    verification:
      - kind: unit
        ref: "src/Tests/.../Binders/CommandLine/ArgumentsTests.cs#Build_WhenSpaceSeparatedValueHasSpaces_BindsFullValue"
        status: pass
    human_judgment: false
  - id: D2
    description: "SkipFirstArgument default false binds arg[0]; true skips it (split-by-entry-point exe skip, D-05)"
    requirement: SRC-02
    verification:
      - kind: unit
        ref: "src/Tests/.../Binders/CommandLine/ArgumentsTests.cs#Build_WhenSkipFirstArgumentTrue_SkipsIndexZero"
        status: pass
    human_judgment: false
  - id: D3
    description: "Prefixed next-token is a new key; prefixed space-separated value does not bind (D-04/A4)"
    requirement: SRC-02
    verification:
      - kind: unit
        ref: "src/Tests/.../Binders/CommandLine/ArgumentsTests.cs#Build_WhenNextTokenIsPrefixed_KeyIsNotStored, #Build_WhenSpaceSeparatedValueIsPrefixed_DoesNotBind"
        status: pass
    human_judgment: false
  - id: D4
    description: "Empty-string token parsed safely with no IndexOutOfRangeException (A3/P1)"
    requirement: SRC-02
    verification:
      - kind: unit
        ref: "src/Tests/.../Binders/CommandLine/ArgumentsTests.cs#Build_WhenEmptyToken_ParsesSafelyAndBindsRest"
        status: pass
    human_judgment: false
  - id: D5
    description: "CLI-path unconvertible value never leaks: exception ToString() chain excludes the value while naming Age + Int32 (SEC-01/S1/S2)"
    requirement: SRC-02
    verification:
      - kind: unit
        ref: "src/Tests/.../Binders/CommandLine/ArgumentsTests.cs#Build_WhenCliValueUnconvertible_ExceptionExcludesValue"
        status: pass
    human_judgment: false
  - id: D6
    description: "AddCommandLine sources Environment.GetCommandLineArgs() and enables SkipFirstArgument=true (Open Q #1 resolved, in-scope)"
    requirement: SRC-02
    verification:
      - kind: automated
        ref: "grep Environment.GetCommandLineArgs() && grep 'SkipFirstArgument = true' in SettingsBuilderFactoryExtensions.cs + Release build"
        status: pass
    human_judgment: false

# Metrics
duration: 4min
completed: 2026-07-14
status: complete
---

# Phase 3 Plan 2: Command-Line Binder Cleanup Summary

**Command-line binder now parses real-world argv — space-separated `--k v` and quoted-with-spaces values bind via single-token lookahead, the exe is dropped only at the `AddCommandLine()` entry point (SkipFirstArgument default false), and prefix detection is empty-safe and zero-alloc.**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-07-14T12:50:07Z
- **Completed:** 2026-07-14T12:53:49Z
- **Tasks:** 2 completed
- **Files modified:** 4

## Accomplishments
- Rewrote `CommandLineSettingsBinder.Parse` from a plain foreach into an index/lookahead loop: inline `--k=v`/`--k:v` preserved, new space-separated `--k v` support, a prefixed next-token treated as a new key (SimpleSettings deliberately diverges from Microsoft's unconditional consume).
- Added `SkipFirstArgument` (default **false**) implementing the refined split-by-entry-point exe skip: `AddArguments(Main(string[]))` binds exactly what it is handed; `AddCommandLine()` enables the skip internally.
- Switched `AddCommandLine` from `Environment.CommandLine.Trim().Split(' ')` to `Environment.GetCommandLineArgs()`, so a quoted value with spaces binds end-to-end unconditionally (Open Q #1 resolved, in-scope).
- Prefix detection made empty-safe and zero-alloc (length-delta after `TrimStart` for the current token; `Array.IndexOf` over a cached prefix `char[]` for the next token) — no bare `token[0]`, no per-token allocation.
- Added TUnit coverage incl. quoted-with-spaces headline, prefixed-next=new-key, prefixed-value non-binding, empty-token no-crash, explicit SkipFirstArgument true/false, and a CLI-path secret-redaction regression (S2).

## Task Commits

Each task was committed atomically:

1. **Task 1 (RED): failing CLI binder cases** - `a9558fb` (test)
2. **Task 1 (GREEN): SkipFirstArgument option + lookahead Parse** - `29f5469` (feat)
3. **Task 2: AddCommandLine uses GetCommandLineArgs + owns exe skip** - `ac537cf` (feat)

_Note: Task 1 is TDD — RED (test) then GREEN (feat). No refactor commit was needed._

## Files Created/Modified
- `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/CommandLineSettingsBinderOptions.cs` - Added `SkipFirstArgument` (default false) + XML-doc note on `ArgumentPrefixes` (prefixed space-separated value is a new key, not a value).
- `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/CommandLineSettingsBinder.cs` - `Parse` rewritten to an index/lookahead loop with optional index-0 skip and empty-safe zero-alloc prefix detection; `SplitByDelimiter` unchanged; `BindPropertySettings` untouched (store-only, S1).
- `src/Core/ExistForAll.SimpleSettings.Extensions.Binders/SettingsBuilderFactoryExtensions.cs` - `AddCommandLine` sources `Environment.GetCommandLineArgs()` and wraps the caller action to enable `SkipFirstArgument=true` (exe skip owned by this entry point, caller-overridable); `AddArguments` and signatures unchanged.
- `src/Tests/ExistForAll.SimpleSettings.UnitTests/Binders/CommandLine/ArgumentsTests.cs` - 10 new `[Test]` cases; class name kept `ArgumentsTests` for the treenode-filter; existing leading-space fixture unchanged.

## Decisions Made
- Kept the exe skip split by entry point (D-05 refined) rather than a shared default-true — prevents silently dropping a real first argument for `AddArguments` callers.
- Prefix detection reuses the already-computed `name`/`key` length delta for the current token (no second scan) and `Array.IndexOf` over a cached `char[]` for the next token — allocation-free and empty-safe.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. RED build failed to compile as expected (missing `SkipFirstArgument`), which is the intended RED gate; GREEN implementation turned all 26 `ArgumentsTests` green and the full 208-test suite passes on the local net10 runtime (both TFM DLLs), 0 warnings / 0 errors on the CI-flavored Release build.

## User Setup Required

None - no external service configuration required.

## Verification Evidence

- Targeted: `dotnet test SimpleSettings.slnx -c Release --treenode-filter "/*/*/ArgumentsTests/*"` — 26 passed, 0 failed.
- Plan-completion gate (both TFMs, CI-flavored): `dotnet restore` up-to-date; `dotnet build -c Release --no-restore -p:ContinuousIntegrationBuild=true` — Build succeeded, 0 Warning(s), 0 Error(s); `dotnet test -c Release --no-build` — **208 passed, 0 failed, 0 skipped**.
- Task 2 grep gate: `Environment.GetCommandLineArgs()` and `SkipFirstArgument = true` both present in `SettingsBuilderFactoryExtensions.cs`.

## Threat Model Discharge
- T-03-04 (Tampering): values are stored as strings only; lookahead adds no injection sink; the empty-safe prefix detection removes the reachable `IndexOutOfRangeException` (A3). Mitigated.
- T-03-05 (Info Disclosure — exe path): accepted; `AddCommandLine` drops the exe at [0] via internally-enabled `SkipFirstArgument`.
- T-03-06 (Info Disclosure — secret redaction, S1): the rewritten `Parse` runs at construction/config-time outside the `ValuesPopulator` try/catch; `BindPropertySettings` stays store-only and untouched; CLI-path regression test (D5) asserts the unconvertible sentinel is absent from the thrown exception's full `ToString()` chain while `Age` + `Int32` remain. Mitigated.

## Self-Check: PASSED
