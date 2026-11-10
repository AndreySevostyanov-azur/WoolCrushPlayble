using System.Collections.Generic;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    public sealed class BlockMechanicsState
    {
        private readonly EcsWorld _world;
        private readonly EcsPool<BlockFrozen> _frozenPool;
        private readonly EcsPool<BlockChained> _chainedPool;
        private readonly EcsPool<BlockChainBlocker> _blockerPool;
        private readonly EcsPool<ChainLink> _linkPool;
        private readonly EcsPool<BlockNudgeEvent> _nudgePool;
        private readonly EcsPool<BlockStrikeEvent> _strikePool;
        private readonly EcsFilter _linkFilter;
        private readonly EcsPool<BlockHidden> _hiddenPool;
        private readonly EcsFilter _hiddenFilter;

        public BlockMechanicsState(EcsWorld world)
        {
            _world = world;
            _frozenPool = world.GetPool<BlockFrozen>();
            _chainedPool = world.GetPool<BlockChained>();
            _blockerPool = world.GetPool<BlockChainBlocker>();
            _linkPool = world.GetPool<ChainLink>();
            _nudgePool = world.GetPool<BlockNudgeEvent>();
            _strikePool = world.GetPool<BlockStrikeEvent>();
            _linkFilter = world.Filter<ChainLink>().End();
            _hiddenPool = world.GetPool<BlockHidden>();
            _hiddenFilter = world.Filter<BlockHidden>().End();
        }

        public BlockMechanicsConfig Config { get; set; }
        public bool HasAny { get; set; }
        public Dictionary<DragonColorBlock, int> EntityByBlock { get; } = new Dictionary<DragonColorBlock, int>();

        public bool IsFrozen(int entity)
        {
            return _frozenPool.Has(entity);
        }

        public int ActiveLinks(int entity)
        {
            if (!_chainedPool.Has(entity))
            {
                return 0;
            }

            return _chainedPool.Get(entity).ActiveLinks;
        }

        public bool IsChainBlocker(int entity)
        {
            return _blockerPool.Has(entity);
        }

        public int EntityOf(DragonColorBlock block)
        {
            if (block != null && EntityByBlock.TryGetValue(block, out var entity))
            {
                return entity;
            }

            return -1;
        }

        public void RaiseNudge(int entity, bool showMarker = false)
        {
            if (entity < 0)
            {
                return;
            }

            var ev = _world.NewEntity();
            _nudgePool.Add(ev);
            EcsUtils.SetComponent(_nudgePool, ev, new BlockNudgeEvent { BlockEntity = entity, ShowMarker = showMarker });
        }

        public void RaiseStrike(int entity)
        {
            if (entity < 0 || !_frozenPool.Has(entity))
            {
                return;
            }

            var ev = _world.NewEntity();
            _strikePool.Add(ev);
            EcsUtils.SetComponent(_strikePool, ev, new BlockStrikeEvent { BlockEntity = entity });
        }

        public void TickHidden(float dt)
        {
            var count = _hiddenFilter.GetEntitiesCount();
            if (count == 0)
            {
                return;
            }

            var entities = _hiddenFilter.GetRawEntities();
            var dense = _hiddenPool.GetRawDenseItems();
            var sparse = _hiddenPool.GetRawSparseItems();
            for (var i = 0; i < count; i++)
            {
                var e = entities[i];
                if (e < 0 || e >= sparse.Length || sparse[e] <= 0)
                {
                    continue;
                }

                var idx = sparse[e];
                var hidden = dense[idx];
                if (hidden.Phase == HiddenPhase.Finished || hidden.Material == null)
                {
                    continue;
                }

                StepHidden(ref hidden, dt);
                dense[idx] = hidden;
            }
        }

        private static void StepHidden(ref BlockHidden hidden, float dt)
        {
            switch (hidden.Phase)
            {
                case HiddenPhase.None:
                    if (hidden.PulseCounter > 0)
                    {
                        hidden.SubTimer = 0f;
                        hidden.Phase = HiddenPhase.PulseUp;
                        ApplyHidden(hidden.Material, 0f, 1f);
                    }
                    else
                    {
                        StartSolid(ref hidden);
                    }

                    break;

                case HiddenPhase.PulseUp:
                    hidden.SubTimer += dt;
                    ApplyHidden(hidden.Material, Mathf.Clamp01(hidden.SubTimer / hidden.HalfPulse), 1f);
                    if (hidden.SubTimer >= hidden.HalfPulse)
                    {
                        if (hidden.PulseCounter > 1)
                        {
                            hidden.Phase = HiddenPhase.PulseDown;
                            hidden.SubTimer = 0f;
                        }
                        else
                        {
                            hidden.PulseCounter = 0;
                            StartSolid(ref hidden);
                        }
                    }

                    break;

                case HiddenPhase.PulseDown:
                    hidden.SubTimer += dt;
                    ApplyHidden(hidden.Material, Mathf.Clamp01(1f - hidden.SubTimer / hidden.HalfPulse), 1f);
                    if (hidden.SubTimer >= hidden.HalfPulse)
                    {
                        hidden.PulseCounter--;
                        hidden.Phase = HiddenPhase.PulseUp;
                        hidden.SubTimer = 0f;
                        ApplyHidden(hidden.Material, 0f, 1f);
                    }

                    break;

                case HiddenPhase.Solid:
                    if (hidden.SolidDuration <= 0f || hidden.SubTimer >= hidden.SolidDuration)
                    {
                        ApplyHidden(hidden.Material, 1f, 0f);
                        hidden.Phase = HiddenPhase.Finished;
                    }
                    else
                    {
                        hidden.SubTimer += dt;
                        ApplyHidden(hidden.Material, 1f, Mathf.Clamp01(1f - hidden.SubTimer / hidden.SolidDuration));
                    }

                    break;
            }
        }

        private static void StartSolid(ref BlockHidden hidden)
        {
            hidden.SubTimer = 0f;
            hidden.Phase = HiddenPhase.Solid;
            ApplyHidden(hidden.Material, 1f, 1f);
        }

        private static void ApplyHidden(Material material, float alpha, float rainbow)
        {
            material.SetFloat(BlockMechanicViews.AlphaId, alpha);
            material.SetFloat(BlockMechanicViews.RainbowAlphaId, rainbow);
        }

        public void RaiseNudgeChainGroup(int target)
        {
            RaiseNudge(target, true);

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
                if (link.Target.Unpack(_world, out var linkTarget) && linkTarget == target
                    && link.Blocker.Unpack(_world, out var linkBlocker))
                {
                    RaiseNudge(linkBlocker);
                }
            }
        }
    }
}
