using AzurGames.Wool.Gameplay;
using Playeble.Scripts.Gameplay.Dragon;

namespace Playeble.Scripts.Gameplay.Movements
{
    public struct BlockArrivedToSlotEvent
    {
        public int SlotIndex;
        public int Turns;
        public BoxType BoxType;
        public Colors Color;
        public int ColorOffset;
    }
}

