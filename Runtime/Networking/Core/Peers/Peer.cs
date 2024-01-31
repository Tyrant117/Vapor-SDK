using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.ObjectLogging;

namespace VaporNetcode
{
    public delegate void PeerActionHandler(Peer peer);

    public delegate void ResponseCallback(ResponseStatus status, ISerializablePacket response);

    [Serializable]
    public class Peer : IDisposable, INetConnection
    {
        // Server and Connection Info
        private int connectionID;
        public bool IsConnected { get; set; }
        public bool IsAuthenticated { get; private set; }
        public bool IsReady { get; set; }
        public int ConnectionID => connectionID;
        public ulong GenericULongID { get; set; }
        public string GenericStringID { get; set; }
        public int SpamCount { get; set; }
        public double RemoteTimestamp { get; set; }
        public uint NetID { get; set; }
        public float LastMessageTime { get; set; }

        public Unbatcher Unbatcher = new();
        public Batcher ReliableBatcher;
        public Batcher UnreliableBatcher;

        protected UDPTransport.Source source;
        public ObjectLogger Logger { get; set; }

        public SyncBatcher SyncBatcher { get; private set; }

        public event Action<bool, bool> PreUpdated;

        #region Responses
        private float _lastResponseCheckTime;
        protected readonly Dictionary<ushort, float> responseTimeoutQueue; // queue to handle when messages timeout
        protected readonly List<ushort> timedOutResponses;
        #endregion

        public Peer(int connectionID, UDPTransport.Source source)
        {
            this.connectionID = connectionID;
            this.source = source;

            ReliableBatcher = new Batcher(UDPTransport.GetBatchThreshold(Channels.Reliable));
            UnreliableBatcher = new Batcher(UDPTransport.GetBatchThreshold(Channels.Unreliable));

            SyncBatcher = new();
            Logger = new(false);

            if (source == UDPTransport.Source.Client)
            {
                responseTimeoutQueue = new();
                timedOutResponses = new();
            }
            else
            {
            }
        }

        public virtual void Authenticated(int connID)
        {
            IsAuthenticated = true;
            if (source == UDPTransport.Source.Client)
            {
                connectionID = connID;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose)
        {
            if (source == UDPTransport.Source.Client)
            {
                responseTimeoutQueue.Clear();
            }
        }

        #region - Connection -
        /// <summary>
        ///     Force a disconnect. Only works on the server.
        /// </summary>
        /// <param name="reason">Reason error code</param>
        public void Disconnect(int reason = 0)
        {
            switch (source)
            {
                case UDPTransport.Source.Default:
                    break;
                case UDPTransport.Source.Client:
                    UDPClient.Disconnect();
                    break;
                case UDPTransport.Source.Server:
                    IsConnected = false;
                    UDPTransport.DisconnectPeer(connectionID);
                    break;
            }
        }
        #endregion

        #region - Messaging -
        public void PreUpdate()
        {
            PreUpdated?.Invoke(IsAuthenticated, IsReady);
        }

        public void Update()
        {
            if(source == UDPTransport.Source.Client)
            {
                ClientUpdate();
            }

            // make and send as many batches as necessary from the stored
            // messages.
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            // make a batch with our local time (double precision)
            while (ReliableBatcher.GetBatch(writer))
            {
                // validate packet before handing the batch to the
                // transport. this guarantees that we always stay
                // within transport's max message size limit.
                // => just in case transport forgets to check it
                // => just in case mirror miscalulated it etc.
                ArraySegment<byte> segment = writer.ToArraySegment();
                if (ValidatePacketSize(segment, Channels.Reliable))
                {
                    // send to transport
                    SendToTransport(segment, Channels.Reliable);
                    //UnityEngine.Debug.Log($"sending batch of {writer.Position} bytes for channel={kvp.Key} connId={connectionId}");

                    // reset writer for each new batch
                    writer.Position = 0;
                }
            }

            while (UnreliableBatcher.GetBatch(writer))
            {
                ArraySegment<byte> segment = writer.ToArraySegment();
                if (ValidatePacketSize(segment, Channels.Unreliable))
                {
                    // send to transport
                    SendToTransport(segment, Channels.Unreliable);
                    //UnityEngine.Debug.Log($"sending batch of {writer.Position} bytes for channel={kvp.Key} connId={connectionId}");

                    // reset writer for each new batch
                    writer.Position = 0;
                }
            }
        }

        private void ClientUpdate()
        {
            if (Time.time - _lastResponseCheckTime > 1f)
            {
                HandleResponseDisposalTick();
                _lastResponseCheckTime = Time.time;
            }
        }

        protected static bool ValidatePacketSize(ArraySegment<byte> segment, int channelId)
        {
            int max = UDPTransport.GetMaxPacketSize(channelId);
            if (segment.Count > max)
            {
                Debug.LogError($"NetworkConnection.ValidatePacketSize: cannot send packet larger than {max} bytes, was {segment.Count} bytes");
                return false;
            }

            if (segment.Count == 0)
            {
                // zero length packets getting into the packet queues are bad.
                Debug.LogError("NetworkConnection.ValidatePacketSize: cannot send zero bytes");
                return false;
            }

            // good size
            return true;
        }

        /// <summary>
        ///     General implentation to send a message over the network. 
        /// </summary>
        public void SendMessage(ArraySegment<byte> msg, int channelID)
        {
            if(channelID == Channels.Reliable)
            {
                ReliableBatcher.AddMessage(msg, Time.timeAsDouble);
            }
            else
            {
                UnreliableBatcher.AddMessage(msg, Time.timeAsDouble);
            }
        }

        private void SendToTransport(ArraySegment<byte> segment, int channelId)
        {
            UDPTransport.Send(connectionID, segment, source, channelId);
        }

        public bool SendSimulatedMessage(ArraySegment<byte> msg)
        {
            return IsConnected && UDPTransport.SendSimulated(connectionID, msg, source);
        }

        public void SendSteamPeerToPeer(short opcode, ISerializablePacket packet, ulong steamGenericID)
        {

        }
        #endregion

        #region - Response Management -
        /// <summary>
        ///     Registers a response.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="timeout">Seconds</param>
        /// <returns></returns>
        public void RegisterResponse(ushort id, float timeout)
        {
            // +1, because it might be about to tick in a few miliseconds
            responseTimeoutQueue[id] = Time.time + timeout;
        }

        /// <summary>
        ///     Used for logging the timeout.
        /// </summary>
        /// <param name="currentTick"></param>
        private void HandleResponseDisposalTick()
        {
            timedOutResponses.Clear();
            foreach (var endTick in responseTimeoutQueue)
            {
                if (Time.timeAsDouble > endTick.Value)
                {
                    timedOutResponses.Add(endTick.Key);
                }
            }

            foreach (var over in timedOutResponses)
            {
                UDPClient.TimeoutResponse(over);
            }
        }

        /// <summary>
        ///     Triggers the response if the msg has one.
        /// </summary>
        /// <param name="msg"></param>
        public bool TriggerResponse<T>(ResponseStatus status) where T : struct, IResponseMessage
        {
            ushort responseID = NetworkMessageId<T>.Id;
            if (status == ResponseStatus.Timeout)
            {
                responseTimeoutQueue.Remove(responseID);
                return false;
            }
            else
            {
                return responseTimeoutQueue.Remove(responseID);
            }
        }
        #endregion
    }
}