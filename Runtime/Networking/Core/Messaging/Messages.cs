using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace VaporNetcode
{
    public struct EmptyMessage : INetMessage
    {
        public EmptyMessage(NetworkReader r)
        {

        }

        public void Serialize(NetworkWriter w)
        {

        }
    }

    public struct CommandMessage : INetMessage
    {
        public int Command;
        public byte[] Packet; // Recieves The Data

        public CommandMessage(NetworkReader r)
        {
            Command = r.ReadInt();
            Packet = r.ReadBytesAndSize();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteInt(Command);
            w.WriteBytesAndSize(Packet);
        }

        public T GetPacket<T>() where T : struct, ISerializablePacket
        {
            using var r = NetworkReaderPool.Get(Packet);
            return PacketManager.Deserialize<T>(r);
        }
    }

    public struct PacketMessage : INetMessage
    {
        public ArraySegment<byte> data; // Recieves The Data

        public PacketMessage(NetworkReader r)
        {
            data = r.ReadBytesAndSizeSegment();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteBytesAndSizeSegment(data);
        }

        public T GetPacket<T>() where T : struct, ISerializablePacket
        {
            using var r = NetworkReaderPool.Get(data);
            return PacketManager.Deserialize<T>(r);
        }
    }

    public struct SyncDataMessage : INetMessage
    {
        public ArraySegment<byte> data;

        public SyncDataMessage(NetworkReader r)
        {
            data = r.ReadBytesAndSizeSegment();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteBytesAndSizeSegment(data);
        }
    }

    // A client sends this message to the server
    // to calculate RTT and synchronize time
    public struct NetworkPingMessage : INetMessage
    {
        public double clientTime;

        public NetworkPingMessage(NetworkReader r)
        {
            clientTime = r.ReadDouble();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteDouble(clientTime);
        }
    }

    // The server responds with this message
    // The client can use this to calculate RTT and sync time
    public struct NetworkPongMessage : INetMessage
    {
        public double clientTime;

        public NetworkPongMessage(NetworkReader r)
        {
            clientTime = r.ReadDouble();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteDouble(clientTime);
        }
    }

    public struct StringMessage : INetMessage
    {
        public string message;

        public StringMessage(NetworkReader r)
        {
            message = r.ReadString();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteString(message);
        }
    }

    public struct TeleportMessage : INetMessage
    {
        public Vector3 Position;
        public Quaternion? Rotation;

        public TeleportMessage(NetworkReader r)
        {
            Position = r.ReadVector3();
            var decompress = r.ReadUIntNullable();
            Rotation = decompress.HasValue ? Compression.DecompressQuaternion(decompress.Value) : null;
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteVector3(Position);
            if (Rotation.HasValue)
            {
                w.WriteUIntNullable(Compression.CompressQuaternion(Rotation.Value));
            }
            else
            {
                w.WriteUIntNullable(null);
            }
        }
    }

    public struct TimeSnapshotMessage : INetMessage
    {
        public TimeSnapshotMessage(NetworkReader r)
        {

        }

        public void Serialize(NetworkWriter w)
        {

        }
    }

    public struct TransformSnapshotMessage : INetMessage
    {
        public uint? NetID;
        public Vector3? Position;
        public Quaternion? Rotation;
        public Vector3? Scale;

        public TransformSnapshotMessage(NetworkReader r)
        {
            NetID = r.ReadUIntNullable();
            Position = r.ReadVector3Nullable();
            Rotation = r.ReadQuaternionNullable();
            Scale = r.ReadVector3Nullable();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteUIntNullable(NetID);
            w.WriteVector3Nullable(Position);
            w.WriteQuaternionNullable(Rotation);
            w.WriteVector3Nullable(Scale);
        }
    }

    public struct TransformSnapshotDeltaMessage : INetMessage
    {
        public uint? NetID;
        public Vector3Long? DeltaPosition;
        public Quaternion? Rotation;
        public Vector3Long? DeltaScale;

        public TransformSnapshotDeltaMessage(NetworkReader r)
        {
            NetID = r.ReadUIntNullable();
            DeltaPosition = r.ReadBool() ? (new(Compression.DecompressVarInt(r), Compression.DecompressVarInt(r), Compression.DecompressVarInt(r))) : null;
            Rotation = r.ReadQuaternionNullable();
            DeltaScale = r.ReadBool() ? (new(Compression.DecompressVarInt(r), Compression.DecompressVarInt(r), Compression.DecompressVarInt(r))) : null;
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteUIntNullable(NetID);
            w.WriteBool(DeltaPosition.HasValue);
            if (DeltaPosition.HasValue)
            {
                Compression.CompressVarInt(w, DeltaPosition.Value.x);
                Compression.CompressVarInt(w, DeltaPosition.Value.y);
                Compression.CompressVarInt(w, DeltaPosition.Value.z);
            }
            w.WriteQuaternionNullable(Rotation);
            w.WriteBool(DeltaScale.HasValue);
            if (DeltaScale.HasValue)
            {
                Compression.CompressVarInt(w, DeltaScale.Value.x);
                Compression.CompressVarInt(w, DeltaScale.Value.y);
                Compression.CompressVarInt(w, DeltaScale.Value.z);
            }
        }
    }

    public struct InterestMessage : INetMessage
    {
        public List<EntityInterestPacket> packets;

        public InterestMessage(NetworkReader r)
        {
            packets = new();
            int count = r.ReadInt();
            for (int i = 0; i < count; i++)
            {
                packets.Add(new EntityInterestPacket(r));
            }
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteInt(packets.Count);
            for (int i = 0; i < packets.Count; i++)
            {
                packets[i].Serialize(w);
            }
        }
    }

    public struct LostInterestMessage : INetMessage
    {
        public List<EntityLostInterestPacket> Packets;

        public LostInterestMessage(NetworkReader r)
        {
            Packets = new();
            int count = r.ReadInt();
            for (int i = 0; i < count; i++)
            {
                Packets.Add(new EntityLostInterestPacket(r));
            }
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteInt(Packets.Count);
            for (int i = 0; i < Packets.Count; i++)
            {
                Packets[i].Serialize(w);
            }
        }
    }
}
