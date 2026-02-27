using Azur.PlayableTemplate.Logger;
using UnityEngine;

namespace Azur.Playable
{
    public class LoggerExample : MonoBehaviour
    {
        private int _counter;

        private void Start()
        {
            EventLogger.Instance.LogEvent(LogName.Start);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                EventLogger.Instance.LogEvent(LogName.Click);
                EventLogger.Instance.LogEvent(LogName.DifferentClick);
                EventLogger.Instance.LogEvent(LogName.StaticValue, 42);
            }
        }
    }
}
