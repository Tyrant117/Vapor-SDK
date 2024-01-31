using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode.Transport
{
    public abstract class Transport : MonoBehaviour
    {
        protected static Transport s_Current;
        /// <summary>
        /// The current transport being used.
        /// </summary>
        public static Transport Current => s_Current;

        /// <summary>
        /// Only valid for transports that need a port.
        /// </summary>
        public ushort Port { get; set; }

        #region Client
        /// <summary>Called by Transport when the client connected to the server.</summary>
        public Action OnClientConnected;

        /// <summary>Called by Transport when the client received a message from the server.</summary>
        public Action<ArraySegment<byte>, int> OnClientDataReceived;

        /// <summary>Called by Transport when the client sent a message to the server.</summary>
        // Transports are responsible for calling it because:
        // - groups it together with OnReceived responsibility
        // - allows transports to decide if anything was sent or not
        // - allows transports to decide the actual used channel (i.e. tcp always sending reliable)
        public Action<ArraySegment<byte>, int> OnClientDataSent;

        /// <summary>Called by Transport when the client encountered an error.</summary>
        public Action<TransportError, string> OnClientError;

        /// <summary>Called by Transport when the client disconnected from the server.</summary>
        public Action OnClientDisconnected;
        #endregion

        #region Server
        /// <summary>Called by Transport when a new client connected to the server.</summary>
        public Action<int> OnServerConnected;

        /// <summary>Called by Transport when the server received a message from a client.</summary>
        public Action<int, ArraySegment<byte>, int> OnServerDataReceived;

        /// <summary>Called by Transport when the server sent a message to a client.</summary>
        // Transports are responsible for calling it because:
        // - groups it together with OnReceived responsibility
        // - allows transports to decide if anything was sent or not
        // - allows transports to decide the actual used channel (i.e. tcp always sending reliable)
        public Action<int, ArraySegment<byte>, int> OnServerDataSent;

        /// <summary>Called by Transport when a server's connection encountered a problem.</summary>
        /// If a Disconnect will also be raised, raise the Error first.
        public Action<int, TransportError, string> OnServerError;

        /// <summary>Called by Transport when a client disconnected from the server.</summary>
        public Action<int> OnServerDisconnected;
        #endregion

        #region - Unity Methods -
        public virtual void OnApplicationQuit()
        {
            Shutdown();
        }
        #endregion

        #region - Client -
        /// <summary>True if the client is currently connected to the server.</summary>
        public abstract bool ClientConnected();

        /// <summary>Connects the client to the server at the address.</summary>
        public abstract void ClientConnect(string address);

        /// <summary>Connects the client to the server at the Uri.</summary>
        public virtual void ClientConnect(Uri uri)
        {
            // By default, to keep backwards compatibility, just connect to the host
            // in the uri
            ClientConnect(uri.Host);
        }

        /// <summary>Sends a message to the server over the given channel.</summary>
        // The ArraySegment is only valid until returning. Copy if needed.
        public abstract void ClientSend(ArraySegment<byte> segment, int channelId = Channels.Reliable);

        /// <summary>Disconnects the client from the server</summary>
        public abstract void ClientDisconnect();

        public virtual void ClientEarlyUpdate() { }

        public virtual void ClientLateUpdate() { }
        #endregion

        #region - Server -
        /// <summary>Returns server address as Uri.</summary>
        // Useful for NetworkDiscovery.
        public abstract Uri ServerUri();

        /// <summary>True if the server is currently listening for connections.</summary>
        public abstract bool ServerActive();

        /// <summary>Start listening for connections.</summary>
        public abstract void ServerStart();

        /// <summary>Send a message to a client over the given channel.</summary>
        public abstract void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId = Channels.Reliable);

        /// <summary>Disconnect a client from the server.</summary>
        public abstract void ServerDisconnect(int connectionId);

        /// <summary>Get a client's address on the server.</summary>
        // Can be useful for Game Master IP bans etc.
        public abstract string ServerGetClientAddress(int connectionId);

        /// <summary>Stop listening and disconnect all connections.</summary>
        public abstract void ServerStop();

        public virtual void ServerEarlyUpdate() { }

        public virtual void ServerLateUpdate() { }
        #endregion

        #region - Helpers -
        /// <summary>Is this transport available in the current platform?</summary>
        public abstract bool Available();

        /// <summary>Shut down the transport, both as client and server</summary>
        public abstract void Shutdown();


        /// <summary>Maximum message size for the given channel.</summary>
        // Different channels often have different sizes, ranging from MTU to
        // several megabytes.
        //
        // Needs to return a value at all times, even if the Transport isn't
        // running or available because it's needed for initializations.
        public abstract int GetMaxPacketSize(int channelId = Channels.Reliable);

        /// <summary>Recommended Batching threshold for this transport.</summary>
        // Uses GetMaxPacketSize by default.
        // Some transports like kcp support large max packet sizes which should
        // not be used for batching all the time because they end up being too
        // slow (head of line blocking etc.).
        public virtual int GetBatchThreshold(int channelId = Channels.Reliable)
        {
            return GetMaxPacketSize(channelId);
        }
        #endregion
    }
}
