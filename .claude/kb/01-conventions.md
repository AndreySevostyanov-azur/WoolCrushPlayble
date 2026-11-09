# 01 - Conventions

`Last verified: 2026-11-09` (derived from the existing code, not from a style guide).

## Style observed in the code

- Allman braces, 4 spaces, `var` for obvious right-hand sides, explicit types in public APIs.
- `sealed` classes where there is no inheritance; private fields `_camelCase`; constants `PascalCase`
  (`MaxSpawnPerFrame`) or `const` locals (`epsilon`).
- Serialized fields: `[SerializeField] private`, exposed through read-only properties
  (`SlotView` and a few older classes use public fields - leave them).
- `[Header]`, `[Min]`, `[Range]` on tuning values; ScriptableObject configs use
  `[CreateAssetMenu(menuName = "Playable/...")]`.
- Unity-object null checks use `== null` / `!= null`.
- Comments in the repo are a mix of English and Russian. Root `AGENTS.md` rule 5: no comments that narrate code;
  short comments for Luna footguns or a non-obvious "why" are fine; existing ones are left alone.

## Naming

| Kind | Rule | Examples |
|------|------|----------|
| System | `*System`, implements `IEcsInitSystem` and/or `IEcsRunSystem` | `BlockPathMoveSystem`, `ComputeGameBordersInitSystem` |
| Component | plain `struct`, name states what it holds; `*Ref` when it wraps a Unity object | `DragonHeadMove`, `BlockViewRef`, `TransformRef` |
| Event | `struct` with `Event` suffix, lives one tick as its own entity | `BlockClickedEvent`, `DragonReachedEndEvent`, `BlockArrivedToSlotEvent`, `DragonRebukeRequestEvent` |
| View | `MonoBehaviour` with `*View` / `*Marker` / `*Mesh`; holds serialized refs only | `SlotView`, `DragonHeadView`, `DragonScaleMarker` |
| Config | ScriptableObject `*Config` / `*Settings` | `DragonBreathConfig`, `VariableAccelerationSettings` |
| Tutorial / Luna fields | `[LunaPlaygroundField("title", order, "group")]` on serialized fields | `GameBootstrap._windZonePercent` |

Files: one type per file for systems and views; small related component structs share a file
(`DragonComponents.cs`, `ReelSlotComponents.cs`).

## ECS data-access idiom (EcsLite) - follow it in new systems

The code never uses `ref` returns or `foreach` over filters. Pattern in every system:

1. In `Init(IEcsSystems systems)`: `_world = systems.GetWorld();`, cache every `EcsPool<T>` and `EcsFilter`
   (`_world.Filter<A>().Inc<B>().End()`).
2. In `Run`: take raw arrays once - `filter.GetRawEntities()`, `filter.GetEntitiesCount()`,
   `pool.GetRawDenseItems()`, `pool.GetRawSparseItems()` - and loop with a `for` index.
3. Presence check: `idx = sparse[entity]`; **`idx <= 0` means the component is absent** (dense slot 0 is reserved).
   Always bound-check `entity < sparse.Length` first.
4. Read by copying `var c = dense[idx];`, mutate the copy, **write back** `dense[idx] = c;`.
5. Add components with `pool.Add(entity)` (or `((IEcsPool)pool).AddRaw(entity, default(T))`), then write
   the value through the dense array. `EcsUtils.SetComponent<T>(pool, entity, value)`
   (`Dragon/DragonSpawnInitSystem.cs`) does steps 3-4 for you and is reused by other systems.
6. Never delete entities while iterating a filter. Collect ids into a reused `List<int>`
   (`_events`, `_eventsToDelete`, `_entitiesToDelete`), then `_world.DelEntity` after the loop.
7. Cross-entity references: either an int entity id looked up through a pool each frame
   (`ReelWindState.TargetEntity`) or `EcsPackedEntity` (`DragonFollowHead.Head`, created with
   `_world.PackEntity`). Re-validate the referenced pool slot before use.
8. Events: create an entity, add the event component, fill it. The consuming system, or a dedicated
   `*CleanupSystem`, deletes it the same frame. Events are order-dependent - see the system order in
   [02-gameplay-ecs.md](02-gameplay-ecs.md).
9. System construction: every system exposes `public XxxSystem(GameContext ctx)`;
   `GameBootstrap.CreateEcsSystems` instantiates them with `Activator.CreateInstance(type, _gameContext)`.
   Systems that do not need the context still take it (`EventCleanup`, `BlockClickEventCleanup` use `GameContext _`).
   A system without a `GameContext` constructor will throw at startup.

## Luna-safe coding (code comments in the repo state these; keep to them)

Luna converts the C# build to JavaScript, so the existing code avoids some patterns:

- no `ref` returns and no `foreach` enumerators in hot paths (`DragonGrowSpawnSystem` header comment);
- prefer `GetComponent<Transform>()` over `GameObject.transform` and wrap `Object.Instantiate` in a
  safe helper with a fallback overload (`InstantiateSafe` in `DragonSpawnInitSystem` / `DragonGrowSpawnSystem`,
  comments: "some JS pipelines may fail on GameObject.transform", "Web/JS pipelines can be picky about Instantiate overloads");
- do initialisation in `Awake`/`Start`, not in MonoBehaviour constructors (`GameBootstrap.Awake` comment);
- one-tick events are filled through dense arrays, not `out`/`ref` helpers (`BlockPathMoveSystem` comment "Luna-safe: no out/ref");
- avoid LINQ and per-frame allocations (also the `.cursor/rules` performance rule).

These constraints are inferred from the code comments; the Luna documentation was not consulted.
Anything new that touches reflection, threading, `Task`/`async`, `System.IO` or networking must be
tested in a real Luna export first.

## Things to avoid

- Gameplay logic in `MonoBehaviour.Update`: the only legitimate `Update` users are `GameBootstrap`
  (drives the systems) and `ClickRaycastToEcsEvent` (input bridge).
- Reordering `GameBootstrap.BindSystems` without reading the order dependencies in
  [02-gameplay-ecs.md](02-gameplay-ecs.md).
- Adding third-party packages: the Luna build size and compatibility are the main constraints.
