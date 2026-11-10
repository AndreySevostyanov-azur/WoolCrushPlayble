using System.Collections.Generic;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using Playeble.Scripts.Gameplay.Movements;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    public sealed class BlockChainSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly GameContext _ctx;
        private EcsWorld _world;
        private EcsFilter _linkFilter;
        private EcsFilter _chainedFilter;
        private EcsFilter _blockerFilter;
        private EcsPool<ChainLink> _linkPool;
        private EcsPool<BlockChained> _chainedPool;
        private EcsPool<BlockChainBlocker> _blockerPool;
        private EcsPool<BlockViewRef> _viewPool;
        private EcsPool<BlockMoveData> _movePool;
        private readonly List<int> _brokenLinks = new List<int>(8);
        private readonly List<int> _freedTargets = new List<int>(8);
        private readonly List<int> _freedBlockers = new List<int>(8);

        public BlockChainSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _linkPool = _world.GetPool<ChainLink>();
            _chainedPool = _world.GetPool<BlockChained>();
            _blockerPool = _world.GetPool<BlockChainBlocker>();
            _viewPool = _world.GetPool<BlockViewRef>();
            _movePool = _world.GetPool<BlockMoveData>();
            _linkFilter = _world.Filter<ChainLink>().End();
            _chainedFilter = _world.Filter<BlockChained>().End();
            _blockerFilter = _world.Filter<BlockChainBlocker>().End();

            Refresh();
        }

        public void Run(IEcsSystems systems)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_linkFilter.GetEntitiesCount() == 0)
            {
                return;
            }

            var config = _ctx != null && _ctx.Mechanics != null ? _ctx.Mechanics.Config : null;
            var camera = _ctx != null && _ctx.WorldCamera != null ? _ctx.WorldCamera : Camera.main;

            _brokenLinks.Clear();
            ProcessLinks(config, camera);
            ReleaseBroken(config);
            UpdateAnchors(config, camera);
        }

        private void ProcessLinks(BlockMechanicsConfig config, Camera camera)
        {
            var entities = _linkFilter.GetRawEntities();
            var count = _linkFilter.GetEntitiesCount();
            var dense = _linkPool.GetRawDenseItems();
            var sparse = _linkPool.GetRawSparseItems();
            for (var i = 0; i < count; i++)
            {
                var e = entities[i];
                if (e < 0 || e >= sparse.Length || sparse[e] <= 0)
                {
                    continue;
                }

                var link = dense[sparse[e]];
                var hasTarget = link.Target.Unpack(_world, out var target);
                var hasBlocker = link.Blocker.Unpack(_world, out var blocker);
                if (!hasTarget || !hasBlocker || IsLeaving(blocker))
                {
                    _brokenLinks.Add(e);
                    continue;
                }

                if (link.Visual != null)
                {
                    link.Visual.Update(AnchorOf(target, config), AnchorOf(blocker, config), camera);
                }
            }
        }

        private bool IsLeaving(int block)
        {
            if (!_movePool.Has(block))
            {
                return true;
            }

            return _movePool.Get(block).MoveType == BlockMoveType.ToSlot;
        }

        private void ReleaseBroken(BlockMechanicsConfig config)
        {
            _freedTargets.Clear();
            _freedBlockers.Clear();
            for (var i = 0; i < _brokenLinks.Count; i++)
            {
                var linkEntity = _brokenLinks[i];
                var link = _linkPool.Get(linkEntity);
                if (link.Visual != null)
                {
                    link.Visual.Destroy();
                }

                if (link.Target.Unpack(_world, out var target) && _chainedPool.Has(target))
                {
                    var chained = _chainedPool.Get(target);
                    chained.ActiveLinks--;
                    EcsUtils.SetComponent(_chainedPool, target, chained);
                    if (chained.ActiveLinks <= 0)
                    {
                        _freedTargets.Add(target);
                    }
                }

                if (link.Blocker.Unpack(_world, out var blocker) && _blockerPool.Has(blocker))
                {
                    var pin = _blockerPool.Get(blocker);
                    pin.ActiveLinks--;
                    EcsUtils.SetComponent(_blockerPool, blocker, pin);
                    if (pin.ActiveLinks <= 0)
                    {
                        _freedBlockers.Add(blocker);
                    }

                    if (config != null && pin.Pin != null)
                    {
                        BlockMechanicViews.SpawnVfx(config.ChainBreakVfx, pin.Pin.transform.position, config.VfxLifetime);
                    }
                }

                _world.DelEntity(linkEntity);
            }

            for (var i = 0; i < _freedTargets.Count; i++)
            {
                var target = _freedTargets[i];
                var chained = _chainedPool.Get(target);
                if (chained.Lock != null)
                {
                    if (config != null)
                    {
                        BlockMechanicViews.SpawnVfx(config.LockBreakVfx, chained.Lock.transform.position, config.VfxLifetime);
                    }

                    Object.Destroy(chained.Lock.gameObject);
                }

                _chainedPool.Del(target);
            }

            for (var i = 0; i < _freedBlockers.Count; i++)
            {
                var blocker = _freedBlockers[i];
                var pin = _blockerPool.Get(blocker);
                if (pin.Pin != null)
                {
                    Object.Destroy(pin.Pin.gameObject);
                }

                _blockerPool.Del(blocker);
            }
        }

        private void UpdateAnchors(BlockMechanicsConfig config, Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            var rotation = camera.transform.rotation;
            var chainedEntities = _chainedFilter.GetRawEntities();
            var chainedCount = _chainedFilter.GetEntitiesCount();
            var chainedDense = _chainedPool.GetRawDenseItems();
            var chainedSparse = _chainedPool.GetRawSparseItems();
            for (var i = 0; i < chainedCount; i++)
            {
                var e = chainedEntities[i];
                if (e < 0 || e >= chainedSparse.Length || chainedSparse[e] <= 0)
                {
                    continue;
                }

                var lockView = chainedDense[chainedSparse[e]].Lock;
                if (lockView != null)
                {
                    lockView.transform.SetPositionAndRotation(AnchorOf(e, config), rotation);
                }
            }

            var blockerEntities = _blockerFilter.GetRawEntities();
            var blockerCount = _blockerFilter.GetEntitiesCount();
            var blockerDense = _blockerPool.GetRawDenseItems();
            var blockerSparse = _blockerPool.GetRawSparseItems();
            for (var i = 0; i < blockerCount; i++)
            {
                var e = blockerEntities[i];
                if (e < 0 || e >= blockerSparse.Length || blockerSparse[e] <= 0)
                {
                    continue;
                }

                var pinView = blockerDense[blockerSparse[e]].Pin;
                if (pinView != null)
                {
                    pinView.transform.SetPositionAndRotation(AnchorOf(e, config), rotation);
                }
            }
        }

        private Vector3 AnchorOf(int block, BlockMechanicsConfig config)
        {
            var transform = _viewPool.Has(block) ? _viewPool.Get(block).Transform : null;
            return transform != null ? BlockMechanicViews.AnchorOf(transform, config) : Vector3.zero;
        }
    }
}
