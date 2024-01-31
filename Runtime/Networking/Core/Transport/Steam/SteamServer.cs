using Steamworks;
using System;
using UnityEngine;

namespace VaporNetcode.Transport.Steam
{
    public class SteamServer : Common, IServer
    {
        private event Action<int> OnConnected;
        private event Action<int, byte[], int> OnReceivedData;
        private event Action<int> OnDisconnected;
        private event Action<int, TransportError, string> OnReceivedError;

        private readonly BidirectionalDictionary<HSteamNetConnection, int> connToId;
        private readonly BidirectionalDictionary<CSteamID, int> steamIDToId;
        private readonly int maxConnections;
        private int nextConnectionID;

        private HSteamListenSocket listenSocket;
        private Callback<SteamNetConnectionStatusChangedCallback_t> c_onConnectionChange = null;

        public SteamServer(int maxConnections)
        {
            this.maxConnections = maxConnections;
            connToId = new BidirectionalDictionary<HSteamNetConnection, int>();
            nextConnectionID = 1;
            c_onConnectionChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
        }

        public static SteamServer CreateServer(SteamTransport transport, int maxConnections)
        {
            SteamServer s = new(maxConnections);

            s.OnConnected += (id) => transport.OnServerConnected.Invoke(id);
            s.OnDisconnected += (id) => transport.OnServerDisconnected.Invoke(id);
            s.OnReceivedData += (id, data, ch) => transport.OnServerDataReceived.Invoke(id, new ArraySegment<byte>(data), ch);
            s.OnReceivedError += (id, error, reason) => transport.OnServerError.Invoke(id, error, reason);

            try
            {
#if UNITY_SERVER
                SteamGameServerNetworkingUtils.InitRelayNetworkAccess();
#else
                SteamNetworkingUtils.InitRelayNetworkAccess();
#endif
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            s.Host();

            return s;
        }

        private void Host()
        {
            SteamNetworkingConfigValue_t[] options = new SteamNetworkingConfigValue_t[] { };
#if UNITY_SERVER
            listenSocket = SteamGameServerNetworkingSockets.CreateListenSocketP2P(0, options.Length, options);
#else
            listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, options.Length, options);
#endif
        }

        public void Shutdown()
        {
#if UNITY_SERVER
            SteamGameServerNetworkingSockets.CloseListenSocket(listenSocket);
#else
            SteamNetworkingSockets.CloseListenSocket(listenSocket);
#endif

            if (c_onConnectionChange != null)
            {
                c_onConnectionChange.Dispose();
                c_onConnectionChange = null;
            }
        }

        #region - Connection -
        private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
        {
            ulong clientSteamID = param.m_info.m_identityRemote.GetSteamID64();
            if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
            {
                if (connToId.Count >= maxConnections)
                {
                    Debug.Log($"Incoming connection {clientSteamID} would exceed max connection count. Rejecting.");
#if UNITY_SERVER
                    SteamGameServerNetworkingSockets.CloseConnection(param.m_hConn, 0, "Max Connection Count", false);
#else
                    SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, "Max Connection Count", false);
#endif
                    return;
                }

                EResult res;

#if UNITY_SERVER
                if ((res = SteamGameServerNetworkingSockets.AcceptConnection(param.m_hConn)) == EResult.k_EResultOK)
#else
                if ((res = SteamNetworkingSockets.AcceptConnection(param.m_hConn)) == EResult.k_EResultOK)
#endif
                {
                    Debug.Log($"Accepting connection {clientSteamID}");
                }
                else
                {
                    Debug.Log($"Connection {clientSteamID} could not be accepted: {res.ToString()}");
                }
            }
            else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
            {
                int connectionId = nextConnectionID++;
                connToId.Add(param.m_hConn, connectionId);
                steamIDToId.Add(param.m_info.m_identityRemote.GetSteamID(), connectionId);
                OnConnected.Invoke(connectionId);
                Debug.Log($"Client with SteamID {clientSteamID} connected. Assigning connection id {connectionId}");
            }
            else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer || param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
            {
                if (connToId.TryGetValue(param.m_hConn, out int connId))
                {
                    InternalDisconnect(connId, param.m_hConn);
                }
            }
            else
            {
                Debug.Log($"Connection {clientSteamID} state changed: {param.m_info.m_eState.ToString()}");
            }
        }

        public void Disconnect(int connectionId)
        {
            if (connToId.TryGetValue(connectionId, out HSteamNetConnection conn))
            {
                Debug.Log($"Connection id {connectionId} disconnected.");
#if UNITY_SERVER
                SteamGameServerNetworkingSockets.CloseConnection(conn, 0, "Disconnected by server", false);
#else
                SteamNetworkingSockets.CloseConnection(conn, 0, "Disconnected by server", false);
#endif
                steamIDToId.Remove(connectionId);
                connToId.Remove(connectionId);
                OnDisconnected(connectionId);
            }
            else
            {
                Debug.LogWarning("Trying to disconnect unknown connection id: " + connectionId);
            }
        }

        private void InternalDisconnect(int connId, HSteamNetConnection socket)
        {
            OnDisconnected.Invoke(connId);
#if UNITY_SERVER
            SteamGameServerNetworkingSockets.CloseConnection(socket, 0, "Graceful disconnect", false);
#else
            SteamNetworkingSockets.CloseConnection(socket, 0, "Graceful disconnect", false);
#endif
            connToId.Remove(connId);
            steamIDToId.Remove(connId);
            Debug.Log($"Client with ConnectionID {connId} disconnected.");
        }
        #endregion

        #region - Transport -
        public void ReceiveData()
        {
            foreach (HSteamNetConnection conn in connToId.FirstTypes)
            {
                if (connToId.TryGetValue(conn, out int connId))
                {
                    IntPtr[] ptrs = new IntPtr[MAX_MESSAGES];
                    int messageCount;

#if UNITY_SERVER
                    if ((messageCount = SteamGameServerNetworkingSockets.ReceiveMessagesOnConnection(conn, ptrs, MAX_MESSAGES)) > 0)
#else
                    if ((messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, ptrs, MAX_MESSAGES)) > 0)
#endif
                    {
                        for (int i = 0; i < messageCount; i++)
                        {
                            (byte[] data, int ch) = ProcessMessage(ptrs[i]);
                            OnReceivedData(connId, data, ch);
                        }
                    }
                }
            }
        }

        public void FlushData()
        {
            foreach (HSteamNetConnection conn in connToId.FirstTypes)
            {
#if UNITY_SERVER
                SteamGameServerNetworkingSockets.FlushMessagesOnConnection(conn);
#else
                SteamNetworkingSockets.FlushMessagesOnConnection(conn);
#endif
            }
        }

        public void Send(int connectionId, byte[] data, int channelId)
        {
            if (connToId.TryGetValue(connectionId, out HSteamNetConnection conn))
            {
                EResult res = SendSocket(conn, data, channelId);

                if (res is EResult.k_EResultNoConnection or EResult.k_EResultInvalidParam)
                {
                    Debug.Log($"Connection to {connectionId} was lost.");
                    InternalDisconnect(connectionId, conn);
                }
                else if (res != EResult.k_EResultOK)
                {
                    Debug.LogError($"Could not send: {res.ToString()}");
                }
            }
            else
            {
                Debug.LogError("Trying to send on unknown connection: " + connectionId);
                OnReceivedError.Invoke(connectionId, TransportError.Unexpected, "ERROR Unknown Connection");
            }
        }
        #endregion

        #region - Helpers -
        public string ServerGetClientAddress(int connectionId)
        {
            if (steamIDToId.TryGetValue(connectionId, out CSteamID steamId))
            {
                return steamId.ToString();
            }
            else
            {
                Debug.LogError("Trying to get info on unknown connection: " + connectionId);
                OnReceivedError.Invoke(connectionId, TransportError.Unexpected, "ERROR Unknown Connection");
                return string.Empty;
            }
        }
        #endregion
    }
}
