using Cinemachine;
using AzurGames.Wool.Gameplay;
using UnityEngine;
using Playeble.Scripts.Gameplay;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public sealed class GameContext
    {
        public CinemachinePath DragonPath { get; }
        public Transform DragonRoot { get; }

        public GameObject DragonHeadPrefab { get; }
        public GameObject DragonBodyPrefab { get; }
        public GameObject DragonTailPrefab { get; }

        public int DragonBodySegmentsCount { get; }
        public float DragonSegmentSpacing { get; }
        public float DragonHeadSpeed { get; }
        public float DragonInitialHeadDistance { get; }

        public DragonScaleColorSlot[] ScaleColors { get; }

        public DragonColorBlock[] Blocks { get; }
        public float FieldBordersOffset { get; }
        public SlotView[] ReelSlots { get; }
        public SpoolSpriteConfig SpoolSpriteConfig { get; }
        public float WindSecondsPerScale { get; }
        public float DragonRebukeDuration { get; }
        public float BlockMoveSpeed { get; }

        public GameContext(
            CinemachinePath dragonPath,
            Transform dragonRoot,
            GameObject dragonHeadPrefab,
            GameObject dragonBodyPrefab,
            GameObject dragonTailPrefab,
            int dragonBodySegmentsCount,
            float dragonSegmentSpacing,
            float dragonHeadSpeed,
            float dragonInitialHeadDistance,
            DragonScaleColorSlot[] scaleColors,
            DragonColorBlock[] blocks,
            float fieldBordersOffset,
            SlotView[] reelSlots,
            SpoolSpriteConfig spoolSpriteConfig,
            float windSecondsPerScale,
            float dragonRebukeDuration,
            float blockMoveSpeed)
        {
            DragonPath = dragonPath;
            DragonRoot = dragonRoot;
            DragonHeadPrefab = dragonHeadPrefab;
            DragonBodyPrefab = dragonBodyPrefab;
            DragonTailPrefab = dragonTailPrefab;
            DragonBodySegmentsCount = dragonBodySegmentsCount;
            DragonSegmentSpacing = dragonSegmentSpacing;
            DragonHeadSpeed = dragonHeadSpeed;
            DragonInitialHeadDistance = dragonInitialHeadDistance;

            ScaleColors = scaleColors;

            Blocks = blocks;
            FieldBordersOffset = fieldBordersOffset;
            ReelSlots = reelSlots;
            SpoolSpriteConfig = spoolSpriteConfig;
            WindSecondsPerScale = windSecondsPerScale;
            DragonRebukeDuration = dragonRebukeDuration;
            BlockMoveSpeed = blockMoveSpeed;
        }

        [System.Serializable]
        public struct DragonScaleColorSlot
        {
            public Colors Type;
            [Min(0)] public int Count;
            [Range(0, 9)] public int ColorOffset;
        }
    }
}

