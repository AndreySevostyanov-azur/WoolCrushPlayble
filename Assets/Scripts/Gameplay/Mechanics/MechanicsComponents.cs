using Leopotam.EcsLite;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    public enum HiddenPhase : byte
    {
        None = 0,
        PulseUp = 1,
        PulseDown = 2,
        Solid = 3,
        Finished = 4,
    }

    public struct BlockHidden
    {
        public Material Material;
        public HiddenPhase Phase;
        public float SubTimer;
        public int PulseCounter;
        public float HalfPulse;
        public float SolidDuration;
    }

    public struct BlockFrozen
    {
        public MeshRenderer Ice;
        public Transform VfxPoint;
    }

    public struct BlockMelt
    {
        public GameObject View;
        public Material Material;
        public float Elapsed;
        public float Duration;
    }

    public struct BlockChained
    {
        public int ActiveLinks;
        public SpriteRenderer Lock;
    }

    public struct BlockChainBlocker
    {
        public int ActiveLinks;
        public SpriteRenderer Pin;
    }

    public struct ChainLink
    {
        public EcsPackedEntity Target;
        public EcsPackedEntity Blocker;
        public ChainVisual Visual;
    }

    public struct BlockNudge
    {
        public float Time;
        public float Duration;
        public Vector3 Offset;
    }

    public struct BlockNudgeEvent
    {
        public int BlockEntity;
        public bool ShowMarker;
    }

    public struct BlockStrikeEvent
    {
        public int BlockEntity;
    }
}
