using Leopotam.EcsLite;
using Luna.Unity;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public sealed class DragonGameEndSystem : IEcsInitSystem, IEcsRunSystem
    {
        // Keeping ctor compatible with GameBootstrap injection.
        public DragonGameEndSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public DragonGameEndSystem()
        {
        }

        private EcsWorld _world;
        private EcsFilter _reachedEndEventFilter;
        private EcsFilter _partsFilter;
        private EcsFilter _spawnProgressFilter;
        private EcsPool<DragonPart> _partPool;
        private EcsPool<DragonSpawnProgress> _progressPool;
        private readonly GameContext _ctx;
        private bool _gameEndedSent;

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _reachedEndEventFilter = _world.Filter<DragonReachedEndEvent>().End();
            _partsFilter = _world.Filter<DragonPart>().End();
            _spawnProgressFilter = _world.Filter<DragonSpawnProgress>().End();
            _partPool = _world.GetPool<DragonPart>();
            _progressPool = _world.GetPool<DragonSpawnProgress>();
        }

        public void Run(IEcsSystems systems)
        {
            var hasReachedEndEvent = _reachedEndEventFilter.GetEntitiesCount() > 0;
            if (!_gameEndedSent && (hasReachedEndEvent || HasAllBodyPartsUnwound()))
            {
                _gameEndedSent = true;
                LifeCycle.GameEnded();
                _ctx.RiseGameEndAction();
            }
        }

        private bool HasAllBodyPartsUnwound()
        {
            if (_ctx == null || _ctx.DragonBodySegmentsCount <= 0)
            {
                return false;
            }

            if (!HasSpawnedAllBodyAndTail())
            {
                return false;
            }

            return !HasAnyBodyPart();
        }

        private bool HasSpawnedAllBodyAndTail()
        {
            var expectedBodyCount = _ctx.DragonBodySegmentsCount;
            var entities = _spawnProgressFilter.GetRawEntities();
            var count = _spawnProgressFilter.GetEntitiesCount();
            var progressDense = _progressPool.GetRawDenseItems();
            var progressSparse = _progressPool.GetRawSparseItems();

            for (var i = 0; i < count; i++)
            {
                var e = entities[i];
                if (e < 0 || e >= progressSparse.Length)
                {
                    continue;
                }

                var idx = progressSparse[e];
                if (idx <= 0)
                {
                    continue;
                }

                var progress = progressDense[idx];
                if (progress.TailSpawned && progress.BodySpawned >= expectedBodyCount)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAnyBodyPart()
        {
            var entities = _partsFilter.GetRawEntities();
            var count = _partsFilter.GetEntitiesCount();
            var partDense = _partPool.GetRawDenseItems();
            var partSparse = _partPool.GetRawSparseItems();
            for (var i = 0; i < count; i++)
            {
                var e = entities[i];
                if (e < 0 || e >= partSparse.Length)
                {
                    continue;
                }

                var idx = partSparse[e];
                if (idx <= 0)
                {
                    continue;
                }

                if (partDense[idx].Kind == DragonPartKind.Body)
                {
                    return true;
                }
            }

            return false;
        }
    }
}