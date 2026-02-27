using System.Collections.Generic;
using Cinemachine;
using Leopotam.EcsLite;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public sealed class DragonMoveAlongPathSystem : IEcsInitSystem, IEcsRunSystem
    {
        private struct ActiveRebuke
        {
            public int RemovedBodyIndex;
            public float ShiftDistance;
            public float Elapsed;
            public float Duration;
            public float AppliedShift;
        }

        // Keeping ctor compatible with GameBootstrap injection.
        public DragonMoveAlongPathSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public DragonMoveAlongPathSystem()
        {
        }

        private EcsWorld _world;
        private EcsFilter _headFilter;
        private EcsFilter _followFilter;
        private EcsFilter _rebukeRequestFilter;
        private EcsPool<TransformRef> _transformPool;
        private EcsPool<SplinePathRef> _pathPool;
        private EcsPool<DragonHeadMove> _headMovePool;
        private EcsPool<DragonSpawnProgress> _progressPool;
        private EcsPool<DragonFollowHead> _followPool;
        private EcsPool<DragonPart> _partPool;
        private EcsPool<DragonRebukeState> _rebukeStatePool;
        private EcsPool<DragonRebukeRequestEvent> _rebukeRequestPool;
        private EcsPool<DragonReachedEndEvent> _reachedEndEventPool;
        private readonly List<int> _rebukeEventsToDelete = new List<int>(8);
        private readonly List<ActiveRebuke> _activeRebukes = new List<ActiveRebuke>(8);
        private readonly GameContext _ctx;

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _transformPool = _world.GetPool<TransformRef>();
            _pathPool = _world.GetPool<SplinePathRef>();
            _headMovePool = _world.GetPool<DragonHeadMove>();
            _progressPool = _world.GetPool<DragonSpawnProgress>();
            _followPool = _world.GetPool<DragonFollowHead>();
            _partPool = _world.GetPool<DragonPart>();
            _rebukeStatePool = _world.GetPool<DragonRebukeState>();
            _rebukeRequestPool = _world.GetPool<DragonRebukeRequestEvent>();
            _reachedEndEventPool = _world.GetPool<DragonReachedEndEvent>();

            _headFilter = _world
                .Filter<DragonHeadMove>()
                .Inc<TransformRef>()
                .Inc<SplinePathRef>()
                .End();

            _followFilter = _world
                .Filter<DragonFollowHead>()
                .Inc<TransformRef>()
                .Inc<SplinePathRef>()
                .End();

            _rebukeRequestFilter = _world
                .Filter<DragonRebukeRequestEvent>()
                .End();
        }

        public void Run(IEcsSystems systems)
        {
            var dt = Time.deltaTime;

            var headEntities = _headFilter.GetRawEntities();
            var headCount = _headFilter.GetEntitiesCount();
            var headMoveDense = _headMovePool.GetRawDenseItems();
            var headMoveSparse = _headMovePool.GetRawSparseItems();
            var progressDense = _progressPool.GetRawDenseItems();
            var progressSparse = _progressPool.GetRawSparseItems();
            var pathDense = _pathPool.GetRawDenseItems();
            var pathSparse = _pathPool.GetRawSparseItems();
            var trDense = _transformPool.GetRawDenseItems();
            var trSparse = _transformPool.GetRawSparseItems();
            var partDense = _partPool.GetRawDenseItems();
            var partSparse = _partPool.GetRawSparseItems();
            var rebukeStateDense = _rebukeStatePool.GetRawDenseItems();
            var rebukeStateSparse = _rebukeStatePool.GetRawSparseItems();
            var rebukeRequestDense = _rebukeRequestPool.GetRawDenseItems();
            var rebukeRequestSparse = _rebukeRequestPool.GetRawSparseItems();

            ReadAndQueueRebukeEvents(rebukeRequestDense, rebukeRequestSparse);
            var headShiftDelta = TickActiveRebukesAndGetHeadDelta(dt);
            var totalAppliedShift = GetTotalAppliedShift();

            for (var i = 0; i < headCount; i++)
            {
                var headEntity = headEntities[i];
                if (headEntity < 0
                    || headEntity >= headMoveSparse.Length
                    || headEntity >= pathSparse.Length
                    || headEntity >= trSparse.Length
                    || headEntity >= partSparse.Length)
                {
                    continue;
                }

                var headMoveIdx = headMoveSparse[headEntity];
                var headPathIdx = pathSparse[headEntity];
                var headTrIdx = trSparse[headEntity];
                var headPartIdx = partSparse[headEntity];
                if (headMoveIdx <= 0 || headPathIdx <= 0 || headTrIdx <= 0 || headPartIdx <= 0)
                {
                    continue;
                }

                var headPart = partDense[headPartIdx];
                if (headPart.Kind != DragonPartKind.Head)
                {
                    continue;
                }

                var headMove = headMoveDense[headMoveIdx];
                var headPath = pathDense[headPathIdx];
                var headTransformRef = trDense[headTrIdx];

                var path = headPath.Path;
                var tr = headTransformRef.Value;
                if (path == null || tr == null)
                {
                    continue;
                }

                headMove.Distance += headMove.Speed * dt;
                var forwardDistanceForSpawn = headMove.Distance;
                headMove.Distance -= headShiftDelta;
                if (headMove.Distance < 0f)
                {
                    headMove.Distance = 0f;
                }

                var length = path.PathLength;
                if (headMove.Distance >= length)
                {
                    headMove.Distance = length;
                    if (!headMove.ReachedEndRaised)
                    {
                        headMove.ReachedEndRaised = true;
                        var eventEntity = _world.NewEntity();
                        _reachedEndEventPool.Add(eventEntity);
                    }
                }

                // Spawn progression should use pure forward travel and should not be rewound by rebuke.
                if (headEntity >= 0 && headEntity < progressSparse.Length)
                {
                    var progressIdx = progressSparse[headEntity];
                    if (progressIdx > 0)
                    {
                        var progress = progressDense[progressIdx];
                        var spawnDistance = forwardDistanceForSpawn;
                        if (spawnDistance > length)
                        {
                            spawnDistance = length;
                        }

                        if (spawnDistance > progress.MaxHeadDistanceReached)
                        {
                            progress.MaxHeadDistanceReached = spawnDistance;
                            progressDense[progressIdx] = progress;
                        }
                    }
                }

                // Mirror current rebuke envelope in ECS state for observability / stacking behavior.
                var rebukeIdx = rebukeStateSparse[headEntity];
                if (totalAppliedShift > 0f)
                {
                    if (rebukeIdx <= 0)
                    {
                        _rebukeStatePool.Add(headEntity);
                        rebukeIdx = rebukeStateSparse[headEntity];
                    }

                    if (rebukeIdx > 0)
                    {
                        var rebukeState = rebukeStateDense[rebukeIdx];
                        rebukeState.StartDistance = headMove.Distance;
                        rebukeState.TargetDistance = headMove.Distance - totalAppliedShift;
                        rebukeState.Elapsed = 0.5f;
                        rebukeState.Duration = GetRebukeDuration();
                        rebukeStateDense[rebukeIdx] = rebukeState;
                    }
                }
                else if (rebukeIdx > 0)
                {
                    _rebukeStatePool.Del(headEntity);
                }

                ApplyTransform(path, headMove.Distance, tr);
                headMoveDense[headMoveIdx] = headMove;
            }

            var followEntities = _followFilter.GetRawEntities();
            var followCount = _followFilter.GetEntitiesCount();
            var followDense = _followPool.GetRawDenseItems();
            var followSparse = _followPool.GetRawSparseItems();
            for (var i = 0; i < followCount; i++)
            {
                var followEntity = followEntities[i];
                if (followEntity < 0
                    || followEntity >= followSparse.Length
                    || followEntity >= pathSparse.Length
                    || followEntity >= trSparse.Length
                    || followEntity >= partSparse.Length)
                {
                    continue;
                }

                var followIdx = followSparse[followEntity];
                var pathIdx = pathSparse[followEntity];
                var trIdx = trSparse[followEntity];
                var partIdx = partSparse[followEntity];
                if (followIdx <= 0 || pathIdx <= 0 || trIdx <= 0 || partIdx <= 0)
                {
                    continue;
                }

                var follow = followDense[followIdx];
                var pathRef = pathDense[pathIdx];
                var transformRef = trDense[trIdx];
                var part = partDense[partIdx];

                var path = pathRef.Path;
                var tr = transformRef.Value;
                if (path == null || tr == null)
                {
                    continue;
                }

                var packedHead = follow.Head;
                var headEntity = packedHead.Id;
                if (headEntity < 0 || headEntity >= headMoveSparse.Length)
                {
                    continue;
                }

                var headMoveIdx = headMoveSparse[headEntity];
                if (headMoveIdx <= 0)
                {
                    continue;
                }

                var baseHeadDistance = headMoveDense[headMoveIdx].Distance;
                var distance = baseHeadDistance - follow.OffsetDistance;
                if (part.Kind == DragonPartKind.Body)
                {
                    distance += GetBodyCompensation(part.Index);
                }
                else if (part.Kind == DragonPartKind.Tail)
                {
                    distance += totalAppliedShift;
                }

                if (distance < 0f)
                {
                    distance = 0f;
                }

                var length = path.PathLength;
                if (distance > length)
                {
                    distance = length;
                }

                ApplyTransform(path, distance, tr);
            }

            for (var i = 0; i < _rebukeEventsToDelete.Count; i++)
            {
                _world.DelEntity(_rebukeEventsToDelete[i]);
            }
        }

        private void ReadAndQueueRebukeEvents(DragonRebukeRequestEvent[] rebukeRequestDense, int[] rebukeRequestSparse)
        {
            _rebukeEventsToDelete.Clear();

            var rebukeEntities = _rebukeRequestFilter.GetRawEntities();
            var rebukeCount = _rebukeRequestFilter.GetEntitiesCount();
            for (var i = 0; i < rebukeCount; i++)
            {
                var eventEntity = rebukeEntities[i];
                if (eventEntity < 0 || eventEntity >= rebukeRequestSparse.Length)
                {
                    continue;
                }

                var requestIdx = rebukeRequestSparse[eventEntity];
                if (requestIdx <= 0)
                {
                    continue;
                }

                var request = rebukeRequestDense[requestIdx];
                if (request.RemovedBodyIndex >= 0 && request.ShiftDistance > 0f)
                {
                    var rebuke = new ActiveRebuke();
                    rebuke.RemovedBodyIndex = request.RemovedBodyIndex;
                    rebuke.ShiftDistance = request.ShiftDistance;
                    rebuke.Elapsed = 0f;
                    rebuke.Duration = GetRebukeDuration();
                    rebuke.AppliedShift = 0f;
                    _activeRebukes.Add(rebuke);
                }

                _rebukeEventsToDelete.Add(eventEntity);
            }
        }

        private float TickActiveRebukesAndGetHeadDelta(float dt)
        {
            var deltaSum = 0f;
            for (var i = 0; i < _activeRebukes.Count; i++)
            {
                var rebuke = _activeRebukes[i];
                rebuke.Elapsed += dt;
                if (rebuke.Elapsed > rebuke.Duration)
                {
                    rebuke.Elapsed = rebuke.Duration;
                }

                var newApplied = EvaluateRebukeShift(rebuke.ShiftDistance, rebuke.Elapsed, rebuke.Duration);
                var delta = newApplied - rebuke.AppliedShift;
                if (delta > 0f)
                {
                    deltaSum += delta;
                }

                rebuke.AppliedShift = newApplied;
                _activeRebukes[i] = rebuke;
            }

            return deltaSum;
        }

        private float GetTotalAppliedShift()
        {
            var sum = 0f;
            for (var i = 0; i < _activeRebukes.Count; i++)
            {
                sum += _activeRebukes[i].AppliedShift;
            }

            return sum;
        }

        private float GetBodyCompensation(int bodyIndex)
        {
            var sum = 0f;
            for (var i = 0; i < _activeRebukes.Count; i++)
            {
                var rebuke = _activeRebukes[i];
                if (bodyIndex >= rebuke.RemovedBodyIndex)
                {
                    sum += rebuke.AppliedShift;
                }
            }

            return sum;
        }

        private float GetRebukeDuration()
        {
            var d = _ctx != null ? _ctx.DragonRebukeDuration : 0.08f;
            if (d < 0.01f)
            {
                d = 0.01f;
            }

            return d;
        }

        private static float EvaluateRebukeShift(float shiftDistance, float elapsed, float duration)
        {
            if (duration <= 0.0001f)
            {
                return shiftDistance;
            }

            var t = Mathf.Clamp01(elapsed / duration);
            var smooth = Mathf.SmoothStep(0f, 1f, t);
            return Mathf.Lerp(0f, shiftDistance, smooth);
        }

        private static void ApplyTransform(CinemachinePath path, float distance, Transform target)
        {
            if (path == null || target == null)
            {
                return;
            }

            var pos = path.EvaluatePositionAtUnit(distance, CinemachinePathBase.PositionUnits.Distance);
            var rot = path.EvaluateOrientationAtUnit(distance, CinemachinePathBase.PositionUnits.Distance);
            target.position = pos;
            target.rotation = rot;
        }
    }
}

