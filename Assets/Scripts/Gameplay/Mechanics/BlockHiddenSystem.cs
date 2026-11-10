using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    public sealed class BlockHiddenSystem : IEcsRunSystem
    {
        private readonly GameContext _ctx;

        public BlockHiddenSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void Run(IEcsSystems systems)
        {
            if (_ctx != null && _ctx.Mechanics != null)
            {
                _ctx.Mechanics.TickHidden(Time.deltaTime);
            }
        }
    }
}
