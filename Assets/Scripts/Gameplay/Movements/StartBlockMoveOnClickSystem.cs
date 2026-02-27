using AzurGames.Wool.Gameplay;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using Playeble.Scripts.Gameplay.ReelSlots;
using Playeble.Scripts.Gameplay.Selection;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Movements
{
    public sealed class StartBlockMoveOnClickSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly GameContext _ctx;

        public StartBlockMoveOnClickSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        private EcsWorld _world;
        private EcsFilter _clickEvents;
        private EcsFilter _blocks;
        private EcsFilter _slotAssigned;
        private EcsFilter _slots;
        private EcsFilter _borders;

        private EcsPool<BlockClickedEvent> _clickPool;
        private EcsPool<BlockViewRef> _blockViewPool;
        private EcsPool<BlockMoveData> _blockMovePool;
        private EcsPool<GameBordersComponent> _bordersPool;

        private EcsPool<ReelSlotAssignedBlock> _assignedBlockPool;
        private EcsPool<ReelSlotViewRef> _slotViewPool;
        private EcsPool<ReelSlotState> _slotStatePool;

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();

            _clickPool = _world.GetPool<BlockClickedEvent>();
            _blockViewPool = _world.GetPool<BlockViewRef>();
            _blockMovePool = _world.GetPool<BlockMoveData>();
            _bordersPool = _world.GetPool<GameBordersComponent>();
            _assignedBlockPool = _world.GetPool<ReelSlotAssignedBlock>();
            _slotViewPool = _world.GetPool<ReelSlotViewRef>();
            _slotStatePool = _world.GetPool<ReelSlotState>();

            _clickEvents = _world.Filter<BlockClickedEvent>().End();
            _blocks = _world.Filter<BlockViewRef>().Inc<BlockMoveData>().End();
            _slotAssigned = _world.Filter<ReelSlotAssignedBlock>().Inc<ReelSlotViewRef>().End();
            _slots = _world.Filter<ReelSlotViewRef>().Inc<ReelSlotState>().End();
            _borders = _world.Filter<GameBordersComponent>().End();
        }

        public void Run(IEcsSystems systems)
        {
            var clickEntities = _clickEvents.GetRawEntities();
            var clickCount = _clickEvents.GetEntitiesCount();
            var clickDense = _clickPool.GetRawDenseItems();
            var clickSparse = _clickPool.GetRawSparseItems();

            var blockEntities = _blocks.GetRawEntities();
            var blockCount = _blocks.GetEntitiesCount();
            var viewDense = _blockViewPool.GetRawDenseItems();
            var viewSparse = _blockViewPool.GetRawSparseItems();
            var moveDense = _blockMovePool.GetRawDenseItems();
            var moveSparse = _blockMovePool.GetRawSparseItems();

            var assignedEntities = _slotAssigned.GetRawEntities();
            var assignedCount = _slotAssigned.GetEntitiesCount();
            var assignedDense = _assignedBlockPool.GetRawDenseItems();
            var assignedSparse = _assignedBlockPool.GetRawSparseItems();
            var slotViewDense = _slotViewPool.GetRawDenseItems();
            var slotViewSparse = _slotViewPool.GetRawSparseItems();
            var slotStateDense = _slotStatePool.GetRawDenseItems();
            var slotStateSparse = _slotStatePool.GetRawSparseItems();

            var slotEntities = _slots.GetRawEntities();
            var slotCount = _slots.GetEntitiesCount();

            var hasBorders = false;
            var borders = default(GameBordersComponent);
            if (_borders.GetEntitiesCount() > 0)
            {
                var bordersEntity = _borders.GetRawEntities()[0];
                var bordersSparse = _bordersPool.GetRawSparseItems();
                if (bordersEntity >= 0 && bordersEntity < bordersSparse.Length)
                {
                    var bordersIdx = bordersSparse[bordersEntity];
                    if (bordersIdx > 0)
                    {
                        borders = _bordersPool.GetRawDenseItems()[bordersIdx];
                        hasBorders = true;
                    }
                }
            }

            for (var i = 0; i < clickCount; i++)
            {
                var evEntity = clickEntities[i];
                if (evEntity < 0 || evEntity >= clickSparse.Length)
                {
                    continue;
                }

                var clickIdx = clickSparse[evEntity];
                if (clickIdx <= 0)
                {
                    continue;
                }

                var ev = clickDense[clickIdx];
                var clickedView = ev.Block;
                if (clickedView == null)
                {
                    continue;
                }

                var block = clickedView.GetComponentInParent<DragonColorBlock>();
                if (block == null)
                {
                    continue;
                }

                var blockEntity = FindBlockEntity(blockEntities, blockCount, viewDense, viewSparse, block);
                if (blockEntity < 0 || blockEntity >= moveSparse.Length)
                {
                    continue;
                }

                var moveIdx = moveSparse[blockEntity];
                if (moveIdx <= 0)
                {
                    continue;
                }

                var move = moveDense[moveIdx];
                if (move.MoveType != BlockMoveType.None)
                {
                    continue;
                }

                if (blockEntity < 0 || blockEntity >= viewSparse.Length)
                {
                    // Can't resolve view - keep block idle.
                    moveDense[moveIdx] = move;
                    continue;
                }

                var viewIdx = viewSparse[blockEntity];
                if (viewIdx <= 0)
                {
                    // Can't resolve view - keep block idle.
                    moveDense[moveIdx] = move;
                    continue;
                }

                var view = viewDense[viewIdx];
                var startPos = view.OriginPosition;
                var forward = Vector3.forward;
                if (view.Transform != null)
                {
                    startPos = view.Transform.position;
                    forward = view.Transform.forward;
                }

                // 1) Check forward blocker first (collision on the way).
                var blocker = GetForwardBlockerOrBoundary(view, startPos, forward, hasBorders, borders);
                if (blocker.Found)
                {
                    move.MoveType = BlockMoveType.ToBlocker;
                    move.TargetSlotIndex = -1;
                    move.PathIndex = 0;
                    move.PathCount = 0;
                    move.CachedBlockerPoint = blocker.Point;
                    moveDense[moveIdx] = move;
                    continue;
                }

                // 2) No blocker - proceed with slot logic.
                var slotIndex = GetAssignedSlotIndex(
                    assignedEntities,
                    assignedCount,
                    assignedDense,
                    assignedSparse,
                    slotViewDense,
                    slotViewSparse,
                    block);

                if (slotIndex >= 0)
                {
                    move.MoveType = BlockMoveType.ToSlot;
                    move.TargetSlotIndex = slotIndex;
                    move.PathIndex = 0;
                    move.PathCount = 0;
                    moveDense[moveIdx] = move;
                    continue;
                }

                // 3) No blocker and no reserved slot yet:
                // reserve first empty slot and move to it.
                var emptySlotEntity = FindFirstEmptySlotEntity(slotEntities, slotCount, slotViewDense, slotViewSparse, slotStateDense, slotStateSparse);
                if (emptySlotEntity >= 0
                    && emptySlotEntity < slotViewSparse.Length
                    && emptySlotEntity < slotStateSparse.Length
                    && emptySlotEntity < assignedSparse.Length)
                {
                    var viewIdxSlot = slotViewSparse[emptySlotEntity];
                    var stateIdxSlot = slotStateSparse[emptySlotEntity];
                    if (viewIdxSlot > 0 && stateIdxSlot > 0)
                    {
                        var slotIdx = slotViewDense[viewIdxSlot].Index;

                        // Reserve slot immediately while block is moving ToSlot.
                        var st = slotStateDense[stateIdxSlot];
                        st.Status = SlotStates.Reserved;
                        slotStateDense[stateIdxSlot] = st;

                        var assignIdx = assignedSparse[emptySlotEntity];
                        if (assignIdx <= 0)
                        {
                            _assignedBlockPool.Add(emptySlotEntity);
                            assignIdx = assignedSparse[emptySlotEntity];
                        }

                        if (assignIdx > 0)
                        {
                            var a = assignedDense[assignIdx];
                            a.Block = block;
                            assignedDense[assignIdx] = a;
                        }

                        move.MoveType = BlockMoveType.ToSlot;
                        move.TargetSlotIndex = slotIdx;
                        move.PathIndex = 0;
                        move.PathCount = 0;
                        moveDense[moveIdx] = move;
                        continue;
                    }
                }

                // 4) No empty slot - go to boundary point and return.
                move.MoveType = BlockMoveType.ToBlocker;
                move.TargetSlotIndex = -1;
                move.PathIndex = 0;
                move.PathCount = 0;
                move.CachedBlockerPoint = blocker.Point; // boundary fallback
                moveDense[moveIdx] = move;
            }
        }

        private static int FindFirstEmptySlotEntity(
            int[] slotEntities,
            int slotCount,
            ReelSlotViewRef[] viewDense,
            int[] viewSparse,
            ReelSlotState[] stateDense,
            int[] stateSparse)
        {
            var bestEntity = -1;
            var bestIndex = int.MaxValue;

            for (var i = 0; i < slotCount; i++)
            {
                var e = slotEntities[i];
                if (e < 0 || e >= viewSparse.Length || e >= stateSparse.Length)
                {
                    continue;
                }

                var stateIdx = stateSparse[e];
                var viewIdx = viewSparse[e];
                if (stateIdx <= 0 || viewIdx <= 0)
                {
                    continue;
                }

                if (stateDense[stateIdx].Status != SlotStates.Empty)
                {
                    continue;
                }

                var idx = viewDense[viewIdx].Index;
                if (idx < bestIndex)
                {
                    bestIndex = idx;
                    bestEntity = e;
                }
            }

            return bestEntity;
        }

        private static int GetAssignedSlotIndex(
            int[] assignedEntities,
            int assignedCount,
            ReelSlotAssignedBlock[] assignedDense,
            int[] assignedSparse,
            ReelSlotViewRef[] viewDense,
            int[] viewSparse,
            DragonColorBlock block)
        {
            if (block == null)
            {
                return -1;
            }

            var bestIndex = int.MaxValue;
            var bestSlotIndex = -1;

            for (var i = 0; i < assignedCount; i++)
            {
                var e = assignedEntities[i];
                if (e < 0 || e >= assignedSparse.Length || e >= viewSparse.Length)
                {
                    continue;
                }

                var assignedIdx = assignedSparse[e];
                var viewIdx = viewSparse[e];
                if (assignedIdx <= 0 || viewIdx <= 0)
                {
                    continue;
                }

                if (assignedDense[assignedIdx].Block != block)
                {
                    continue;
                }

                var idx = viewDense[viewIdx].Index;
                if (idx < bestIndex)
                {
                    bestIndex = idx;
                    bestSlotIndex = idx;
                }
            }

            return bestSlotIndex;
        }

        private static int FindBlockEntity(int[] blockEntities, int blockCount, BlockViewRef[] viewDense, int[] viewSparse, DragonColorBlock block)
        {
            if (block == null)
            {
                return -1;
            }

            for (var i = 0; i < blockCount; i++)
            {
                var e = blockEntities[i];
                if (e < 0 || e >= viewSparse.Length)
                {
                    continue;
                }

                var viewIdx = viewSparse[e];
                if (viewIdx <= 0)
                {
                    continue;
                }

                if (viewDense[viewIdx].Block == block)
                {
                    return e;
                }
            }

            return -1;
        }

        private struct BlockerResult
        {
            public bool Found;
            public Vector3 Point;
        }

        private BlockerResult GetForwardBlockerOrBoundary(BlockViewRef self, Vector3 startPos, Vector3 forward, bool hasBorders, GameBordersComponent borders)
        {
            // Find closest block in forward direction; fallback to boundary point inside borders.
            var result = new BlockerResult { Found = false, Point = startPos };

            var forwardXZ = new Vector3(forward.x, 0f, forward.z);
            if (forwardXZ.sqrMagnitude < 0.0001f)
            {
                forwardXZ = Vector3.forward;
            }
            forwardXZ.Normalize();

            // Only consider blockers before the first border hit (prevents false blockers for edge blocks).
            var maxT = float.PositiveInfinity;
            if (hasBorders)
            {
                maxT = GetBoundaryDistance(borders, startPos, forwardXZ);
                if (maxT < 0f)
                {
                    maxT = 0f;
                }
            }

            DragonColorBlock bestBlock = null;
            float bestT = float.PositiveInfinity;
            Bounds bestBounds = default(Bounds);
            Bounds selfBounds = default(Bounds);
            var hasSelfBounds = self.Collider != null;
            if (hasSelfBounds)
            {
                selfBounds = self.Collider.bounds;
            }

            var blockers = self.Block != null ? self.Block.BlockingBlocks : null;
            if (blockers != null && blockers.Length > 0)
            {
                for (var i = 0; i < blockers.Length; i++)
                {
                    var other = blockers[i];
                    if (other == null)
                    {
                        continue;
                    }

                    var col = other.GetComponentInChildren<Collider>(true);
                    if (col == null)
                    {
                        continue;
                    }

                    var b = col.bounds;
                    var delta = b.center - startPos;
                    var deltaXZ = new Vector3(delta.x, 0f, delta.z);
                    var t = Vector3.Dot(forwardXZ, deltaXZ);
                    if (t <= 0f)
                    {
                        continue;
                    }

                    if (t >= bestT)
                    {
                        continue;
                    }

                    if (t > maxT)
                    {
                        continue;
                    }

                    // Lateral distance check (rough) to avoid selecting far-off blocks.
                    var lateral = deltaXZ - forwardXZ * t;
                    var selfRadius = hasSelfBounds ? Mathf.Max(selfBounds.extents.x, selfBounds.extents.z) : 0.5f;
                    var otherRadius = Mathf.Max(b.extents.x, b.extents.z);
                    var lateralLimit = selfRadius + otherRadius + 0.05f;
                    if (lateral.sqrMagnitude > lateralLimit * lateralLimit)
                    {
                        continue;
                    }

                    bestT = t;
                    bestBlock = other;
                    bestBounds = b;
                }
            }

            if (bestBlock != null)
            {
                result.Found = true;

                // Move to the middle of the blocking block (no side offset).
                result.Point = new Vector3(bestBounds.center.x, startPos.y, bestBounds.center.z);
                return result;
            }

            // Fallback: go to border point in forward direction (inside border), then return.
            if (hasBorders)
            {
                result.Found = false;
                result.Point = GetBoundaryPoint(borders, startPos, forwardXZ);
                return result;
            }

            result.Found = false;
            result.Point = startPos + forwardXZ * 1f;
            return result;
        }

        private static float GetBoundaryDistance(GameBordersComponent borders, Vector3 origin, Vector3 directionXZ)
        {
            var dir = new Vector3(directionXZ.x, 0f, directionXZ.z);
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.forward;
            }
            dir.Normalize();

            var min = borders.Center - borders.Extents;
            var max = borders.Center + borders.Extents;
            var t = float.PositiveInfinity;

            if (Mathf.Abs(dir.x) > 0.0001f)
            {
                var tx = ((dir.x > 0f ? max.x : min.x) - origin.x) / dir.x;
                if (tx > 0f) t = Mathf.Min(t, tx);
            }

            if (Mathf.Abs(dir.z) > 0.0001f)
            {
                var tz = ((dir.z > 0f ? max.z : min.z) - origin.z) / dir.z;
                if (tz > 0f) t = Mathf.Min(t, tz);
            }

            if (float.IsInfinity(t))
            {
                return 0f;
            }

            // Keep a tiny inset so we don't count blockers beyond the border.
            t -= 0.05f;
            if (t < 0f) t = 0f;
            return t;
        }

        private static Vector3 GetBoundaryPoint(GameBordersComponent borders, Vector3 origin, Vector3 directionXZ)
        {
            var dir = new Vector3(directionXZ.x, 0f, directionXZ.z);
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.forward;
            }
            dir.Normalize();

            var min = borders.Center - borders.Extents;
            var max = borders.Center + borders.Extents;
            var t = float.PositiveInfinity;

            if (Mathf.Abs(dir.x) > 0.0001f)
            {
                var tx = ((dir.x > 0f ? max.x : min.x) - origin.x) / dir.x;
                if (tx > 0f) t = Mathf.Min(t, tx);
            }

            if (Mathf.Abs(dir.z) > 0.0001f)
            {
                var tz = ((dir.z > 0f ? max.z : min.z) - origin.z) / dir.z;
                if (tz > 0f) t = Mathf.Min(t, tz);
            }

            if (float.IsInfinity(t))
            {
                return origin;
            }

            return origin + dir * t;
        }
    }
}

