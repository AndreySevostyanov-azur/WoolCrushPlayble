# 03 - Luna integration, scenes, editor tooling

`Last verified: 2026-11-09` (scripts, `luna.json`, `EditorBuildSettings.asset` and scene YAML read; no Luna export or Editor run was done).

## Luna plumbing

- Luna Playworks plugin `com.unity.playworks.upp` 7.1.0 (file package, `Packages/manifest.json`). Config lives in
  `luna.json` (root) and `Assets/SVC_Luna.asset`. Luna API used from code: `Luna.Unity.LifeCycle.GameEnded()`,
  `Luna.Unity.Playable.InstallFullGame()`, `[LunaPlaygroundField]`.
- `luna.json` highlights: `creativeName` `WQ_Playable_3`; export scenes `["Assets/Playable/Scenes/Playable_3.unity"]`,
  `disabledScenes` = `Playable_4`, `Template`, `Template_2`; `startupScene` 0; `minify` true; `disableAntiAliasing` true;
  `enableRealtimeShadows` false; `enableLunaCompilerV2` false; module `physics3d` is `forceIncluded` (block raycasts and colliders need it).
  The `scripts.excludes` list drops `**/Editor/`, ads/analytics SDK stubs, `Assets/AnimationBaker/`, `MeshCombiner`, `LanguageSupport` etc. from the Luna script build.
- Luna package templates (`com.azur-marketing.*`) supply `InputFacade`, `Audio`, `Screens`, CTA button, orientation, post-process and time-scaler helpers used by the template scripts below.
- `EditorBuildSettings` has all four scenes enabled in the order `Template`, `Template_2`, `Playable_4`, `Playable_3`; Luna ignores that and uses its own list above.

### Template scripts (`Assets/Playable/Scripts`, namespace `Azur.Playable*`)

| Script | Role | Notes |
|--------|------|-------|
| `LunaInspector` | Luna playground fields: music volume, sound volume, tap count before store redirect (`_clickCount`, default 9999) | always on the same object as `LunaSettings` (`RequireComponent`) |
| `LunaSettings` | Counts taps (`InputFacade.DownTouched` -> `ClickCounter.RegistrateClick`), registers volumes in `Audio.Instance.AddVolumeSettings("Luna", music, sound)`, calls `InstallFullGame()` when the limit is hit, calls `LifeCycle.GameEnded()` once | the counter is independent of the ECS game: any tap counts, not only block taps |
| `Level` | template hook: serialized `Screens` and `InputFacade`, event `GameEnded` | `StopLevel()` is private and never called, so `Level.GameEnded` never fires in this project; end of game is signalled by `DragonGameEndSystem` instead |
| `Screens/StartScreen` | empty `ScreenBase` subclass | present on all four scenes |
| `Screens/WinScreen`, `LoseScreen`, `GameScreen` | empty `ScreenBase` subclasses | **not** on the four playable scenes (only on `Examples/SelectorExample.unity`; `GameScreen` nowhere) |
| `PlayablesPlugins/Luna/ClickCounter` | `LimitReached` at `count >= max`, `OneClickLeft` at `count == max-1`, `ForceComplete()` sets `count = max` | |

Tap-limit behaviour (from code): on the tap that makes `count == max - 1` the counter raises `OneClickLeft`, `LunaSettings.GameEnded()` runs
(`ForceComplete()` + `LifeCycle.GameEnded()`), so the next tap hits `LimitReached` -> `InstallFullGame()`. `Playable_3` has `_clickCount: 12`; the other scenes keep 9999 (effectively off).

### Playground fields (editable in the Luna playground)

| Title (group) | Field | Where |
|---------------|-------|-------|
| "Положение пальцы" (СтартовыйЭкран) | `GameBootstrap._fingerOffsetPosition` | tutorial finger offset |
| "Зона смотки %" (Игровое поле) | `GameBootstrap._windZonePercent` | 0-100, see [02](02-gameplay-ecs.md) wind zone |
| "Громкость музыки" / "Громкость звуков" (Эффекты) | `LunaInspector._music` / `_sound` | 0-1 |
| "Количество нажатий до перехода в стор" (Редирект) | `LunaInspector._clickCount` | |

## End screen

`GameBootstrap._endScreenView` (`EndScreenView`, namespace `Gameplay`) is activated on game end. Its `_goToStoreButton` calls
`Luna.Unity.Playable.InstallFullGame()` once (listener removed on click). It is the only end UI in use; win and lose look the same.

## Scenes (`Assets/Playable/Scenes`)

Every scene has the same structure: a `GameBootstrap`, `ClickRaycastToEcsEvent`, `EndScreenView`, `StartScreen`, `LunaSettings` + `LunaInspector`, ~44-48 `DragonColorBlock`s,
slots, dragon path. Values below are the serialized ones read from the scene YAML.

| Scene | In Luna export | Accel. settings asset (`!Configs`) | `_delayBeforePause` | `_windZonePercent` | `_dragonHeadSpeed` | `_dragonSegmentSpacing` | `_blockMoveSpeed` | `_clickCount` |
|-------|----------------|------------------------------------|---------------------|--------------------|--------------------|-------------------------|-------------------|---------------|
| `Playable_3` | yes | `DragonVariableAccelerationSettings Play3` | 1.2 | 100 | 1.6 | 0.88 | 20 | 12 |
| `Playable_4` | no | `... Play4` | 1.2 | 100 | 1.3 | 0.9 | 20 | 9999 |
| `Template` | no | `DragonVariableAccelerationSettings` | 1.3 | 85 | 1.1 | 0.8 | 15 | 9999 |
| `Template_2` | no | `DragonVariableAccelerationSettings 1` | 1.0 | 85 | 1.0 | 0.9 | 16 | 9999 |

`_spawnProgressively` is on and `_showBootOverlay` is on in all four. `Template`/`Template_2` look like earlier level iterations
(git history: "lv1", "lv2", then "playable_3/4 scene added"); this was not verified beyond the history subjects.

When creating a new level: duplicate a scene, rearrange blocks, set `Turns`/`BoxType`/`BlockingBlocks`, choose or duplicate a
`DragonVariableAccelerationSettings` asset, run the baker (below), set the tutorial block (`_fingerTargetBlock`), set the wind zone, add the scene to
`luna.json` (`scenes` / `disabledScenes`) and `EditorBuildSettings`.

## Blocking baker (`Assets/Editor/DragonColorBlockBlockingBaker.cs`)

Menu `Tools/Blocks/Bake Blocking Blocks (All)` (also a button in the `DragonColorBlock` inspector, namespace `Playeble.EditorTools`):

1. Finds every `DragonColorBlock` in the open scene (including inactive), sorts by hierarchy path for a stable order.
2. Writes that list into `GameBootstrap._blocks` (`EditorSetBlocks`). **Blocks not on the scene at bake time are not in the game.**
3. For each block computes `BlockingBlocks`: other blocks in front along the block's real `forward` on XZ (`dot > 0`), within the lateral limit
   (projected half extents + `Margin` 0.05), overlapping vertically, sorted by distance (`EditorSetBlockingBlocks`).
4. Marks objects dirty and saves assets; the scene itself still has to be saved.

The runtime (`StartBlockMoveOnClickSystem.GetForwardBlockerOrBoundary`) uses the same forward/lateral rules, so keep both in sync when changing either.
Re-bake after moving, rotating or adding blocks; stale lists make blocks drive through each other or stop for no reason.

## Block mechanics tooling (`Assets/Editor/BlockMechanicsTools.cs`, 2026-11-09)

Menu `Tools/Blocks/Mechanics/` (details of the mechanics in [06](06-new-block-mechanics.md)):

| Item | What it does |
|------|--------------|
| `Add Hidden To Selected`, `Add Ice To Selected`, `Add Chain To Selected` | for every selected object with `DragonColorBlock`: adds `HiddenBlock` / `FrozenBlock` / `ChainedBlock` (Undo-able), assigns `Assets/!Configs/BlockMechanicsConfig.asset` (found by type), builds the `Hidden` / `Ice` child view persistently so it shows in the scene |
| `Rebuild Views (Scene)` | re-creates missing overlay views for all blocks that have the components |
| `Validate (Scene)` | errors for: frozen block that no block lists in `BlockingBlocks` (nothing can hit it), empty/self/duplicate/missing chain blockers, chain cycles (deadlock) |

Level workflow for a scene: place blocks, bake blocking blocks (above), select blocks -> add mechanics, for chains fill `ChainedBlock._blockers` (gizmos show the links), run `Validate`, save the scene. A scene with none of the components behaves exactly as before.
