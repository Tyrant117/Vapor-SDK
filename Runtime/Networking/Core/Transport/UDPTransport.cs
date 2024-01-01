using kcp2k;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

namespace VaporNetcode
{
    public static class Channels
    {
        public const int Reliable = 0;
        public const int Unreliable = 1;
    }

    public enum TransportError : byte
    {
        DnsResolve,       // failed to resolve a host name
        Refused,          // connection refused by other end. server full etc.
        Timeout,          // ping timeout or dead link
        Congestion,       // more messages than transport / network can process
        InvalidReceive,   // recv invalid packet (possibly intentional attack)
        InvalidSend,      // user tried to send invalid data
        ConnectionClosed, // connection closed voluntarily or lost involuntarily
        Unexpected        // unexpected error / exception, requires fix.
    }

    public class UDPTransport
    {
        public const string TAG = "<color=purple><b>[Transport]</b></color>";

        public enum TransportEvent { Connected, Data, Disconnected }
        public enum Source { Default = 0, Client = 1, Server = 2 }

        // scheme used by this transport
        public const string Scheme = "kcp";

        // common
        [Header("Transport Configuration")]
        public static ushort port = 7777;
        public static ushort Port { get => port; set => port = value; }
        [Tooltip("DualMode listens to IPv6 and IPv4 simultaneously. Disable if the platform only supports IPv4.")]
        public static bool DualMode = true;
        [Tooltip("NoDelay is recommended to reduce latency. This also scales better without buffers getting full.")]
        public static bool NoDelay = true;
        [Tooltip("KCP internal update interval. 100ms is KCP default, but a lower interval is recommended to minimize latency and to scale to more networked entities.")]
        public static uint Interval = 10;
        [Tooltip("KCP timeout in milliseconds. Note that KCP sends a ping automatically.")]
        public static int Timeout = 10000;
        [Tooltip("Socket receive buffer size. Large buffer helps support more connections. Increase operating system socket buffer size limits if needed.")]
        public static int RecvBufferSize = 1024 * 1027 * 7;
        [Tooltip("Socket send buffer size. Large buffer helps support more connections. Increase operating system socket buffer size limits if needed.")]
        public static int SendBufferSize = 1024 * 1027 * 7;

        [Header("Advanced")]
        [Tooltip("KCP fastresend parameter. Faster resend for the cost of higher bandwidth. 0 in normal mode, 2 in turbo mode.")]
        public static int FastResend = 2;
        [Tooltip("KCP congestion window. Restricts window size to reduce congestion. Results in only 2-3 MTU messages per Flush even on loopback. Best to keept his disabled.")]
        public static bool CongestionWindow = false; // KCP 'NoCongestionWindow' is false by default. here we negate it for ease of use.
        [Tooltip("KCP window size can be modified to support higher loads. This also increases max message size.")]
        public static uint ReceiveWindowSize = 4096; //Kcp.WND_RCV; 128 by default. Mirror sends a lot, so we need a lot more.
        [Tooltip("KCP window size can be modified to support higher loads.")]
        public static uint SendWindowSize = 4096; //Kcp.WND_SND; 32 by default. Mirror sends a lot, so we need a lot more.
        [Tooltip("KCP will try to retransmit lost messages up to MaxRetransmit (aka dead_link) before disconnecting.")]
        public static uint MaxRetransmit = Kcp.DEADLINK * 2; // default prematurely disconnects a lot of people (#3022). use 2x.
        [Tooltip("Enable to automatically set client & server send/recv buffers to OS limit. Avoids issues with too small buffers under heavy load, potentially dropping connections. Increase the OS limit if this is still too small.")]
        public static bool MaximizeSocketBuffers = true;

        [Header("Allowed Max Message Sizes\nBased on Receive Window Size")]
        [Tooltip("KCP reliable max message size shown for convenience. Can be changed via ReceiveWindowSize.")]
        public static int ReliableMaxMessageSize = 0; // readonly, displayed from OnValidate
        [Tooltip("KCP unreliable channel max message size for convenience. Not changeable.")]
        public static int UnreliableMaxMessageSize = 0; // readonly, displayed from OnValidate

        // use default MTU for this transport.
        const int MTU = Kcp.MTU_DEF;

        private static KcpConfig _serverConfig;
        private static KcpConfig _clientConfig;

        // server & client
        private static KcpServer server;
        private static KcpClient client;

        private static bool IsServer;
        private static bool IsClient;

        private static bool IsSimulated;
        private static readonly ConcurrentQueue<SimulatedMessage> simulatedServerQueue = new();
        private static readonly ConcurrentQueue<SimulatedMessage> simulatedClientQueue = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            _serverConfig = null;
            _clientConfig = null;
            server = null;
            client = null;
            IsServer = false;
            IsClient = false;
            IsSimulated = false;
            simulatedServerQueue.Clear();
            simulatedClientQueue.Clear();

            //OnClientConnected = () => Debug.LogWarning("OnClientConnected called with no handler");
            //OnClientDataReceived = (data, channel) => Debug.LogWarning("OnClientDataReceived called with no handler");
            //OnClientError = (error, msg) => Debug.LogWarning("OnClientError called with no handler");
            //OnClientDisconnected = () => Debug.LogWarning("OnClientDisconnected called with no handler");

            //OnServerConnected = (connId) => Debug.LogWarning("OnServerConnected called with no handler");
            //OnServerDataReceived = (connId, data, channel) => Debug.LogWarning("OnServerDataReceived called with no handler");
            //OnServerError = (connId, error, msg) => Debug.LogWarning("OnServerError called with no handler");
            //OnServerDisconnected = (connId) => Debug.LogWarning("OnServerDisconnected called with no handler");
        }

        // translate Kcp <-> Mirror channels
        public static int FromKcpChannel(KcpChannel channel) =>
            channel == KcpChannel.Reliable ? Channels.Reliable : Channels.Unreliable;

        public static KcpChannel ToKcpChannel(int channel) =>
            channel == Channels.Reliable ? KcpChannel.Reliable : KcpChannel.Unreliable;

        public static TransportError ToTransportError(ErrorCode error)
        {
            switch (error)
            {
                case ErrorCode.DnsResolve: return TransportError.DnsResolve;
                case ErrorCode.Timeout: return TransportError.Timeout;
                case ErrorCode.Congestion: return TransportError.Congestion;
                case ErrorCode.InvalidReceive: return TransportError.InvalidReceive;
                case ErrorCode.InvalidSend: return TransportError.InvalidSend;
                case ErrorCode.ConnectionClosed: return TransportError.ConnectionClosed;
                case ErrorCode.Unexpected: return TransportError.Unexpected;
                default: throw new InvalidCastException($"KCP: missing error translation for {error}");
            }
        }

        public static void Init(bool isServer = false, bool isClient = false, bool isSimulated = false)
        {
            IsServer = isServer || IsServer;
            IsClient = isClient || IsClient;
            IsSimulated = isSimulated || IsSimulated;

            // create config from serialized settings
            simulatedClientQueue.Clear();
            simulatedServerQueue.Clear();

            if (isSimulated)
            {
                if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Transport Layer Initialized: Simulation"); }
                return;
            }

            if (IsServer && server == null)
            {
                _serverConfig = new KcpConfig(DualMode, RecvBufferSize, SendBufferSize, MTU, NoDelay, Interval, FastResend, CongestionWindow, SendWindowSize, ReceiveWindowSize, Timeout, MaxRetransmit);
                server = new KcpServer(
                (connectionId) => OnServerConnected.Invoke(connectionId),
                (connectionId, message, channel) => OnServerDataReceived.Invoke(connectionId, message, (int)FromKcpChannel(channel)),
                (connectionId) => OnServerDisconnected.Invoke(connectionId),
                (connectionId, error, reason) => OnServerError.Invoke(connectionId, ToTransportError(error), reason),
                _serverConfig);

                if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Transport Layer Initialized: Server"); }
            }

            if (IsClient && client == null)
            {
                _clientConfig = new KcpConfig(DualMode, RecvBufferSize, SendBufferSize, MTU, NoDelay, Interval, FastResend, CongestionWindow, SendWindowSize, ReceiveWindowSize, Timeout, MaxRetransmit);
                client = new KcpClient(
                () => OnClientConnected.Invoke(),
                (message, channel) => OnClientDataReceived.Invoke(message, (int)FromKcpChannel(channel)),
                () => OnClientDisconnected.Invoke(),
                (error, reason) => OnClientError.Invoke(ToTransportError(error), reason),
                _clientConfig);

                if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Transport Layer Initialized: Client"); }
            }
        }

        public static bool Send(int connectionID, ArraySegment<byte> segment, Source source, int channelId = Channels.Reliable)
        {
            if (source == Source.Default) { return false; }

            if (source == Source.Server && IsServer)
            {
                server.Send(connectionID, segment, ToKcpChannel(channelId));

                // call event. might be null if no statistics are listening etc.
                OnServerDataSent?.Invoke(connectionID, segment, (int)channelId);
                return true;
            }

            if (source == Source.Client && IsClient)
            {
                /*client*/
                client.Send(segment, ToKcpChannel(channelId));

                // call event. might be null if no statistics are listening etc.
                OnClientDataSent?.Invoke(segment, (int)channelId);
                return true;
            }

            return false;
        }

        public static bool SendSimulated(int connectionID, ArraySegment<byte> slice, Source source)
        {
            byte[] data = new byte[slice.Count];
            Array.Copy(slice.Array, slice.Offset, data, 0, slice.Count);

            if (source == Source.Server)
            {
                simulatedClientQueue.Enqueue(new SimulatedMessage(connectionID, SimulatedEventType.Data, data));
            }
            if (source == Source.Client)
            {
                simulatedServerQueue.Enqueue(new SimulatedMessage(connectionID, SimulatedEventType.Data, data));
            }
            return true;
        }

        public static bool ReceiveSimulatedMessage(Source source, out int connectionID, out TransportEvent transportEvent, out ArraySegment<byte> data)
        {
            if (source == Source.Server)
            {
                if (simulatedServerQueue.TryDequeue(out SimulatedMessage message))
                {
                    // convert Telepathy EventType to TransportEvent
                    transportEvent = (TransportEvent)message.eventType;

                    // assign rest of the values and return true
                    connectionID = message.connectionID;
                    data = message.data != null ? new ArraySegment<byte>(message.data) : default;
                    return true;
                }
            }

            if (source == Source.Client)
            {
                if (simulatedClientQueue.TryDequeue(out SimulatedMessage message))
                {
                    // convert Telepathy EventType to TransportEvent
                    transportEvent = (TransportEvent)message.eventType;

                    // assign rest of the values and return true
                    connectionID = -1;
                    data = message.data != null ? new ArraySegment<byte>(message.data) : default;
                    return true;
                }
            }

            connectionID = -1;
            transportEvent = TransportEvent.Data;
            data = default;
            return false;
        }

        #region - Client -
        /// <summary>
        /// Notify subscribers when when this client establish a successful connection to the server
        /// <para>callback()</para>
        /// </summary>
        public static Action OnClientConnected;

        /// <summary>
        /// Notify subscribers when this client receive data from the server
        /// <para>callback(ArraySegment&lt;byte&gt; data, int channel)</para>
        /// </summary>
        public static Action<ArraySegment<byte>, int> OnClientDataReceived;

        /// <summary>Called by Transport when the client sent a message to the server.</summary>
        // Transports are responsible for calling it because:
        // - groups it together with OnReceived responsibility
        // - allows transports to decide if anything was sent or not
        // - allows transports to decide the actual used channel (i.e. tcp always sending reliable)
        public static Action<ArraySegment<byte>, int> OnClientDataSent;

        /// <summary>Called by Transport when the client encountered an error.</summary>
        public static Action<TransportError, string> OnClientError;

        /// <summary>
        /// Notify subscribers when this client disconnects from the server
        /// <para>callback()</para>
        /// </summary>
        public static Action OnClientDisconnected;

        public bool Connected() => client.connected;
        public static void Connect(string address)
        {
            client.Connect(address, Port);
        }
        public static void Connect(Uri uri)
        {
            if (uri.Scheme != Scheme)
            {
                throw new ArgumentException($"Invalid url {uri}, use {Scheme}://host:port instead", nameof(uri));
            }

            int serverPort = uri.IsDefaultPort ? Port : uri.Port;
            client.Connect(uri.Host, (ushort)serverPort);
        }

        public static void Disconnect() => client.Disconnect();
        // process incoming in early update
        public static void ClientEarlyUpdate()
        {
            if (IsSimulated) { return; }
            if (IsClient)
            {
                /*client*/
                client.TickIncoming();
            }
        }
        // process outgoing in late update
        public static void ClientLateUpdate()
        {
            if (IsSimulated) { return; }
            if (IsClient)
            {
                /*client*/
                client.TickOutgoing();
            }
        }

        public static void SimulatedConnect(int connectionID) => simulatedServerQueue.Enqueue(new SimulatedMessage(connectionID, SimulatedEventType.Connected, default));
        public static void SimulateDisconnect(int connectionID) => simulatedServerQueue.Enqueue(new SimulatedMessage(connectionID, SimulatedEventType.Disconnected, default));
        #endregion

        #region - Server -
        /// <summary>
        /// Notify subscribers when a client connects to this server
        /// <para>callback(int connId)</para>
        /// </summary>
        public static Action<int> OnServerConnected;

        /// <summary>
        /// Notify subscribers when this server receives data from the client
        /// <para>callback(int connId, ArraySegment&lt;byte&gt; data, int channel)</para>
        /// </summary>
        public static Action<int, ArraySegment<byte>, int> OnServerDataReceived;

        /// <summary>Called by Transport when the server sent a message to a client.</summary>
        // Transports are responsible for calling it because:
        // - groups it together with OnReceived responsibility
        // - allows transports to decide if anything was sent or not
        // - allows transports to decide the actual used channel (i.e. tcp always sending reliable)
        public static Action<int, ArraySegment<byte>, int> OnServerDataSent;

        /// <summary>Called by Transport when a server's connection encountered a problem.</summary>
        /// If a Disconnect will also be raised, raise the Error first.
        public static Action<int, TransportError, string> OnServerError;

        /// <summary>
        /// Notify subscribers when a client disconnects from this server
        /// <para>callback(int connId)</para>
        /// </summary>
        public static Action<int> OnServerDisconnected;

        public static Uri ServerUri()
        {
            UriBuilder builder = new()
            {
                Scheme = Scheme,
                Host = Dns.GetHostName(),
                Port = Port
            };
            return builder.Uri;
        }
        public static bool Active => !IsSimulated && server.IsActive();
        public static void StartServer() => server.Start(Port);
        public static void StopServer()
        {
            if (NetLogFilter.LogInfo) { Debug.Log($"{TAG} Server Stopped"); }
            server?.Stop();
        }
        public static void DisconnectPeer(int connectionId) => server.Disconnect(connectionId);
        public static string ServerGetClientAddress(int connectionId)
        {
            IPEndPoint endPoint = server.GetClientEndPoint(connectionId);
            return endPoint != null
                // Map to IPv4 if "IsIPv4MappedToIPv6"
                // "::ffff:127.0.0.1" -> "127.0.0.1"
                ? (endPoint.Address.IsIPv4MappedToIPv6
                ? endPoint.Address.MapToIPv4().ToString()
                : endPoint.Address.ToString())
                : "";
        }
        public static void ServerEarlyUpdate()
        {
            if (IsSimulated) { return; }
            if (IsServer)
            {
                server.TickIncoming();
            }
        }
        public static void ServerLateUpdate()
        {
            if (IsSimulated) { return; }
            if (IsServer)
            {
                server.TickOutgoing();
            }
        }
        #endregion

        #region - Helper -
        // max message size
        public static int GetMaxPacketSize(int channelId = 1)
        {
            // switch to kcp channel.
            // unreliable or reliable.
            // default to reliable just to be sure.
            return channelId switch
            {
                Channels.Unreliable => KcpPeer.UnreliableMaxMessageSize(Kcp.MTU_DEF),
                _ => KcpPeer.ReliableMaxMessageSize(Kcp.MTU_DEF, ReceiveWindowSize),
            };
        }

        // kcp reliable channel max packet size is MTU * WND_RCV
        // this allows 144kb messages. but due to head of line blocking, all
        // other messages would have to wait until the maxed size one is
        // delivered. batching 144kb messages each time would be EXTREMELY slow
        // and fill the send queue nearly immediately when using it over the
        // network.
        // => instead we always use MTU sized batches.
        // => people can still send maxed size if needed.
        public static int GetBatchThreshold(int channelId) => KcpPeer.UnreliableMaxMessageSize(Kcp.MTU_DEF);

        // Server Statistics
        // server statistics
        // LONG to avoid int overflows with connections.Sum.
        // see also: https://github.com/vis2k/Mirror/pull/2777
        public static long GetAverageMaxSendRate() => server.connections.Count > 0
                ? server.connections.Values.Sum(conn => conn.peer.MaxSendRate) / server.connections.Count
                : 0;
        public static long GetAverageMaxReceiveRate() => server.connections.Count > 0
                ? server.connections.Values.Sum(conn => conn.peer.MaxReceiveRate) / server.connections.Count
                : 0;
        static long GetTotalSendQueue() => server.connections.Values.Sum(conn => conn.peer.SendQueueCount);
        static long GetTotalReceiveQueue() => server.connections.Values.Sum(conn => conn.peer.ReceiveQueueCount);
        static long GetTotalSendBuffer() => server.connections.Values.Sum(conn => conn.peer.SendBufferCount);
        static long GetTotalReceiveBuffer() => server.connections.Values.Sum(conn => conn.peer.ReceiveBufferCount);

        // PrettyBytes function from DOTSNET
        // pretty prints bytes as KB/MB/GB/etc.
        // long to support > 2GB
        // divides by floats to return "2.5MB" etc.
        public static string PrettyBytes(long bytes)
        {
            // bytes
            if (bytes < 1024)
                return $"{bytes} B";
            // kilobytes
            else if (bytes < 1024L * 1024L)
                return $"{(bytes / 1024f):F2} KB";
            // megabytes
            else if (bytes < 1024 * 1024L * 1024L)
                return $"{(bytes / (1024f * 1024f)):F2} MB";
            // gigabytes
            return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
        }
        #endregion
    }
}