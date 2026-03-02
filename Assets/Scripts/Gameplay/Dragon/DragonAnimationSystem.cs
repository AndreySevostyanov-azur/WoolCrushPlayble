using Leopotam.EcsLite;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public class DragonAnimationSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsWorld _world;
        private EcsPool<DragonBreathComponent> _dragonBreathPool;
        private EcsPool<DragonHeadComponent> _dragonHeadPool;
        private EcsFilter _dragonHeadFilter;
        private EcsFilter _reachedFilter;
        
        private int _dragonBreathEntity;
        private GameContext _gameContext;

        public DragonAnimationSystem(GameContext gameContext)
        {
            _gameContext = gameContext;
        }
        
        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _dragonBreathPool = _world.GetPool<DragonBreathComponent>();
            _dragonHeadPool = _world.GetPool<DragonHeadComponent>();

            _dragonHeadFilter = _world.Filter<DragonHeadComponent>().End();
            _reachedFilter = _world.Filter<DragonReachedEndEvent>().End();
            
            _dragonBreathEntity = _world.NewEntity();
            _dragonBreathPool.Add(_dragonBreathEntity);
            
            
        }

        public void Run(IEcsSystems systems)
        {
            var dragonHeadEntities = _dragonHeadFilter.GetRawEntities();
            var dragonHeadEntitiesCount = _dragonHeadFilter.GetEntitiesCount();

            for (var i = 0; i < dragonHeadEntitiesCount; i++)
            {
                var entity = dragonHeadEntities[i];
                var breath = _dragonBreathPool.Get(_dragonBreathEntity);
                EcsUtils.SetComponent(_dragonBreathPool, _dragonBreathEntity, new DragonBreathComponent
                {
                    State = breath.State,
                    Time = breath.Time + Time.deltaTime,
                });

                if (_reachedFilter.GetEntitiesCount() > 0)
                {
                    var dragonHead = _dragonHeadPool.Get(entity);
                    dragonHead.DragonFireEffect.SetActive(true);
                    dragonHead.DragonHeadAnimator.SetBool("Fire_On", true);
                    EcsUtils.SetComponent(_dragonBreathPool, _dragonBreathEntity, new DragonBreathComponent
                    {
                        State = DragonBreathState.Breathing,
                        Time = 0,
                    });
                    return;
                }
                
                if (breath.State == DragonBreathState.Idle &&
                    breath.Time > _gameContext.DragonBreathConfig.BreathInterval)
                {
                    var dragonHead = _dragonHeadPool.Get(entity);
                    dragonHead.DragonFireEffect.SetActive(true);
                    dragonHead.DragonHeadAnimator.SetBool("Fire_On", true);
                    EcsUtils.SetComponent(_dragonBreathPool, _dragonBreathEntity, new DragonBreathComponent
                    {
                        State = DragonBreathState.Breathing,
                        Time = 0,
                    });
                }
                
                if (breath.State == DragonBreathState.Breathing &&
                          breath.Time > _gameContext.DragonBreathConfig.BreathDuration)
                {
                    var dragonHead = _dragonHeadPool.Get(entity);
                    dragonHead.DragonFireEffect.SetActive(false);
                    dragonHead.DragonHeadAnimator.SetBool("Fire_On", false);
                    EcsUtils.SetComponent(_dragonBreathPool, _dragonBreathEntity, new DragonBreathComponent
                    {
                        State = DragonBreathState.Idle,
                        Time = 0,
                    });
                }
            }
        }
    }
}