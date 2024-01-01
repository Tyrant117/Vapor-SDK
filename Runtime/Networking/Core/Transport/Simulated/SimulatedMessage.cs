using System;

namespace VaporNetcode
{
    public struct SimulatedMessage
    {
        public int connectionID;
        public SimulatedEventType eventType;
        public byte[] data;

        public SimulatedMessage(int connID, SimulatedEventType type, byte[] data)
        {
            connectionID = connID;
            eventType = type;
            this.data = data;
        }

        public T GetPacket<T>() where T : struct, ISerializablePacket
        {
            using var r = NetworkReaderPool.Get(data);
            return PacketManager.Deserialize<T>(r);
        }        
    }
}