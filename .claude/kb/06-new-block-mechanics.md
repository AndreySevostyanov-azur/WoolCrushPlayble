# 06 - New block mechanics (Ice, Chain, Hidden): source analysis and port plan

`Last verified: 2026-11-09`. Status: **code, assets and editor tools are implemented (section 4); it compiles (`dotnet build` against the Unity DLLs) but was NOT run in the Unity Editor or in a Luna export.** Sections 1-2 are read from the original project and the Notion specs; section 3 is the original plan; section 4 is what was built and where it differs.

Sources:
- Original project: `C:\JobProjects\wool-crush-clone` (Unity DOTS, Zenject). Mechanic code: `Assets/!Project/Scripts/Gameplay/Boxes/*` (Frozen*, Chained*, Concealed*, Lock/Pin/Chain views, Melt/Shatter/Unfreeze systems), click validation in `ReelSlots/SlotSelectionSystem.cs` + `SlotSelectionExtensions.cs`, `BoxExtensions.cs`, `ObstructionData.cs`, hit handling in `Movements/ObstacleReboundSystem.cs`, shake in `Effects/NudgeSystem.cs`. Its `.claude/kb` history is mostly about the level-start reveal animation (TileReveal) and unrelated features; it adds nothing the code does not show.
- Notion (read 2026-11-09): [Level Design / New Blocks 6.0](https://app.notion.com/p/3528b3b3dc3680cf8dd5dc6db1ee6953), [Ice Block](https://app.notion.com/p/3528b3b3dc368020bbddd619a676c542), [Chain Block](https://app.notion.com/p/3528b3b3dc36808cbd20f21d7f0b9db8), [Hidden Block](https://app.notion.com/p/3528b3b3dc36803881bed76c6af13162), [Luna guide](https://app.notion.com/p/30c8b3b3dc368092ab6ac0be49157f9b). The Luna guide says to take **mechanics** from the original but the **architecture** from the playable template, never to port the original code base as is.

## 1. How the mechanics work in the original

### Layering
A block can carry several types at once; draw order bottom to top: Hidden (replaces base colour with a black box with "?") -> Ice (ice crust over base/hidden) -> Chain (lock + chains over everything). The block is free only when **every** mechanic's unlock condition is met, in any order. Colour must stay readable under ice and chains. The block keeps its direction arrow in all states.

### Shared model
`ObstructionData(Blocker, Target, Type)` entities are edges of a blocking graph. `Type` = `Box` (baked from spatial scan, closest blocker in front), `Chains` (baked from `ChainedComponent._blockers`), `Frozen`, `ConveyorBounds` (conveyor only). `ClearObstructionsSystem` destroys every edge whose Blocker or Target has started moving to a slot (`BoxToSlotMovementData`) or is a rocket. Each mechanic is a tag on the block entity (`FrozenTag`, `ChainedTag`, `ConcealedTag`) plus a view child (`FrozenView`, `ConcealedView`) and a `MechanicService` that shows the mechanic-intro window.

### Tap validation order (`SlotSelectionExtensions.TryValidateSelection`, `SlotSelectionSystem`)
1. Tapped block has `FrozenTag` -> reject, bounce-scale animation on the block + "frozen" feedback plaque. No movement.
2. `GetObstruction`: if the block is the **Target** of any `Chains` edge -> that edge wins immediately (before physical blockers): reject, feedback "Need to break chains", nudge the block **and every chain blocker of it** (whole group shakes). No movement.
3. Otherwise the closest physical blocker (`Box` edge): 
   - if the tapped block is itself a chain **Blocker** of some alive target: it does **not** drive into the blocker; it nudges in place together with the blocker that stops it (no chain stretching); if that blocker is frozen it gets a `ShatterRequest`;
   - else the block drives to the blocker and rebounds (`ObstacleRebound`); at contact: collision VFX/haptic/audio, the blocker gets a nudge, **if the blocker is frozen -> `ShatterRequest`** (melt).
4. No blockers and a free slot -> normal move to slot. At that moment `ClearObstructionsSystem` deletes the edges where this block is Blocker: one chain disappears (VFX), and when the target has no chain edges left the lock breaks (VFX) and it becomes normal.

### Ice
- The only way to melt is a **strike**: another movable block whose closest forward blocker is the frozen one is tapped and hits it (spec: any block, several strikers allowed, one hit is enough; blocks in between make the strike impossible).
- `ShatterSystem` removes `FrozenTag`, spawns `BlockUnfreeze` VFX at the ice anchor; `MeltSystem` fades the ice view alpha to 0 over `FrozenSettings.VfxDuration`, then destroys the view. After melting the block is a normal block (may still be blocked by others).
- Safety net: `UnfreezeSystem` shatters a frozen block that is no longer the Blocker of any `Box` edge (nobody can strike it any more, e.g. rocket removed the last striker).
- View: separate mesh (`Tile_*_Ice.fbx`) with shader `Wool/Ice Tile` (`IceTile.shader`, `_Alpha`, `_NoiseScale`), material `Wool_Ice.mat`.
- Spec: cannot be used in garages/conveyors; rocket treats ice as a normal block; bot needs rules; analytics `level_type = ice_block`/`multiple_mechanics`.

### Chain
- Group = one **locked block** (Target) + one or more **blocker blocks**, authored as a list of blocker references on the locked block. Groups can overlap; a blocker may belong to several groups and may itself be locked. Positions and directions in the group do not matter, blocks may be at opposite ends of the field.
- Visual: lock on the target, pin marker on each blocker, a chain (`ChainView` over `RopeSpriteShape.UpdateRopeWorld`) per link, pooled and re-synced **every frame** by `ChainsService` (also drives lock/pin appear animation during the level intro). Releasing a chain plays `ChainedPin` VFX, last chain -> `ChainedLock` VFX. Scene preview of chains in the editor is required by the spec (gizmos in `BlockedComponent.Gizmos`).
- Spec: not allowed in garages/conveyors (blockers and locked alike), rocket frees a locked block and removes its chains.

### Hidden
- A purely visual state of the block on the field: a child `ConcealedView` with material `Wool_Hidden.mat` (`WoolHidden.shader`, `_Alpha`, `_RainbowAlpha`) is shown over the block. At level start `ConcealedAnimSystem` runs `PulseUp/PulseDown` (count and duration from `ConcealedSettings`: 3 s, 3 pulses) then `Solid` (1 s rainbow fade out) -> black "?" box. **Nothing removes the hidden state in code**: the real colour appears because the block is destroyed in the slot and the spool UI shows its real colour (spec: "colour appears when the block gets into the slot").
- Behaviour otherwise identical to a normal block (blocks and is blocked, same tap rules). Allowed in garages/conveyors; bot must "forget" colours.
- Hidden + Ice + Chain on one tile is allowed by the spec, but the original plays only the highest reveal phase (`Chained > Frozen > Hidden`) and logs an error for such bakes (level-intro limitation, irrelevant for the playable).

### Shake and bounce
`NudgeRequest.Nudge(direction)` = punch offset `0.2 * forward`, 0.1 s, `0 -> 1 -> 0` smoothstep. Frozen tap uses a scale bounce (`ScaleAnimationType.FrozenBlockBounce`).

## 2. Mapping to the playable (current state, verified in code)

- Block entity = `BlockViewRef + BlockMoveData` built in `BlocksInitSystem`; blocking is a **static baked list** `DragonColorBlock.BlockingBlocks` (editor baker); there is no edge-entity graph. Destroyed blocks become `null` and are skipped.
- `StartBlockMoveOnClickSystem` picks the move; `GetForwardBlockerOrBoundary` finds the closest blocker but **only keeps a point** (`CachedBlockerPoint`); `BlockMoveData` does not remember **which** block it will hit. `BlockPathMoveSystem` detects contact with `TryGetCollisionImpact` (AABB vs every blocker) and also does not report which block was hit.
- No nudge/shake, no feedback plaque, no per-block overlay views, no boosters, garages, conveyors, bot or analytics in the playable. Slot-full case is silent (block goes to the boundary and back).
- Shader `WoolInst.shader` only has `_ColorOffset`; no hidden/ice shaders or materials exist in the playable yet (original ones listed below).

## 3. Port plan (proposal, not agreed)

Principle: port the **rules** (sections 1.2-1.5), not the DOTS code; keep the playable's EcsLite/`GameContext` style (`01-conventions.md`).

### Authoring (per block, components next to `DragonColorBlock`, a block may have any combination)
- `HiddenBlock`: reference to the hidden overlay view (renderer with the hidden material) + animation settings (pulse count/duration, solid duration, or "instant").
- `FrozenBlock`: reference to the ice view, VFX point, melt duration.
- `ChainedBlock`: list of blocker `DragonColorBlock`s, lock anchor; blockers get a pin anchor computed at runtime. Scene preview via gizmos/edit-mode line, as the spec requires.
- Extend the editor baker (or a sibling tool) to: fill `GameBootstrap._blocks` as today, validate levels (ice without any possible striker, chain list with null/self/duplicate, chain group where every blocker is itself locked by the target - deadlock) and list mechanics per scene.

### Runtime (EcsLite)
- Tags/components: `BlockFrozenTag`, `BlockHiddenTag`, `BlockChainedState{RemainingLinks}`, `ChainLink{TargetEntity, BlockerEntity, view}` (entity per link), `BlockMeltRequest{Elapsed}`, `BlockNudge{...}`; a `Dictionary<DragonColorBlock,int>` block->entity map built in `BlocksInitSystem`.
- Tap rules in `StartBlockMoveOnClickSystem` in the original order (1.2): frozen reject -> chained reject (+ group nudge) -> chain-blocker blocked by a block: nudge both (+ melt if frozen) -> existing flow.
- Hit target identity: store the blocker block/entity in `BlockMoveData` when the tap picks `ToBlocker`; `BlockPathMoveSystem` raises melt when the impact/arrival is against a frozen blocker (one hit is enough, only the closest blocker counts, as in the original).
- New systems (positions are a proposal, system order is a contract - confirm before coding): `BlockChainSystem` after `BlockPathMoveSystem` (poll: a link breaks when its blocker block is in `ToSlot` or destroyed; VFX; when no links left the lock breaks), `BlockMeltSystem`, `BlockNudgeSystem`, `HiddenBlockSystem` (phases `None/PulseUp/PulseDown/Solid/Finished`, runs only while the game is unpaused). Chain/lock/pin views updated each frame from block positions (nudged blocks move the chain).
- Luna: reuse the existing `LineRenderer` rope approach, no new packages; new shaders must be tested in a Luna export (Runtime Analysis can strip them), keep draw order Hidden < Ice < Chain and verify colour readability under ice.

### Reusable assets in the original (copy with new GUIDs, check size)
`Shaders/WoolHidden.shader`, `Shaders/IceTile.shader`, `Materials/Wool_Hidden.mat`, `Materials/Wool_Ice.mat`, `Models/Tiles/Tile_4_Ice.fbx` / `Tile_6_Ice.fbx` / `Tile_10_Ice.fbx`, `Prefabs/Tiles/Chain.prefab`, `LockView.prefab`, `PinView.prefab`, `HiddenBlock_Feel.prefab` (uses MMF_Player, **not usable**: Feel is not in the playable), VFX behind `EffectTypes.BlockUnfreeze/ChainedLock/ChainedPin/BoxConcealed` (effect prefabs are mapped by enum index in an effects config - locate in phase 0), audio `sfx_ability_frozen.wav`, `sfx_BlocksCollision.wav`.

### Out of scope for the playable (not present there)
Garages, conveyors and their restrictions, rocket/boosters, the unfreeze-on-last-striker safety net (no boosters), bot rules, analytics events, level-intro (TileReveal) animations, the mechanic-intro popup with GIF and per-level tutor config, localisation of plaque texts.

### Phases
0. Assets and foundation: copy shaders/materials/models/prefabs, multi-mechanic authoring components, block->entity map, blocker identity in `BlockMoveData`.
1. Hidden (lowest risk; first Luna shader test).
2. Ice (tap reject, strike -> melt).
3. Chains (links, visuals, group nudge, chain-blocker rule, release).
4. Combinations (hidden+ice+chain), editor validator and scene preview, sample level scene, KB update, Luna export test on a real device.

### Open decisions (defaults proposed)
- Rejected-tap feedback: shake + red cross (spec tutor 3.5) with no text plaque - proposed; text plaques ("Need to break chains") need UI + localisation.
- Hidden reveal timing: the pulse + black fill runs when the game unpauses after the first tap (= start of the round), with a per-block "instant" option.
- Scripted multi-step finger tutorials for the new mechanics: not planned; the existing single finger (`_fingerTargetBlock`) stays.

## 4. Implementation (2026-11-09)

Namespace `Playeble.Scripts.Gameplay.Mechanics`, folder `Assets/Scripts/Gameplay/Mechanics/`. System order and component list: [02-gameplay-ecs.md](02-gameplay-ecs.md). Editor tooling: [03-luna-and-scenes.md](03-luna-and-scenes.md).

### Files
| File | Role |
|------|------|
| `BlockMechanicsConfig.cs` + `Assets/!Configs/BlockMechanicsConfig.asset` | one ScriptableObject for all mechanics: hidden material and timings (3 s / 3 pulses / 1 s solid, from the original `ConcealedBoxSettings`), ice material + source-mesh -> ice-mesh table (Tile_4/6/10), melt duration 0.5 s (original `FrozenSettings`), chain/lock/pin sprites, anchor offset, nudge 0.2 / 0.1 s, VFX prefabs (unfreeze, lock break, chain/pin break), optional reject marker prefab (empty) |
| `HiddenBlock`, `FrozenBlock`, `ChainedBlock` | per-block authoring components next to `DragonColorBlock` (any combination). Each holds `_config`; `HiddenBlock`/`FrozenBlock` also hold the overlay renderer (created at runtime if empty), `ChainedBlock` holds the blockers list and draws gizmos in the editor |
| `BlockMechanicViews.cs` | static builders for the overlay children `Hidden` / `Ice` (child of the block's mesh object; hidden = same mesh scaled 1.02/1.01/1.02 with the hidden material; ice = ice mesh with local euler (0,180,0), as in the original prefab), sprites, VFX spawn; `ChainVisual` = a row of `SpriteRenderer` links between two anchors |
| `BlockMechanicsState.cs` | plain class on `GameContext.Mechanics`: block->entity map, rule queries (`IsFrozen`, `ActiveLinks`, `IsChainBlocker`) and event raisers |
| `BlockMechanicsInitSystem`, `BlockHiddenSystem`, `BlockFrozenSystem`, `BlockChainSystem`, `BlockNudgeSystem` | systems, see 02 |

Changed existing code: `BlockMoveData.Blocker` (packed entity of the block a `ToBlocker` move will hit), `StartBlockMoveOnClickSystem` (reject rules, blocker identity), `BlockPathMoveSystem` (nudge/strike on contact, hit block returned by `TryGetCollisionImpact`), `GameContext.Mechanics`, `GameBootstrap.BindSystems`.

### Behaviour (rules ported from section 1)
- Tap frozen -> nudge + optional marker, no move. Tap block with chain links -> nudge it and every blocker of its chains. Tap a chain blocker that has a block in front -> it and that block nudge in place, a frozen blocker melts. Otherwise the old flow; contact with a blocker nudges it and melts it if frozen.
- A chain link breaks when its blocker starts `ToSlot` (or its entity is gone): link visual destroyed, pin destroyed when the blocker holds no more links, lock destroyed (+ lock VFX) and the block becomes normal when its last link is gone.
- Hidden: overlay alpha/rainbow phases ported 1:1 from `ConcealedAnimSystem`; starts with the scene (the systems run until `_delayBeforePause`) and **keeps running while the game is paused** by the start screen/tutorial: `GameBootstrap.Update` ticks `BlockMechanicsState.TickHidden` in the paused branch (user requirement 2026-11-10; it stops only at game end). `HiddenBlock._instant` skips the animation. Nothing "reveals" the block: it is destroyed in the slot and the slot UI shows the real colour.
- Without any mechanic component on the scene `GameContext.Mechanics.HasAny` is false and the click/move code takes the old paths.

### Authoring gotchas
- `FrozenBlock._ice` (visible in the inspector, set by `Build View`) and `HiddenBlock._overlay` (hidden in the inspector, same tools) must point to a **child** overlay view, never to the block's own `MeshRenderer`. The first manual test (2026-11-09) dragged the block's own renderer into `_ice`; the block then looked normal because the runtime treated the field as an existing ice view. Runtime now ignores a reference that is not a child (warning) and builds the view; in the editor adding the component builds the view automatically (`Reset`), and the component context menu `Build View` / `Tools/Blocks/Mechanics/Rebuild Views (Scene)` fixes a bad reference.

### Differences from the plan
- Overlay views and chain visuals are created from code (`BlockMechanicViews`), not from prefabs: no scene/prefab YAML had to be edited by hand. `Tools/Blocks/Mechanics/*` can create the overlays persistently so they are visible in the editor.
- Chain = sprites from `Tile_lock.psd` (`Tile_lock_chain` links in a row, `Tile_lock` lock, `Tile_lock_loint` pin), not the original `SpriteShape` rope (the playable has no SpriteShape package). Lock/pin/links face the camera.
- Chain links, lock and pin use material `!Project/Mechanics/Materials/Mechanics_SpriteOnTop.mat` (shader `Playable/Mechanics/SpriteOnTop`, `ZTest Always`, queue Transparent+10): the first test with the depth-tested `UnlitSpriteRenderer.mat` (`Sprites/Default`) showed the camera-facing sprite planes cutting into the neighbouring block meshes. Side effect: they draw over blocks that stand in front of them (UI-like). Not verified in Unity/Luna (new shader).
- The frozen-tap feedback is a nudge (original: scale bounce); no text plaques; the red cross needs a prefab in the config (`_rejectMarkerPrefab`, empty now).
- The unfreeze-on-last-striker safety net is not ported (no boosters); `Validate (Scene)` reports frozen blocks nobody can hit instead.

### Copied assets (GUIDs kept from the original, no collisions)
`!Project/Mechanics/`: `Shaders/WoolHidden.shader`, `Shaders/IceTile.shader`, `Materials/Wool_Hidden.mat`, `Materials/Wool_Ice.mat`, `Textures/iceBlock.png`, `Textures/TilesShadow_atlass.psd`, `Models/Tile_{4,6,10}_Ice.fbx`, `Sprites/Tile_lock.psd` (import max size lowered to 512 in its `.meta`). VFX: `!Project/Prefabs/VFX/Tiles_VFX/{FrozenTile_VFX,Lock_VFX,Lock_Joint_VFX}.prefab` plus `Materials/VFX/Particles_ADD.mat`, `Textures/VFX/Ice_Shards.png`, `Textures/VFX/Smoke 08_#[512,288].png` (everything else they use already existed with the same GUIDs).

### Not verified (needs Unity / Luna)
- Everything at runtime: overlay orientation (ice local euler (0,180,0) is copied from the original prefab override, the FBX root rotation could not be checked), hidden/ice shader look and sorting, chain link size/spacing/angle (`_chainScale`, `_chainLinkSpacing`, `_chainAngleOffset`, `_anchorLocalOffset` are guesses from the original prefab scale), VFX look, that the sprite ids in the config asset resolve.
- Luna: the two new shaders (Runtime Analysis may strip them), `SpriteRenderer` chains, `Renderer.material` instancing, texture weight (two 1024 px sources, `iceBlock.png` has 512 px platform overrides in its meta).
- Whether `GetComponent<T>()` and `HashSet`/`Dictionary` of Unity objects behave in the Luna JS build (the existing code already uses `GetComponent` and generic collections).
