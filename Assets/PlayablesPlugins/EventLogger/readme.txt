To log event in Luna call one of this methods:

Event Logger.Instance.LogEvent(name) - just log message
Event Logger.Instance.Log Event(name, value) - add some value at the end of message

If you enable "IsAutoCountable" in Log Event, use "Event Logger.Instance.Log Event(name)" 
method, and the value added to the message automatically