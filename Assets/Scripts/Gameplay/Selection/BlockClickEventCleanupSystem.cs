using System.Collections.Generic;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;

namespace Playeble.Scripts.Gameplay.Selection
{
    public sealed class BlockClickEventCleanupSystem : IEcsInitSystem, IEcsRunSystem
    {
        // Keeping ctor compatible with GameBootstrap injection.
        public BlockClickEventCleanupSystem(GameContext _)
        {
        }

        public BlockClickEventCleanupSystem()
        {
        }

        private EcsWorld _world;
        private EcsFilter _eventFilter;
        private readonly List<int> _events = new List<int>(32);

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _eventFilter = _world.Filter<BlockClickedEvent>().End();
        }

        public void Run(IEcsSystems systems)
        {
            _events.Clear();
            var entities = _eventFilter.GetRawEntities();
            var count = _eventFilter.GetEntitiesCount();
            for (var i = 0; i < count; i++)
            {
                var e = entities[i];
                _events.Add(e);
            }

            for (var i = 0; i < _events.Count; i++)
            {
                _world.DelEntity(_events[i]);
            }
        }
    }
}

