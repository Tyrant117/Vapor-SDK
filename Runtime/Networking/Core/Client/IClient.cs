using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public interface IClient
    {
        bool Connected { get; }
        bool Error { get; }


        void ReceiveData();
        void Disconnect();
        void FlushData();
        void Send(byte[] data, int channelId);
    }
}
