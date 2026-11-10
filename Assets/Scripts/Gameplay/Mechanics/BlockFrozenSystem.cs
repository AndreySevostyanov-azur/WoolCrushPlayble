using System.Collections.Generic;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    public sealed class BlockFrozenSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly GameContext _ctx;
        private EcsWorld _world;
        private EcsFilter _strikeFilter;
        private EcsFilter _meltFilter;
        private EcsPool<BlockStrikeEvent> _strikePool;
        private EcsPool<BlockFrozen> _frozenPool;
        private EcsPool<BlockMelt> _meltPool;
        private readonly List<int> _events = new List<int>(8);
        private readonly List<int> _finished = new List<int>(8);

        public BlockFrozenSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _strikePool = _world.GetPool<BlockStrikeEvent>();
            _frozenPool = _world.GetPool<BlockFrozen>();
            _meltPool = _world.GetPool<BlockMelt>();
            _strikeFilter = _world.Filter<BlockStrikeEvent>().End();
            _meltFilter = _world.Filter<BlockMelt>().End();
        }

        public void Run(IEcsSystems systems)
        {
            var config = _ctx != null && _ctx.Mechanics != null ? _ctx.Mechanics.Config : null;
            ConsumeStrikes(config);
            TickMelts(Time.deltaTime);
        }

        private void ConsumeStrikes(BlockMechanicsConfig config)
        {
            var count = _strikeFilter.GetEntitiesCount();
            if (count == 0)
            {
                return;
            }

            _events.Clear();
            var entities = _strikeFilter.GetRawEntities();
            var strikeDense = _strikePool.GetRawDenseItems();
            var strikeSparse = _strikePool.GetRawSparseItems();
            for (var i = 0; i < count; i++)
            {
                var ev = entities[i];
                _events.Add(ev);
                if (ev < 0 || ev >= strikeSparse.Length || strikeSparse[ev] <= 0)
                {
                    continue;
                }

                Melt(strikeDense[strikeSparse[ev]].BlockEntity, config);
            }

            for (var i = 0; i < _events.Count; i++)
            {
                _world.DelEntity(_events[i]);
            }
        }

        private void Melt(int block, BlockMechanicsConfig config)
        {
            if (block < 0 || !_frozenPool.Has(block))
            {
                return;
            }

            var frozen = _frozenPool.Get(block);
            _frozenPool.Del(block);

            if (config != null)
            {
                var point = frozen.VfxPoint != null
                    ? frozen.VfxPoint.position
                    : frozen.Ice != null ? frozen.Ice.bounds.center : Vector3.zero;
                BlockMechanicViews.SpawnVfx(config.UnfreezeVfx, point, config.VfxLifetime);
            }

            if (frozen.Ice == null)
            {
                return;
            }

            _meltPool.Add(block);
            EcsUtils.SetComponent(_meltPool, block, new BlockMelt
            {
                View = frozen.Ice.gameObject,
                Material = frozen.Ice.material,
                Elapsed = 0f,
                Duration = config != null ? config.MeltDuration : 0.5f,
            });
        }

        private void TickMelts(float dt)
        {
            var count = _meltFilter.GetEntitiesCount();
            if (count == 0)
            {
                return;
            }

            _finished.Clear();
            var entities = _meltFilter.GetRawEntities();
            var dense = _meltPool.GetRawDenseItems();
            var sparse = _meltPool.GetRawSparseItems();
            for (var i = 0; i < count; i++)
            {
                var e = entities[i];
                if (e < 0 || e >= sparse.Length || sparse[e] <= 0)
                {
                    continue;
                }

                var idx = sparse[e];
                var melt = dense[idx];
                melt.Elapsed += dt;
                var alpha = Mathf.Max(0f, 1f - melt.Elapsed / melt.Duration);
                if (melt.Material != null)
                {
                    melt.Material.SetFloat(BlockMechanicViews.AlphaId, alpha);
                }

                dense[idx] = melt;
                if (melt.Elapsed >= melt.Duration)
                {
                    _finished.Add(e);
                }
            }

            for (var i = 0; i < _finished.Count; i++)
            {
                var e = _finished[i];
                var view = _meltPool.Get(e).View;
                if (view != null)
                {
                    Object.Destroy(view);
                }

                _meltPool.Del(e);
            }
        }
    }
}
