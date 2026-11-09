---
name: woolcrush-kb-update
description: After-task KB update for WoolCrushPlayble - maps changed files to KB docs, edits them, bumps `Last verified:` dates and appends the `.claude/kb/UPDATELOG.md` line, or records the explicit "no KB change needed" outcome. Use after any non-trivial task that changed architecture, system order, components, scene wiring, Luna settings or file paths; also when the user says "update the KB" or as the closing step of `/feature`.
---

# KB-update procedure

## 1. Find what changed

`git status --short` and `git diff HEAD --stat`. Ignore unrelated Unity churn (`luna.json`, `Assets/SVC_Luna.asset`, `ProjectSettings/ProjectVersion.txt`, `Packages/*`) unless the task was about it.

## 2. Map files to docs

| Changed | Doc |
|---------|-----|
| `Assets/Scripts/GameBootstrap.cs`, `Assets/Scripts/Gameplay/**`, `Assets/!Project/Scripts/Gameplay/**` (systems, components, events, views, configs) | `02-gameplay-ecs.md` |
| Scenes, `luna.json`, `Assets/Playable/Scripts/**`, `PlayablesPlugins/**`, `Assets/Editor/**`, `EndScreenView` | `03-luna-and-scenes.md` |
| New folders, packages, namespaces, top-level layout | `00-overview.md` |
| New coding pattern or footgun | `01-conventions.md` |
| New domain term | `04-glossary.md` |
| Verify/export/git practice | `05-workflows.md` |

## 3. Decide what to record

Record: new or removed system/component/event, a changed system order or event timing, new `GameContext` field or serialized tunable, changed end conditions, new scene or per-scene tuning,
new Luna playground field, new package, moved/renamed file, a footgun you hit. Skip: pure bug-fixes with no structural change, cosmetic tuning of one scene value (unless the scene table lists it).

## 4. Write facts only

- Grep every symbol, path and `file:line` before writing it. Check claims from generated files or subagent reports at their source.
- A claim about runtime control flow (what runs, is skipped, returns) is written only after reading every call on that path; otherwise write the observed symptom and mark it "not verified at runtime".
- If an earlier KB statement turns out wrong, fix it in place and note the correction in `UPDATELOG.md`.
- Re-wrap edited prose paragraphs to the file's width; leave tables and code as they are.

## 5. Bump and log

- Update the `Last verified:` line of each doc whose sections you re-confirmed.
- Append to `.claude/kb/UPDATELOG.md`: `YYYY-MM-DD | task summary | docs touched` (today's date from the session context).
- No KB change needed: say so explicitly in the summary and name what you grepped.
