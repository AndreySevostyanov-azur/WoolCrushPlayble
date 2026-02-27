using AzurGames.Wool.Gameplay;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using System.Collections.Generic;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Movements
{
    public sealed class BlockPathMoveSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly GameContext _ctx;

        public BlockPathMoveSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        private EcsWorld _world;
        private EcsFilter _movers;
        private EcsFilter _borders;
        private EcsPool<BlockViewRef> _viewPool;
        private EcsPool<BlockMoveData> _movePool;
        private EcsPool<GameBordersComponent> _bordersPool;
        private EcsPool<BlockArrivedToSlotEvent> _arrivedPool;
        private readonly List<int> _entitiesToDelete = new List<int>(32);

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _viewPool = _world.GetPool<BlockViewRef>();
            _movePool = _world.GetPool<BlockMoveData>();
            _bordersPool = _world.GetPool<GameBordersComponent>();
            _arrivedPool = _world.GetPool<BlockArrivedToSlotEvent>();
            _movers = _world.Filter<BlockViewRef>().Inc<BlockMoveData>().End();
            _borders = _world.Filter<GameBordersComponent>().End();
        }

        public void Run(IEcsSystems systems)
        {
            _entitiesToDelete.Clear();

            if (_borders.GetEntitiesCount() <= 0)
            {
                return;
            }

            var bordersEntity = _borders.GetRawEntities()[0];
            var bordersSparse = _bordersPool.GetRawSparseItems();
            if (bordersEntity < 0 || bordersEntity >= bordersSparse.Length)
            {
                return;
            }

            var bordersIdx = bordersSparse[bordersEntity];
            if (bordersIdx <= 0)
            {
                return;
            }

            var bordersDense = _bordersPool.GetRawDenseItems();
            var borders = bordersDense[bordersIdx];

            var speed = _ctx != null ? _ctx.BlockMoveSpeed : 0f;
            if (speed <= 0f)
            {
                return;
            }

            var dt = Time.deltaTime;
            var stepDistance = speed * dt;
            const float epsilon = 0.05f;

            var moverEntities = _movers.GetRawEntities();
            var moverCount = _movers.GetEntitiesCount();
            var viewDense = _viewPool.GetRawDenseItems();
            var viewSparse = _viewPool.GetRawSparseItems();
            var moveDense = _movePool.GetRawDenseItems();
            var moveSparse = _movePool.GetRawSparseItems();

            for (var i = 0; i < moverCount; i++)
            {
                var e = moverEntities[i];
                if (e < 0 || e >= viewSparse.Length || e >= moveSparse.Length)
                {
                    continue;
                }

                var viewIdx = viewSparse[e];
                var moveIdx = moveSparse[e];
                if (viewIdx <= 0 || moveIdx <= 0)
                {
                    continue;
                }

                var view = viewDense[viewIdx];
                var move = moveDense[moveIdx];

                if (move.MoveType == BlockMoveType.None)
                {
                    continue;
                }

                if (view.Block == null || view.Transform == null)
                {
                    _entitiesToDelete.Add(e);
                    continue;
                }

                var tr = view.Transform;
                var pos = tr.position;
                var prevPos = pos;
                var prevMoveType = move.MoveType;

                switch (move.MoveType)
                {
                    case BlockMoveType.ToSlot:
                        var toSlotResult = MoveToSlot(e, move, view, pos, borders, stepDistance, epsilon);
                        move = toSlotResult.Move;
                        pos = toSlotResult.Position;
                        if (toSlotResult.Destroyed)
                        {
                            // Block was destroyed and entity deleted.
                            continue;
                        }
                        tr.position = pos;
                        break;

                    case BlockMoveType.ToBlocker:
                        var prevPosBlocker = pos;
                        pos = MoveTowardsPoint(pos, move.CachedBlockerPoint, stepDistance, epsilon);
                        tr.position = pos;

                        if (Vector3.Distance(pos, move.CachedBlockerPoint) <= epsilon || IsIntersectingAnyBlock(view))
                        {
                            move.MoveType = BlockMoveType.ToOrigin;
                        }
                        break;

                    case BlockMoveType.ToOrigin:
                        var prevPosOrigin = pos;
                        pos = MoveTowardsPoint(pos, view.OriginPosition, stepDistance, epsilon);
                        tr.position = pos;
                        if (Vector3.Distance(pos, view.OriginPosition) <= epsilon)
                        {
                            tr.position = view.OriginPosition;
                            move.MoveType = BlockMoveType.None;
                            move.PathCount = 0;
                            move.PathIndex = 0;
                        }
                        break;
                }

                if (move.MoveType != BlockMoveType.None && !ContainsXZ(borders, tr.position))
                {
                    // Outside borders: return to origin (but never interrupt ToSlot).
                    if (move.MoveType != BlockMoveType.ToSlot)
                    {
                        move.MoveType = BlockMoveType.ToOrigin;
                        move.PathCount = 0;
                        move.PathIndex = 0;
                    }
                }

                // reset stall counter when leaving ToSlot (or when move type changes)
                if (prevMoveType == BlockMoveType.ToSlot && move.MoveType != BlockMoveType.ToSlot)
                {
                }

                moveDense[moveIdx] = move;
                viewDense[viewIdx] = view;
            }

            for (var i = 0; i < _entitiesToDelete.Count; i++)
            {
                _world.DelEntity(_entitiesToDelete[i]);
            }
        }

        private struct MoveToSlotResult
        {
            public BlockMoveData Move;
            public Vector3 Position;
            public bool Destroyed;
        }

        private struct SlotTargetResult
        {
            public bool Ok;
            public Vector3 Target;
        }

        private MoveToSlotResult MoveToSlot(int entity, BlockMoveData move, BlockViewRef view, Vector3 position,
            GameBordersComponent borders, float stepDistance, float epsilon)
        {
            var result = new MoveToSlotResult
            {
                Move = move,
                Position = position,
                Destroyed = false,
            };

            if (_ctx == null || _ctx.ReelSlots == null || result.Move.TargetSlotIndex < 0 || result.Move.TargetSlotIndex >= _ctx.ReelSlots.Length)
            {
                var m = result.Move;
                m.MoveType = BlockMoveType.ToOrigin;
                result.Move = m;
                return result;
            }

            var slotView = _ctx.ReelSlots[result.Move.TargetSlotIndex];
            if (slotView == null || slotView.Rect == null)
            {
                var m = result.Move;
                m.MoveType = BlockMoveType.ToOrigin;
                result.Move = m;
                return result;
            }

            if (result.Move.PathCount == 0)
            {
                var forward = view.Transform.forward;
                var slotTargetResult = TryGetSlotTarget(slotView.Rect, position.y);
                if (!slotTargetResult.Ok)
                {
                    // Can't compute slot target -> do not delete, just return.
                    var m = result.Move;
                    m.MoveType = BlockMoveType.ToOrigin;
                    result.Move = m;
                    return result;
                }

                var m2 = result.Move;
                m2 = BuildPath(m2, position, forward, slotTargetResult.Target, borders);
                result.Move = m2;
            }

            if (result.Move.PathIndex >= result.Move.PathCount)
            {
                var m = result.Move;
                m.MoveType = BlockMoveType.None;
                result.Move = m;
                return result;
            }

            var target = GetPoint(result.Move, result.Move.PathIndex);
            var distanceToTarget = Vector3.Distance(position, target);
            if (distanceToTarget <= epsilon)
            {
                position = target;
                var m = result.Move;
                m.PathIndex = m.PathIndex + 1;
                result.Move = m;
                result.Position = position;
                return result;
            }

            var displacement = result.Move.PathIndex == 0
                ? GetDirectDisplacement(position, target, stepDistance)
                : GetAxisAlignedDisplacement(position, target, stepDistance);

            // Fallback: if axis-aligned step can't move us (rare Bridge/WebGL edge-cases),
            // use direct step to prevent stalling on borders.
            if (displacement.sqrMagnitude <= 0.000001f)
            {
                displacement = GetDirectDisplacement(position, target, stepDistance);
            }

            if (displacement.sqrMagnitude > 0.000001f)
            {
                position += displacement;
                RotateToDisplacement(view.Transform, displacement);
            }

            if (Vector3.Distance(position, target) <= epsilon)
            {
                position = target;
                var m = result.Move;
                m.PathIndex = m.PathIndex + 1;
                result.Move = m;
            }

            result.Position = position;

            // If reached final point (slot), destroy block.
            if (result.Move.PathIndex >= result.Move.PathCount)
            {
                // Emit one-tick event for slot assignment (Luna-safe: no out/ref).
                if (result.Move.TargetSlotIndex >= 0)
                {
                    var evEntity = _world.NewEntity();
                    _arrivedPool.Add(evEntity);

                    var evDense = _arrivedPool.GetRawDenseItems();
                    var evSparse = _arrivedPool.GetRawSparseItems();
                    if (evEntity >= 0 && evEntity < evSparse.Length)
                    {
                        var evIdx = evSparse[evEntity];
                        if (evIdx > 0)
                        {
                            var ev = evDense[evIdx];
                            ev.SlotIndex = result.Move.TargetSlotIndex;
                            ev.Turns = view.Block != null ? view.Block.Turns : 0;
                            ev.BoxType = view.Block != null ? view.Block.BoxType : default(BoxType);
                            ev.Color = view.Block != null ? view.Block.Type : default(Colors);
                            ev.ColorOffset = view.Block != null ? view.Block.ColorOffset : 0;
                            evDense[evIdx] = ev;
                        }
                    }
                }

                Object.Destroy(view.Block.gameObject);
                var m = result.Move;
                m.MoveType = BlockMoveType.None;
                result.Move = m;
                _entitiesToDelete.Add(entity);
                result.Destroyed = true;
            }

            return result;
        }

        private static SlotTargetResult TryGetSlotTarget(RectTransform rect, float yPlane)
        {
            var result = new SlotTargetResult { Ok = false, Target = default(Vector3) };

            if (rect == null)
            {
                return result;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                return result;
            }

            var screenPos = RectTransformUtility.WorldToScreenPoint(cam, rect.position);
            var ray = cam.ScreenPointToRay(screenPos);

            // Manual ray-plane intersection (Luna-safe).
            var denom = ray.direction.y;
            if (Mathf.Abs(denom) < 0.000001f)
            {
                return result;
            }

            var enter = (yPlane - ray.origin.y) / denom;
            if (enter < 0f)
            {
                return result;
            }

            var hit = ray.GetPoint(enter);
            result.Target = new Vector3(hit.x, yPlane, hit.z);
            result.Ok = true;
            return result;
        }

        private static BlockMoveData BuildPath(BlockMoveData move, Vector3 startPos, Vector3 forward, Vector3 slotTarget, GameBordersComponent borders)
        {
            const float borderInset = 0.05f;

            move.PathIndex = 0;
            move.PathCount = 0;

            var forwardXZ = new Vector3(forward.x, 0f, forward.z);
            if (forwardXZ.sqrMagnitude < 0.0001f)
            {
                forwardXZ = Vector3.forward;
            }
            forwardXZ.Normalize();

            var boundary = GetBoundaryPoint(borders, startPos, forwardXZ, borderInset);
            boundary.y = startPos.y;

            var min = borders.Center - borders.Extents;
            var max = borders.Center + borders.Extents;
            var topZ = max.z - borderInset;
            if (topZ < min.z + borderInset)
            {
                topZ = max.z;
            }

            var slotX = slotTarget.x;
            var y = startPos.y;

            // If we're moving "down" (negative Z direction), then at the bottom border
            // we first turn to the nearest side (left/right) and only then continue the standard route.
            // This prevents trying to go straight down outside the field.
            var down = forwardXZ.z < -0.0001f;
            if (down)
            {
                // Force boundary point to be on the bottom edge (inside inset).
                boundary.z = min.z + borderInset;

                // Choose nearest side from current boundary.x
                var leftX = min.x + borderInset;
                var rightX = max.x - borderInset;
                var toLeft = Mathf.Abs(boundary.x - leftX);
                var toRight = Mathf.Abs(rightX - boundary.x);
                var sideX = (toLeft <= toRight) ? leftX : rightX;

                move = AddPoint(move, boundary);
                move = AddPoint(move, new Vector3(sideX, y, boundary.z));
                move = AddPoint(move, new Vector3(sideX, y, topZ));
                move = AddPoint(move, new Vector3(slotX, y, topZ));
                move = AddPoint(move, new Vector3(slotTarget.x, y, slotTarget.z));
                return move;
            }

            move = AddPoint(move, boundary);
            move = AddPoint(move, new Vector3(boundary.x, y, topZ));
            move = AddPoint(move, new Vector3(slotX, y, topZ));
            move = AddPoint(move, new Vector3(slotTarget.x, y, slotTarget.z));
            return move;
        }

        private static BlockMoveData AddPoint(BlockMoveData move, Vector3 point)
        {
            var count = move.PathCount;
            if (count > 0)
            {
                var prev = GetPoint(move, count - 1);
                if ((prev - point).sqrMagnitude < 0.0001f)
                {
                    return move;
                }
            }

            if (count >= 6)
            {
                return move;
            }

            move = SetPoint(move, count, point);
            move.PathCount = (byte)(count + 1);
            return move;
        }

        private static void RotateToDisplacement(Transform tr, Vector3 displacement)
        {
            var forward = new Vector3(displacement.x, 0f, displacement.z);
            if (forward.sqrMagnitude < 0.000001f)
            {
                return;
            }

            tr.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private static void RotateToTargetXZ(Transform tr, Vector3 fromPos, Vector3 targetPos)
        {
            if (tr == null)
            {
                return;
            }

            var dir = targetPos - fromPos;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.000001f)
            {
                return;
            }

            tr.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private static bool ContainsXZ(GameBordersComponent borders, Vector3 pos)
        {
            var min = borders.Center - borders.Extents;
            var max = borders.Center + borders.Extents;
            return pos.x >= min.x && pos.x <= max.x && pos.z >= min.z && pos.z <= max.z;
        }

        private static Vector3 GetBoundaryPoint(GameBordersComponent borders, Vector3 origin, Vector3 directionXZ, float inset)
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

            if (inset > 0f && t > inset)
            {
                t -= inset;
            }

            return origin + dir * t;
        }

        private static Vector3 GetAxisAlignedDisplacement(Vector3 position, Vector3 target, float stepDistance)
        {
            var delta = target - position;
            var absX = Mathf.Abs(delta.x);
            var absZ = Mathf.Abs(delta.z);

            if (absX <= 0.0001f && absZ <= 0.0001f)
            {
                return Vector3.zero;
            }

            if (absX >= absZ)
            {
                var move = Mathf.Min(absX, stepDistance);
                return new Vector3(Mathf.Sign(delta.x) * move, 0f, 0f);
            }

            var moveZ = Mathf.Min(absZ, stepDistance);
            return new Vector3(0f, 0f, Mathf.Sign(delta.z) * moveZ);
        }

        private static Vector3 GetDirectDisplacement(Vector3 position, Vector3 target, float stepDistance)
        {
            var delta = target - position;
            var distance = delta.magnitude;
            if (distance <= 0.0001f)
            {
                return Vector3.zero;
            }

            var step = Mathf.Min(distance, stepDistance);
            return delta / distance * step;
        }

        private static Vector3 GetPoint(BlockMoveData move, int index)
        {
            switch (index)
            {
                case 0: return move.P0;
                case 1: return move.P1;
                case 2: return move.P2;
                case 3: return move.P3;
                case 4: return move.P4;
                case 5: return move.P5;
                default: return move.P5;
            }
        }

        private static BlockMoveData SetPoint(BlockMoveData move, int index, Vector3 value)
        {
            switch (index)
            {
                case 0:
                    move.P0 = value;
                    break;
                case 1:
                    move.P1 = value;
                    break;
                case 2:
                    move.P2 = value;
                    break;
                case 3:
                    move.P3 = value;
                    break;
                case 4:
                    move.P4 = value;
                    break;
                case 5:
                    move.P5 = value;
                    break;
            }

            return move;
        }

        private static Vector3 MoveTowardsPoint(Vector3 position, Vector3 target, float stepDistance, float epsilon)
        {
            var delta = target - position;
            var dist = delta.magnitude;
            if (dist <= epsilon)
            {
                return target;
            }

            var step = Mathf.Min(dist, stepDistance);
            return position + delta / dist * step;
        }

        private bool IsIntersectingAnyBlock(BlockViewRef self)
        {
            if (self.Collider == null)
            {
                return false;
            }

            var a = self.Collider.bounds;
            var blockers = self.Block != null ? self.Block.BlockingBlocks : null;
            if (blockers == null || blockers.Length == 0)
            {
                return false;
            }

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

                if (AabbIntersects(a, col.bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AabbIntersects(Bounds a, Bounds b)
        {
            var aMin = a.min;
            var aMax = a.max;
            var bMin = b.min;
            var bMax = b.max;

            if (aMax.x < bMin.x) return false;
            if (aMin.x > bMax.x) return false;
            if (aMax.y < bMin.y) return false;
            if (aMin.y > bMax.y) return false;
            if (aMax.z < bMin.z) return false;
            if (aMin.z > bMax.z) return false;

            return true;
        }
    }
}

