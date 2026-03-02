namespace Playeble.Scripts.Gameplay.Dragon
{
    public struct DragonBreathComponent
    {
        public float Time;
        public DragonBreathState State;
    }

    public enum DragonBreathState : byte
    {
        Idle,
        Breathing,
    }
}