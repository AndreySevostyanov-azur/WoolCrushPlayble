using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Movements
{
    public enum BlockMoveType : byte
    {
        None = 0,
        ToSlot = 1,
        ToBlocker = 2,
        ToOrigin = 3,
    }

    public struct BlockViewRef
    {
        public DragonColorBlock Block;
        public Transform Transform;
        public Collider Collider;
        public Vector3 OriginPosition;
    }

    public struct BlockMoveData
    {
        public BlockMoveType MoveType;

        public int TargetSlotIndex;

        public int PathIndex;
        public byte PathCount;

        public Vector3 P0;
        public Vector3 P1;
        public Vector3 P2;
        public Vector3 P3;
        public Vector3 P4;
        public Vector3 P5;

        public Vector3 CachedBlockerPoint;
    }
}

