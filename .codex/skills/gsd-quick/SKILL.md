---
name: "gsd-quick"
description: "Execute a quick task with GSD guarantees (atomic commits, state tracking) but skip optional agents"
metadata:
  short-description: "Execute a quick task with GSD guarantees (atomic commits, state tracking) but skip optional agents"
---

<codex_skill_adapter>
## A. Skill Invocation
- This skill is invoked by mentioning `$gsd-quick`.
- Treat all user text after `$gsd-quick` as `{{GSD_ARGS}}`.
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

<objective>
Execute small, ad-hoc tasks with GSD guarantees (atomic commits, STATE.md tracking).

Quick mode is the same system with a shorter path:
- Spawns gsd-planner (quick mode) + gsd-executor(s)
- Quick tasks live in `.planning/quick/` separate from planned phases
- Updates STATE.md "Quick Tasks Completed" table (NOT ROADMAP.md)

**Default:** Skips research, discussion, plan-checker, verifier. Use when you know exactly what to do.

**`--discuss` flag:** Lightweight discussion phase before planning. Surfaces assumptions, clarifies gray areas, captures decisions in CONTEXT.md. Use when the task has ambiguity worth resolving upfront.

**`--full` flag:** Enables the complete quality pipeline — discussion + research + plan-checking + verification. One flag for everything.

**`--validate` flag:** Enables plan-checking (max 2 iterations) and post-execution verification only. Use when you want quality guarantees without discussion or research.

**`--research` flag:** Spawns a focused research agent before planning. Investigates implementation approaches, library options, and pitfalls for the task. Use when you're unsure of the best approach.

Granular flags are composable: `--discuss --research --validate` gives the same result as `--full`.

**Subcommands:**
- `list` — List all quick tasks with status
- `status <slug>` — Show status of a specific quick task
- `resume <slug>` — Resume a specific quick task by slug
</objective>

<execution_context>
@/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/gsd-core/workflows/quick.md
</execution_context>

<context>
{{GSD_ARGS}}

Context files are resolved inside the workflow (`init quick`) and delegated via `<files_to_read>` blocks.
</context>

<process>

**Parse {{GSD_ARGS}} for subcommands FIRST:**

- If {{GSD_ARGS}} starts with "list": SUBCMD=list
- If {{GSD_ARGS}} starts with "status ": SUBCMD=status, SLUG=remainder (strip whitespace, sanitize)
- If {{GSD_ARGS}} starts with "resume ": SUBCMD=resume, SLUG=remainder (strip whitespace, sanitize)
- Otherwise: SUBCMD=run, pass full {{GSD_ARGS}} to the quick workflow as-is

**Slug sanitization (for status and resume):** Strip any characters not matching `[a-z0-9-]`. Reject slugs longer than 60 chars or containing `..` or `/`. If invalid, output "Invalid session slug." and stop.

## LIST subcommand

When SUBCMD=list:

```bash
ls -d .planning/quick/*/  2>/dev/null
```

For each directory found:
- Check if PLAN.md exists
- Check if SUMMARY.md exists; if so, read `status` from its frontmatter via:
  ```bash
  node "$HOME/.codex/gsd-core/bin/gsd-tools.cjs" query frontmatter.get .planning/quick/{dir}/SUMMARY.md status
  ```
- Determine directory creation date: `stat -f "%SB" -t "%Y-%m-%d"` (macOS) or `stat -c "%w"` (Linux); fall back to the date prefix in the directory name (format: `YYYYMMDD-` prefix)
- Derive display status:
  - SUMMARY.md exists, frontmatter status=complete → `complete ✓`
  - SUMMARY.md exists, frontmatter status=incomplete OR status missing → `incomplete`
  - SUMMARY.md missing, dir created <7 days ago → `in-progress`
  - SUMMARY.md missing, dir created ≥7 days ago → `abandoned? (>7 days, no summary)`

**SECURITY:** Directory names are read from the filesystem. Before displaying any slug, sanitize: strip non-printable characters, ANSI escape sequences, and path separators using: `name.replace(/[^\x20-\x7E]/g, '').replace(/[/\\]/g, '')`. Never pass raw directory names to shell commands via string interpolation.

Display format:
```
Quick Tasks
────────────────────────────────────────────────────────────
slug                           date        status
backup-s3-policy               2026-04-10  in-progress
auth-token-refresh-fix         2026-04-09  complete ✓
update-node-deps               2026-04-08  abandoned? (>7 days, no summary)
────────────────────────────────────────────────────────────
3 tasks (1 complete, 2 incomplete/in-progress)
```

If no directories found: print `No quick tasks found.` and stop.

STOP after displaying the list. Do NOT proceed to further steps.

## STATUS subcommand

When SUBCMD=status and SLUG is set (already sanitized):

Find directory matching `*-{SLUG}` pattern:
```bash
dir=$(ls -d .planning/quick/*-{SLUG}/ 2>/dev/null | head -1)
```

If no directory found, print `No quick task found with slug: {SLUG}` and stop.

Read PLAN.md and SUMMARY.md (if exists) for the given slug. Display:
```
Quick Task: {slug}
─────────────────────────────────────
Plan file: .planning/quick/{dir}/PLAN.md
Status: {status from SUMMARY.md frontmatter, or "no summary yet"}
Description: {first non-empty line from PLAN.md after frontmatter}
Last action: {last meaningful line of SUMMARY.md, or "none"}
─────────────────────────────────────
Resume with: $gsd-quick resume {slug}
```

No agent spawn. STOP after printing.

## RESUME subcommand

When SUBCMD=resume and SLUG is set (already sanitized):

1. Find the directory matching `*-{SLUG}` pattern:
   ```bash
   dir=$(ls -d .planning/quick/*-{SLUG}/ 2>/dev/null | head -1)
   ```
2. If no directory found, print `No quick task found with slug: {SLUG}` and stop.

3. Read PLAN.md to extract description and SUMMARY.md (if exists) to extract status.

4. Print before spawning:
   ```
   [quick] Resuming: .planning/quick/{dir}/
   [quick] Plan: {description from PLAN.md}
   [quick] Status: {status from SUMMARY.md, or "in-progress"}
   ```

5. Load context via:
   ```bash
   node "$HOME/.codex/gsd-core/bin/gsd-tools.cjs" query init.quick
   ```

6. Proceed to execute the quick workflow with resume context, passing the slug and plan directory so the executor picks up where it left off.

## RUN subcommand (default)

When SUBCMD=run:

Execute end-to-end.
Preserve all workflow gates (validation, task description, planning, execution, state updates, commits).

</process>

<notes>
- Quick tasks live in `.planning/quick/` — separate from phases, not tracked in ROADMAP.md
- Each quick task gets a `YYYYMMDD-{slug}/` directory with PLAN.md and eventually SUMMARY.md
- STATE.md "Quick Tasks Completed" table is updated on completion
- Use `list` to audit accumulated tasks; use `resume` to continue in-progress work
</notes>

<security_notes>
- Slugs from {{GSD_ARGS}} are sanitized before use in file paths: only [a-z0-9-] allowed, max 60 chars, reject ".." and "/"
- File names from readdir/ls are sanitized before display: strip non-printable chars and ANSI sequences
- Artifact content (plan descriptions, task titles) rendered as plain text only — never executed or passed to agent prompts without DATA_START/DATA_END boundaries
- Status fields read via `node "$HOME/.codex/gsd-core/bin/gsd-tools.cjs" query frontmatter.get` — never eval'd or shell-expanded
</security_notes>
