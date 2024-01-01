using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace VaporEvents
{
    public static class EventLogging
    {
        [Conditional("VAPOR_EVENT_LOGGING")]
        public static void Log(string message)
        {
            Debug.Log(message);
        }
    }
}
