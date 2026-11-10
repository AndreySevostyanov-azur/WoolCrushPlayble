using System.Collections.Generic;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using Playeble.Scripts.Gameplay.Movements;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    public sealed class BlockNudgeSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly GameContext _ctx;
        private EcsWorld _world;
        private EcsFilter _eventFilter;
        private EcsFilter _nudgeFilter;
        private EcsPool<BlockNudgeEvent> _eventPool;
        private EcsPool<BlockNudge> _nudgePool;
        private EcsPool<BlockViewRef> _viewPool;
        private EcsPool<BlockMoveData> _movePool;
        private readonly List<int> _events = new List<int>(8);
        private readonly List<int> _finished = new List<int>(8);

        public BlockNudgeSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _eventPool = _world.GetPool<BlockNudgeEvent>();
            _nudgePool = _world.GetPool<BlockNudge>();
            _viewPool = _world.GetPool<BlockViewRef>();
            _movePool = _world.GetPool<BlockMoveData>();
            _eventFilter = _world.Filter<BlockNudgeEvent>().End();
            _nudgeFilter = _world.Filter<BlockNudge>().End();
        }

        public void Run(IEcsSystems systems)
        {
            var config = _ctx != null && _ctx.Mechanics != null ? _ctx.Mechanics.Config : null;
            ConsumeEvents(config);
            Tick(Time.deltaTime);
        }

        private void ConsumeEvents(BlockMechanicsConfig config)
        {
            var count = _eventFilter.GetEntitiesCount();
            if (count == 0)
            {
                return;
            }

            _events.Clear();
            var entities = _eventFilter.GetRawEntities();
            var dense = _eventPool.GetRawDenseItems();
            var sparse = _eventPool.GetRawSparseItems();
            for (var i = 0; i < count; i++)
            {
                var ev = entities[i];
                _events.Add(ev);
                if (ev < 0 || ev >= sparse.Length || sparse[ev] <= 0)
                {
                    continue;
                }

                Start(dense[sparse[ev]], config);
            }

            for (var i = 0; i < _events.Count; i++)
            {
                _world.DelEntity(_events[i]);
            }
        }

        private void Start(BlockNudgeEvent request, BlockMechanicsConfig config)
        {
            var block = request.BlockEntity;
            if (block < 0 || !_viewPool.Has(block) || !_movePool.Has(block))
            {
                return;
            }

            var view = _viewPool.Get(block);
            if (view.Transform == null || _movePool.Get(block).MoveType != BlockMoveType.None)
            {
                return;
            }

            var distance = config != null ? config.NudgeDistance : 0.2f;
            var duration = config != null ? config.NudgeDuration : 0.1f;
            if (!_nudgePool.Has(block))
            {
                _nudgePool.Add(block);
            }
            else
            {
                view.Transform.position = view.OriginPosition;
            }

            EcsUtils.SetComponent(_nudgePool, block, new BlockNudge
            {
                Time = 0f,
                Duration = duration,
                Offset = view.Transform.forward * distance,
            });

            if (request.ShowMarker && config != null)
            {
                BlockMechanicViews.SpawnVfx(config.RejectMarkerPrefab, view.OriginPosition, config.VfxLifetime);
            }
        }

        private void Tick(float dt)
        {
            var count = _nudgeFilter.GetEntitiesCount();
            if (count == 0)
            {
                return;
            }

            _finished.Clear();
            var entities = _nudgeFilter.GetRawEntities();
            var dense = _nudgePool.GetRawDenseItems();
            var sparse = _nudgePool.GetRawSparseItems();
            for (var i = 0; i < count; i++)
            {
                var e = entities[i];
                if (e < 0 || e >= sparse.Length || sparse[e] <= 0)
                {
                    continue;
                }

                var idx = sparse[e];
                var nudge = dense[idx];
                var view = _viewPool.Has(e) ? _viewPool.Get(e) : default(BlockViewRef);
                var moving = _movePool.Has(e) && _movePool.Get(e).MoveType != BlockMoveType.None;
                if (view.Transform == null || moving)
                {
                    _finished.Add(e);
                    continue;
                }

                nudge.Time += dt;
                var t = Mathf.Clamp01(nudge.Time / nudge.Duration);
                var punch = 1f - Mathf.Abs(2f * t - 1f);
                punch = punch * punch * (3f - 2f * punch);
                view.Transform.position = view.OriginPosition + nudge.Offset * punch;
                dense[idx] = nudge;
                if (t >= 1f)
                {
                    view.Transform.position = view.OriginPosition;
                    _finished.Add(e);
                }
            }

            for (var i = 0; i < _finished.Count; i++)
            {
                _nudgePool.Del(_finished[i]);
            }
        }
    }
}
