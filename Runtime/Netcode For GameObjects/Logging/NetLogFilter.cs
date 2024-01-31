using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace VaporNetcodeForGo
{
    public static class NetLogFilter
    {
        public static bool LogDeveloper => NetworkLog.CurrentLogLevel <= LogLevel.Developer;
        public static bool LogNormal => NetworkLog.CurrentLogLevel <= LogLevel.Normal;
        public static bool LogError => NetworkLog.CurrentLogLevel <= LogLevel.Error;
    }
}
