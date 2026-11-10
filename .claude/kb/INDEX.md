# WoolCrushPlayble Knowledge Base - INDEX

Entry point of the project knowledge base (KB). Read this first, then the doc(s) for the
area you are about to touch. KB docs are point-in-time snapshots: verify cited paths and
symbols against current code before relying on them (`Last verified:` at the top of each doc).

| Doc | Scope | Read this when |
|-----|-------|----------------|
| [00-overview.md](00-overview.md) | What the playable is, directory layout, scenes, packages, namespaces | Always - orientation for any task |
| [01-conventions.md](01-conventions.md) | Code style, naming, ECS data-access idiom, Luna-safe coding rules | Writing or editing any C# |
| [02-gameplay-ecs.md](02-gameplay-ecs.md) | LeoECS Lite gameplay: `GameBootstrap`, `GameContext`, system order, components, game flows | Touching anything under `Assets/Scripts/` |
| [03-luna-and-scenes.md](03-luna-and-scenes.md) | Luna integration, template scripts, `luna.json`, scene setup and per-scene tuning, editor tool | Touching scenes, Luna settings, export, tutorial finger, end screen |
| [04-glossary.md](04-glossary.md) | Domain vocabulary (block, spool, reel slot, scale, wind, rebuke) | Unsure what a term means |
| [05-workflows.md](05-workflows.md) | Verify/run/export, git conventions, definition of done | Before verifying or committing |
| [06-new-block-mechanics.md](06-new-block-mechanics.md) | Ice / Chain / Hidden block mechanics: how they work in the original project (with sources), mapping to the playable, implementation notes, what is unverified | Touching Ice/Chain/Hidden blocks, `Scripts/Gameplay/Mechanics/`, `BlockMechanicsConfig` |
| [UPDATELOG.md](UPDATELOG.md) | Append-only log of KB changes | After any task that changed architecture, patterns or paths |

## Protocol (summary - full version in root `AGENTS.md`)

- **Before a task:** read this INDEX, then the relevant doc(s).
- **After a task** that changed architecture, patterns, public APIs, file paths, scene wiring
  or domain concepts: update the relevant doc and append a line to `UPDATELOG.md`
  (`YYYY-MM-DD | task summary | docs touched`). Pure bug-fixes with no structural change need no KB edit.
- Record verified facts only. A claim about runtime behaviour is written only after reading the
  whole code path; otherwise state the observed symptom and mark it unverified.

## Project skills and commands

| Name | Location | When |
|------|----------|------|
| `woolcrush-kb-update` | `.claude/skills/woolcrush-kb-update/SKILL.md` | After-task KB step: maps changed files to KB docs, edits them, writes the UPDATELOG line |
| `/feature` | `.claude/commands/feature.md` | Full pipeline: KB -> planner -> coder -> reviewer -> KB update |

Subagents: `planner`, `coder`, `reviewer` in `.claude/agents/`.
