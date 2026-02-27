using System.Collections.Generic;
using UnityEngine;

namespace Azur.PlayableTemplate.Logger
{
    public class EventLogger : MonoBehaviour
    {
        [HideInInspector] public List<Log> Logs = new List<Log>();

        private static EventLogger _instance;
        public static EventLogger Instance => _instance;

        private void Awake()
        {
            _instance = this;
        }
        
        public void LogEvent(LogName name)
        {
            var currentLog = Logs[(int)name];

            var message = currentLog.Message;
            
            if (currentLog.IsAutoCountable)
            {
                message += $"_{++currentLog.Number}";
            }

            global::Luna.Unity.Analytics.LogEvent(message, 0);
        }
        
        public void LogEvent(LogName name, int number)
        {
            var currentLog = Logs[(int)name];

            var message = $"{currentLog.Message}_{number}";

            global::Luna.Unity.Analytics.LogEvent(message, 0);
        }
    }
}
