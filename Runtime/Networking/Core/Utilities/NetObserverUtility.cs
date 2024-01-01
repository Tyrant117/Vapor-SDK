#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class NetObserverUtility : MonoBehaviour
    {
#if ODIN_INSPECTOR
        [BoxGroup("Network Info"), SuffixLabel("ms"), ReadOnly]
#endif
        [SerializeField]
        private float ping;
#if ODIN_INSPECTOR
        [BoxGroup("Network Info"), SuffixLabel("kb/s"), ReadOnly]
#endif
        [SerializeField]
        private float outRate;
        [SerializeField]
        private List<string> outMessages = new();
#if ODIN_INSPECTOR
        [BoxGroup("Network Info"), SuffixLabel("kb/s"), ReadOnly]
#endif
        [SerializeField]
        private float inRate;
        [SerializeField]
        private List<string> inMessages = new();


        public bool observeClient;
        public bool observerServer;

        private void Awake()
        {
            if (!(observeClient || observerServer))
            {
                Destroy(gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            NetDiagnostics.OutMessageEvent += NetDiagnostics_OutMessageEvent;
            NetDiagnostics.InMessageEvent += NetDiagnostics_InMessageEvent;
        }

        private void OnDisable()
        {
            NetDiagnostics.OutMessageEvent -= NetDiagnostics_OutMessageEvent;
            NetDiagnostics.InMessageEvent -= NetDiagnostics_InMessageEvent;
        }

        private void NetDiagnostics_InMessageEvent(NetDiagnostics.MessageInfo msg)
        {
            if (msg.message is NetworkPingMessage or NetworkPongMessage) { return; }
            if (inMessages.Count > 14)
            {
                inMessages.Clear();
            }
            inMessages.Add(msg.ToString());
        }

        private void NetDiagnostics_OutMessageEvent(NetDiagnostics.MessageInfo msg)
        {
            if (msg.message is NetworkPingMessage or NetworkPongMessage) { return; }
            if (outMessages.Count > 14)
            {
                outMessages.Clear();
            }
            outMessages.Add(msg.ToString());
        }

        private void Update()
        {
            ping = (float)NetTime.Rtt * 0.5f;
            if (NetLogFilter.MessageDiagnostics)
            {
                NetDiagnostics.AverageThroughput(Time.time);
                outRate = NetDiagnostics.aveBytesOut / 1000f;
                inRate = NetDiagnostics.aveBytesIn / 1000f;
            }
        }
    }
}
