---
name: "gsd-mempalace-capture"
description: "File a phase artifact into MemPalace; mirror decision facts into its temporal KG"
metadata:
  short-description: "File a phase artifact into MemPalace; mirror decision facts into its temporal KG"
---

<codex_skill_adapter>
## A. Skill Invocation
- This skill is invoked by mentioning `$gsd-mempalace-capture`.
- Treat all user text after `$gsd-mempalace-capture` as `{{GSD_ARGS}}`.
- If no arguments are present, treat `{{GSD_ARGS}}` as empty.

## B. AskUserQuestion → request_user_input Mapping
GSD workflows use `AskUserQuestion` (Claude Code syntax). Translate to Codex `request_user_input`:

Parameter mapping:
- `header` → `header`
- `question` → `question`
- Options formatted as `"Label" — description` → `{label: "Label", description: "description"}`
- Generate `id` from header: lowercase, replace spaces with underscores

Batched calls:
- `AskUserQuestion([q1, q2])` → single `request_user_input` with multiple entries in `questions[]`

Multi-select workaround:
- Codex has no `multiSelect`. Use sequential single-selects, or present a numbered freeform list asking the user to enter comma-separated numbers.

Execute mode fallback:
- When `request_user_input` is rejected or unavailable, activate TEXT_MODE: append `--text` to `{{GSD_ARGS}}` so the workflow's built-in text-mode branching takes over. Present every `AskUserQuestion` call as a plain-text numbered list, then stop and wait for the user's reply. Do NOT pick a default and continue (#3018 / #3808).
- You may only proceed without a user answer when one of these is true:
  (a) the invocation included an explicit non-interactive flag (`--auto` or `--all`),
  (b) the user has explicitly approved a specific default for this question, or
  (c) the workflow's documented contract says defaults are safe (e.g. autonomous lifecycle paths).
- Do NOT write workflow artifacts (CONTEXT.md, DISCUSSION-LOG.md, PLAN.md, checkpoint files) until the user has answered the plain-text questions or one of (a)-(c) above applies. Surfacing the questions and waiting is the correct response — silently defaulting and writing artifacts is the #3018 failure mode.

## C. Task() → spawn_agent Mapping
GSD workflows use `Task(...)` (Claude Code syntax). Translate to Codex collaboration tools:

**Schema detection (required first step):** Codex exposes two `spawn_agent` schemas:
- **agent_type-capable schema** (e.g. `multi_agent_v2`): `spawn_agent` accepts `agent_type`, `message`, `reasoning_effort`, `fork_context`, etc. — typed GSD agent dispatch is available.
- **Generic schema** (`multi_agent_v1`): `spawn_agent` accepts only `message`, `items`, `fork_context` — there is **no `agent_type` field**. Typed GSD agent dispatch is unavailable in this session.

Before spawning, inspect the `spawn_agent` tool's visible parameter schema (via `tool_search` or the tool list) to determine which form is active.

Typed mapping (agent_type-capable schema only):
- `Task(subagent_type="X", prompt="Y")` → `spawn_agent(agent_type="X", message="Y")`
- `Agent(subagent_type="X", prompt="Y")` → `spawn_agent(agent_type="X", message="Y")`
- `Task(model="...")` → omit. `spawn_agent` has no inline `model` parameter;
  GSD embeds the resolved per-agent model directly into each agent's `.toml`
  at install time so `model_overrides` from `.planning/config.json` and
  `~/.gsd/defaults.json` are honored automatically by Codex's agent router.
- Resolved `reasoning_effort="low|medium|high|xhigh"` (`xhigh` is a GSD/Codex tier, not a generic runtime enum) → pass `reasoning_effort`
  to `spawn_agent` when the runtime/tool supports it. Omit missing, empty,
  inherited, or unsupported values; do not invent one-off effort literals in
  workflow prose.
- `fork_context: false` by default — GSD agents load their own context via `<files_to_read>` blocks
- `Task(isolation="worktree")` / `Agent(isolation="worktree")` → no direct Codex mapping.
  Codex `spawn_agent` does not create or bind a git worktree automatically.
  Workflows that require this isolation must fail closed or use an explicit
  manual worktree protocol before spawning (#3360).

Generic-agent workaround (multi_agent_v1 schema — NO agent_type field):
When only the generic `multi_agent_v1` schema is available, typed GSD agent dispatch
(`gsd-planner`, `gsd-executor`, etc.) is NOT possible. This is a known Codex limitation
(openai/codex#15250). **This workaround is NOT equivalent to typed gsd-planner/gsd-executor
execution** — GSD agents carry project-aware prompts, audit logging, and workflow context
that a generic subagent lacks. Use the following fallback:
1. Resolve your active Codex config root — the directory that contains your `config.toml`.
   This directory is determined in priority order: `$CODEX_HOME` (if set), the path given
   by `--config-dir` (if passed on invocation), a local `.codex` directory in the current
   project (if `--local` was used), or the default global config directory. Read
   `agents/<agent-name>.toml` relative to that config root to extract the agent's system
   instructions.
2. Inject those instructions as a role-preamble into a generic `spawn_agent(message=...)` call.
3. Label results and logs clearly as "generic-agent workaround" so the orchestrator and user
   know full typed-agent guarantees are not in effect.
4. Where typed dispatch is mandatory for correctness (e.g. worktree isolation), fail closed
   and report the schema limitation rather than silently degrading.

Spawn restriction:
- Codex restricts `spawn_agent` to cases where the user has explicitly
  requested sub-agents. When automatic spawning is not permitted, do the
  work inline in the current agent rather than attempting to force a spawn.
- In some Codex sessions, multi-agent tooling can be deferred. If `spawn_agent`
  is not currently visible, discover tools first via `tool_search` before
  defaulting to inline execution.

Parallel fan-out:
- Spawn multiple agents → collect agent IDs → `wait(ids)` for all to complete

Result parsing:
- Look for structured markers in agent output: `CHECKPOINT`, `PLAN COMPLETE`, `SUMMARY`, etc.
- `close_agent(id)` after collecting results from each agent
</codex_skill_adapter>

**STOP -- DO NOT READ THIS FILE. You are already reading it. This prompt was injected into your context by the command system. Using the Read tool on this file wastes tokens. Begin executing Step 0 immediately.**

## Step 0 -- Banner

**Before ANY tool calls**, display this banner:

```
GSD > MEMPALACE CAPTURE
```

Then proceed to Step 1.

## Step 1 -- Config Gate

Check whether the MemPalace capability is enabled by reading `.planning/config.json` directly with the Read tool.

1. Read `.planning/config.json` with the Read tool.
2. If the file does not exist, or `config.mempalace` is absent, or `config.mempalace.enabled !== true`, or `config.mempalace.capture_artifacts !== true`: display the disabled message and **STOP**.
3. Otherwise proceed to Step 2.

**Disabled message:**

```
GSD > MEMPALACE CAPTURE

MemPalace capture is disabled (mempalace.enabled / mempalace.capture_artifacts).
Nothing was filed; the loop proceeds normally.
```

This step is `onError: skip` at `discuss:post` / `plan:post` / `verify:post` -- capture never fails a phase.

## Step 2 -- Resolve target

1. **Artifact.** Take the artifact from `{{GSD_ARGS}}`. If absent, infer from the loop point: `discuss:post` → `CONTEXT.md`, `plan:post` → `PLAN.md`, `verify:post` → `SUMMARY.md`.
2. **Room.** Map artifact → room:
   - `CONTEXT.md` → `decisions`
   - `PLAN.md` → `planning`
   - `SUMMARY.md` → `milestones`
   (Confirmed problem→fix pairs go to `problems` — see the `capture-problems` fragment used at `execute:wave:post`.)
3. **Wing.** `config.mempalace.wing` if non-empty, else `config.project_code`, else the repo directory name.
4. **Mode / transport.** Read `config.mempalace.memory_mode`. Prefer MCP (`mempalace_*`) when your MemPalace MCP server is registered and your runtime permits those tools; otherwise use the `mempalace` CLI (covered by this skill's `Bash` allow-tool), as in `mempalace-recall`.

## Step 3 -- File verbatim (idempotent)

On any error or timeout, stop and let the phase continue -- capture is best-effort.

1. **Dedup first.** Interactive: `mempalace_check_duplicate` on the artifact's deterministic drawer id. Headless: rely on `mempalace mine`'s content-hash idempotency.
2. **Add the drawer (verbatim).** File the exact artifact text into `room: <room>` of `wing: <wing>` with provenance (`source_file`, phase id). Interactive: `mempalace_add_drawer`. Headless: `mempalace mine <path> --wing <wing> --room <room>`.
3. **Mirror KG facts** when `config.mempalace.mirror_kg` is true: extract decision/delivery facts and `mempalace_kg_add` them with `valid_from` = the phase date (e.g. `(<project>, decided, <decision>)` from CONTEXT; `(<phase>, delivered, <capability>)` from SUMMARY). Only `augment` is currently wired, so these are an *additive* mirror of `.planning/graphs/`. (`kg_backend`/`replace` are forward-declared and behave as `augment` today.)
4. Re-running a phase MUST NOT create duplicate drawers (deterministic ids + `check_duplicate`).

## Step 4 -- Report

Print a one-line summary: `Filed <artifact> → <wing>/<room> (<n> KG facts)` or `MemPalace unavailable — capture skipped`.

## Anti-Patterns

1. DO NOT let any MemPalace error fail the step -- capture is `onError: skip`.
2. DO NOT write lossy summaries -- store the verbatim artifact text (AAAK compression is a separate, optional index).
3. DO NOT prune or delete drawers here -- pruning (`sync --apply`) is the curator agent's job at `ship:post`, wing-scoped only.
4. DO NOT skip the config gate or the dedup check.
