using System;

namespace Azur.PlayableTemplate.Logger
{
    [Serializable]
    public class Log
    {
        public LogName Name;
        public string Message;
        public string CachedName;
        public bool IsAutoCountable;

        public int Number;

        public Log() {}

        public Log(LogName name, string message, bool isAutoCountable)
        {
            Name = name;
            Message = message;
            IsAutoCountable = isAutoCountable;
            Number = 0;
        }
    }
}
