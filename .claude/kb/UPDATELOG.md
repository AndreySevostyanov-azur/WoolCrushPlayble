# KB Update Log

Append-only. One line per KB change: `YYYY-MM-DD | task summary | docs touched`.

2026-11-09 | Replaced KB inherited from the main WoolCrush game (DOTS/Zenject/Meta/Economics docs, Asana/Notion skills, friction log) with a KB written from the playable's code: LeoECS Lite gameplay, Luna integration, scenes, baker, glossary, workflows | INDEX, 00-overview, 01-conventions, 02-gameplay-ecs, 03-luna-and-scenes, 04-glossary, 05-workflows
2026-11-09 | Relaxed the no-comments rule (Luna-footgun / "why" comments allowed); cleared stale local permission | 01-conventions
2026-11-09 | Research for the new block mechanics task (Ice/Chain/Hidden): original project code and Notion specs analysed, port plan recorded (nothing implemented) | 06-new-block-mechanics, INDEX
2026-11-09 | Implemented Hidden/Ice/Chain block mechanics (Scripts/Gameplay/Mechanics, 5 new systems after BlockPathMoveSystem, BlockMoveData.Blocker, GameContext.Mechanics), copied assets from the original project, BlockMechanicsConfig asset, Tools/Blocks/Mechanics editor menu; compiled with dotnet build, not run in Unity/Luna | 02, 03, 04, 00, 06, INDEX
2026-11-09 | Fix: ice/hidden overlay field could hold the block's own renderer (nothing was built); runtime ignores non-child refs, overlay fields hidden in inspector, Reset/ContextMenu 'Build View' builds the view in the editor | 06
2026-11-10 | FrozenBlock._ice shown in the inspector again (still set by Build View, runtime ignores non-child refs) | 06
2026-11-10 | Chain/lock/pin sprites no longer clip into blocks: new shader SpriteOnTop (ZTest Always) + material, BlockMechanicsConfig._spriteMaterial switched | 06
2026-11-10 | Fix: a block blocked by another no longer heads to the blocker centre (diagonal) when the forward ray misses the blocker box - target is the point on the forward line; strike/melt only on real AABB contact; BlockMoveData.Blocker removed | 02, 06
2026-11-10 | [supersedes 2026-11-10 movement fix] Reverted the 'forward line instead of blocker centre' fix and the strike-only-on-contact change by user request; BlockMoveData.Blocker and the centre fallback are back | 02, 06
2026-11-10 | Hidden block animation keeps running while the game is paused by the start screen: logic moved from BlockHiddenSystem to BlockMechanicsState.TickHidden, GameBootstrap ticks it in the paused branch (also corrects the earlier note that it starts after the first tap) | 02, 06
