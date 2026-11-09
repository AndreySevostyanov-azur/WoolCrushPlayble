# WoolCrushPlayble - Agent Operating Contract

Always-loaded contract for AI agents working in this repo: a **Luna playable ad** (Unity, URP, Leopotam EcsLite) for the
WoolCrush rope/spool puzzle. It defines behavioral rules, the knowledge-base (KB) protocol and the Plan -> Implement -> Review workflow.
It overrides default behavior.

---

## 1. Behavioral rules

_Adapted from [karpathy/AGENTS.md](https://github.com/multica-ai/andrej-karpathy-skills/blob/main/AGENTS.md)._

1. **Deliberate planning.** Before writing code, surface assumptions and uncertainties. If something is unclear, stop and name what is confusing.
   If several interpretations exist, present them - do not silently pick one. Gameplay rules and tuning values the request does not give are questions, not defaults.
2. **Lean implementation.** Build only what was asked. No features beyond the request, no abstractions for single-use code.
3. **Minimal edits.** Restrict the diff to the request. No drive-by reformatting, no deleting pre-existing dead code unless asked.
   Every changed line traces to the request.
4. **Verification-driven.** Define concrete success criteria before starting. Run the check the user specified, not a proxy for it.
   Report each verification with its method and what that method cannot detect. A fact that a read, grep or git command can settle is checked before it is stated.
   Editor Play proves logic and wiring only; it cannot prove Luna export behaviour, JS compatibility, size, shaders or real touch input - report those as **unverified**
   until the user tests an export. A claim that players see something names the code path that produces it and the scene/prefab/asset that displays it.
5. **Comments only where they carry weight.** Prefer clear names and small functions; do not narrate what the code does, do not add doc banners
   or "what changed" notes. A short comment is welcome for a non-obvious Luna/JS-conversion footgun or a "why" that the code cannot express
   (the existing `Luna-safe` comments are the model). Existing comments in files you edit stay (rule 3).
6. **Name the debt.** If you take a shortcut over a better design you can already see, say so explicitly (a TODO and a note in your report).
   If you deliver less than asked, name the omitted part and the reason.

Coding conventions live in [`.claude/kb/01-conventions.md`](.claude/kb/01-conventions.md). There is no other rules file (no `.github/project-context.md`).

---

## 2. Knowledge-base protocol (MANDATORY)

The KB lives in [`.claude/kb/`](.claude/kb/) and is the source of truth for how the project is built.

### Before any task
1. Read [`.claude/kb/INDEX.md`](.claude/kb/INDEX.md).
2. Read the KB doc(s) for the area you touch (the INDEX table says which), plus `00-overview.md` and `01-conventions.md`.
3. KB docs are point-in-time snapshots - **verify cited paths and symbols against current code** before relying on them.

### After any task
If the task changed architecture, patterns, the system order in `GameBootstrap`, components/events, scene wiring, Luna settings or file paths:
update the relevant doc **and** append one line to [`.claude/kb/UPDATELOG.md`](.claude/kb/UPDATELOG.md) (`YYYY-MM-DD | task summary | docs touched`);
bump `Last verified:` for sections you re-confirmed. Pure bug-fixes with no structural change need no KB edit - say so explicitly.
Record verified facts only: grep each symbol and `file:line` before saving; a claim about runtime control flow is written only after reading every call on that path,
otherwise state the observed symptom and mark it unverified. Use the `woolcrush-kb-update` skill for the procedure.

---

## 3. Workflow: Plan -> Implement -> Review

| Task size | Flow |
|-----------|------|
| **Trivial** (typo, rename, one-liner, tuning value) | Do it directly, check it compiles, apply the KB after-task check |
| **Determined fix** (the verified root cause dictates every changed line) | Edit directly, verify, one review pass on the diff |
| **Non-trivial** (new system, changed rules, new mechanic, scene rework) | Plan -> implement -> review -> fix -> update KB |

Custom subagents live in [`.claude/agents/`](.claude/agents/): `planner`, `coder`, `reviewer`; `/feature` runs the whole pipeline.
Use them when the user asks for subagents, delegation or `/feature`; otherwise run the same phases sequentially in the main thread.

**Loop rule:** if review returns FAIL, apply the fixes, then review again. Do not mark the task done while review is FAIL.

**Finding classification (FAIL routing).**
- **Mechanical** (naming, formatting, unused using, missing null guard): apply directly.
- **Structural** (new type, changed API, logic moved between systems, changed system order, scene wiring): present the finding and proposed fix to the user and wait for approval.

### Critical changes (extra care, user confirmation before coding)
- Changing the system order in `GameBootstrap.BindSystems` or the timing of an event (created/consumed/cleaned up).
- Changing win/lose conditions or the end-of-game path (`DragonGameEndSystem`, `EndScreenView`, `LifeCycle.GameEnded`, `InstallFullGame`).
- Changing `luna.json`, `Assets/SVC_Luna.asset`, `Packages/manifest.json`, or the scene list.
- Anything that may break under Luna's JS conversion (reflection, async, threads, new packages, new shaders).

### Dirty working tree
The tree normally holds unrelated, machine-specific Unity churn (`luna.json`, `Assets/SVC_Luna.asset`, `ProjectSettings/ProjectVersion.txt`, `Packages/*`).
Identify the task's files, leave the rest alone, never stage broadly. Treat Unity YAML (scenes, prefabs, assets) as high risk: prefer Editor-side wiring;
if YAML must be edited by hand, keep GUIDs/fileIDs intact and run `git diff --check`.

---

## 4. KB index

- [`00-overview.md`](.claude/kb/00-overview.md) - what the playable is, layout, scenes, packages, namespaces
- [`01-conventions.md`](.claude/kb/01-conventions.md) - style, naming, EcsLite data-access idiom, Luna-safe rules
- [`02-gameplay-ecs.md`](.claude/kb/02-gameplay-ecs.md) - `GameBootstrap`, `GameContext`, system order, components, flows
- [`03-luna-and-scenes.md`](.claude/kb/03-luna-and-scenes.md) - Luna integration, template scripts, scenes and tuning, blocking baker
- [`04-glossary.md`](.claude/kb/04-glossary.md) - domain vocabulary
- [`05-workflows.md`](.claude/kb/05-workflows.md) - verify, export, git, definition of done
- [`UPDATELOG.md`](.claude/kb/UPDATELOG.md) - KB change log

Other agent config: [`.claude/settings.json`](.claude/settings.json) (hooks that inject the KB reminder), [`.cursor/rules/`](.cursor/rules/) (the same rules for Cursor).
