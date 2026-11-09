---
description: Run the full WoolCrushPlayble pipeline (KB -> planner -> coder -> reviewer -> KB update) for a task.
---

Run the pipeline for: **$ARGUMENTS**

Follow the contract in root `AGENTS.md`. Steps:

1. **Read the KB.** `.claude/kb/INDEX.md`, then the doc(s) for the area in scope (+ `00-overview.md`, `01-conventions.md`). Verify cited paths against current code.

2. **Plan.** Spawn the `planner` subagent (`subagent_type: planner`) with the task and constraints. It returns an ordered plan, files to touch, reuse, risks (system order, scenes, Luna),
   a verification plan and KB impact.

3. **Confirm scope.** If the planner surfaced ambiguity, or the plan changes player-facing rules, tuning values, system order or scene wiring, confirm with the user before coding.

4. **Implement.** Spawn the `coder` subagent with the approved plan. It implements exactly the plan with minimal diffs and reports what it verified and what it could not.

5. **Review.** Spawn the `reviewer` subagent naming the exact files this task touched (the working tree has unrelated Unity churn). On **FAIL**: mechanical findings go back to `coder`;
   structural findings are presented to the user with a proposed fix and applied only after approval. Review again until PASS.

6. **Update the KB.** Invoke the `woolcrush-kb-update` skill (or record the explicit no-op for pure bug-fixes).

7. **Summarize.** Files changed, how it was verified (Editor Play, which scene, which end states) and what was not verified (Luna export, device), review verdict, KB update made.
