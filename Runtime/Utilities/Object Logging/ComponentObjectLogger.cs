using UnityEngine;

namespace Vapor.ObjectLogging
{
    public class ComponentObjectLogger : MonoBehaviour
    {
        public ObjectLogger Logger;

        public void Log(LogLevel logLevel, string log, int traceSkipFrames = 1)
        {
            Logger.Log(logLevel,log, traceSkipFrames);
        }

        public void LogWithConsole(LogLevel logLevel, string log, int traceSkipFrames = 1)
        {
            Logger.LogWithConsole(logLevel, log, traceSkipFrames);
        }
    }
}
