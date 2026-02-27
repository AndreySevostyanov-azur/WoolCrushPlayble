using AzurGames.Wool.Gameplay;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.ReelSlots
{
    public sealed class ReelWindSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly GameContext _ctx;
        private const float DisposeDuration = 0.12f;

        private EcsWorld _world;
        private EcsFilter _slots;
        private EcsFilter _scaleCandidates;
        private EcsPool<ReelSlotState> _statePool;
        private EcsPool<ReelSpoolData> _spoolPool;
        private EcsPool<ReelSpoolColor> _colorPool;
        private EcsPool<ReelWindState> _windPool;
        private EcsPool<DragonScaleMarkerRef> _markerPool;
        private EcsPool<DragonScaleColor> _scaleColorPool;
        private EcsPool<DragonPart> _partPool;
        private EcsPool<DragonRebukeRequestEvent> _rebukeEventPool;

        public ReelWindSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public ReelWindSystem()
        {
        }

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _statePool = _world.GetPool<ReelSlotState>();
            _spoolPool = _world.GetPool<ReelSpoolData>();
            _colorPool = _world.GetPool<ReelSpoolColor>();
            _windPool = _world.GetPool<ReelWindState>();
            _markerPool = _world.GetPool<DragonScaleMarkerRef>();
            _scaleColorPool = _world.GetPool<DragonScaleColor>();
            _partPool = _world.GetPool<DragonPart>();
            _rebukeEventPool = _world.GetPool<DragonRebukeRequestEvent>();
            _slots = _world.Filter<ReelSlotState>().Inc<ReelSpoolData>().Inc<ReelSpoolColor>().End();
            _scaleCandidates = _world.Filter<DragonScaleMarkerRef>().Inc<DragonScaleColor>().Inc<DragonPart>().End();
        }

        public void Run(IEcsSystems systems)
        {
            var dt = Time.deltaTime;
            var secondsPerScale = _ctx != null ? _ctx.WindSecondsPerScale : 0.35f;
            if (secondsPerScale < 0.01f)
            {
                secondsPerScale = 0.01f;
            }

            var hasPathEnd = false;
            var pathEndPos = default(Vector3);
            if (_ctx != null && _ctx.DragonPath != null)
            {
                var path = _ctx.DragonPath;
                var len = path.PathLength;
                pathEndPos = path.EvaluatePositionAtUnit(len, Cinemachine.CinemachinePathBase.PositionUnits.Distance);
                hasPathEnd = true;
            }

            var slotEntities = _slots.GetRawEntities();
            var slotCount = _slots.GetEntitiesCount();
            var stateDense = _statePool.GetRawDenseItems();
            var stateSparse = _statePool.GetRawSparseItems();
            var spoolDense = _spoolPool.GetRawDenseItems();
            var spoolSparse = _spoolPool.GetRawSparseItems();
            var colorDense = _colorPool.GetRawDenseItems();
            var colorSparse = _colorPool.GetRawSparseItems();
            var windDense = _windPool.GetRawDenseItems();
            var windSparse = _windPool.GetRawSparseItems();
            var markerDense = _markerPool.GetRawDenseItems();
            var markerSparse = _markerPool.GetRawSparseItems();
            var scaleColorDense = _scaleColorPool.GetRawDenseItems();
            var scaleColorSparse = _scaleColorPool.GetRawSparseItems();
            var partDense = _partPool.GetRawDenseItems();
            var partSparse = _partPool.GetRawSparseItems();

            for (var i = 0; i < slotCount; i++)
            {
                var e = slotEntities[i];
                if (e < 0 || e >= stateSparse.Length || e >= spoolSparse.Length || e >= colorSparse.Length || e >= windSparse.Length)
                {
                    continue;
                }

                var stateIdx = stateSparse[e];
                var spoolIdx = spoolSparse[e];
                var colorIdx = colorSparse[e];
                if (stateIdx <= 0 || spoolIdx <= 0 || colorIdx <= 0)
                {
                    continue;
                }

                var state = stateDense[stateIdx];
                var spool = spoolDense[spoolIdx];
                var color = colorDense[colorIdx];

                if (state.Status == SlotStates.Dispose)
                {
                    var disposeIdx = EnsureWindState(e, windSparse, windDense);
                    if (disposeIdx > 0)
                    {
                        var w = windDense[disposeIdx];
                        w.DisposeTimer += dt;
                        if (w.DisposeTimer >= DisposeDuration)
                        {
                            state.Status = SlotStates.Empty;
                            stateDense[stateIdx] = state;
                            _spoolPool.Del(e);
                            _colorPool.Del(e);
                            _windPool.Del(e);
                        }
                        else
                        {
                            windDense[disposeIdx] = w;
                        }
                    }

                    continue;
                }

                if (state.Status != SlotStates.Occupied)
                {
                    continue;
                }

                if (spool.Max <= 0)
                {
                    state.Status = SlotStates.Dispose;
                    stateDense[stateIdx] = state;
                    continue;
                }

                if (spool.Current >= spool.Max)
                {
                    state.Status = SlotStates.Dispose;
                    stateDense[stateIdx] = state;
                    continue;
                }

                var windIdx = EnsureWindState(e, windSparse, windDense);
                if (windIdx <= 0)
                {
                    continue;
                }

                var wind = windDense[windIdx];
                var targetEntity = wind.TargetEntity;
                var target = GetMarkerByEntity(targetEntity, markerDense, markerSparse);
                if (target == null)
                {
                    targetEntity = FindBestTargetEntity(
                        color.Color,
                        hasPathEnd,
                        pathEndPos,
                        markerDense,
                        markerSparse,
                        scaleColorDense,
                        scaleColorSparse,
                        partDense,
                        partSparse);
                    target = GetMarkerByEntity(targetEntity, markerDense, markerSparse);
                    if (targetEntity < 0 || target == null)
                    {
                        wind.TargetEntity = -1;
                        windDense[windIdx] = wind;
                        continue;
                    }

                    target.SetUnwinding(true);
                    target.SetCutout(1f);
                    wind.TargetEntity = targetEntity;
                    wind.Progress01 = 0f;
                }

                wind.Progress01 += dt / secondsPerScale;
                var visible = 1f - wind.Progress01;
                target.SetCutout(visible);

                if (wind.Progress01 >= 1f)
                {
                    var removedBodyIndex = GetBodyIndexByEntity(targetEntity, partDense, partSparse);
                    if (removedBodyIndex >= 0)
                    {
                        var rebukeEntity = _world.NewEntity();
                        _rebukeEventPool.Add(rebukeEntity);
                        var rebukeDense = _rebukeEventPool.GetRawDenseItems();
                        var rebukeSparse = _rebukeEventPool.GetRawSparseItems();
                        if (rebukeEntity >= 0 && rebukeEntity < rebukeSparse.Length)
                        {
                            var rebukeIdx = rebukeSparse[rebukeEntity];
                            if (rebukeIdx > 0)
                            {
                                var ev = rebukeDense[rebukeIdx];
                                ev.RemovedBodyIndex = removedBodyIndex;
                                ev.ShiftDistance = _ctx != null ? _ctx.DragonSegmentSpacing : 0f;
                                rebukeDense[rebukeIdx] = ev;
                            }
                        }
                    }

                    target.SetCutout(0f);
                    target.SetUnwinding(false);
                    Object.Destroy(target.gameObject);
                    _world.DelEntity(targetEntity);

                    spool.Current = spool.Current + 1;
                    spoolDense[spoolIdx] = spool;

                    wind.TargetEntity = -1;
                    wind.Progress01 = 0f;

                    if (spool.Current >= spool.Max)
                    {
                        state.Status = SlotStates.Dispose;
                        stateDense[stateIdx] = state;
                    }
                }

                windDense[windIdx] = wind;
            }
        }

        private DragonScaleMarker GetMarkerByEntity(int entity, DragonScaleMarkerRef[] markerDense, int[] markerSparse)
        {
            if (entity < 0 || entity >= markerSparse.Length)
            {
                return null;
            }

            var idx = markerSparse[entity];
            if (idx <= 0)
            {
                return null;
            }

            return markerDense[idx].Value;
        }

        private int FindBestTargetEntity(
            Colors spoolColor,
            bool hasPathEnd,
            Vector3 pathEndPos,
            DragonScaleMarkerRef[] markerDense,
            int[] markerSparse,
            DragonScaleColor[] scaleColorDense,
            int[] scaleColorSparse,
            DragonPart[] partDense,
            int[] partSparse)
        {
            var entities = _scaleCandidates.GetRawEntities();
            var count = _scaleCandidates.GetEntitiesCount();
            var bestEntity = -1;
            var bestPartIndex = int.MaxValue;
            var bestSqrToEnd = float.MaxValue;
            var bestSqrFallback = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var e = entities[i];
                if (e < 0 || e >= markerSparse.Length || e >= scaleColorSparse.Length || e >= partSparse.Length)
                {
                    continue;
                }

                var markerIdx = markerSparse[e];
                var colorIdx = scaleColorSparse[e];
                var partIdx = partSparse[e];
                if (markerIdx <= 0 || colorIdx <= 0 || partIdx <= 0)
                {
                    continue;
                }

                if (partDense[partIdx].Kind != DragonPartKind.Body)
                {
                    continue;
                }

                if (scaleColorDense[colorIdx].Type != spoolColor)
                {
                    continue;
                }

                var part = partDense[partIdx];
                var partIndex = part.Index;

                var marker = markerDense[markerIdx].Value;
                if (marker == null || marker.IsUnwinding)
                {
                    continue;
                }

                var p = marker.transform.position;
                if (partIndex < bestPartIndex)
                {
                    bestPartIndex = partIndex;
                    bestSqrToEnd = (p - pathEndPos).sqrMagnitude;
                    bestSqrFallback = p.sqrMagnitude;
                    bestEntity = e;
                    continue;
                }

                if (partIndex > bestPartIndex)
                {
                    continue;
                }

                if (hasPathEnd)
                {
                    var sqrToEnd = (p - pathEndPos).sqrMagnitude;
                    if (sqrToEnd < bestSqrToEnd)
                    {
                        bestSqrToEnd = sqrToEnd;
                        bestEntity = e;
                    }
                }
                else
                {
                    var sqr = p.sqrMagnitude;
                    if (sqr < bestSqrFallback)
                    {
                        bestSqrFallback = sqr;
                        bestEntity = e;
                    }
                }
            }

            return bestEntity;
        }

        private int GetBodyIndexByEntity(int entity, DragonPart[] partDense, int[] partSparse)
        {
            if (entity < 0 || entity >= partSparse.Length)
            {
                return -1;
            }

            var idx = partSparse[entity];
            if (idx <= 0)
            {
                return -1;
            }

            var part = partDense[idx];
            if (part.Kind != DragonPartKind.Body)
            {
                return -1;
            }

            return part.Index;
        }

        private int EnsureWindState(int entity, int[] windSparse, ReelWindState[] windDense)
        {
            if (entity < 0 || entity >= windSparse.Length)
            {
                return 0;
            }

            var idx = windSparse[entity];
            if (idx > 0)
            {
                return idx;
            }

            _windPool.Add(entity);
            idx = windSparse[entity];
            if (idx > 0)
            {
                var w = windDense[idx];
                w.TargetEntity = -1;
                w.Progress01 = 0f;
                w.DisposeTimer = 0f;
                windDense[idx] = w;
            }

            return idx;
        }
    }
}

