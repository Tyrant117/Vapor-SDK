using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VaporNetcode
{
    /// <summary>Synchronizes server time to clients.</summary>
    public static class NetTime
    {
        /// <summary>Ping message frequency, used to calculate network time and RTT</summary>
        public static float PingFrequency = 2;

        /// <summary>Average out the last few results from Ping</summary>
        public static int PingWindowSize = 6;

        private static double _lastPingTime;

        private static ExponentialMovingAverage _rtt = new(PingWindowSize);

        /// <summary>The time in seconds since the server started.</summary>
        // via global NetworkClient snapshot interpolated timeline (if client).
        // on server, this is simply Time.timeAsDouble.
        //
        // I measured the accuracy of float and I got this:
        // for the same day,  accuracy is better than 1 ms
        // after 1 day,  accuracy goes down to 7 ms
        // after 10 days, accuracy is 61 ms
        // after 30 days , accuracy is 238 ms
        // after 60 days, accuracy is 454 ms
        // in other words,  if the server is running for 2 months,
        // and you cast down to float,  then the time will jump in 0.4s intervals.
        public static double Time
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => UDPServer.isRunning
                ? UnityEngine.Time.timeAsDouble
                : UDPClient.LocalTimeline;
        }

        /// <summary>Round trip time (in seconds) that it takes a message to go client->server->client.</summary>
        public static double Rtt => _rtt.Value;

        // RuntimeInitializeOnLoadMethod -> fast playmode without domain reload
        [RuntimeInitializeOnLoadMethod]
        public static void ResetStatics()
        {
            PingFrequency = 2;
            PingWindowSize = 6;
            _lastPingTime = 0;
            _rtt = new ExponentialMovingAverage(PingWindowSize);
        }

        internal static void UpdateClient()
        {
            // localTime (double) instead of Time.time for accuracy over days
            if (UnityEngine.Time.timeAsDouble - _lastPingTime >= PingFrequency)
            {
                NetworkPingMessage pingMessage = new() { clientTime = UnityEngine.Time.timeAsDouble };
                UDPClient.Send(pingMessage, Channels.Unreliable);
                _lastPingTime = UnityEngine.Time.timeAsDouble;
            }
        }

        // executed at the server when we receive a ping message
        // reply with a pong containing the time from the client
        // and time from the server
        internal static void OnServerPing(INetConnection conn, NetworkPingMessage message)
        {
            // Debug.Log($"OnPingServerMessage conn:{conn}");
            var pongMessage = new NetworkPongMessage
            {
                clientTime = message.clientTime,
            };
            UDPServer.Send(conn, pongMessage, Channels.Unreliable);
        }

        // Executed at the client when we receive a Pong message
        // find out how long it took since we sent the Ping
        // and update time offset
        internal static void OnClientPong(INetConnection _, NetworkPongMessage message)
        {
            // how long did this message take to come back
            double newRtt = UnityEngine.Time.timeAsDouble - message.clientTime;
            _rtt.Add(newRtt);
        }
    }
}