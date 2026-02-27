using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Movements
{
    public sealed class ComputeGameBordersInitSystem : IEcsInitSystem
    {
        private readonly GameContext _ctx;

        public ComputeGameBordersInitSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void Init(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var blocks = _ctx != null ? _ctx.Blocks : null;
            if (blocks == null || blocks.Length == 0)
            {
                Debug.LogWarning($"{nameof(ComputeGameBordersInitSystem)}: no blocks provided.");
                return;
            }

            var hasAny = false;
            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (var i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];
                if (block == null)
                {
                    continue;
                }

                var col = block.GetComponentInChildren<Collider>(true);
                if (col == null)
                {
                    Debug.LogWarning($"{nameof(ComputeGameBordersInitSystem)}: missing Collider on block '{block.name}'.");
                    continue;
                }

                var b = col.bounds;
                min = Vector3.Min(min, b.min);
                max = Vector3.Max(max, b.max);
                hasAny = true;
            }

            if (!hasAny)
            {
                Debug.LogWarning($"{nameof(ComputeGameBordersInitSystem)}: no valid blocks with Collider found.");
                return;
            }

            var offset = _ctx != null ? Mathf.Max(0f, _ctx.FieldBordersOffset) : 0f;
            if (offset > 0f)
            {
                var off = Vector3.one * offset;
                min -= off;
                max += off;
            }

            var entity = world.NewEntity();
            var pool = world.GetPool<GameBordersComponent>();
            pool.Add(entity);
            var dense = pool.GetRawDenseItems();
            var sparse = pool.GetRawSparseItems();
            if (entity >= 0 && entity < sparse.Length)
            {
                var idx = sparse[entity];
                if (idx > 0)
                {
                    var borders = dense[idx];
                    borders.Center = (min + max) * 0.5f;
                    borders.Extents = (max - min) * 0.5f;
                    dense[idx] = borders;
                }
            }
        }
    }
}

