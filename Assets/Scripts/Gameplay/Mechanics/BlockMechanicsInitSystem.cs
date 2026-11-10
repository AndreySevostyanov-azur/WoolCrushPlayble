using System.Collections.Generic;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using Playeble.Scripts.Gameplay.Movements;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    public sealed class BlockMechanicsInitSystem : IEcsInitSystem
    {
        private readonly GameContext _ctx;
        private EcsWorld _world;
        private BlockMechanicsState _state;
        private Transform _viewsRoot;
        private EcsPool<BlockHidden> _hiddenPool;
        private EcsPool<BlockFrozen> _frozenPool;
        private EcsPool<BlockChained> _chainedPool;
        private EcsPool<BlockChainBlocker> _blockerPool;
        private EcsPool<ChainLink> _linkPool;

        public BlockMechanicsInitSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _state = new BlockMechanicsState(_world);
            if (_ctx != null)
            {
                _ctx.Mechanics = _state;
            }

            _hiddenPool = _world.GetPool<BlockHidden>();
            _frozenPool = _world.GetPool<BlockFrozen>();
            _chainedPool = _world.GetPool<BlockChained>();
            _blockerPool = _world.GetPool<BlockChainBlocker>();
            _linkPool = _world.GetPool<ChainLink>();

            var viewPool = _world.GetPool<BlockViewRef>();
            var filter = _world.Filter<BlockViewRef>().End();
            var count = filter.GetEntitiesCount();
            var entities = new int[count];
            for (var i = 0; i < count; i++)
            {
                entities[i] = filter.GetRawEntities()[i];
            }

            for (var i = 0; i < count; i++)
            {
                var block = viewPool.Get(entities[i]).Block;
                if (block != null)
                {
                    _state.EntityByBlock[block] = entities[i];
                }
            }

            for (var i = 0; i < count; i++)
            {
                var block = viewPool.Get(entities[i]).Block;
                if (block == null)
                {
                    continue;
                }

                SetupHidden(entities[i], block);
                SetupFrozen(entities[i], block);
                SetupChained(entities[i], block);
            }
        }

        private void Adopt(BlockMechanicsConfig config)
        {
            _state.HasAny = true;
            if (_state.Config == null)
            {
                _state.Config = config;
            }
        }

        private void SetupHidden(int entity, DragonColorBlock block)
        {
            var hidden = block.GetComponent<HiddenBlock>();
            if (hidden == null)
            {
                return;
            }

            var config = hidden.Config;
            var overlay = hidden.Overlay;
            if (overlay != null && !BlockMechanicViews.IsOverlayOf(block, overlay))
            {
                Debug.LogWarning($"{nameof(HiddenBlock)} on '{block.name}': the overlay reference is not a child view of the block, it is ignored. Use 'Build View' on the component.", block);
                overlay = null;
            }

            if (overlay == null)
            {
                overlay = BlockMechanicViews.CreateHiddenOverlay(block, config);
            }

            if (config == null || overlay == null)
            {
                Debug.LogWarning($"{nameof(HiddenBlock)} on '{block.name}': config or hidden material/mesh is missing, block stays visible.", block);
                return;
            }

            overlay.gameObject.SetActive(true);
            var material = overlay.material;
            var data = new BlockHidden
            {
                Material = material,
                SubTimer = 0f,
                PulseCounter = config.HiddenPulseCount,
                HalfPulse = config.HiddenPulseDuration / (2 * config.HiddenPulseCount - 1),
                SolidDuration = config.HiddenSolidDuration,
                Phase = HiddenPhase.None,
            };

            if (hidden.Instant)
            {
                data.Phase = HiddenPhase.Finished;
                material.SetFloat(BlockMechanicViews.AlphaId, 1f);
                material.SetFloat(BlockMechanicViews.RainbowAlphaId, 0f);
            }
            else
            {
                material.SetFloat(BlockMechanicViews.AlphaId, 0f);
                material.SetFloat(BlockMechanicViews.RainbowAlphaId, 1f);
            }

            _hiddenPool.Add(entity);
            EcsUtils.SetComponent(_hiddenPool, entity, data);
            Adopt(config);
        }

        private void SetupFrozen(int entity, DragonColorBlock block)
        {
            var frozen = block.GetComponent<FrozenBlock>();
            if (frozen == null)
            {
                return;
            }

            var config = frozen.Config;
            var ice = frozen.Ice;
            if (ice != null && !BlockMechanicViews.IsOverlayOf(block, ice))
            {
                Debug.LogWarning($"{nameof(FrozenBlock)} on '{block.name}': the ice reference is not a child view of the block (it was probably the block's own renderer), it is ignored. Use 'Build View' on the component.", block);
                ice = null;
            }

            if (ice == null)
            {
                ice = BlockMechanicViews.CreateIceOverlay(block, config);
            }

            if (ice == null)
            {
                Debug.LogWarning($"{nameof(FrozenBlock)} on '{block.name}': ice view is missing (config, ice material or mesh for this tile), the block is frozen without a visual.", block);
            }
            else
            {
                ice.gameObject.SetActive(true);
            }

            _frozenPool.Add(entity);
            EcsUtils.SetComponent(_frozenPool, entity, new BlockFrozen { Ice = ice, VfxPoint = frozen.VfxPoint });
            Adopt(config);
        }

        private void SetupChained(int entity, DragonColorBlock block)
        {
            var chained = block.GetComponent<ChainedBlock>();
            if (chained == null)
            {
                return;
            }

            var config = chained.Config;
            var seen = new HashSet<DragonColorBlock>();
            var blockers = chained.Blockers;
            for (var i = 0; i < blockers.Count; i++)
            {
                var blocker = blockers[i];
                var blockerEntity = _state.EntityOf(blocker);
                if (blocker == null || blocker == block || blockerEntity < 0 || !seen.Add(blocker))
                {
                    Debug.LogWarning($"{nameof(ChainedBlock)} on '{block.name}': blockers[{i}] is empty, itself, duplicated or not in GameBootstrap blocks and is ignored.", block);
                    continue;
                }

                AddLink(entity, block, blockerEntity, blocker, config);
            }

            if (_chainedPool.Has(entity))
            {
                Adopt(config);
            }
        }

        private void AddLink(int target, DragonColorBlock targetBlock, int blocker, DragonColorBlock blockerBlock, BlockMechanicsConfig config)
        {
            EnsureViewsRoot();

            if (!_chainedPool.Has(target))
            {
                _chainedPool.Add(target);
                var lockSprite = CreateSprite("Lock", config != null ? config.LockSprite : null, config, config != null ? config.LockScale : 1f);
                EcsUtils.SetComponent(_chainedPool, target, new BlockChained { ActiveLinks = 0, Lock = lockSprite });
            }

            if (!_blockerPool.Has(blocker))
            {
                _blockerPool.Add(blocker);
                var pinSprite = CreateSprite("Pin", config != null ? config.PinSprite : null, config, config != null ? config.PinScale : 1f);
                EcsUtils.SetComponent(_blockerPool, blocker, new BlockChainBlocker { ActiveLinks = 0, Pin = pinSprite });
            }

            var chained = _chainedPool.Get(target);
            chained.ActiveLinks++;
            EcsUtils.SetComponent(_chainedPool, target, chained);

            var pin = _blockerPool.Get(blocker);
            pin.ActiveLinks++;
            EcsUtils.SetComponent(_blockerPool, blocker, pin);

            var link = _world.NewEntity();
            _linkPool.Add(link);
            EcsUtils.SetComponent(_linkPool, link, new ChainLink
            {
                Target = _world.PackEntity(target),
                Blocker = _world.PackEntity(blocker),
                Visual = config != null && config.ChainSprite != null ? new ChainVisual(config, _viewsRoot) : null,
            });
        }

        private SpriteRenderer CreateSprite(string name, Sprite sprite, BlockMechanicsConfig config, float scale)
        {
            if (sprite == null || config == null)
            {
                return null;
            }

            return BlockMechanicViews.CreateSprite(_viewsRoot, name, sprite, config.SpriteMaterial, scale, config.SortingOrder + 1);
        }

        private void EnsureViewsRoot()
        {
            if (_viewsRoot == null)
            {
                _viewsRoot = new GameObject("BlockMechanicsViews").transform;
            }
        }
    }
}
