# Adoption Plan

> **Generated**: 2026-11-09
> **Project phase**: Production (inferred — 45 C# files under the code root; no `project.stage` set, which is not a gap at `workflow: minimal`)
> **Engine**: Unity 6000.6.8f1 (Unity 6.6, URP)
> **Template version**: CCGS 1.3.0
> **Workflow tier**: minimal (rigor: minimal) — the design record is `design/game-brief.md`; GDDs, ADRs and UX specs are not expected.

Work through these steps in order. Check off each item as you complete it.
Re-run `/adopt` anytime to check remaining gaps.

**Audit scope.** Audited: the game brief, the engine reference, project config
(`project.yaml` + `technical-preferences.md`), the v1.0 migration check (not
needed: `project.yaml` exists), plus three project-health items outside the
format audit (git, tests, `jq`). Not audited, by tier: GDD/ADR/systems-index/story
formats (none exist; not expected at `minimal`), TR registry, control manifest,
sprint status, stage file.

---

## Step 1: Fix Blocking Gaps

### 1.1 `design/game-brief.md` is missing
**Problem:** At the minimal tier the one-page brief is the whole design record,
and `/create-stories`, `/cut`, `/prototype` and `/next-step` read it. The design
currently lives in the root `GAME_DESIGN.md` (Turkish headings, not the template
format), so those skills find nothing.
**Fix:** Write `design/game-brief.md` from `.claude/docs/templates/game-brief.md`,
filling it from `GAME_DESIGN.md`, `DESIGN_RULES.md`, `CHARACTERS.md` and what is
already built. Headings and marks stay in English; content may be Turkish. Mark
every built feature `[x]`. Keep `GAME_DESIGN.md` as the detailed reference — do
not delete or rewrite it.
Required fields: Pitch, Core loop, Goal & fail state, Features, Out of scope, Build order.
**Time:** 30 min
- [x] `design/game-brief.md` written and approved (2026-11-09)

---

## Step 2: Fix High-Priority Gaps

### 2.1 The project is not under version control
**Problem:** There is no git repository. 45 scripts, the Blender generators and
all generated assets have no history and no undo; `CLAUDE.md` assumes Git, and
`/patch-notes`, `/write-changelog` and `/retrospective` read git history.
**Fix (manual, needs your OK):** `git init` at the repo root, a Unity-aware
`.gitignore` (exclude `Unity/TavernaTycoon/Library/`, `Temp/`, `Logs/`, `obj/`,
`UserSettings/`, `Unity/Builds/`, `Unity/captures/`, `production/session-state/`,
`production/session-logs/`), Git LFS for `.fbx`/`.png`/`.blend` if the repo will
be pushed, then a first commit.
**Time:** 30 min
- [x] Git repository initialised with `.gitignore` (2026-11-09; 2,275 files / 73.9 MB tracked, largest file 2.8 MB — no LFS needed while the repo stays local)
- [ ] First commit made

---

## Step 3: Bootstrap Infrastructure

Not on the minimal path — the brief's build order is the plan. Next:
`/create-stories` (no stories exist yet).

---

## Step 4: Medium-Priority Gaps

### 4.1 No automated tests
**Problem:** `Assets/Tests/` does not exist, so the balance formulas in
`Scripts/Core/Balance.cs` (level cost, upgrade curves, patience) are untested,
and Logic stories cannot pass their evidence gate.
**Fix:** `/test-setup` (Unity Test Framework, EditMode folder + asmdef), then
EditMode tests for `Balance.cs` first.
**Time:** 1 session
- [ ] Test framework set up; `commands.test` runs and reports > 0 tests
- [ ] `Balance.cs` covered by EditMode tests

### 4.2 `jq` is not installed
**Problem:** Several hooks parse JSON with `jq`; without it their checks are
skipped, which looks the same as passing.
**Fix:** `winget install jqlang.jq`, then restart Claude Code.
**Time:** 5 min
- [ ] `jq --version` works in the Bash tool

### 4.3 `project.goal` is not set
**Problem:** `/cut` and `/prototype` guess the goal (default `hobby`); a
`mobile`/`sell` goal changes what `/cut` asks (target and freeze dates).
**Fix:** `/project-settings project.goal=mobile` (or `sell`, `hobby`, …).
**Time:** 5 min
- [ ] `project.goal` set

---

## Step 5: Optional Improvements

### 5.1 `GAME_DESIGN.md` is not in GDD format
Advisory at `minimal` — no conversion needed. It stays the detailed reference
next to the brief. If the project later moves to `standard`, split it with
`/design-system` per system.
- [ ] (optional) Decide whether to stay at `minimal`

### 5.2 Minimum test coverage not set
`technical-preferences.md` → Testing → Minimum Coverage is `[TO BE CONFIGURED]`.
Set it once tests exist.
- [ ] Coverage target set

---

## What to Expect from Existing Stories

There are no stories yet. Stories written by `/create-stories` from the new brief
will follow the current format.

---

## Re-run

Run `/adopt` again after completing Steps 1–2 to verify the blocking and high
gaps are resolved. The new run will reflect the current state of the project.
