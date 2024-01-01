#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading;
using UnityEditor;

namespace VaporNetcode
{
    [Serializable]
    public class ServerConfig
    {
        #region Inspector
#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#else
        [Header("Properties")]
#endif
        [Tooltip("Should server start by itself.")]
        public bool autoStart;
#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#endif
        [Tooltip("Max connections the server can have")]
        public int maxServerPlayers = 2000;
#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#endif
        [Tooltip("Server Address")]
        public string address = "127.0.0.1";
#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#endif
        [Tooltip("Server Port")]
        public int port = 7777;
#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#endif
        [Tooltip("Server Target Framerate")]
        public int serverUpdateRate = 30;
#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#endif
        [Tooltip("Snapshot interpolation setting")]
        public SnapshotInterpolationSettings SnapshotSettings;

#if ODIN_INSPECTOR
        [TitleGroup("Debug")]
#else
        [Header("Debug")]
#endif
        public bool isSimulated;
        #endregion
    }


    public static class UDPServer
    {
        private const string TAG = "<color=teal><b>[Server]</b></color>";
        public const string WARNING = "<color=yellow><b>[!]</b></color>";


        public static bool isRunning;
        private static bool isInitialized;
        private static bool isSetup;
        private static bool isSimulated;

        public static int SendRate => _config.serverUpdateRate;
        public static float SendInterval => 1f / _config.serverUpdateRate; // for 30 Hz, that's 33ms
        public static double BufferTime => SendInterval * _config.SnapshotSettings.bufferTimeMultiplier;
        public static SnapshotInterpolationSettings SnapshotSettings => _config.SnapshotSettings;
        private static double _lastSendTime;

        private static ServerConfig _config;

        // keep track of actual achieved tick rate.
        // might become lower under heavy load.
        // very useful for profiling etc.
        // measured over 1s each, same as frame rate. no EMA here.
        public static int actualTickRate;
        private static double actualTickRateStart;   // start time when counting
        private static int actualTickRateCounter; // current counter since start
        public static long tick;

        // profiling
        // includes transport update time, because transport calls handlers etc.
        // averaged over 1s by passing 'tickRate' to constructor.
        public static TimeSample earlyUpdateDuration;
        public static TimeSample lateUpdateDuration;

        // capture full Unity update time from before Early- to after LateUpdate
        public static TimeSample fullUpdateDuration;

        #region Properties
        public static string Address => _config.address;
        public static int Port => UDPTransport.Port;
        #endregion

        #region Connections
        public static Dictionary<int, Peer> connectedPeers = new(); // All peers connected to the server through the connectionID.

        //Network IDs for Objects
        private static long counter = 0;
        public static ulong NextNetworkID()
        {
            long id = Interlocked.Increment(ref counter);

            if (id == long.MaxValue)
            {
                throw new Exception("connection ID Limit Reached: " + id);
            }

            if (NetLogFilter.LogDebug && NetLogFilter.Spew) { Debug.LogFormat("Generated ID: {0}", id); }
            return (ulong)id;
        }
        #endregion

        #region Modules
        private static readonly Dictionary<Type, ServerModule> modules = new(); // Modules added to the network manager
        private static readonly HashSet<Type> initializedModules = new(); // set of initialized modules on the network manager
        #endregion

        #region Messaging
        private static readonly Dictionary<ushort, IPacketHandler> handlers = new(); // key value pair to handle messages.
        #endregion

        #region Event Handling
        public static event Action Started;
        private static Func<int, UDPTransport.Source, Peer> PeerCreator;

        public static event PeerActionHandler PeerConnected;
        public static event PeerActionHandler PeerDisconnected;
        #endregion

        #region - Unity Methods and Initialization -
        public static void Listen(ServerConfig config, Func<int, UDPTransport.Source, Peer> peerCreator, params ServerModule[] staringModules)
        {
            isInitialized = false;
            _config = config;
            //Application.targetFrameRate = _config.serverUpdateRate;
            //QualitySettings.vSyncCount = 0;
            isSimulated = _config.isSimulated;

            PeerCreator = peerCreator;

            NetTime.ResetStatics();
            // profiling
            earlyUpdateDuration = new TimeSample(_config.serverUpdateRate);
            lateUpdateDuration = new TimeSample(_config.serverUpdateRate);
            fullUpdateDuration = new TimeSample(_config.serverUpdateRate);

            foreach (var mod in staringModules)
            {
                AddModule(mod);
            }
            InitializeModules();
            _SetupServer();

            if (_config.autoStart)
            {
                StartServer();
            }
            if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Server Listening"); }

            // ---------------------------------------------------------------//
            static void _SetupServer()
            {
                if (isSetup) { return; }
                UDPTransport.OnServerConnected = HandleConnect;
                UDPTransport.OnServerDataReceived = HandleData;
                UDPTransport.OnServerDisconnected = HandleDisconnect;
                UDPTransport.OnServerError = HandleTransportError;


                RegisterHandler<NetworkPingMessage>(NetTime.OnServerPing, false);
                _lastSendTime = 0;
                isSetup = true;
            }
        }

        public static void StartServer()
        {
            UDPTransport.Init(true, false, _config.isSimulated);
            if (!_config.isSimulated)
            {
                UDPTransport.StartServer();
            }
            isInitialized = true;

            Started?.Invoke();
            isRunning = true;
            if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Server Started"); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Shutdown()
        {
            if (isInitialized)
            {
                UDPTransport.StopServer();
            }
            _lastSendTime = 0;

            actualTickRate = 0;
            actualTickRateStart = 0;
            actualTickRateCounter = 0;
            tick = 0;

            UDPTransport.OnServerConnected = null;
            UDPTransport.OnServerDataReceived = null;
            UDPTransport.OnServerDisconnected = null;
            UDPTransport.OnServerError = null;

            Started = null;
            PeerConnected = null;
            PeerDisconnected = null;
            PeerCreator = null;

            _config = null;
            isInitialized = false;
            isSetup = false;
            isRunning = false;
            isSimulated = false;
            counter = 0;

            connectedPeers.Clear();
            modules.Clear();
            initializedModules.Clear();
            handlers.Clear();


            if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Server Shutdown"); }
        }

        internal static void NetworkEarlyUpdate()
        {
            if (isRunning)
            {
                earlyUpdateDuration.Begin();
                fullUpdateDuration.Begin();

                tick++;
                UDPTransport.ServerEarlyUpdate();

                earlyUpdateDuration.End();
            }
        }

        internal static void NetworkLateUpdate()
        {
            if (isRunning && isSimulated)
            {
                while (UDPTransport.ReceiveSimulatedMessage(UDPTransport.Source.Server, out int connectionId, out UDPTransport.TransportEvent transportEvent, out ArraySegment<byte> data))
                {
                    switch (transportEvent)
                    {
                        case UDPTransport.TransportEvent.Connected:
                            HandleConnect(connectionId);
                            break;
                        case UDPTransport.TransportEvent.Data:
                            HandleData(connectionId, data, 1);
                            break;
                        case UDPTransport.TransportEvent.Disconnected:
                            HandleDisconnect(connectionId);
                            break;
                    }
                }
            }

            if (isRunning && UDPTransport.Active)
            {
                lateUpdateDuration.Begin();

                float deltaTime = (float)(Time.timeAsDouble - _lastSendTime);
                if (!Application.isPlaying || AccurateInterval.Elapsed(Time.timeAsDouble, SendInterval, ref _lastSendTime))
                {
                    foreach (var mod in modules.Values)
                    {
                        mod.Update(deltaTime);
                    }

                    foreach (var peer in connectedPeers.Values)
                    {
                        if (peer.IsAuthenticated)
                        {
                            Send(peer, new TimeSnapshotMessage(), Channels.Unreliable);
                        }

                        peer.PreUpdate();
                        peer.Update();
                    }
                }

                UDPTransport.ServerLateUpdate();

                ++actualTickRateCounter;

                if (Time.timeAsDouble >= actualTickRateStart + 1)
                {
                    // calculate avg by exact elapsed time.
                    // assuming 1s wouldn't be accurate, usually a few more ms passed.
                    float elapsed = (float)(Time.timeAsDouble - actualTickRateStart);
                    actualTickRate = Mathf.RoundToInt(actualTickRateCounter / elapsed);
                    actualTickRateStart = Time.timeAsDouble;
                    actualTickRateCounter = 0;
                }

                // measure total update time. including transport.
                // because in early update, transport update calls handlers.
                lateUpdateDuration.End();
                fullUpdateDuration.End();
            }
        }
        #endregion

        #region - Module Methods -
        /// <summary>
        ///     Adds a network module to the manager.
        /// </summary>
        /// <param name="module"></param>
        private static void AddModule(ServerModule module)
        {
            if (modules.ContainsKey(module.GetType()))
            {
                if (NetLogFilter.LogWarn) { Debug.Log(string.Format("{0} Module has already been added. {1} || ({2})", TAG, module, Time.time)); }
            }
            modules.Add(module.GetType(), module);
        }

        /// <summary>
        ///     Adds a network module to the manager and initializes all modules.
        /// </summary>
        /// <param name="module"></param>
        public static void AddModuleAndInitialize(ServerModule module)
        {
            AddModule(module);
            InitializeModules();
        }

        /// <summary>
        ///     Checks if the maanger has the module.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public static bool HasModule(ServerModule module)
        {
            return modules.ContainsKey(module.GetType());
        }

        /// <summary>
        ///     Initializes all uninitialized modules
        /// </summary>
        /// <returns></returns>
        public static bool InitializeModules()
        {
            while (true)
            {
                var changed = false;
                foreach (var mod in modules)
                {
                    // Module is already initialized
                    if (initializedModules.Contains(mod.Key)) { continue; }

                    mod.Value.Initialize();
                    initializedModules.Add(mod.Key);
                    changed = true;
                }

                // If nothing else can be initialized
                if (!changed)
                {
                    return !GetUninitializedModules().Any();
                }
            }
        }

        /// <summary>
        ///     Gets the module of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetModule<T>() where T : ServerModule
        {
            modules.TryGetValue(typeof(T), out ServerModule module);
            if (module == null)
            {
                module = modules.Values.FirstOrDefault(m => m is T);
            }
            return module as T;
        }

        /// <summary>
        ///     Gets all initialized modules.
        /// </summary>
        /// <returns></returns>
        private static List<ServerModule> GetInitializedModules()
        {
            return modules
                .Where(m => initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }

        /// <summary>
        ///     Gets all unitialized modules.
        /// </summary>
        /// <returns></returns>
        private static List<ServerModule> GetUninitializedModules()
        {
            return modules
                .Where(m => !initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }
        #endregion

        #region - Remote Connection Methods -
        private static void HandleConnect(int connectionID)
        {
            if (NetLogFilter.LogDebug) { Debug.Log($"{TAG} Connection ID: {connectionID} Connected"); }

            if (connectionID == 0)
            {
                Debug.LogError($"Server.HandleConnect: invalid connectionId: {connectionID} . Needs to be != 0, because 0 is reserved for local player.");
                UDPTransport.DisconnectPeer(connectionID);
                return;
            }

            // connectionId not in use yet?
            if (connectedPeers.ContainsKey(connectionID))
            {
                UDPTransport.DisconnectPeer(connectionID);
                return;
            }

            if (connectedPeers.Count < _config.maxServerPlayers)
            {
                // add connection
                var peer = PeerCreator.Invoke(connectionID, UDPTransport.Source.Server);

                connectedPeers[peer.ConnectionID] = peer;
                OnPeerConnected(peer);
            }
            else
            {
                UDPTransport.DisconnectPeer(connectionID);
            }
        }

        public static Peer GeneratePeer(int connectionID, UDPTransport.Source source)
        {
            return new Peer(connectionID, source)
            {
                IsConnected = true
            };
        }

        private static void HandleDisconnect(int connectionID)
        {
            if (NetLogFilter.LogDebug) { Debug.Log($"{TAG} Connection ID: {connectionID} Disconnected"); }
            if (connectedPeers.TryGetValue(connectionID, out Peer peer))
            {
                peer.Dispose();

                connectedPeers.Remove(peer.ConnectionID);

                OnPeerDisconnected(peer);
            }
        }

        private static void OnPeerConnected(Peer peer)
        {
            PeerConnected?.Invoke(peer);
        }

        private static void OnPeerDisconnected(Peer peer)
        {
            PeerDisconnected?.Invoke(peer);
        }

        public static void FlagPossibleSpam(INetConnection conn)
        {
            conn.SpamCount++;
            if (conn.SpamCount > 10)
            {
                ((Peer)conn).Disconnect();
            }
        }
        #endregion

        #region - Handle Message Methods -
        private static void HandleTransportError(int connectionId, TransportError error, string reason)
        {
            // transport errors will happen. logging a warning is enough.
            // make sure the user does not panic.
            Debug.Log($"{WARNING} {TAG} Server Transport Error for connId={connectionId}: {error}: {reason}.");
        }

        private static void HandleData(int connectionID, ArraySegment<byte> buffer, int channelID)
        {
            if (connectedPeers.TryGetValue(connectionID, out Peer peer))
            {
                if (!peer.Unbatcher.AddBatch(buffer))
                {
                    Debug.Log($"{WARNING} {TAG} Received Message was too short ({buffer.Count} < {Batcher.HeaderSize}) (messages should start with message id)");
                    peer.Disconnect();
                    return;
                }

                while (peer.Unbatcher.GetNextMessage(out var reader, out var remoteTimestamp))
                {
                    // enough to read at least header size?
                    if (reader.Remaining >= NetworkMessages.IdSize)
                    {
                        // make remoteTimeStamp available to the user
                        peer.RemoteTimestamp = remoteTimestamp;

                        if (!PeerMessageReceived(peer, reader, channelID))
                        {
                            Debug.Log($"{WARNING} {TAG} Failed to unpack and invoke message. Disconnecting {connectionID}.");
                            peer.Disconnect();
                            return;
                        }
                    }
                    else
                    {
                        // WARNING, not error. can happen if attacker sends random data.
                        Debug.Log($"{WARNING} {TAG} Received Message was too short (messages should start with message id). Disconnecting {connectionID}");
                        peer.Disconnect();
                        return;
                    }
                }

                // IMPORTANT: always keep this check to detect memory leaks.
                //            this took half a day to debug last time.
                if (peer.Unbatcher.BatchesCount > 0)
                {
                    Debug.LogError($"Still had {peer.Unbatcher.BatchesCount} batches remaining after processing, even though processing was not interrupted by a scene change. This should never happen, as it would cause ever growing batches.\nPossible reasons:\n* A message didn't deserialize as much as it serialized\n*There was no message handler for a message id, so the reader wasn't read until the end.");
                }
            }
            else
            {
                Debug.LogError($"HandleData Unknown connectionId:{connectionID}");
                UDPTransport.DisconnectPeer(connectionID);
            }
        }

        /// <summary>
        ///     Called after the peer parses the message. Only assigned to the local player. Use <see cref="IncomingMessage.Sender"/> to determine what peer sent the message.
        /// </summary>
        /// <param name="msg"></param>
        private static bool PeerMessageReceived(Peer peer, NetworkReader reader, int channelID)
        {
            if (NetworkMessages.UnpackId(reader, out var opCode))
            {
                if (handlers.TryGetValue(opCode, out var handler))
                {
                    handler.Handle(peer, reader, channelID);
                    peer.LastMessageTime = Time.time;
                    return true;
                }
                else
                {
                    // => WARNING, not error. can happen if attacker sends random data.
                    Debug.Log($"{WARNING} {TAG} Unknown message id: {opCode} for connection: {peer}. This can happen if no handler was registered for this message.");
                    return false;
                }
            }
            else
            {
                // => WARNING, not error. can happen if attacker sends random data.
                Debug.Log($"{WARNING} {TAG} Invalid message header for connection: {peer}.");
                return false;
            }
        }

        public static void RegisterHandler<T>(IncomingMessageHandler<T> handler, bool requireAuthentication = true) where T : struct, INetMessage
        {
            ushort opCode = NetworkMessageId<T>.Id;
            if (handlers.Remove(opCode))
            {
                if (NetLogFilter.LogInfo) { Debug.LogFormat("{0} Handler Overwritten", opCode); }
            }
            handlers.Add(opCode, new PacketHandler<T>(opCode, handler, requireAuthentication));

        }

        /// <summary>
        ///     Remove a specific message handler
        /// </summary>
        /// <param name="opCode"></param>
        /// <returns></returns>
        public static bool RemoveHandler<T>() where T : struct, INetMessage
        {
            return handlers.Remove(NetworkMessageId<T>.Id);
        }
        #endregion

        #region - Messaging Methods -
        public static void SendAll<T>(T message, int channelId = Channels.Reliable) where T : struct, INetMessage
        {
            using var w = NetworkWriterPool.Get();
            NetworkMessages.Pack(message, w);
            ArraySegment<byte> segment = w.ToArraySegment();

            int count = 0;
            if (!isSimulated)
            {
                foreach (var peer in connectedPeers.Values)
                {
                    count++;
                    peer.SendMessage(segment, channelId);
                }
            }
            else
            {
                foreach (var peer in connectedPeers.Values)
                {
                    peer.SendSimulatedMessage(segment);
                }
            }
            NetDiagnostics.OnSend(message, channelId, segment.Count, count);
        }

        public static void SendToObservers<T>(IServerIdentity identity, T message, int channelId = Channels.Reliable) where T : struct, INetMessage
        {
            if (identity == null || identity.Observers.Count == 0)
            {
                return;
            }

            using var w = NetworkWriterPool.Get();
            // pack message into byte[] once
            NetworkMessages.Pack(message, w);
            ArraySegment<byte> segment = w.ToArraySegment();

            int count = 0;
            if (!isSimulated)
            {
                foreach (var id in identity.Observers.Values)
                {
                    count++;
                    id.Peer.SendMessage(segment, channelId);
                }
            }
            else
            {
                foreach (var id in identity.Observers.Values)
                {
                    id.Peer.SendSimulatedMessage(segment);
                }
            }
            NetDiagnostics.OnSend(message, channelId, segment.Count, count);
        }

        public static void Send<T>(INetConnection conn, T message, int channelId = Channels.Reliable) where T : struct, INetMessage
        {
            if (conn == null || !conn.IsConnected) { return; }

            using var w = NetworkWriterPool.Get();
            NetworkMessages.Pack(message, w);
            ArraySegment<byte> segment = w.ToArraySegment();

            if (connectedPeers.TryGetValue(conn.ConnectionID, out var peer))
            {
                if (!isSimulated)
                {
                    peer.SendMessage(segment, channelId);
                }
                else
                {
                    peer.SendSimulatedMessage(segment);
                }
            }
            NetDiagnostics.OnSend(message, channelId, segment.Count, 1);
        }

        /// <summary>
        ///     Response message to the sender
        /// </summary>
        /// <param name="msg"></param>
        public static void Respond<T>(INetConnection conn, T message) where T : struct, INetMessage
        {
            Send(conn, message);
        }
        #endregion

    }
}