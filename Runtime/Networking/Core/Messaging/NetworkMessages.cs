using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VaporNetcode
{

    // for performance, we (ab)use c# generics to cache the message id in a static field
    // this is significantly faster than doing the computation at runtime or looking up cached results via Dictionary
    // generic classes have separate static fields per type specification
    public static class NetworkMessageId<T> where T : struct, INetMessage
    {
        // automated message id from type hash.
        // platform independent via stable hashcode.
        // => convenient so we don't need to track messageIds across projects
        // => addons can work with each other without knowing their ids before
        // => 2 bytes is enough to avoid collisions.
        //    registering a messageId twice will log a warning anyway.
        public static readonly ushort Id = (ushort)(typeof(T).FullName.GetStableHashCode());
    }

    // message packing all in one place, instead of constructing headers in all
    // kinds of different places
    //
    //   MsgType     (2 bytes)
    //   Content     (ContentSize bytes)
    public static class NetworkMessages
    {
        // size of message id header in bytes
        public const int IdSize = sizeof(ushort);

        // max message content size (without header) calculation for convenience
        // -> Transport.GetMaxPacketSize is the raw maximum
        // -> Every message gets serialized into <<id, content>>
        // -> Every serialized message get put into a batch with a header
        public static int MaxContentSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => UDPTransport.GetMaxPacketSize()
                - IdSize
                - Batcher.HeaderSize;
        }

        // pack message before sending
        // -> NetworkWriter passed as arg so that we can use .ToArraySegment
        //    and do an allocation free send before recycling it.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pack<T>(T message, NetworkWriter writer)
            where T : struct, INetMessage
        {
            writer.WriteUShort(NetworkMessageId<T>.Id);
            message.Serialize(writer);
        }

        // read only the message id.
        // common function in case we ever change the header size.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UnpackId(NetworkReader reader, out ushort messageId)
        {
            // read message type
            try
            {
                messageId = reader.ReadUShort();
                return true;
            }
            catch (System.IO.EndOfStreamException)
            {
                messageId = 0;
                return false;
            }
        }
    }
}
