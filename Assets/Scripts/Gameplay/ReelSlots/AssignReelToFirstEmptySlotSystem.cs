using AzurGames.Wool.Gameplay;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Movements;
using System.Collections.Generic;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.ReelSlots
{
    public sealed class AssignReelToFirstEmptySlotSystem : IEcsInitSystem, IEcsRunSystem
    {
        public AssignReelToFirstEmptySlotSystem(GameContext ctx)
        {
        }

        private EcsWorld _world;
        private EcsFilter _arrivedEvents;
        private EcsFilter _slots;
        private EcsPool<BlockArrivedToSlotEvent> _arrivedPool;
        private EcsPool<ReelSlotViewRef> _viewPool;
        private EcsPool<ReelSlotState> _statePool;
        private EcsPool<ReelSpoolData> _spoolPool;
        private EcsPool<ReelSpoolColor> _colorPool;
        private EcsPool<ReelSlotAssignedBlock> _assignedBlockPool;
        private readonly List<int> _eventsToDelete = new List<int>(16);

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _arrivedPool = _world.GetPool<BlockArrivedToSlotEvent>();
            _viewPool = _world.GetPool<ReelSlotViewRef>();
            _statePool = _world.GetPool<ReelSlotState>();
            _spoolPool = _world.GetPool<ReelSpoolData>();
            _colorPool = _world.GetPool<ReelSpoolColor>();
            _assignedBlockPool = _world.GetPool<ReelSlotAssignedBlock>();

            _arrivedEvents = _world.Filter<BlockArrivedToSlotEvent>().End();
            _slots = _world.Filter<ReelSlotViewRef>().Inc<ReelSlotState>().End();
        }

        public void Run(IEcsSystems systems)
        {
            _eventsToDelete.Clear();

            var eventEntities = _arrivedEvents.GetRawEntities();
            var eventCount = _arrivedEvents.GetEntitiesCount();
            var eventDense = _arrivedPool.GetRawDenseItems();
            var eventSparse = _arrivedPool.GetRawSparseItems();

            var stateDense = _statePool.GetRawDenseItems();
            var stateSparse = _statePool.GetRawSparseItems();
            var spoolDense = _spoolPool.GetRawDenseItems();
            var spoolSparse = _spoolPool.GetRawSparseItems();
            var colorDense = _colorPool.GetRawDenseItems();
            var colorSparse = _colorPool.GetRawSparseItems();
            var assignedSparse = _assignedBlockPool.GetRawSparseItems();

            for (var i = 0; i < eventCount; i++)
            {
                var evEntity = eventEntities[i];
                if (evEntity < 0 || evEntity >= eventSparse.Length)
                {
                    continue;
                }

                var evIdx = eventSparse[evEntity];
                if (evIdx <= 0)
                {
                    continue;
                }

                var arrived = eventDense[evIdx];
                _eventsToDelete.Add(evEntity);
                if (arrived.SlotIndex < 0)
                {
                    continue;
                }

                var slotEntity = FindSlotEntityByIndex(arrived.SlotIndex);
                if (slotEntity < 0)
                {
                    continue;
                }

                // Mark as occupied.
                if (slotEntity >= 0 && slotEntity < stateSparse.Length)
                {
                    var stateIdx = stateSparse[slotEntity];
                    if (stateIdx <= 0)
                    {
                        _statePool.Add(slotEntity);
                        stateIdx = stateSparse[slotEntity];
                    }

                    if (stateIdx > 0)
                    {
                        var state = stateDense[stateIdx];
                        state.Status = SlotStates.Occupied;
                        stateDense[stateIdx] = state;
                    }
                }

                // Apply spool data (size + turns).
                if (slotEntity >= 0 && slotEntity < spoolSparse.Length)
                {
                    var spoolIdx = spoolSparse[slotEntity];
                    if (spoolIdx <= 0)
                    {
                        _spoolPool.Add(slotEntity);
                        spoolIdx = spoolSparse[slotEntity];
                    }

                    if (spoolIdx > 0)
                    {
                        var spool = spoolDense[spoolIdx];
                        spool.Current = 0;
                        spool.Max = Mathf.Max(0, arrived.Turns);
                        spool.BoxType = arrived.BoxType;
                        spoolDense[spoolIdx] = spool;
                    }
                }

                // Apply color.
                if (slotEntity >= 0 && slotEntity < colorSparse.Length)
                {
                    var colorIdx = colorSparse[slotEntity];
                    if (colorIdx <= 0)
                    {
                        _colorPool.Add(slotEntity);
                        colorIdx = colorSparse[slotEntity];
                    }

                    if (colorIdx > 0)
                    {
                        var color = colorDense[colorIdx];
                        color.Color = arrived.Color;
                        color.ColorOffset = arrived.ColorOffset;
                        colorDense[colorIdx] = color;
                    }
                }

                // Reservation mapping is no longer needed once occupied.
                if (slotEntity >= 0 && slotEntity < assignedSparse.Length)
                {
                    _assignedBlockPool.Del(slotEntity);
                }
            }

            for (var i = 0; i < _eventsToDelete.Count; i++)
            {
                _world.DelEntity(_eventsToDelete[i]);
            }
        }

        private int FindSlotEntityByIndex(int slotIndex)
        {
            var slotEntities = _slots.GetRawEntities();
            var slotCount = _slots.GetEntitiesCount();
            var viewDense = _viewPool.GetRawDenseItems();
            var viewSparse = _viewPool.GetRawSparseItems();

            for (var i = 0; i < slotCount; i++)
            {
                var e = slotEntities[i];
                if (e < 0 || e >= viewSparse.Length)
                {
                    continue;
                }

                var viewIdx = viewSparse[e];
                if (viewIdx <= 0)
                {
                    continue;
                }

                if (viewDense[viewIdx].Index == slotIndex)
                {
                    return e;
                }
            }

            return -1;
        }
    }
}