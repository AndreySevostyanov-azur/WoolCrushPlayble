using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Movements
{
    public sealed class BlocksInitSystem : IEcsInitSystem
    {
        private readonly GameContext _ctx;

        public BlocksInitSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void Init(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            if (_ctx == null || _ctx.Blocks == null || _ctx.Blocks.Length == 0)
            {
                return;
            }

            var viewPool = world.GetPool<BlockViewRef>();
            var movePool = world.GetPool<BlockMoveData>();
            var viewDense = viewPool.GetRawDenseItems();
            var viewSparse = viewPool.GetRawSparseItems();
            var moveDense = movePool.GetRawDenseItems();
            var moveSparse = movePool.GetRawSparseItems();

            for (var i = 0; i < _ctx.Blocks.Length; i++)
            {
                var block = _ctx.Blocks[i];
                if (block == null)
                {
                    continue;
                }

                var tr = block.transform;
                var col = block.GetComponentInChildren<Collider>(true);
                if (col == null)
                {
                }

                var e = world.NewEntity();
                AddComponent(viewPool, e);
                AddComponent(movePool, e);

                var viewIdx = (e >= 0 && e < viewSparse.Length) ? viewSparse[e] : 0;
                if (viewIdx > 0 && viewIdx < viewDense.Length)
                {
                    viewDense[viewIdx] = new BlockViewRef
                    {
                        Block = block,
                        Transform = tr,
                        Collider = col,
                        OriginPosition = tr.position
                    };
                }

                var moveIdx = (e >= 0 && e < moveSparse.Length) ? moveSparse[e] : 0;
                if (moveIdx > 0 && moveIdx < moveDense.Length)
                {
                    moveDense[moveIdx] = new BlockMoveData
                    {
                        MoveType = BlockMoveType.None,
                        TargetSlotIndex = -1,
                        PathIndex = 0,
                        PathCount = 0,
                        CachedBlockerPoint = Vector3.zero
                    };
                }
            }
        }

        private static void AddComponent<T>(EcsPool<T> pool, int entity) where T : struct
        {
            ((IEcsPool)pool).AddRaw(entity, default(T));
        }
    }
}

