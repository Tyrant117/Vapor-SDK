
using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace VaporNetcode
{
    public delegate void IncomingMessageHandler<T>(INetConnection conn, T msg);

    public interface IPacketHandler
    {
        void Handle(INetConnection conn, NetworkReader reader, int channelID);
        void TimeoutResponse(INetConnection conn);
    }

    public class PacketHandler<T> : IPacketHandler where T : struct, INetMessage
    {
        private readonly Func<NetworkReader, T> _activator;
        private readonly IncomingMessageHandler<T> handler;
        private readonly ushort opCode;
        private readonly bool _requireAuthentication;
        public ushort OpCode => opCode;

        public IncomingMessageHandler<T> Handler { get; }
        public bool RequireAuthentication { get; }

        public PacketHandler(ushort opCode, IncomingMessageHandler<T> handler, bool requireAuthentication)
        {
            this.opCode = opCode;
            this.handler = handler;
            ConstructorInfo ctor = typeof(T).GetConstructors()[0];
            ParameterExpression param = Expression.Parameter(typeof(NetworkReader));
            NewExpression newExp = Expression.New(ctor, param);
            _activator = Expression.Lambda<Func<NetworkReader, T>>(newExp, param).Compile();
            _requireAuthentication = requireAuthentication;
        }

        public void Handle(INetConnection conn, NetworkReader reader, int channelID)
        {
            T message = default;
            int startPos = reader.Position;
            try
            {
                if (_requireAuthentication && !conn.IsAuthenticated)
                {
                    // message requires authentication, but the connection was not authenticated
                    Debug.Log($"<color=yellow><b>[!]</b></color> Closing connection: {conn}. Received message {typeof(T)} that required authentication, but the user has not authenticated yet");
                    conn.Disconnect();
                    return;
                }

                message = _activator(reader);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Closed connection: {conn}. This can happen if the other side accidentally (or an attacker intentionally) sent invalid data. Reason: {exception}");
                conn.Disconnect();
                return;
            }
            finally
            {
                int endPos = reader.Position;
                // TODO: Figure out the correct channel
                NetDiagnostics.OnReceive(message, channelID, endPos - startPos);
            }

            // user handler exception should not stop the whole server
            try
            {
                // user implemented handler
                handler(conn, message);
            }
            catch (Exception e)
            {
                Debug.LogError($"Disconnecting connId={conn.ConnectionID} to prevent exploits from an Exception in MessageHandler: {e.GetType().Name} {e.Message}\n{e.StackTrace}");
                conn.Disconnect();
            }            
        }

        public void TimeoutResponse(INetConnection conn)
        {
            handler(conn, default);
        }
    }
}