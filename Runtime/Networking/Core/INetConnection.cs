using System;

namespace VaporNetcode
{
    public interface INetConnection
    {
        bool IsConnected { get; set; }
        bool IsAuthenticated { get; }
        int ConnectionID { get; }
        ulong GenericULongID { get; set; }
        string GenericStringID { get; set; }
        int SpamCount { get; set; }
        double RemoteTimestamp { get; set; }
        bool IsReady { get; set; }
        uint NetID { get; set; }

        void Authenticated(int connID);
        void Disconnect(int reason = 0);
    }
}
