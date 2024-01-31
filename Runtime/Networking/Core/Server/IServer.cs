using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public interface IServer
    {
        void ReceiveData();
        void Send(int connectionId, byte[] data, int channelId);
        void Disconnect(int connectionId);
        void FlushData();
        string ServerGetClientAddress(int connectionId);
        void Shutdown();
    }
}
