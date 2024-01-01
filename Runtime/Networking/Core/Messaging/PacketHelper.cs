using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace VaporNetcode
{
    public interface IPacketHelper 
    {
        ISerializablePacket Deserialize(NetworkReader reader);
    }

    public static class PacketID<T> where T : struct, ISerializablePacket
    {
        // automated message id from type hash.
        // platform independent via stable hashcode.
        // => convenient so we don't need to track messageIds across projects
        // => addons can work with each other without knowing their ids before
        // => 2 bytes is enough to avoid collisions.
        //    registering a messageId twice will log a warning anyway.
        public static readonly ushort ID = (ushort)(typeof(T).FullName.GetStableHashCode());
    }

    public class PacketHelper<T> : IPacketHelper where T : struct, ISerializablePacket
    {
        private readonly Func<NetworkReader, T> _activator;
        private readonly ushort opCode;
        public ushort OpCode => opCode;

        public IncomingMessageHandler<T> Handler { get; }
        public bool RequireAuthentication { get; }

        public PacketHelper()
        {
            opCode = PacketID<T>.ID;
            ConstructorInfo ctor = typeof(T).GetConstructors()[0];
            ParameterExpression param = Expression.Parameter(typeof(NetworkReader));
            NewExpression newExp = Expression.New(ctor, param);
            _activator = Expression.Lambda<Func<NetworkReader, T>>(newExp, param).Compile();
        }

        public ISerializablePacket Deserialize(NetworkReader reader)
        {
            return _activator.Invoke(reader);
        }
    }

    public static class PacketManager
    {
        private static readonly Dictionary<ushort, IPacketHelper> _packets = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _packets.Clear();
        }

        /// <summary>
        ///     Deserialized data into the provided packet and returns it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public static T Deserialize<T>(NetworkReader r) where T : struct, ISerializablePacket
        {
            if (!_packets.TryGetValue(PacketID<T>.ID, out var helper))
            {
                helper = new PacketHelper<T>();
                _packets.Add(PacketID<T>.ID, helper);
                //ConstructorInfo ctor = typeof(T).GetConstructors()[0];
                //ParameterExpression param = Expression.Parameter(typeof(NetworkReader));
                //NewExpression newExp = Expression.New(ctor, param);
                //activator = Expression.Lambda<Func<NetworkReader, T>>(newExp, param).Compile();
                //_packets[PacketID<T>.ID] = activator;
            }
            return (T)helper.Deserialize(r);
        }
    }
}