using System;

namespace VaporNetcode
{
    public struct EntityLostInterestPacket : ISerializablePacket
    {
        public byte InterestType;
        public uint NetID;

        public EntityLostInterestPacket(NetworkReader r)
        {
            InterestType = r.ReadByte();
            NetID = r.ReadUInt();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteByte(InterestType);
            w.WriteUInt(NetID);
        }

        public int AsConnectionID()
        {
            return Convert.ToInt32(NetID);
        }
    }
}
