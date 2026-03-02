using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public sealed class EcsUtils
    {
        public static void SetComponent<T>(EcsPool<T> pool, int entity, T value) where T : struct
        {
            var sparse = pool.GetRawSparseItems();
            if (entity < 0 || entity >= sparse.Length)
            {
                return;
            }

            var idx = sparse[entity];
            if (idx <= 0)
            {
                return;
            }

            var dense = pool.GetRawDenseItems();
            dense[idx] = value;
        }
    }

    public sealed class DragonSpawnInitSystem : IEcsInitSystem
    {
        private readonly GameContext _ctx;

        public DragonSpawnInitSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        private static Transform GetTransformSafe(MonoBehaviour go)
        {
            if (go == null) { return null; }

            // Prefer component lookup first - some JS pipelines may fail on GameObject.transform.
            var tr = go.GetComponent<Transform>();
            if (tr != null) { return tr; }

            return go.transform;
        }

        private static T InstantiateSafe<T>(T prefab, Transform parent) where T : MonoBehaviour
        {
            if (prefab == null) { return null; }

            // Web/JS pipelines can be picky about Instantiate overloads.
            T go = null;

            try
            {
                go = Object.Instantiate(prefab);
            }
            catch
            {
                // ignored
            }

            var tr = GetTransformSafe(go);
            if (tr == null)
            {
                try
                {
                    go = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
                }
                catch
                {
                    // ignored
                }

                tr = GetTransformSafe(go);
            }

            if (tr != null && parent != null)
            {
                tr.SetParent(parent, false);
            }

            return go;
        }

        private static void ApplyColorOffset(GameObject go, int colorOffset)
        {
            if (go == null)
            {
                return;
            }

            var scale = go.GetComponentInChildren<DragonScaleMesh>(true);
            if (scale == null)
            {
              return;
            }

            scale.ApplyColorOffset(colorOffset);
        }

        public void Init(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            if (_ctx == null)
            {
                Debug.LogError($"{nameof(DragonSpawnInitSystem)}: missing GameContext.");
                return;
            }

            if (_ctx.DragonPath == null)
            {
                Debug.LogError($"{nameof(DragonSpawnInitSystem)}: DragonPath is not set.");
                return;
            }

            if (_ctx.DragonHeadPrefab == null || _ctx.DragonBodyPrefab == null || _ctx.DragonTailPrefab == null)
            {
                Debug.LogError($"{nameof(DragonSpawnInitSystem)}: One of dragon prefabs is not set.");
                return;
            }

            var transformPool = world.GetPool<TransformRef>();
            var partPool = world.GetPool<DragonPart>();
            var pathPool = world.GetPool<SplinePathRef>();
            var headMovePool = world.GetPool<DragonHeadMove>();
            var progressPool = world.GetPool<DragonSpawnProgress>();
            var headComponentsPool = world.GetPool<DragonHeadComponent>();

            var parent = _ctx.DragonRoot;

            var headGo = InstantiateSafe(_ctx.DragonHeadPrefab, parent);
            if (headGo == null)
            {
                Debug.LogError($"{nameof(DragonSpawnInitSystem)}: Failed to instantiate head prefab.");
                return;
            }
            var headEntity = world.NewEntity();

            transformPool.Add(headEntity);
            partPool.Add(headEntity);
            pathPool.Add(headEntity);
            headMovePool.Add(headEntity);
            progressPool.Add(headEntity);
            headComponentsPool.Add(headEntity);

            var headTransformValue = GetTransformSafe(headGo);
            if (headTransformValue == null)
            {
                Debug.LogError($"{nameof(DragonSpawnInitSystem)}: Head transform is null after instantiate.");
                world.DelEntity(headEntity);
                return;
            }

            EcsUtils.SetComponent(headComponentsPool, headEntity, new DragonHeadComponent
            {
                DragonDeathEffect = headGo.DragonDeathObject,
                DragonFireEffect = headGo.DragonBreathObject,
                DragonHeadAnimator = headGo.DragonHeadAnimator,
            });
            EcsUtils.SetComponent(transformPool, headEntity, new TransformRef { Value = headTransformValue });
            EcsUtils.SetComponent(partPool, headEntity, new DragonPart { Kind = DragonPartKind.Head, Index = 0 });
            EcsUtils.SetComponent(pathPool, headEntity, new SplinePathRef { Path = _ctx.DragonPath });

            EcsUtils.SetComponent(headMovePool, headEntity, new DragonHeadMove
            {
                Distance = _ctx.DragonInitialHeadDistance,
                BaseSpeed = _ctx.DragonHeadSpeed,
                Speed = _ctx.DragonHeadSpeed,
                ReachedEndRaised = false
            });

            // Head exists, body/tail will be spawned progressively by DragonGrowSpawnSystem.
            EcsUtils.SetComponent(progressPool, headEntity, new DragonSpawnProgress
            {
                BodySpawned = 0,
                TailSpawned = false,
                MaxHeadDistanceReached = _ctx.DragonInitialHeadDistance
            });
        }
    }
}

