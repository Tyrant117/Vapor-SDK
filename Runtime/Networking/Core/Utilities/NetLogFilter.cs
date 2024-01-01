namespace VaporNetcode
{
    /// <summary>
    /// Simple logger to show level of concern for the network.
    /// </summary>
    public static class NetLogFilter
    {

        public static bool Spew;
        public static bool MessageDiagnostics;
        public static bool SyncVars;

        public const int Debug = 0;
        public const int Info = 1;
        public const int Warn = 2;
        public const int Error = 3;
        public const int Fatal = 4;

        public static int CurrentLogLevel { get; set; } = Info;

        public static bool LogDebug => CurrentLogLevel <= Debug;
        public static bool LogInfo => CurrentLogLevel <= Info;
        public static bool LogWarn => CurrentLogLevel <= Warn;
        public static bool LogError => CurrentLogLevel <= Error;
        public static bool LogFatal => CurrentLogLevel <= Fatal;
    }
}
