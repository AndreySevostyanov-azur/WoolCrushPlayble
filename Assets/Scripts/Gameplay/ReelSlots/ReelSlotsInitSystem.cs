using AzurGames.Wool.Gameplay;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.ReelSlots
{
    public sealed class ReelSlotsInitSystem : IEcsInitSystem
    {
        private readonly GameContext _ctx;

        public ReelSlotsInitSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void Init(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            if (_ctx == null || _ctx.ReelSlots == null || _ctx.ReelSlots.Length == 0)
            {
                return;
            }

            var viewPool = world.GetPool<ReelSlotViewRef>();
            var statePool = world.GetPool<ReelSlotState>();
            var viewDense = viewPool.GetRawDenseItems();
            var viewSparse = viewPool.GetRawSparseItems();
            var stateDense = statePool.GetRawDenseItems();
            var stateSparse = statePool.GetRawSparseItems();

            for (var i = 0; i < _ctx.ReelSlots.Length; i++)
            {
                var view = _ctx.ReelSlots[i];
                if (view == null)
                {
                    continue;
                }

                var e = world.NewEntity();

                viewPool.Add(e);
                statePool.Add(e);

                var viewIdx = (e >= 0 && e < viewSparse.Length) ? viewSparse[e] : 0;
                if (viewIdx > 0)
                {
                    var viewRef = viewDense[viewIdx];
                    viewRef.View = view;
                    viewRef.Index = i;
                    viewDense[viewIdx] = viewRef;
                }

                var stateIdx = (e >= 0 && e < stateSparse.Length) ? stateSparse[e] : 0;
                if (stateIdx > 0)
                {
                    var state = stateDense[stateIdx];
                    state.Status = SlotStates.Empty;
                    stateDense[stateIdx] = state;
                }

                // Reset UI to empty state.
                view.Refresh(SlotStates.Empty);
                view.ShowSpool(false);
                view.RemainReelText.Clear();
                view.Rope.Show(false);
            }
        }
    }
}

