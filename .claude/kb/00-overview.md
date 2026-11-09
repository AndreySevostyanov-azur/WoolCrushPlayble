# 00 - Project Overview

`Last verified: 2026-11-09` (full rewrite from a code read; the previous docs came from the main WoolCrush game and were removed).

## Identity

- **What:** a Luna Playworks playable ad for the WoolCrush rope/spool puzzle (publisher AzurGames).
  Repo: `AndreySevostyanov-azur/WoolCrushPlayble`, main branch `main`.
- **Gameplay in one paragraph:** a dragon crawls along a `CinemachinePath` toward the end of the
  screen. The player taps coloured blocks on the field; a tapped block drives to the first free
  reel slot (spool) in the bottom UI and disappears into it. The spool then winds scales of its
  colour off the dragon, one by one, each with a rope drawn from the spool to the scale. Every
  removed scale pushes the dragon back. **Win:** all scales removed. **Lose:** dragon head reaches
  the path end. Either ends the game and shows an end screen whose button opens the store.
- **Engine:** Unity `6000.0.x` (working tree `ProjectVersion.txt` says `6000.0.71f1`; `HEAD` says
  `6000.0.62f1`), URP 17.0.4, Luna 7.1.0 (`com.unity.playworks.upp`, file package at
  `Packages/../../Luna/7.1.0/scripts`, i.e. a `Luna` folder next to the repo folder).
- **Architecture:** one `MonoBehaviour` (`GameBootstrap`) owns two **Leopotam EcsLite** worlds and
  runs the systems from `Update`. No DI container, no async library, no addressables, no save data.
  Views are plain `MonoBehaviour`s referenced from components.

## Directory map (`Assets/`)

```
Scripts/
  GameBootstrap.cs            entry point: serialized scene config -> GameContext -> ECS worlds/systems
  Gameplay/
    Colors.cs                 ushort enum, one bit per colour, + GetOffset() (bit index)
    DragonBreathConfig.cs, RopeColorOffsetConfig.cs   ScriptableObjects
    DragonHeadView.cs, EndScreenView.cs               views (EndScreenView calls Luna InstallFullGame)
    Dragon/                   dragon spawn/move/grow/end systems, GameContext, DragonColorBlock, settings
    Movements/                block move components + init/click/path-move systems
    ReelSlots/                slot components + init/assign/wind/sync-view systems
    Selection/                click raycast -> ECS event bridge
!Project/
  Scripts/Gameplay/           view-side classes in namespace AzurGames.Wool.* (SlotView, CounterView,
                              SpoolSpriteConfig, SpoolVisualSet, BoxType, SlotStates, RopeSpriteShape, UI/*)
  Prefabs/ Models/ Materials/ Shaders/ Sprites/ Textures/ Animations/ Fonts/   art + prefabs
  Audio.prefab
!Configs/                     ScriptableObject assets (DragonBreathConfig, DragonVariableAccelerationSettings*,
                              RopeColorOffsetConfig, SpoolSpriteConfig) + ambient mp3, small anims
Playable/
  Scenes/                     Playable_3, Playable_4, Template, Template_2 (+ Examples/SelectorExample)
  Scripts/                    Luna template: Level, LunaSettings, LunaInspector, Screens/*
  Prefabs/ Shaders/ Materials/ Sounds/ Animations/
PlayablesPlugins/             Luna/ClickCounter.cs, EventLogger, OneScriptEffects (Azur playable template plugins)
LeoEcs/ecslite-2025.4.22/     embedded EcsLite source (own asmdef Leopotam.EcsLite)
Editor/DragonColorBlockBlockingBaker.cs   menu "Tools/Blocks/Bake Blocking Blocks (All)"
Plugins/DOTween, Resources/DOTweenSettings.asset, TextMesh Pro, Epic Toon FX, AnimationBaker,
MeshCombiner, LanguageSupport   third-party / tooling (several are excluded from the Luna build in luna.json)
SVC_Luna.asset                Luna settings asset
```

Repo root: `luna.json` (Luna export config), `Packages/manifest.json`, `AGENTS.md`, `.claude/`, `.cursor/`.

There are **no project asmdefs** (only `Leopotam.EcsLite` and third-party ones), no tests, no `.github/` CI.

## Namespaces (note the project-wide spelling "Playeble")

| Namespace | Where |
|-----------|-------|
| `Playeble.Scripts` | `GameBootstrap` |
| `Playeble.Scripts.Gameplay` | `Colors`, `RopeColorOffsetConfig` |
| `Playeble.Scripts.Gameplay.Dragon` / `.Movements` / `.ReelSlots` / `.Selection` | ECS code per feature folder |
| `Gameplay` | `DragonBreathConfig`, `EndScreenView` (bare namespace, no prefix) |
| `AzurGames.Wool.Gameplay` (+ `.Rope`), `AzurGames.Wool.UI` | `!Project/Scripts/*` views and enums |
| `Azur.Playable`, `Azur.PlayableTemplate.*` | Luna template scripts and plugins |
| `Playeble.EditorTools` | `Assets/Editor` |

Do not "fix" the `Playeble` spelling: scenes and prefabs reference scripts by GUID, but renaming
namespaces ripples through every `using`.

## Key packages (`Packages/manifest.json`)

Azur marketing template packages from the scoped registry `https://npm.azurgames.com`
(`com.azur-marketing.*`: audio 3.4.0, input, screens, ctabutton, orientation, postprocess,
particles-prewarmer, time-scaler, fpscounter, helpers), Cinemachine 2.10.6 (dragon path),
URP 17.0.4, UGUI, Newtonsoft JSON, `com.unity.playworks.upp` (Luna). EcsLite is embedded under
`Assets/LeoEcs`; DOTween, TextMesh Pro and Epic Toon FX (VFX) are imported assets.

## Working-tree drift to know about

`git status` shows uncommitted, machine-specific changes: `luna.json` (`applicationID` 0 instead of
`36343`, other `environmentID`, `msbuildWin64` path), `Assets/SVC_Luna.asset`, `ProjectVersion.txt`,
`Packages/manifest.json`/`packages-lock.json` (package bumps), `ShaderGraphSettings.asset`.
Do not commit these as part of a feature unless the task is about them.
