using System.Diagnostics;
using Unity.Netcode;

namespace VaporNetcodeForGo
{
    public abstract class PeerModule : NetworkBehaviour
    {
        protected Peer Peer;
        protected bool Unloaded;

        public void Initialize(Peer peer)
        {
            Peer = peer;
            Unloaded = false;
            if (Peer.IsHost)
            {
                if (Peer.IsOwner)
                {
                    OnLocalClientInitialize();
                }
                else
                {
                    OnServerInitialize();
                }
            }
            else if (Peer.IsServer)
            {
                OnServerInitialize();
            }
            else // IsClient
            {
                if (Peer.IsOwner)
                {
                    OnLocalClientInitialize();
                }
                else
                {
                    OnRemoteClientInitialize();
                }
            }
        }

        protected abstract void OnLocalClientInitialize();
        protected abstract void OnRemoteClientInitialize();
        protected abstract void OnServerInitialize();

        public abstract void OnLocalClientUpdate(PeerUpdateOrder.UpdatePhase updatePhase);
        public abstract void OnRemoteClientUpdate(PeerUpdateOrder.UpdatePhase updatePhase);
        public abstract void OnServerUpdate(PeerUpdateOrder.UpdatePhase updatePhase);

        public override void OnDestroy()
        {
            if (!Unloaded)
            {
                OnPeerUnload();
            }

            base.OnDestroy();
        }

        public virtual void OnPeerUnload()
        {
            Unloaded = true;
        }
    }
}
