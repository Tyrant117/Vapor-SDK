using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Vapor.ObjectLogging;

namespace VaporNetcode
{
    [DefaultExecutionOrder(-1000)]
    public class NetManager : MonoBehaviour
    {
        public static NetManager Instance;

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Client")]
#else
        [Header("Client")]
#endif
        [SerializeField]
        private bool _isClient;
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Client"), InlineProperty, HideLabel]
#endif
        [SerializeField]
        private ClientConfig _clientConfig;
#if ODIN_INSPECTOR
        [TitleGroup("Tabs/Client/Modules")]
#endif
        [SerializeReference]
        public List<ClientModule> ClientModules = new();

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Server")]
#else
        [Header("Server")]
#endif
        [SerializeField]
        private bool _isServer;
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Server"), InlineProperty, HideLabel]
#endif
        [SerializeField]
        private ServerConfig _serverConfig;
#if ODIN_INSPECTOR
        [TitleGroup("Tabs/Server/Modules")]
#endif
        [SerializeReference]
        public List<ServerModule> ServerModules = new();

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Logging")]
#else
        [Header("Logging")]
#endif
        [Tooltip("Log level for network debugging")]
        public LogLevel logLevel;
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Logging")]
#endif
        [Tooltip("Spews all debug logs that come from update methods. Warning: could be a lot of messages")]
        public bool spewDebug;
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Logging")]
#endif
        [Tooltip("True if you want to recieve diagnostics on the messages being sent and recieved.")]
        public bool messageDiagnostics;
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Logging")]
#endif
        [Tooltip("True if you want to get logs for observer sync data")]
        public bool debugSyncVars;
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Logging"), Button, PropertyOrder(1)]
#endif
        private void GenerateMessageLookupData()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            _messageLookupList.Clear();
            foreach (var assembly in assemblies)
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (typeof(INetMessage).IsAssignableFrom(t))
                    {
                        var id = (ushort)t.FullName.GetStableHashCode();
                        _messageLookupList.Add($"{id} - {t.Name}");
                    }
                }
            }
        }
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Logging"), Button, PropertyOrder(1)]
#endif
        private void GenerateSyncLookupData()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            _syncClassLookupList.Clear();
            foreach (var assembly in assemblies)
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (typeof(SyncClass).IsAssignableFrom(t))
                    {
                        var id = /*(ushort)*/t.Name.GetStableHashCode();
                        _syncClassLookupList.Add($"{id} - {t.Name}");
                    }
                }
            }
        }
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Logging"), PropertyOrder(2)]
#endif
        [Tooltip("List of ID:Type pairs for INetMessage"), SerializeField]
        private List<string> _messageLookupList = new();
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Logging"), PropertyOrder(2)]
#endif
        [Tooltip("List of ID:Type pairs for SyncClass"), SerializeField]
        private List<string> _syncClassLookupList = new();

        #region Properties
        public int MaxConnections { get; set; }
        #endregion

        public event Action ServerInitialized;
        public event Action ClientInitialized;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            NetLogFilter.CurrentLogLevel = (int)logLevel;
            NetLogFilter.Spew = spewDebug;
            NetLogFilter.MessageDiagnostics = messageDiagnostics;
            NetLogFilter.SyncVars = debugSyncVars;

            if (_isServer)
            {
                //var serverMods = GetComponentsInChildren<ServerModule>();
                UDPServer.Listen(_serverConfig, UDPServer.GeneratePeer, ServerModules.ToArray());
                ServerInitialized?.Invoke();
            }

            if (_isClient)
            {
                //var clientMods = GetComponentsInChildren<ClientModule>();
                UDPClient.Initialize(_clientConfig, UDPClient.GeneratePeer, ClientModules.ToArray());
                ClientInitialized?.Invoke();
                if (_clientConfig.AutoConnect)
                {
                    StartCoroutine(WaitToAutoConnect());
                }
            }
        }

        private void OnDestroy()
        {
            if (_isServer)
            {
                UDPServer.Shutdown();
            }

            if (_isClient)
            {
                UDPClient.Shutdown();
            }
        }

        private IEnumerator WaitToAutoConnect()
        {
            yield return new WaitForSeconds(0.2f);
            UDPClient.Connect(_clientConfig.GameServerIp, _clientConfig.GameServerPort);
        }
    }
}
