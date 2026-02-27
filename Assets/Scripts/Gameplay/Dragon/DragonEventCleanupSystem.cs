using System.Collections.Generic;
using Leopotam.EcsLite;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public sealed class DragonEventCleanupSystem : IEcsInitSystem, IEcsRunSystem
    {
        // Keeping ctor compatible with GameBootstrap injection.
        public DragonEventCleanupSystem(GameContext _)
        {
        }

        public DragonEventCleanupSystem()
        {
        }

        private EcsWorld _world;
        private EcsFilter _eventFilter;
        private readonly List<int> _events = new List<int>(16);

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _eventFilter = _world.Filter<DragonReachedEndEvent>().End();
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

