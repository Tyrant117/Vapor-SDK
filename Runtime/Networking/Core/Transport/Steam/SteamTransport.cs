using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

namespace VaporNetcode.Transport.Steam
{
    public class SteamTransport : Transport
    {
        private const string STEAM_SCHEME = "steam";

        private static IClient client;
        private static IServer server;

        [SerializeField]
        public EP2PSend[] Channels = new EP2PSend[2] { EP2PSend.k_EP2PSendReliable, EP2PSend.k_EP2PSendUnreliableNoDelay };

        [Tooltip("Timeout for connecting in seconds.")]
        public int Timeout = 25;

        #region - Unity Methods -
        private void OnEnable()
        {
            Debug.Assert(Channels != null && Channels.Length > 0, "No channel configured for Steam Transport.");
            Invoke(nameof(InitRelayNetworkAccess), 1f);
        }

        private void OnDestroy()
        {
            Shutdown();
        }
        #endregion

        #region - Client -
        public bool ClientActive() => client != null;
        public override bool ClientConnected() => ClientActive() && client.Connected;

        public override void ClientConnect(string address)
        {
            try
            {
#if UNITY_SERVER
                SteamGameServerNetworkingUtils.InitRelayNetworkAccess();
#else
                SteamNetworkingUtils.InitRelayNetworkAccess();
#endif

                InitRelayNetworkAccess();

                if (ServerActive())
                {
                    Debug.LogError("Transport already running as server!");
                    return;
                }

                if (!ClientActive() || client.Error)
                {
                    Debug.Log($"Starting client [SteamSockets], target address {address}.");
                    client = SteamClient.CreateClient(this, address);
                }
                else
                {
                    Debug.LogError("Client already running!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception: " + ex.Message + ". Client could not be started.");
                OnClientDisconnected.Invoke();
            }
        }

        public override void ClientConnect(Uri uri)
        {
            ClientConnect(uri.Host);
        }

        public override void ClientDisconnect()
        {
            if (ClientActive())
            {
                Shutdown();
            }
        }

        public override void ClientSend(ArraySegment<byte> segment, int channelId = 0)
        {
            byte[] data = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);
            client.Send(data, channelId);
        }

        public override void ClientEarlyUpdate()
        {
            if (enabled)
            {
                client?.ReceiveData();
            }
        }

        public override void ClientLateUpdate()
        {
            if (enabled)
            {
                client?.FlushData();
            }
        }
        #endregion


        #region - Server -
        public override bool ServerActive() => server != null;
        public override string ServerGetClientAddress(int connectionId) => ServerActive() ? server.ServerGetClientAddress(connectionId) : string.Empty;

        public override void ServerStart()
        {
            try
            {
#if UNITY_SERVER
                SteamGameServerNetworkingUtils.InitRelayNetworkAccess();
#else
                SteamNetworkingUtils.InitRelayNetworkAccess();
#endif


                InitRelayNetworkAccess();

                if (ClientActive())
                {
                    Debug.LogError("Transport already running as client!");
                    return;
                }

                if (!ServerActive())
                {
                    Debug.Log($"Starting server [SteamSockets].");
                    server = SteamServer.CreateServer(this, NetManager.Instance.MaxConnections);
                }
                else
                {
                    Debug.LogError("Server already started!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return;
            }
        }

        public override Uri ServerUri()
        {
            var steamBuilder = new UriBuilder
            {
                Scheme = STEAM_SCHEME,
#if UNITY_SERVER
                Host = SteamGameServer.GetSteamID().m_SteamID.ToString()
#else
                Host = SteamUser.GetSteamID().m_SteamID.ToString()
#endif
            };

            return steamBuilder.Uri;
        }

        public override void ServerDisconnect(int connectionId)
        {
            if (ServerActive())
            {
                server.Disconnect(connectionId);
            }
        }        

        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId = 0)
        {
            if (ServerActive())
            {
                byte[] data = new byte[segment.Count];
                Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);
                server.Send(connectionId, data, channelId);
            }
        }        

        public override void ServerStop()
        {
            if (ServerActive())
            {
                Shutdown();
            }
        }        

        public override void ServerEarlyUpdate()
        {
            if (enabled)
            {
                server?.ReceiveData();
            }
        }

        public override void ServerLateUpdate()
        {
            if (enabled)
            {
                server?.FlushData();
            }
        }
        #endregion

        #region - Helpers -
        public override void Shutdown()
        {
            if (server != null)
            {
                server.Shutdown();
                server = null;
                Debug.Log("Transport shut down - was server.");
            }

            if (client != null)
            {
                client.Disconnect();
                client = null;
                Debug.Log("Transport shut down - was client.");
            }
        }

        public override int GetMaxPacketSize(int channelId = 0)
        {
            return Constants.k_cbMaxSteamNetworkingSocketsMessageSizeSend;
        }

        public override bool Available()
        {
            try
            {
#if UNITY_SERVER
                SteamGameServerNetworkingUtils.InitRelayNetworkAccess();
#else
                SteamNetworkingUtils.InitRelayNetworkAccess();
#endif
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void InitRelayNetworkAccess()
        {
            try
            {
#if UNITY_SERVER
                    SteamGameServerNetworkingUtils.InitRelayNetworkAccess();
#else
                    SteamNetworkingUtils.InitRelayNetworkAccess();
#endif
            }
            catch { }
        }
        #endregion


    }
}
