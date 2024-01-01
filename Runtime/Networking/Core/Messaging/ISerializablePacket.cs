using System;

namespace VaporNetcode
{
    public interface ISerializablePacket
    {
        /// <summary>
        ///     Serializes the data contained in the class to send over the network.
        /// </summary>
        /// <param name="writer"></param>
        void Serialize(NetworkWriter w);
    }
}