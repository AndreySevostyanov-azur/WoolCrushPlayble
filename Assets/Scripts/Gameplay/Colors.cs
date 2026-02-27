namespace Playeble.Scripts.Gameplay
{
    public enum Colors : ushort
    {
        Red = 1 << 0,
        Yellow = 1 << 1,
        Cyan = 1 << 2,
        Purple = 1 << 3,
        Pink = 1 << 4,
        Green = 1 << 5,
        Blue = 1 << 6,
        Orange = 1 << 7,
        White = 1 << 8, // special: no color
    }
    
    public static class ColorExtensions
    {
        public static int GetOffset(this Colors color)
        {
            int offset = -1;
            int mask = (int)color;

            //NOTE - this way will never cycle endlessly for color = default(0)
            while (mask != 0)
            {
                mask >>= 1;
                offset++;
            }

            return offset;
        }
    }
}

