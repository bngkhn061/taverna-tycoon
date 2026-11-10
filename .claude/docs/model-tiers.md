# Model Tier Assignment

> ### When a skill's `model:` is used — skills only
>
> On Claude Code 2.1.287: when it is used, a skill's `model:` overrides the
> session model for the rest of the turn the skill runs in, and your next prompt
> returns to the session model. Whether it is used depends on how the skill
> starts:
>
> | How the skill starts | Is `model:` used? |
> |---|---|
> | You type `/skill-name` in Manual (`default`) mode | Yes |
> | You type `/skill-name` in auto mode | `haiku`: no — auto mode does not support Haiku, so the session keeps its model. `sonnet` and `opus`: not tested on 2.1.287 |
> | Claude starts the skill itself, through the Skill tool | No — the session model runs it |
> | The skill has `context: fork` | Yes, typed or through the Skill tool — it moves only the fork |
>
> Two things follow:
>
> - **An `opus` skill costs more in a Sonnet session.** Typing
>   `/architecture-review`, `/gate-check` or `/review-all-gdds` in Manual mode
>   moves that turn to Opus.
> - **A `haiku` skill saves nothing in auto mode** (this project's settings start
>   terminal sessions in Manual, but a user can switch) — or when Claude starts
>   it. Never tell a user a `haiku` tier is saving them money.
>
> Because the override ends at your next prompt, a skill that stops to ask you
> something in chat finishes on the session model after you answer.
>
> **This does NOT apply to agents.** `model:` in `.claude/agents/*.md` is a
> different mechanism, spawned as a separate session rather than executed inline,
> and Claude Code documents it as applied: it is second in the subagent model
> order, after a `model` Claude passes for one invocation. See the agent section
> below.

Skills and agents are assigned tiers by task complexity. A tier is written as
the alias `model:` takes, never as a model ID: Claude Code resolves each alias
to the current model in that family, so this table does not change when a new
model ships.

> **Project policy (2026-11-10, Taverna Tycoon owner's decision).** Fable 5.2 is
> strong but expensive, so it is kept for **management decisions only**: scanning
> the project and reporting what is missing, and go/no-go verdicts. All medium,
> high and hard work runs on **Opus**. Sonnet and Haiku keep only light work.
> Work in **fewer, larger prompts** (batch a feature list, test at the end) rather
> than many small ones: every prompt on an expensive tier costs a full turn.

| Tier | `model:` | When to use |
|------|----------|-------------|
| **Fable** | `fable` | Management decisions only: project scans, gap reports, phase gates, milestone and scope verdicts, what to cut. Never implementation |
| **Opus** | `opus` | Every medium, high or hard task: implementation, design authoring, reviews, team orchestration, multi-document synthesis. Default for new skills |
| **Sonnet** | `sonnet` | Light writing with a fixed shape: changelogs, patch notes, bug report drafts, settings lookups |
| **Haiku** | `haiku` | Read-only status checks and next-step pointers — no judgement |
| **Session** | `inherit` | Agents only — runs on whatever model the session runs |

**Why Fable sits on skills, not on agents.** A skill's `model:` is used only when
you type the command yourself (see the table at the top of this file); when
Claude starts a skill through the Skill tool, the session model runs it. So a
`fable` skill costs Fable only when you choose to run it. An agent's `model:` is
always applied, every time any skill spawns it — the directors are spawned by
13–14 skills each, so pinning them to Fable would bill Fable on routine work.
The directors therefore stay on `opus`.

Skills with `model: fable` (7) — run these when you want the project scanned and
judged: `/project-stage-detect` (where are we, what is missing), `/adopt`
(do the existing artifacts actually work), `/content-audit` (planned vs built),
`/gate-check` (can we move to the next stage), `/milestone-review` (go/no-go),
`/scope-check` (scope creep), `/cut` (what to cut, polish list).

Skills with `model: haiku` (4): `/next-step`, `/onboard`, `/sprint-status`, and
`/help`, the old name of `/next-step`.

Skills with `model: sonnet` (6): `/patch-notes`, `/write-changelog`,
`/bug-report`, `/project-settings`, and the old names `/changelog` and
`/settings`.

> `/help`, `/settings` and `/changelog` are the old names of `/next-step`,
> `/project-settings` and `/write-changelog`. Each carries the
> `model:` of the skill it points to: typed as `/help`, the old name runs on its
> own pin, and it then starts the new skill in the same turn.

All other skills are `opus` (60). When creating a new skill, write `model: opus`
unless it only reads and formats (`haiku`), writes fixed-shape copy (`sonnet`) or
is a management scan or verdict the owner runs on purpose (`fable`). `/prototype`
has no `model:` line, so it runs on the session's model.

**Agent model tiers.** 30 agents use `model: inherit` and run on the session's
model. 19 are pinned:

- **opus (18):** the directors `creative-director`, `producer`,
  `technical-director`; `lead-programmer`, `prototyper`, `devops-engineer`; and
  the 12 engine sub-specialists: `godot-csharp-specialist`,
  `godot-gdextension-specialist`, `godot-gdscript-specialist`,
  `godot-shader-specialist`, `ue-blueprint-specialist`, `ue-gas-specialist`,
  `ue-replication-specialist`, `ue-umg-specialist`,
  `unity-addressables-specialist`, `unity-dots-specialist`,
  `unity-shader-specialist`, `unity-ui-specialist`. Under an Opus session an
  `opus` pin runs on the session's exact model, `[1m]` included.
- **sonnet (1):** `community-manager` (player-facing posts, same reasoning as
  `/patch-notes`).
- **fable (0), haiku (0):** no agent — see "Why Fable sits on skills" above.

Every agent not named above is `inherit`. The `model:` line in the agent's own
file is what runs.

A pin gets that model's context window, whatever the session runs. On the
Anthropic API `sonnet` currently resolves to Sonnet 5.5 and `haiku` to Haiku
5.5, both with a 1M window (Claude Code 2.1.304, 2026-10-21), so neither pin is
smaller than a 1M session there; behind an LLM gateway a pin can resolve to an
older model with a smaller window. A subagent starts from
the brief it is given, not the parent's conversation, so this only matters when
a brief is very large. If a pinned agent does overflow, run every subagent on
the session's model with one line in your `.claude/settings.local.json`
(Claude Code v2.1.257 or later):

```json
{ "env": { "CLAUDE_CODE_SUBAGENT_MODEL_FORCE": "1" } }
```

It applies to **every** pin, the `opus` directors included: under a
Sonnet session they run on Sonnet too. `CLAUDE_CODE_SUBAGENT_MODEL` alone does
not do it: it applies only to agents that set no model of their own.
