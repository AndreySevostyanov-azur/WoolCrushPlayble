# 02 - Gameplay (LeoECS Lite)

`Last verified: 2026-11-09` (all files under `Assets/Scripts/` read, mechanics added the same day; runtime behaviour was **not** run in the Editor, statements about "what happens" come from reading code).

## Composition root: `GameBootstrap` (`Assets/Scripts/GameBootstrap.cs`)

- `MonoBehaviour` in the scene; holds all tuning as serialized fields (dragon, blocks, slots, speeds, wind zone,
  collision effect, tutorial finger, UI refs). Some fields are `[LunaPlaygroundField]` (see [03](03-luna-and-scenes.md)).
- `Awake`: `BindSystems()` fills `_gameplaySystemsTypes`, caches the finger `RectTransform`, stops the ambient `AudioSource`.
- `Start` -> `InitEcsGame()`: builds dragon colour slots and body count from the blocks, builds `GameContext`,
  creates two `EcsWorld`s, creates both `EcsSystems`, calls `Init()`, then `ApplyRopeColorOffsetConfig()`
  (pushes `RopeColorOffsetConfig` into every `SlotView.Rope`) and `ApplyWindZonePosition()`.
- `Update`: `UpdateFingerPosition()`; if not paused and active -> `_gameplaySystems.Run()` then `_collisionSystems.Run()`;
  pause/start-screen logic (below). Stops entirely once `_gameFinished`.
- `GetWorld()` returns the game world; `ClickRaycastToEcsEvent` uses it to push click events.
- `_worldPhysics` / `_collisionSystems` are created, initialised and run, but **no system is bound to them**
  (`BindType.Collision` is never used). Vestigial; remove only as a deliberate cleanup.
- `_showBootOverlay` creates a `DontDestroyOnLoad` canvas with the text "Loaded" (debug overlay, on in all scenes).

### Start / pause logic (in `Update`)

`_time` counts from start. When `_time > _delayBeforePause` and no input has happened yet (`_alreadyPaused == false`),
`IsPaused = true` and the `_startScreen` object is shown. First touch / mouse-down: starts the ambient audio, unpauses,
hides the start screen, sets `_alreadyPaused = true` (so the delayed pause never fires again).
`ClickRaycastToEcsEvent` does not check `IsPaused`; click events created while paused stay in the world until the systems run again.

### Tutorial finger

`_clickHand` follows `_fingerTargetBlock` every frame (world -> screen -> canvas local point + `_fingerOffsetPosition`).
`ResolveTutorialBlock()` takes the `DragonColorBlock` on/under that transform. In `InitEcsGame` that block's colour slot is moved to
position 0 of the dragon colour slots, so the first dragon scales have the tapped block's colour and its size (`Turns`).

## `GameContext` (`Dragon/GameContext.cs`)

Plain immutable bag built once in `InitEcsGame` and passed to every system: dragon path/root/prefabs/spacing/speed/initial distance,
`ScaleColors` (`DragonScaleColorSlot { Colors Type; int Count; int ColorOffset }`), `Blocks`, `FieldBordersOffset`, `ReelSlots`,
`SpoolSpriteConfig`, `WindSecondsPerScale`, `DragonRebukeDuration`, `DragonVariableAccelerationSettings`, `BlockMoveSpeed`,
`DragonBreathConfig`, `WorldCamera`, `UiCanvas`, `WindZoneLine`, block-collision effect prefab/lifetime/offset.
`public Action GameEnd` is subscribed by `GameBootstrap.GameEnd`; systems call `RiseGameEndAction()`.
Adding a tunable = serialized field on `GameBootstrap` + ctor parameter + property here.

Body segment count = sum of `Turns` over all blocks (`_dragonBodySegmentsCount` is overwritten when `_blocks` is non-empty).
`Colors` -> scale colour: `GetColorSlot(slots, index)` walks the slots, each covering `Count` consecutive body indices.

## System order (`GameBootstrap.BindSystems`) - run order = list order

| # | System | Kind | Role |
|---|--------|------|------|
| 1 | `DragonSpawnInitSystem` | Init | Instantiates the head (`DragonHeadView` prefab) as an entity with `TransformRef, DragonPart, SplinePathRef, DragonHeadMove, DragonSpawnProgress, DragonHeadComponent` |
| 2 | `ComputeGameBordersInitSystem` | Init | AABB over all block colliders (+ `FieldBordersOffset`) -> one `GameBordersComponent` entity |
| 3 | `BlocksInitSystem` | Init | One entity per `DragonColorBlock`: `BlockViewRef` + `BlockMoveData` |
| 4 | `BlockMechanicsInitSystem` | Init | Builds `BlockMechanicsState` (put on `GameContext.Mechanics`), block->entity map, `BlockHidden/BlockFrozen` tags, chain links; creates missing overlay views. See [06](06-new-block-mechanics.md) |
| 5 | `DragonVariableAccelerationSystem` | Run | Head `Speed` = `settings.EvaluateSpeed(BaseSpeed, Distance, PathLength)`; no settings asset -> constant speed + one warning |
| 6 | `DragonMoveAlongPathSystem` | Run | Advances head, applies rebuke shifts, positions followers, raises `DragonReachedEndEvent` |
| 7 | `DragonGrowSpawnSystem` | Run | Spawns body scales progressively (max 1 per frame) and finally the tail |
| 8 | `DragonGameEndSystem` | Run | Win/lose detection -> `LifeCycle.GameEnded()` + `ctx.RiseGameEndAction()` |
| 9 | `DragonEventCleanupSystem` | Run | Deletes every `DragonReachedEndEvent` entity |
| 10 | `ReelSlotsInitSystem` | Init | One entity per `SlotView`: `ReelSlotViewRef` + `ReelSlotState{Empty}`; resets slot UI |
| 11 | `StartBlockMoveOnClickSystem` | Run | `BlockClickedEvent` -> rejects frozen/chained taps (nudge events), else chooses `BlockMoveType` for the block |
| 12 | `BlockPathMoveSystem` | Run | Moves blocks; on slot arrival emits `BlockArrivedToSlotEvent` and destroys the block; on contact with a blocker raises nudge + strike events |
| 13 | `BlockFrozenSystem` | Run | Consumes `BlockStrikeEvent`: starts melt (VFX, ice alpha fade), removes `BlockFrozen` |
| 14 | `BlockChainSystem` | Run | Breaks a chain link when its blocker block starts `ToSlot`; updates lock/pin/chain visuals every frame |
| 15 | `BlockNudgeSystem` | Run | Consumes `BlockNudgeEvent`: short punch offset of an idle block (+ optional reject marker) |
| 16 | `BlockHiddenSystem` | Run | Calls `BlockMechanicsState.TickHidden` (pulse/solid animation of the hidden overlay material). While the game is paused (start screen / tutorial) `GameBootstrap.Update` calls the same `TickHidden` itself, so the animation never stops |
| 17 | `AssignReelToFirstEmptySlotSystem` | Run | `BlockArrivedToSlotEvent` -> slot becomes `Occupied` with spool data/colour |
| 18 | `DragonAnimationSystem` | Run | Periodic fire-breath on the head (`Fire_On` animator bool) |
| 19 | `ReelWindSystem` | Run | Occupied spool unwinds matching dragon scales one by one; emits `DragonRebukeRequestEvent` |
| 20 | `ReelSlotSyncViewSystem` | Run | Pushes slot state to `SlotView` (spool visual, coils, counter, rope) |
| 21 | `BlockClickEventCleanupSystem` | Run | Deletes every `BlockClickedEvent` entity |

Order dependencies worth knowing before touching the list:

- Click events created in `Update` by `ClickRaycastToEcsEvent` are consumed by #11 and deleted by #21 in the same `Run`.
- `DragonReachedEndEvent` is created by #6, read by #8, deleted by #9 - all before #18. `DragonAnimationSystem` checks
  `_reachedFilter` (that event) to force the breathing state, but by the time it runs the event is already deleted, so that
  branch looks unreachable in the current order (read from code; not observed at runtime). The game ends in #8 the same frame anyway.
- `BlockArrivedToSlotEvent` (#12) is consumed by #17 in the same frame; #19 sees the slot as `Occupied` from the next system on.
- `DragonRebukeRequestEvent` is created by #19 and consumed by #6 on the **next** frame (it is earlier in the list).
- Mechanics events (`BlockNudgeEvent`, `BlockStrikeEvent`) are raised by #11/#12 and consumed by #13/#15 in the same frame, so every one is handled before the frame ends; `BlockChainSystem` (#14) polls `BlockMoveData.MoveType == ToSlot`, which #11 sets in the same frame.

## Components (all `struct`)

| Component | File | Fields / meaning |
|-----------|------|------------------|
| `TransformRef` | `Dragon/DragonComponents.cs` | `Transform Value` |
| `SplinePathRef` | same | `CinemachinePath Path` |
| `DragonPart` | same | `DragonPartKind Kind (Head/Body/Tail)`, `int Index` (body index 0 = closest to head) |
| `DragonHeadMove` | same | `Distance, BaseSpeed, Speed, ReachedEndRaised` - distance along the path |
| `DragonHeadComponent` | same | head `Animator`, fire effect object, death effect object |
| `DragonFollowHead` | same | `EcsPackedEntity Head`, `OffsetDistance = (index + 1) * spacing` |
| `DragonScaleColor` | same | `Colors Type` |
| `DragonScaleMarkerRef` | same | `DragonScaleMarker Value` (view with shader cutout) |
| `DragonRebukeRequestEvent` | same | `RemovedBodyIndex`, `ShiftDistance` |
| `DragonRebukeState` | same | mirror of the active rebuke envelope on the head (observability) |
| `DragonSpawnProgress` | same | `BodySpawned, TailSpawned, MaxHeadDistanceReached` |
| `DragonReachedEndEvent` | same | empty tag event |
| `DragonBreathComponent` | `Dragon/DragonBreathComponent.cs` | `Time`, `DragonBreathState State (Idle/Breathing)` on a separate entity |
| `BlockViewRef` | `Movements/BlockMoveComponents.cs` | `DragonColorBlock Block, Transform, Collider, Vector3 OriginPosition` |
| `BlockMoveData` | same | `BlockMoveType MoveType (None/ToSlot/ToBlocker/ToOrigin)`, `TargetSlotIndex`, `PathIndex/PathCount`, waypoints `P0..P5`, `CachedBlockerPoint`, packed `Blocker` (the block a `ToBlocker` move will hit; default = none) |
| `BlockArrivedToSlotEvent` | `Movements/BlockArrivedToSlotEvent.cs` | `SlotIndex, Turns, BoxType, Color, ColorOffset` |
| `GameBordersComponent` | `Movements/GameBordersComponent.cs` | `Center, Extents` of the field |
| `BlockClickedEvent` | `Selection/BlockClickedEvent.cs` | `ClickableBlockView Block, HitPoint, HitNormal` |
| `ReelSlotViewRef` | `ReelSlots/ReelSlotComponents.cs` | `SlotView View`, `int Index` (position in `ReelSlots` array) |
| `ReelSlotState` | same | `SlotStates Status` (`Empty, Occupied, Dispose, Reserved`) |
| `ReelSpoolData` | same | `Current` (scales wound), `Max` (= block `Turns`), `BoxType` |
| `ReelSpoolColor` | same | `Colors Color`, `ColorOffset` |
| `ReelSlotAssignedBlock` | same | block that reserved this slot while moving to it |
| `ReelWindState` | same | `TargetEntity` (scale being unwound), `Progress01`, `DisposeTimer` |
| `BlockHidden` | `Mechanics/MechanicsComponents.cs` | instanced overlay `Material`, `HiddenPhase` (None/PulseUp/PulseDown/Solid/Finished), timers |
| `BlockFrozen` | same | ice `MeshRenderer`, VFX point; presence = block is frozen |
| `BlockMelt` | same | ice view + material + elapsed/duration while the ice fades out |
| `BlockChained` | same | `ActiveLinks` left on a locked block + its lock `SpriteRenderer` |
| `BlockChainBlocker` | same | `ActiveLinks` held by a blocker block + its pin `SpriteRenderer` |
| `ChainLink` | same | one entity per chain: packed target, packed blocker, `ChainVisual` |
| `BlockNudge` | same | running shake of an idle block (`Offset`, `Time`, `Duration`) |
| `BlockNudgeEvent` / `BlockStrikeEvent` | same | one-tick events: shake a block (optional reject marker) / hit a block (melts it if frozen) |
| `EcsUtils` | `Dragon/DragonSpawnInitSystem.cs` | static `SetComponent<T>` helper (not a component) |

## Flows

### Block tap -> slot

1. `ClickRaycastToEcsEvent` (`Update`, touch phase `Began` first, else mouse-down) raycasts from `_camera`
   (`Camera.main` fallback), finds `ClickableBlockView` in the hit collider's parents, creates a `BlockClickedEvent` entity in the game world.
2. `StartBlockMoveOnClickSystem` resolves the `DragonColorBlock` (`GetComponentInParent`), finds its block entity, and ignores the tap unless
   `MoveType == None`. Decision order (steps 0a-0c only when `GameContext.Mechanics.HasAny`, see [06](06-new-block-mechanics.md)):
   0a. frozen block -> nudge with reject marker, no move;
   0b. block with active chain links -> nudge the block and every blocker of its chains, no move;
   0c. (inside step 1) the tapped block is a chain blocker and has a block in front -> it nudges in place together with that block (and strikes it if frozen), no move.

   1. blocker ahead (`DragonColorBlock.BlockingBlocks`, forward direction on XZ, within the field borders) -> `ToBlocker` to the collision point, remembering the blocker in `BlockMoveData.Blocker`; on contact `BlockPathMoveSystem` nudges the hit block and, if it is frozen, strikes it (melt);
   2. block already has a reserved slot (`ReelSlotAssignedBlock`) -> `ToSlot`;
   3. first `Empty` slot (lowest `Index`) -> mark it `Reserved`, add `ReelSlotAssignedBlock`, `ToSlot`;
   4. no free slot -> `ToBlocker` towards the boundary point, then back.
3. `BlockPathMoveSystem` (speed `GameContext.BlockMoveSpeed`, epsilon 0.05):
   - `ToSlot`: builds a waypoint path (`BuildPath`, up to `P0..P5`) to the slot `RectTransform` projected onto the block plane via `Camera.main`
     (`TryGetSlotTarget`); first leg direct, later legs axis-aligned. On the last waypoint: `BlockArrivedToSlotEvent`, `Object.Destroy(block)`, entity deleted.
   - `ToBlocker`: moves to `CachedBlockerPoint`; on arrival or AABB overlap with a blocker (`TryGetCollisionImpact`) spawns the collision effect
     (`BlockCollisionEffectPrefab` + offset, destroyed after `BlockCollisionEffectLifetime`) and switches to `ToOrigin`.
   - `ToOrigin`: back to `BlockViewRef.OriginPosition`, then `None`.
   - A non-`ToSlot` block outside the borders is sent to `ToOrigin`.
4. `AssignReelToFirstEmptySlotSystem`: slot -> `Occupied`; `ReelSpoolData{Current=0, Max=Turns, BoxType}`; `ReelSpoolColor`; removes `ReelSlotAssignedBlock`.

`BlockingBlocks` lists are baked in the Editor, see [03](03-luna-and-scenes.md) ("Blocking baker").

### Spool unwinds the dragon (`ReelWindSystem`)

For each `Occupied` slot with `Current < Max`:

1. Pick a target scale: body part with the same `Colors`, not already unwinding, **lowest body index** (closest to the head); ties -> closest to the path end.
   A scale is eligible only if it is below the wind-zone line (screen Y <= line Y; the line is `WindZoneLine`, positioned by `GameBootstrap`
   from `_windZonePercent`, 0 = bottom, 100 = top) and fully inside the screen horizontally (`IsHorizontallyVisible`).
2. For `WindSecondsPerScale` seconds the marker cutout goes 1 -> 0 (`DragonScaleMarker.SetCutout`), `ReelSlotSyncViewSystem` draws the rope meanwhile.
3. At the end: emit `DragonRebukeRequestEvent{RemovedBodyIndex, ShiftDistance = DragonSegmentSpacing}`, destroy the scale GameObject, delete its entity, `Current++`.
4. `Current >= Max` (or `Max <= 0`) -> `Dispose` for `DisposeDuration` (0.12 s), then the slot is `Empty` again and its spool/colour/wind components are removed.

### Dragon motion and "rebuke"

- Head `Distance += Speed * dt` along the `CinemachinePath` (`EvaluatePositionAtUnit` / `EvaluateOrientationAtUnit`, `PositionUnits.Distance`).
- `DragonVariableAccelerationSystem` -> `VariableAccelerationSettings.EvaluateSpeed`: multiplier `AccelerationMultiplier` while `distance < AccelerationUntilDistance`,
  `DecelerationMultiplier` while remaining path <= `DecelerationRemainingDistance`, speed 0 when remaining <= `EndStopDistance`. Each scene uses its own settings asset (see [03](03-luna-and-scenes.md)).
- A rebuke request starts a `DragonRebukeDuration` smooth-step shift of `ShiftDistance`: the head moves back by that amount. Body parts with index >= the removed index and the tail get
  the same shift added, so they stay in place while the parts in front move back with the head and close the gap. Several rebukes stack.
- Followers: `distance = headDistance - OffsetDistance (+ rebuke compensation)`, clamped to `[0, PathLength]`.
- Spawn: body segment `n` appears once the pure-forward head distance reaches `(n + 1) * spacing`; the tail after all body segments at `(total + 1) * spacing`.
  `_spawnProgressively` + `_dragonInitialHeadDistance` control where the head starts.

### End of game (`DragonGameEndSystem`)

- **Lose:** `DragonReachedEndEvent` - head `Distance >= PathLength - EndStopDistance` (the stop distance comes from the settings asset; `0` without one).
- **Win:** every body segment and the tail have been spawned and no `DragonPart` of kind `Body` remains (and `DragonBodySegmentsCount > 0`).
- Both do the same: `Luna.Unity.LifeCycle.GameEnded()` once (guard `_gameEndedSent`) and `ctx.RiseGameEndAction()` ->
  `GameBootstrap.GameEnd()`: shows `_endScreenView`, `IsPaused = true`, `_gameFinished = true`. There is **no separate win/lose screen** in the scenes
  (`WinScreen`/`LoseScreen` template classes exist but are not on any of the four playable scenes).

### Fire breath (`DragonAnimationSystem`)

One extra entity holds `DragonBreathComponent`. Idle for `BreathInterval`, then fire effect on + animator `Fire_On = true` for `BreathDuration`, then off.
Values come from `Assets/!Configs/DragonBreathConfig.asset`. The "reached end" branch ends `Run` with `return`.

### Slot view sync (`ReelSlotSyncViewSystem`)

Per slot: `view.Refresh(status)` (plays `NewReelVfx` on `Occupied`, stops rope + plays `RemoveReelVfx` on `Dispose`); for `Occupied`: counter = `Max - Current`,
shows the `SpoolVisualSet` whose `Size == BoxType`, sets spool icon and coil sprite from `SpoolSpriteConfig.TryGetData(color)`, activates the first `Current` coil images,
and shows the rope only while a wind target exists (`RopeSpriteShape.SetColor`, `UpdateRope(coilWorldPos, scaleWorldPos)`).

## Views touched by ECS

| View | Path | Used by |
|------|------|---------|
| `DragonHeadView` | `Scripts/Gameplay/DragonHeadView.cs` | head prefab: animator, breath object, death object |
| `DragonScaleMarker` | `Scripts/Gameplay/Dragon/DragonScaleMarker.cs` | shader props `_cutout` / `_Cutoff`; added at runtime if the body prefab lacks it |
| `DragonScaleMesh` | same folder | sets `_ColorOffset` / `_colorOffset` through a `MaterialPropertyBlock`; added at runtime if missing |
| `DragonColorBlock` | same folder | block data (`Colors _type`, `BoxType`, `_turns`, `_colorOffset`, `_blockingBlocks`); randomises colour in `Awake` when `_randomizeColorOnAwake` |
| `ClickableBlockView` | `Selection/` | marker component for raycast hits |
| `SlotView` | `!Project/Scripts/Gameplay/ReelSlots/SlotView.cs` | slot UI: spool visuals, counter, rope, VFX, `Rect` as block target |
| `RopeSpriteShape` | `!Project/Scripts/Gameplay/Rope/` | `LineRenderer`-based wavy rope between spool and scale |
| `CounterView` | `!Project/Scripts/Gameplay/CounterView.cs` | TMP counter with value cache |

`!Project/Scripts/Gameplay/UI/ClickSound` (empty) and `SlotFeedbacksListener` are referenced only by the prefab `!Project/Prefabs/UI/SpoolSlot (4).prefab`.

## Known quirks (do not "fix" silently)

- Blocks pick a **random colour in `Awake`** unless `_randomizeColorOnAwake` is off, so colours differ per run. The ECS side reads the colour later, in `GameBootstrap.Start`.
- `GameBordersComponent` is built from the colliders of the blocks at `Init`; blocks added later are not covered.
- `StartBlockMoveOnClickSystem` and `BlockPathMoveSystem` look up block entities by linear search; fine for ~50 blocks.
- `TryGetSlotTarget` uses `Camera.main`; the project camera must keep the `MainCamera` tag.
