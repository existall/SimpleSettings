---
name: "gsd-surface"
description: "Toggle which skills are surfaced — apply a profile, list, or disable a cluster without reinstall"
metadata:
  short-description: "Toggle which skills are surfaced — apply a profile, list, or disable a cluster without reinstall"
---

<codex_skill_adapter>
## A. Skill Invocation
- This skill is invoked by mentioning `$gsd-surface`.
- Treat all user text after `$gsd-surface` as `{{GSD_ARGS}}`.
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
Manage the runtime skill surface without reinstall. Reads/writes `/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/.gsd-surface.json`
(sibling to `/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/.gsd-profile`) and re-stages the active skills directory in place.
Skill dirs live at `/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/skills/gsd-*/`.

Sub-commands: list · status · profile · disable · enable · reset
</objective>

## Sub-command routing

Parse the first token of {{GSD_ARGS}}:

| Token | Action |
|---|---|
| `list` | Show enabled + disabled clusters and skills |
| `status` | Alias for `list` plus token cost summary |
| `profile <name>` | Write `baseProfile` and re-stage |
| `profile <n1>,<n2>` | Composed profiles (comma-separated, no spaces) |
| `disable <cluster>` | Add cluster to `disabledClusters`, re-stage |
| `enable <cluster>` | Remove cluster from `disabledClusters`, re-stage |
| `reset` | Delete `.gsd-surface.json`, return to install-time profile |
| *(none)* | Treat as `list` |

---

## list / status

Load the capability registry and call `listSurface(runtimeConfigDir, manifest, CLUSTERS, registry)` from
`gsd-core/bin/lib/surface.cjs`. The registry is loaded via:
```js
const registry = require('gsd-core/bin/lib/capability-registry.cjs');
```
Display:

```
Enabled (N skills, ~T tokens):
  core_loop:   new-project  discuss-phase  plan-phase  execute-phase  help  update
  audit_review: …
  …

Disabled:
  utility:  health  stats  settings  …

Token cost: ~T (budget cap ~500 tokens for 200k context @ 1%)
```

For `status` also append:

```
Base profile:   standard  (from .gsd-surface.json)
Install profile: standard  (from .gsd-profile)
```

---

## profile \<name\>

1. Read current surface: `readSurface(runtimeConfigDir)` → if null, seed from `readActiveProfile(runtimeConfigDir)`.
2. Set `surfaceState.baseProfile = name`.
3. `writeSurface(runtimeConfigDir, surfaceState)`.
4. Resolve and re-apply:
   ```js
   const registry = require('gsd-core/bin/lib/capability-registry.cjs');
   const layout = resolveRuntimeArtifactLayout(runtime, runtimeConfigDir, scope);
   applySurface(runtimeConfigDir, layout, manifest, CLUSTERS, registry);
   ```
5. Confirm: "Surface updated to profile `<name>`. N skills enabled."

---

## disable \<cluster\>

Valid cluster names: `core_loop`, `audit_review`, `milestone`, `research_ideate`,
`workspace_state`, `docs`, `ui`, `ai_eval`, `ns_meta`, `utility`.

1. Validate cluster name against `Object.keys(CLUSTERS)`.
2. Read or initialize surface state.
3. Add cluster to `surfaceState.disabledClusters` (deduplicate).
4. `writeSurface` → resolve layout → `applySurface`:
   ```js
   const registry = require('gsd-core/bin/lib/capability-registry.cjs');
   const layout = resolveRuntimeArtifactLayout(runtime, runtimeConfigDir, scope);
   applySurface(runtimeConfigDir, layout, manifest, CLUSTERS, registry);
   ```
5. Confirm: "Disabled cluster `<cluster>`. N skills removed from surface."

---

## enable \<cluster\>

1. Read surface state; if null, nothing to enable — print "No surface delta active."
2. Remove cluster from `surfaceState.disabledClusters`.
3. `writeSurface` → resolve layout → `applySurface`:
   ```js
   const registry = require('gsd-core/bin/lib/capability-registry.cjs');
   const layout = resolveRuntimeArtifactLayout(runtime, runtimeConfigDir, scope);
   applySurface(runtimeConfigDir, layout, manifest, CLUSTERS, registry);
   ```
4. Confirm: "Enabled cluster `<cluster>`. N skills added back to surface."

---

## reset

1. Check if `.gsd-surface.json` exists.
2. Delete it.
3. Re-apply using only `readActiveProfile(runtimeConfigDir)` (install-time profile).
4. Confirm: "Surface reset to install-time profile `<name>`."

---

## runtimeConfigDir resolution

The `runtimeConfigDir` for `applySurface` is the **base the agent config directory**
(`~/.codex`), NOT the skills sub-directory (`/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/skills`).

This matches `installRuntimeArtifacts` and `uninstallRuntimeArtifacts`, which also
receive `~/.codex` as `configDir`. The skill dirs themselves live at
`/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/skills/gsd-*/` because the `claude global` layout has `destSubpath =
'skills'` — they are derived from `configDir`, not the root for it.

```bash
# Claude Code — global install
RUNTIME_CONFIG_DIR="${CLAUDE_CONFIG_DIR:-$HOME/.codex}"
SCOPE="global"

# Artifact destinations are derived from runtime layout
# via resolveRuntimeArtifactLayout(runtime, RUNTIME_CONFIG_DIR, SCOPE)
# then applySurface(RUNTIME_CONFIG_DIR, layout, manifest, CLUSTERS)
```

Surface state is stored at `${RUNTIME_CONFIG_DIR}/.gsd-surface.json`
(i.e. `/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/.gsd-surface.json`).

All paths can be overridden by reading the `CLAUDE_CONFIG_DIR` env var if set.

---

## Error handling

- Unknown cluster name → list valid cluster names, exit without writing.
- Unknown profile name → list known profiles (`core`, `standard`, `full`), exit.
- Missing `surface.cjs` → prompt: "Run `npm i -g gsd-core` to reinstall GSD."

<execution_context>
Surface state file: `/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/.gsd-surface.json`
Install profile marker: `/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/.gsd-profile`
Skill dirs: `/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/skills/gsd-*/`
Engine module: `/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/gsd-core/bin/lib/surface.cjs`
Cluster definitions: `/Users/guyludvig/frontegg/development/open-source/SimpleSettings/.codex/gsd-core/bin/lib/clusters.cjs`
</execution_context>
