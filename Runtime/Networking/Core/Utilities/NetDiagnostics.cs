using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public static class NetDiagnostics
    {
        public readonly struct MessageInfo
        {
            /// <summary>The message being sent</summary>
            public readonly INetMessage message;
            /// <summary>channel through which the message was sent</summary>
            public readonly int channel;
            /// <summary>how big was the message (does not include transport headers)</summary>
            public readonly int bytes;
            /// <summary>How many connections was the message sent to.</summary>
            public readonly int count;

            internal MessageInfo(INetMessage message, int channel, int bytes, int count)
            {
                this.message = message;
                this.channel = channel;
                this.bytes = bytes;
                this.count = count;
            }

            public override string ToString()
            {
                return $"{message.GetType().Name} | Size: {bytes}";
            }
        }

        /// <summary>Event for when Mirror sends a message. Can be subscribed to.</summary>
        public static event Action<MessageInfo> OutMessageEvent;

        /// <summary>Event for when Mirror receives a message. Can be subscribed to.</summary>
        public static event Action<MessageInfo> InMessageEvent;


        private static readonly Queue<int> bytesIn = new(10);
        private static readonly Queue<int> bytesOut = new(10);

        private static float lastTime;
        private static int outInterval;
        private static int inInterval;

        private static int totalBytesIn;
        private static int totalBytesOut;

        public static int aveBytesIn;
        public static int aveBytesOut;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            InMessageEvent = null;
            OutMessageEvent = null;
        }


        #region Throughput
        public static void AverageThroughput(float time)
        {
            if(time - lastTime > 1)
            {
                lastTime = time;
                bytesIn.Enqueue(inInterval);
                bytesOut.Enqueue(outInterval);
                totalBytesIn += inInterval;
                totalBytesOut += outInterval;

                if(bytesIn.Count > 10)
                {
                    totalBytesIn -= bytesIn.Dequeue();
                }
                if (bytesOut.Count > 10)
                {
                    totalBytesOut -= bytesOut.Dequeue();
                }

                inInterval = 0;
                outInterval = 0;

                aveBytesIn = totalBytesIn / bytesIn.Count;
                aveBytesOut = totalBytesOut / bytesOut.Count;
            }
        }
        #endregion


        #region Outgoing Messages
        public static void OnSend<T>(T message, int channel, int bytes, int count) where T : struct, INetMessage
        {
            if (NetLogFilter.MessageDiagnostics)
            {
                outInterval += bytes * count;
                if (count > 0 && OutMessageEvent != null)
                {
                    MessageInfo outMessage = new(message, channel, bytes, count);
                    OutMessageEvent.Invoke(outMessage);
                }
            }
        }
        #endregion

        #region Incomming Messages

        public static void OnReceive<T>(T message, int channel, int bytes) where T : struct, INetMessage
        {
            if (NetLogFilter.MessageDiagnostics)
            {
                inInterval += bytes;
                if (InMessageEvent != null)
                {
                    MessageInfo inMessage = new(message, channel, bytes + 2, 1);
                    InMessageEvent.Invoke(inMessage);
                }
            }
        }
        #endregion
    }
}