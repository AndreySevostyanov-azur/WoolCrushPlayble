using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Dragon
{
    /// <summary>
    /// Spawns dragon body parts progressively based on head distance traveled.
    /// Luna-safe: avoids ref returns and foreach enumerators in hot paths.
    /// </summary>
    public sealed class DragonGrowSpawnSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int MaxSpawnPerFrame = 1;

        private readonly GameContext _ctx;
        private EcsWorld _world;
        private EcsFilter _headFilter;

        private EcsPool<TransformRef> _transformPool;
        private EcsPool<SplinePathRef> _pathPool;
        private EcsPool<DragonHeadMove> _headMovePool;
        private EcsPool<DragonSpawnProgress> _progressPool;
        private EcsPool<DragonFollowHead> _followPool;
        private EcsPool<DragonPart> _partPool;
        private EcsPool<DragonScaleColor> _colorPool;
        private EcsPool<DragonScaleMarkerRef> _markerPool;

        private bool _loggedInit;
        private bool _loggedRun;

        public DragonGrowSpawnSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public DragonGrowSpawnSystem()
        {
        }

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _transformPool = _world.GetPool<TransformRef>();
            _pathPool = _world.GetPool<SplinePathRef>();
            _headMovePool = _world.GetPool<DragonHeadMove>();
            _progressPool = _world.GetPool<DragonSpawnProgress>();
            _followPool = _world.GetPool<DragonFollowHead>();
            _partPool = _world.GetPool<DragonPart>();
            _colorPool = _world.GetPool<DragonScaleColor>();
            _markerPool = _world.GetPool<DragonScaleMarkerRef>();

            _headFilter = _world
                .Filter<DragonHeadMove>()
                .Inc<TransformRef>()
                .Inc<SplinePathRef>()
                .Inc<DragonSpawnProgress>()
                .End();

            if (!_loggedInit)
            {
                _loggedInit = true;
                Debug.Log(
                    $"DragonGrowSpawnSystem.Init ctx={( _ctx == null ? "null" : "ok")} path={( _ctx != null && _ctx.DragonPath != null ? "ok" : "null")} bodyCount={(_ctx != null ? _ctx.DragonBodySegmentsCount : -1)} spacing={(_ctx != null ? _ctx.DragonSegmentSpacing : -1f):0.###}");
            }
        }

        public void Run(IEcsSystems systems)
        {
            if (_ctx == null || _ctx.DragonPath == null)
            {
                if (!_loggedRun)
                {
                    _loggedRun = true;
                    Debug.Log($"DragonGrowSpawnSystem.Run skipped: ctx/path/root missing. ctx={(_ctx==null?"null":"ok")} path={(_ctx!=null&&_ctx.DragonPath!=null?"ok":"null")} root={(_ctx!=null&&_ctx.DragonRoot!=null?"ok":"null")}");
                }
                return;
            }

            var spacing = _ctx.DragonSegmentSpacing;
            if (spacing <= 0f)
            {
                if (!_loggedRun)
                {
                    _loggedRun = true;
                    Debug.Log($"DragonGrowSpawnSystem.Run skipped: spacing <= 0 ({spacing}).");
                }
                return;
            }

            var totalBody = Mathf.Max(0, _ctx.DragonBodySegmentsCount);
            if (totalBody == 0)
            {
                if (!_loggedRun)
                {
                    _loggedRun = true;
                    Debug.Log("DragonGrowSpawnSystem.Run skipped: totalBody == 0.");
                }
                return;
            }

            var headEntities = _headFilter.GetRawEntities();
            var headCount = _headFilter.GetEntitiesCount();

            var headMoveDense = _headMovePool.GetRawDenseItems();
            var headMoveSparse = _headMovePool.GetRawSparseItems();
            var progressDense = _progressPool.GetRawDenseItems();
            var progressSparse = _progressPool.GetRawSparseItems();

            for (var i = 0; i < headCount; i++)
            {
                var headEntity = headEntities[i];
                if (headEntity < 0
                    || headEntity >= headMoveSparse.Length
                    || headEntity >= progressSparse.Length)
                {
                    continue;
                }

                var headMoveIdx = headMoveSparse[headEntity];
                var progressIdx = progressSparse[headEntity];
                if (headMoveIdx <= 0 || progressIdx <= 0)
                {
                    continue;
                }

                var headMove = headMoveDense[headMoveIdx];
                var progress = progressDense[progressIdx];
                var forwardStep = headMove.Speed * Time.deltaTime;
                if (forwardStep < 0f)
                {
                    forwardStep = 0f;
                }
                progress.MaxHeadDistanceReached += forwardStep;
                if (headMove.Distance > progress.MaxHeadDistanceReached)
                {
                    progress.MaxHeadDistanceReached = headMove.Distance;
                }
                var spawnDistance = progress.MaxHeadDistanceReached;

                if (!_loggedRun)
                {
                    _loggedRun = true;
                    Debug.Log($"DragonGrowSpawnSystem.Run: headDistance={headMove.Distance:0.###} spawned={progress.BodySpawned}/{totalBody} tail={progress.TailSpawned}");
                }

                var spawnedThisFrame = 0;
                while (spawnedThisFrame < MaxSpawnPerFrame
                       && progress.BodySpawned < totalBody
                       && spawnDistance >= (progress.BodySpawned + 1) * spacing)
                {
                    var segmentIndex = progress.BodySpawned;
                    if (!TrySpawnBodySegment(headEntity, segmentIndex, spawnDistance, spacing))
                    {
                        break;
                    }

                    progress.BodySpawned++;
                    spawnedThisFrame++;
                }

                if (!progress.TailSpawned
                    && progress.BodySpawned >= totalBody
                    && spawnDistance >= (totalBody + 1) * spacing)
                {
                    if (TrySpawnTailSegment(headEntity, totalBody, spawnDistance, spacing))
                    {
                        progress.TailSpawned = true;
                    }
                }

                // write back progress (no ref returns)
                progressDense[progressIdx] = progress;
            }
        }

        private bool TrySpawnBodySegment(int headEntity, int index, float headDistance, float spacing)
        {
            if (_ctx.DragonBodyPrefab == null)
            {
                return false;
            }

            var go = InstantiateSafe(_ctx.DragonBodyPrefab, _ctx.DragonRoot);
            if (go == null)
            {
                return false;
            }

            var tr = GetTransformSafe(go);
            if (tr == null)
            {
                Object.Destroy(go);
                return false;
            }

            var e = _world.NewEntity();
            _transformPool.Add(e);
            _pathPool.Add(e);
            _followPool.Add(e);
            _partPool.Add(e);
            _colorPool.Add(e);
            _markerPool.Add(e);

            SetComponent(_transformPool, e, new TransformRef { Value = tr });
            SetComponent(_pathPool, e, new SplinePathRef { Path = _ctx.DragonPath });
            SetComponent(_partPool, e, new DragonPart { Kind = DragonPartKind.Body, Index = index });

            var packedHead = _world.PackEntity(headEntity);
            SetComponent(_followPool, e, new DragonFollowHead { Head = packedHead, OffsetDistance = (index + 1) * spacing });

            var slot = GetColorSlot(_ctx.ScaleColors, index);
            SetComponent(_colorPool, e, new DragonScaleColor { Type = slot.Type });
            ApplyColorOffset(go, slot.ColorOffset);
            var marker = SetupScaleMarker(go, slot.Type);
            SetComponent(_markerPool, e, new DragonScaleMarkerRef { Value = marker });

            // initial placement (avoid 1-frame pop)
            var distance = headDistance - (index + 1) * spacing;
            if (distance < 0f) distance = 0f;
            ApplyTransform(_ctx.DragonPath, distance, tr);

            return true;
        }


        private bool TrySpawnTailSegment(int headEntity, int index, float headDistance, float spacing)
        {
            if (_ctx.DragonTailPrefab == null)
            {
                return false;
            }

            var go = InstantiateSafe(_ctx.DragonTailPrefab, _ctx.DragonRoot);
            if (go == null)
            {
                return false;
            }

            var tr = GetTransformSafe(go);
            if (tr == null)
            {
                Object.Destroy(go);
                return false;
            }

            var e = _world.NewEntity();
            _transformPool.Add(e);
            _pathPool.Add(e);
            _followPool.Add(e);
            _partPool.Add(e);
            _colorPool.Add(e);

            SetComponent(_transformPool, e, new TransformRef { Value = tr });
            SetComponent(_pathPool, e, new SplinePathRef { Path = _ctx.DragonPath });
            SetComponent(_partPool, e, new DragonPart { Kind = DragonPartKind.Tail, Index = index });

            var packedHead = _world.PackEntity(headEntity);
            SetComponent(_followPool, e, new DragonFollowHead { Head = packedHead, OffsetDistance = (index + 1) * spacing });

            var slot = GetColorSlot(_ctx.ScaleColors, index);
            SetComponent(_colorPool, e, new DragonScaleColor { Type = slot.Type });
            ApplyColorOffset(go, slot.ColorOffset);

            var distance = headDistance - (index + 1) * spacing;
            if (distance < 0f) distance = 0f;
            ApplyTransform(_ctx.DragonPath, distance, tr);

            return true;
        }

        private static void ApplyTransform(Cinemachine.CinemachinePath path, float distance, Transform target)
        {
            if (path == null || target == null)
            {
                return;
            }

            var pos = path.EvaluatePositionAtUnit(distance, Cinemachine.CinemachinePathBase.PositionUnits.Distance);
            var rot = path.EvaluateOrientationAtUnit(distance, Cinemachine.CinemachinePathBase.PositionUnits.Distance);
            target.position = pos;
            target.rotation = rot;
        }

        private struct ColorSlotResult
        {
            public Colors Type;
            public int ColorOffset;
        }

        private static ColorSlotResult GetColorSlot(GameContext.DragonScaleColorSlot[] slots, int index)
        {
            var result = new ColorSlotResult { Type = Colors.White, ColorOffset = 0 };
            if (slots == null || slots.Length == 0)
            {
                return result;
            }

            var cursor = 0;
            for (var i = 0; i < slots.Length; i++)
            {
                var count = slots[i].Count;
                if (count <= 0)
                {
                    continue;
                }

                var next = cursor + count;
                if (index >= cursor && index < next)
                {
                    result.Type = slots[i].Type;
                    result.ColorOffset = slots[i].ColorOffset;
                    return result;
                }

                cursor = next;
            }

            for (var i = slots.Length - 1; i >= 0; i--)
            {
                if (slots[i].Count > 0)
                {
                    result.Type = slots[i].Type;
                    result.ColorOffset = slots[i].ColorOffset;
                    return result;
                }
            }

            result.Type = slots[0].Type;
            result.ColorOffset = slots[0].ColorOffset;
            return result;
        }

        private static DragonScaleMarker SetupScaleMarker(GameObject go, Colors color)
        {
            if (go == null)
            {
                return null;
            }

            var marker = go.GetComponentInChildren<DragonScaleMarker>(true);
            if (marker == null)
            {
                marker = go.AddComponent<DragonScaleMarker>();
            }

            marker.Setup(color);
            return marker;
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
                scale = go.AddComponent<DragonScaleMesh>();
            }

            scale.ApplyColorOffset(colorOffset);
        }

        private static Transform GetTransformSafe(GameObject go)
        {
            if (go == null) { return null; }

            var tr = go.GetComponent<Transform>();
            if (tr != null) { return tr; }

            return go.transform;
        }

        private static GameObject InstantiateSafe(GameObject prefab, Transform parent)
        {
            if (prefab == null) { return null; }

            GameObject go = null;
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

        private static void SetComponent<T>(EcsPool<T> pool, int entity, T value) where T : struct
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
}

