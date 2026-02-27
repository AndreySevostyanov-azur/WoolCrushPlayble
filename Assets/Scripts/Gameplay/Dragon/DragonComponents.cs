using Cinemachine;
using Leopotam.EcsLite;
using UnityEngine;
using Playeble.Scripts.Gameplay;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public enum DragonPartKind : byte
    {
        Head = 0,
        Body = 1,
        Tail = 2,
    }

    public struct TransformRef
    {
        public Transform Value;
    }

    public struct SplinePathRef
    {
        public CinemachinePath Path;
    }

    public struct DragonPart
    {
        public DragonPartKind Kind;
        public int Index;
    }

    public struct DragonHeadMove
    {
        public float Distance;
        public float Speed;
        public bool ReachedEndRaised;
    }

    public struct DragonFollowHead
    {
        public EcsPackedEntity Head;
        public float OffsetDistance;
    }

    public struct DragonReachedEndEvent
    {
    }

    public struct DragonScaleColor
    {
        public Colors Type;
    }

    public struct DragonScaleMarkerRef
    {
        public DragonScaleMarker Value;
    }

    public struct DragonRebukeRequestEvent
    {
        public int RemovedBodyIndex;
        public float ShiftDistance;
    }

    public struct DragonRebukeState
    {
        public float StartDistance;
        public float TargetDistance;
        public float Elapsed;
        public float Duration;
    }

    public struct DragonSpawnProgress
    {
        public int BodySpawned;
        public bool TailSpawned;
        public float MaxHeadDistanceReached;
    }
}

