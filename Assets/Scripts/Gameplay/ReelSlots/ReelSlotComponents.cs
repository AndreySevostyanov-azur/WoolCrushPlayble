using AzurGames.Wool.Gameplay;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.ReelSlots
{
    public struct ReelSlotViewRef
    {
        public SlotView View;
        public int Index;
    }

    public struct ReelSlotState
    {
        public SlotStates Status;
    }

    public struct ReelSpoolData
    {
        public int Current;
        public int Max;
        public BoxType BoxType;
    }

    public struct ReelSpoolColor
    {
        public Colors Color;
        public int ColorOffset;
    }

    public struct ReelSlotAssignedBlock
    {
        public DragonColorBlock Block;
    }

    public struct ReelWindState
    {
        public int TargetEntity;
        public float Progress01;
        public float DisposeTimer;
    }
}

