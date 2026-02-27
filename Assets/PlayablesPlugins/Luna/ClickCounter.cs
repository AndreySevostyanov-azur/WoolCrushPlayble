using System;

namespace Azur.PlayableTemplate.LunaPlayable
{
    public class ClickCounter
    {
        public int MaxClicks
        {
            get;
        }
        
        private int _count;

        public ClickCounter(int maxClicks)
        {
            MaxClicks = maxClicks;
        }

        public event Action LimitReached;
        public event Action OneClickLeft;
        
        public void RegistrateClick()
        {
            _count++;
            CheckLimitReached();
			CheckOneClickLeft();
        }

        private void CheckLimitReached()
        {
			if (_count >= MaxClicks)
			{
				LimitReached?.Invoke();
			}
		}
        
        public void CheckOneClickLeft()
        {
            if (_count == MaxClicks - 1)
            {
                OneClickLeft?.Invoke();
            }
        }
        
        public void ForceComplete()
        {
            _count = MaxClicks;
        }
    }
}