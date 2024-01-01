using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditorInternal;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace VaporNetcode
{
    [Serializable]
    public class ClientConfig
    {
        #region Inspector
#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#else
        [Header("Properties")]
#endif
        [Tooltip("Should client connect by itself.")]
        public bool AutoConnect;

#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#endif
        [Tooltip("Address to the server")]
        public string GameServerIp = "127.0.0.1";

#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#endif
        [Tooltip("Port of the server")]
        public int GameServerPort = 7777;

#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#endif
        [Tooltip("Client Target Send Rate")]
        public int ClientUpdateRate = 30;
#if ODIN_INSPECTOR
        [TitleGroup("Properties")]
#endif
        [Tooltip("Server Target Send Rate")]
        public int ServerUpdateRate = 30;
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
        public bool IsSimulated;
        #endregion
    }

    public static class UDPClient
    {
        public const string TAG = "<color=olive><b>[Client]</b></color>";
        public const string WARNING = "<color=yellow><b>[!]</b></color>";

        private static bool isInitialized;
        private static bool isSimulated;

        private static readonly bool retryOnTimeout = true;
        public static float SendInterval => 1f / _config.ClientUpdateRate; // for 30 Hz, that's 33ms
        public static float ServerSendInterval => 1f / _config.ServerUpdateRate;
        private static double _lastSendTime;

        private static ClientConfig _config;

        #region Connections
        private static int connectionID = -1;
        private static float stopConnectingTime;
        private static bool isAttemptingReconnect;

        public static Peer ServerPeer { get; private set; }
        #endregion

        #region Snapshots
        public static double LocalTimeline;
        public static double BufferTime => 1f / _config.ServerUpdateRate * _config.SnapshotSettings.bufferTimeMultiplier;

        // <servertime, snaps>
        public static SortedList<double, TimeSnapshot> snapshots = new();

        // catchup / slowdown adjustments are applied to timescale,
        // to be adjusted in every update instead of when receiving messages.
        private static double localTimescale = 1;

        // we use EMA to average the last second worth of snapshot time diffs.
        // manually averaging the last second worth of values with a for loop
        // would be the same, but a moving average is faster because we only
        // ever add one value.
        private static ExponentialMovingAverage driftEma;

        [Tooltip("Automatically adjust bufferTimeMultiplier for smooth results.\nSets a low multiplier on stable connections, and a high multiplier on jittery connections.")]
        public static bool dynamicAdjustment = true;

        [Tooltip("Safety buffer that is always added to the dynamic bufferTimeMultiplier adjustment.")]
        public static float dynamicAdjustmentTolerance = 1; // 1 is realistically just fine, 2 is very very safe even for 20% jitter. can be half a frame too. (see above comments)

        [Tooltip("Dynamic adjustment is computed over n-second exponential moving average standard deviation.")]
        public static int deliveryTimeEmaDuration = 2;   // 1-2s recommended to capture average delivery time
        private static ExponentialMovingAverage deliveryTimeEma; // average delivery time (standard deviation gives average jitter)
        #endregion

        #region Modules
        private static readonly Dictionary<Type, ClientModule> modules = new(); // Modules added to the network manager
        private static readonly HashSet<Type> initializedModules = new(); // set of initialized modules on the network manager
        #endregion

        #region Messaging
        private static readonly Dictionary<ushort, IPacketHandler> handlers = new(); // key value pair to handle messages.
        #endregion

        #region Current Connection
        private static ConnectionStatus status;
        /// <summary>
        ///     Current connections status. If changed invokes StatusChanged event./>
        /// </summary>
        public static ConnectionStatus Status
        {
            get { return status; }
            set
            {
                if (status != value && StatusChanged != null)
                {
                    status = value;
                    StatusChanged.Invoke(status);
                    return;
                }
                status = value;
            }
        }

        public static bool IsActive => status is ConnectionStatus.Connecting or ConnectionStatus.Connected;

        /// <summary>
        ///     True if we are connected to another socket
        /// </summary>
        public static bool IsConnected { get; private set; }

        /// <summary>
        ///     True if we are trying to connect to another socket
        /// </summary>
        public static bool IsConnecting { get; private set; }

        /// <summary>
        ///     IP Address of the connection
        /// </summary>
        public static string ConnectionIP { get; private set; }

        /// <summary>
        ///     Port of the connection
        /// </summary>
        public static int ConnectionPort { get; private set; }
        #endregion

        #region Event Handling
        /// <summary>
        ///     Event is invoked when we successfully connect to another socket.
        /// </summary>
        public static event Action Connected;

        /// <summary>
        ///     Event is invoked when we are disconnected from another socket.
        /// </summary>
        public static event Action Disconnected;

        /// <summary>
        ///     Event is invoked when the connection status changes.
        /// </summary>
        public static event Action<ConnectionStatus> StatusChanged;

        private static Func<int, UDPTransport.Source, Peer> PeerCreator;
        #endregion

        #region - Unity Methods and Initialization -

        public static void Initialize(ClientConfig config, Func<int, UDPTransport.Source, Peer> peerCreator, params ClientModule[] startingModules)
        {
            isInitialized = false;

            _config = config;            
            isSimulated = _config.IsSimulated;

            PeerCreator = peerCreator;
            Connected += OnConnected;
            Disconnected += OnDisconnected;

            foreach (var mod in startingModules)
            {
                AddModule(mod);
            }
            InitializeModules();

            _InitTimeInterpolation();

            UDPTransport.OnClientConnected = HandleConnect;
            UDPTransport.OnClientDataReceived = HandleData;
            UDPTransport.OnClientDisconnected = HandleDisconnect;
            UDPTransport.OnClientError = HandleTransportError;

            RegisterHandler<NetworkPongMessage>(NetTime.OnClientPong, false);
            RegisterHandler<TimeSnapshotMessage>(OnTimeSnapshotMessage, false);


            UDPTransport.Init(false, true, isSimulated);
            _lastSendTime = 0;
            isInitialized = true;
            if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Client Initialized"); }

            static void _InitTimeInterpolation()
            {
                // reset timeline, localTimescale & snapshots from last session (if any)
                // Don't reset bufferTimeMultiplier here - whatever their network condition
                // was when they disconnected, it won't have changed on immediate reconnect.
                LocalTimeline = 0;
                localTimescale = 1;
                snapshots.Clear();

                // initialize EMA with 'emaDuration' seconds worth of history.
                // 1 second holds 'sendRate' worth of values.
                // multiplied by emaDuration gives n-seconds.
                driftEma = new ExponentialMovingAverage(_config.ServerUpdateRate * _config.SnapshotSettings.driftEmaDuration);
                deliveryTimeEma = new ExponentialMovingAverage(_config.ServerUpdateRate * _config.SnapshotSettings.deliveryTimeEmaDuration);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Shutdown()
        {
            if (isInitialized)
            {
                Disconnect();
            }

            UDPTransport.OnClientConnected = null;
            UDPTransport.OnClientDataReceived = null;
            UDPTransport.OnClientDisconnected = null;
            UDPTransport.OnClientError = null;

            connectionID = -1;
            stopConnectingTime = 0;
            isAttemptingReconnect = false;

            status = ConnectionStatus.None;
            Connected = null;
            Disconnected = null;
            StatusChanged = null;
            PeerCreator = null;

            _config = null;
            isInitialized = false;
            isSimulated = false;
            IsConnected = false;
            IsConnecting = false;
            ConnectionIP = string.Empty;
            ConnectionPort = 0;
            _lastSendTime = 0;

            LocalTimeline = 0;
            snapshots.Clear();
            localTimescale = 1f;
            dynamicAdjustment = true;
            dynamicAdjustmentTolerance = 1f;
            deliveryTimeEmaDuration = 2;

            ServerPeer = null;
            modules.Clear();
            initializedModules.Clear();
            handlers.Clear();


            if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Client Shutdown"); }
        }

        internal static void NetworkEarlyUpdate()
        {
            // process all incoming messages first before updating the world
            if (isInitialized)
            {
                if (IsConnecting && !IsConnected)
                {
                    // Attempt Connection
                    if (Time.time > stopConnectingTime)
                    {
                        StopConnecting(true);
                        return;
                    }
                    Status = ConnectionStatus.Connecting;
                }
            }

            UDPTransport.ClientEarlyUpdate();
            UpdateTimeInterpolation();

            static void UpdateTimeInterpolation()
            {
                // only while we have snapshots.
                // timeline starts when the first snapshot arrives.
                if (snapshots.Count > 0)
                {
                    // progress local timeline.
                    // NetworkTime uses unscaled time and ignores Time.timeScale.
                    // fixes Time.timeScale getting server & client time out of sync:
                    // https://github.com/MirrorNetworking/Mirror/issues/3409
                    SnapshotInterpolation.StepTime(Time.unscaledDeltaTime, ref LocalTimeline, localTimescale);

                    // progress local interpolation.
                    // TimeSnapshot doesn't interpolate anything.
                    // this is merely to keep removing older snapshots.
                    SnapshotInterpolation.StepInterpolation(snapshots, LocalTimeline, out _, out _, out double t);
                }
            }
        }

        internal static void NetworkLateUpdate()
        {
            if (isInitialized && isSimulated)
            {
                while (UDPTransport.ReceiveSimulatedMessage(UDPTransport.Source.Client, out int connID, out UDPTransport.TransportEvent transportEvent, out ArraySegment<byte> data))
                {
                    switch (transportEvent)
                    {
                        case UDPTransport.TransportEvent.Connected:
                            HandleConnect();
                            break;
                        case UDPTransport.TransportEvent.Data:
                            HandleData(data, 1);
                            break;
                        case UDPTransport.TransportEvent.Disconnected:
                            HandleDisconnect();
                            break;
                    }
                }
            }

            if (IsActive)
            {
                if (!Application.isPlaying || AccurateInterval.Elapsed(Time.timeAsDouble, SendInterval, ref _lastSendTime))
                {
                    //Broadcast();
                    if (IsConnected)
                    {
                        ServerPeer.PreUpdate();
                        // update connection to flush out batched messages
                        ServerPeer.Update();
                    }
                }
            }

            if (IsConnected)
            {
                // update NetworkTime
                NetTime.UpdateClient();
            }

            UDPTransport.ClientLateUpdate();
        }
        #endregion

        #region - Module Methods -
        /// <summary>
        ///     Adds a network module to the manager.
        /// </summary>
        /// <param name="module"></param>
        private static void AddModule(ClientModule module)
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
        public static void AddModuleAndInitialize(ClientModule module)
        {
            AddModule(module);
            InitializeModules();
        }

        /// <summary>
        ///     Checks if the maanger has the module.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public static bool HasModule(ClientModule module)
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

                    // Not all dependencies have been initialized. Wait until they are.
                    //if (!mod.Value.Dependencies.All(d => initializedModules.Any(d.IsAssignableFrom))) { continue; }

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
        public static T GetModule<T>() where T : ClientModule
        {
            modules.TryGetValue(typeof(T), out ClientModule module);
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
        private static List<ClientModule> GetInitializedModules()
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
        private static List<ClientModule> GetUninitializedModules()
        {
            return modules
                .Where(m => !initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }
        #endregion

        #region - Connection Methods -
        /// <summary>
        ///     Starts connecting to another socket. Default timeout of 10s.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static void Connect(string ip, int port)
        {
            if (isInitialized)
            {
                Connect(ip, port, 10);
            }
            else
            {
                Debug.LogError("Client must be initialized before connecting");
            }
        }
        /// <summary>
        ///     Starts connecting to another socket with a specified timeout.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="timeout">Milliseconds</param>
        /// <returns></returns>
        private static void Connect(string ip, int port, int timeout)
        {
            connectionID = 1;
            stopConnectingTime = Time.time + timeout;
            ConnectionIP = ip;
            ConnectionPort = port;


            IsConnecting = true;
            if (isSimulated)
            {
                UDPTransport.SimulatedConnect(connectionID);
                //HandleConnect();
            }
            else
            {
                UDPTransport.Connect(ip);
            }
        }

        /// <summary>
        ///     Disconnect the <see cref="ClientSocket"/> from the <see cref="ServerSocket"/>.
        /// </summary>
        public static void Disconnect()
        {
            if (isSimulated)
            {
                UDPTransport.SimulateDisconnect(connectionID);
            }
            else
            {
                UDPTransport.Disconnect();
            }

            //HandleDisconnect();
        }

        /// <summary>
        ///     Disconnects and attempts connecting again.
        /// </summary>
        public static void Reconnect()
        {
            isAttemptingReconnect = true;
            Disconnect();
            Connect(ConnectionIP, ConnectionPort);
        }

        /// <summary>
        ///     Stops trying to connect to the socket
        /// </summary>
        private static void StopConnecting(bool timedOut = false)
        {
            IsConnecting = false;
            Status = ConnectionStatus.Disconnected;
            if (timedOut && retryOnTimeout)
            {
                if (NetLogFilter.LogInfo) { Debug.LogFormat("{2} Retrying to connect to server at || {0}:{1}", _config.GameServerIp, _config.GameServerPort, TAG); }
                Connect(_config.GameServerIp, _config.GameServerPort);
            }
        }

        private static void HandleConnect()
        {
            IsConnecting = false;
            IsConnected = true;

            NetTime.ResetStatics();

            Debug.Log($"{TAG} Connected");

            Status = ConnectionStatus.Connected;
            NetTime.UpdateClient();

            ServerPeer = PeerCreator.Invoke(connectionID, UDPTransport.Source.Client);

            Connected?.Invoke();
        }

        public static Peer GeneratePeer(int connectionID, UDPTransport.Source source)
        {
            return new Peer(connectionID, source)
            {
                IsConnected = true
            };
        }

        private static void HandleDisconnect()
        {
            Status = ConnectionStatus.Disconnected;
            IsConnected = false;
            connectionID = -1;

            if (ServerPeer != null)
            {
                ServerPeer.Dispose();
                ServerPeer.IsConnected = false;
            }
            Disconnected?.Invoke();
        }

        private static void OnDisconnected()
        {
            if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Disconnected from || {_config.GameServerIp}:{_config.GameServerPort}"); }

            if (!isAttemptingReconnect)
            {

            }
            isAttemptingReconnect = false;
        }

        private static void OnConnected()
        {
            if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Connected to || {_config.GameServerIp}:{_config.GameServerPort}"); }
        }
        #endregion

        #region - Handle Message Methods -
        private static void HandleTransportError(TransportError error, string reason)
        {
            Debug.LogWarning($"{TAG} Client Transport Error: {error}: {reason}.");
        }

        private static void HandleData(ArraySegment<byte> buffer, int channelID)
        {
            if (ServerPeer == null) { return; }

            if (!ServerPeer.Unbatcher.AddBatch(buffer))
            {
                Debug.Log($"{WARNING} {TAG} Failed to add batch, disconnecting.");
                Disconnect();
                return;
            }

            while (ServerPeer.Unbatcher.GetNextMessage(out var reader, out var remoteTimestamp))
            {
                if (reader.Remaining >= NetworkMessages.IdSize)
                {
                    ServerPeer.RemoteTimestamp = remoteTimestamp;
                    if (!PeerMessageReceived(reader, channelID))
                    {
                        Debug.Log($"{WARNING} {TAG} Failed to unpack and invoke message. Disconnecting.");
                        Disconnect();
                        return;
                    }
                }
                else
                {
                    // WARNING, not error. can happen if attacker sends random data.
                    Debug.Log($"{WARNING} {TAG} Received Message was too short (messages should start with message id)");
                    Disconnect();
                    return;
                }
            }

            if (ServerPeer.Unbatcher.BatchesCount > 0)
            {
                Debug.LogError($"Still had {ServerPeer.Unbatcher.BatchesCount} batches remaining after processing, even though processing was not interrupted by a scene change. This should never happen, as it would cause ever growing batches.\nPossible reasons:\n* A message didn't deserialize as much as it serialized\n*There was no message handler for a message id, so the reader wasn't read until the end.");
            }
        }

        /// <summary>
        ///     Called after the peer parses the message. Only assigned to the local player. Use <see cref="IncomingMessage.Sender"/> to determine what peer sent the message.
        /// </summary>
        /// <param name="msg"></param>
        private static bool PeerMessageReceived(NetworkReader reader, int channelId)
        {
            if (NetworkMessages.UnpackId(reader, out ushort opCode))
            {
                // try to invoke the handler for that message
                if (handlers.TryGetValue(opCode, out var handler))
                {
                    handler.Handle(ServerPeer, reader, channelId);

                    // message handler may disconnect client, making connection = null
                    // therefore must check for null to avoid NRE.
                    if (ServerPeer != null)
                    {
                        ServerPeer.LastMessageTime = Time.time;
                    }

                    return true;
                }
                else
                {
                    // => WARNING, not error. can happen if attacker sends random data.
                    Debug.Log($"{WARNING} {TAG} Unknown message id: {opCode}. This can happen if no handler was registered for this message.");
                    return false;
                }
            }
            else
            {
                // => WARNING, not error. can happen if attacker sends random data.
                Debug.Log($"{WARNING} {TAG} Invalid message header.");
                return false;
            }
        }

        /// <summary>
        ///     Register a method handler
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="handlerMethod"></param>
        /// <returns></returns>
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
        /// <summary>
        ///     Sends a message to server.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="qos"></param>
        public static void Send<T>(T message, int channelId = Channels.Reliable) where T : struct, INetMessage
        {
            if (!IsConnected) { return; }

            using var w = NetworkWriterPool.Get();
            NetworkMessages.Pack(message, w);
            ArraySegment<byte> segment = w.ToArraySegment();
            if (!isSimulated)
            {
                ServerPeer.SendMessage(segment, channelId);
            }
            else
            {
                ServerPeer.SendSimulatedMessage(segment);
            }
            NetDiagnostics.OnSend(message, channelId, w.Position, 1);
        }

        public static void RegisterResponse<T>(int timeout = 5) where T : struct, INetMessage
        {
            ServerPeer.RegisterResponse(NetworkMessageId<T>.Id, timeout);
        }

        public static void TimeoutResponse(ushort opCode)
        {
            if (handlers.TryGetValue(opCode, out var handler))
            {
                handler.TimeoutResponse(ServerPeer);
            }
        }
        #endregion

        #region - Snapshots -
        // server sends TimeSnapshotMessage every sendInterval.
        // batching already includes the remoteTimestamp.
        // we simply insert it on-message here.
        // => only for reliable channel. unreliable would always arrive earlier.
        private static void OnTimeSnapshotMessage(INetConnection conn, TimeSnapshotMessage _)
        {
            // insert another snapshot for snapshot interpolation.
            // before calling OnDeserialize so components can use
            // NetworkTime.time and NetworkTime.timeStamp.

            // Unity 2019 doesn't have Time.timeAsDouble yet
            OnTimeSnapshot(new TimeSnapshot(ServerPeer.RemoteTimestamp, Time.timeAsDouble));
        }

        // see comments at the top of this file
        public static void OnTimeSnapshot(TimeSnapshot snap)
        {
            // (optional) dynamic adjustment
            if (_config.SnapshotSettings.dynamicAdjustment)
            {
                // set bufferTime on the fly.
                // shows in inspector for easier debugging :)
                _config.SnapshotSettings.bufferTimeMultiplier = SnapshotInterpolation.DynamicAdjustment(
                    _config.ServerUpdateRate,
                    deliveryTimeEma.StandardDeviation,
                    _config.SnapshotSettings.dynamicAdjustmentTolerance
                );
            }

            // insert into the buffer & initialize / adjust / catchup
            SnapshotInterpolation.InsertAndAdjust(
                snapshots,
                snap,
                ref LocalTimeline,
                ref localTimescale,
                _config.ServerUpdateRate,
                BufferTime,
                _config.SnapshotSettings.catchupSpeed,
                _config.SnapshotSettings.slowdownSpeed,
                ref driftEma,
                _config.SnapshotSettings.catchupNegativeThreshold,
                _config.SnapshotSettings.catchupPositiveThreshold,
                ref deliveryTimeEma);

            // Debug.Log($"inserted TimeSnapshot remote={snap.remoteTime:F2} local={snap.localTime:F2} total={snapshots.Count}");
        }
        #endregion
    }
}