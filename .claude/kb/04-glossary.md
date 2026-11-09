# 04 - Glossary

`Last verified: 2026-11-09`

| Term | Meaning (where it lives) |
|------|--------------------------|
| **Block** | Tappable coloured box on the field. `DragonColorBlock` + `ClickableBlockView`. Carries `Colors`, `BoxType`, `Turns`, `BlockingBlocks`. Becomes a spool when it reaches a slot. |
| **Turns** | Number of scales a block's spool will wind (`DragonColorBlock.Turns` -> `ReelSpoolData.Max`). Sum of all `Turns` = dragon body length. |
| **BoxType** | `Small / Medium / Large` (`AzurGames.Wool.Gameplay.BoxType`); chooses which `SpoolVisualSet` of a slot is shown. Not used for any movement rule. |
| **Colors** | `Playeble.Scripts.Gameplay.Colors`: ushort enum, one bit per colour (Red..Orange, `White` = no colour). `GetOffset()` = bit index, used as shader `_ColorOffset` / `ColorOffset` (clamped 0-9). |
| **ColorOffset** | Integer 0-9 picked into material property blocks (`_ColorOffset`, `_colorOffset`) to tint dragon scales and blocks; also looked up for ropes in `RopeColorOffsetConfig`. |
| **Blocking blocks** | Baked list of blocks in front of a block that stop it ("blocker"); see [03](03-luna-and-scenes.md). |
| **Slot / reel slot** | UI element at the bottom (`SlotView`, entity with `ReelSlotViewRef`/`ReelSlotState`). States: `Empty, Reserved, Occupied, Dispose`. |
| **Spool / reel** | The thing inside an occupied slot: icon, coils, remaining counter. Data in `ReelSpoolData` + `ReelSpoolColor`. |
| **Coil** | One of the small images (`SpoolVisualSet.CoilsImage`) activated per wound scale. |
| **Scale** (чешуйка) | One body segment of the dragon (`DragonPart.Kind == Body`, `DragonScaleMarker`, `DragonScaleColor`). |
| **Wind / winding** (смотка) | A spool removing matching scales from the dragon (`ReelWindSystem`, `WindSecondsPerScale`). |
| **Wind zone** (зона смотки) | Screen-height line (`_windZoneLine`, `_windZonePercent`); only scales below it can be wound. |
| **Rope** | `RopeSpriteShape` line drawn from the current coil to the scale being wound. |
| **Rebuke** | Dragon pushed back after a scale is removed (`DragonRebukeRequestEvent`, `DragonRebukeState`, `DragonRebukeDuration`). |
| **Head / Body / Tail** | `DragonPartKind`. Body index 0 is the part closest to the head. |
| **Path** | `CinemachinePath` the dragon follows (`_dragonPath`); positions by distance along it. |
| **End stop** | `VariableAccelerationSettings.EndStopDistance`: distance before the path end at which the head stops and the game is lost. |
| **Borders** | `GameBordersComponent`: AABB of the block field; blocks leaving it return to origin. |
| **Tutorial finger / click hand** | `_clickHand` UI hand that follows `_fingerTargetBlock` until the first tap. |
| **Start screen** | `_startScreen` object shown after `_delayBeforePause` until the first input. |
| **End screen** | `EndScreenView`; store button -> `InstallFullGame`. |
| **Luna** | Playworks playable platform; exports the scene to a single HTML. `luna.json`, `LifeCycle`, `Playable.InstallFullGame`. |
| **Playground field** | `[LunaPlaygroundField]` serialized value editable in the Luna web playground. |
| **GameContext** | Immutable config object handed to every ECS system. |
| **EcsLite** | Leopotam ECS Lite (`Assets/LeoEcs/ecslite-2025.4.22`). Entities are ints, components are structs, `EcsPool<T>`, `EcsFilter`. |
