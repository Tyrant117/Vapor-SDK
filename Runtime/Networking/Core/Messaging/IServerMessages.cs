
namespace VaporNetcode
{
    public interface IServerMessages
    {
        void SendAll<T>(T message, int channelId = Channels.Reliable) where T : struct, INetMessage;

        void Send<T>(INetConnection conn, T message, int channelId = Channels.Reliable) where T : struct, INetMessage;
    }
}