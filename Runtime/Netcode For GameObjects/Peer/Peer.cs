using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace VaporNetcodeForGo
{
    [RequireComponent(typeof(NetworkObject))]
    public class Peer : NetworkBehaviour
    {
        private const string TAG = "[Peer]";


        #region Modules
        private readonly Dictionary<Type, PeerModule> modules = new(); // Modules added to the network manager
        private readonly HashSet<Type> initializedModules = new(); // set of initialized modules on the network manager
        #endregion

        public override void OnNetworkSpawn()
        {
            // Initialize Modules
            foreach (var mod in GetComponentsInChildren<PeerModule>())
            {
                AddModule(mod);
            }
            InitializeModules();
        }

        private void Update()
        {
            if (IsServer) 
            {
                foreach (var mod in modules.Values)
                {
                    mod.OnServerUpdate(PeerUpdateOrder.UpdatePhase.Dynamic);
                }
            }
            if (IsClient)
            {
                foreach (var mod in modules.Values)
                {
                    if (IsOwner)
                    {
                        mod.OnLocalClientUpdate(PeerUpdateOrder.UpdatePhase.Dynamic);
                    }
                    else
                    {
                        mod.OnRemoteClientUpdate(PeerUpdateOrder.UpdatePhase.Dynamic);
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsServer)
            {
                foreach (var mod in modules.Values)
                {
                    mod.OnServerUpdate(PeerUpdateOrder.UpdatePhase.Fixed);
                }
            }
            if (IsClient)
            {
                foreach (var mod in modules.Values)
                {
                    if (IsOwner)
                    {
                        mod.OnLocalClientUpdate(PeerUpdateOrder.UpdatePhase.Fixed);
                    }
                    else
                    {
                        mod.OnRemoteClientUpdate(PeerUpdateOrder.UpdatePhase.Fixed);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (IsServer)
            {
                foreach (var mod in modules.Values)
                {
                    mod.OnServerUpdate(PeerUpdateOrder.UpdatePhase.Late);
                }
            }
            if (IsClient)
            {
                foreach (var mod in modules.Values)
                {
                    if (IsOwner)
                    {
                        mod.OnLocalClientUpdate(PeerUpdateOrder.UpdatePhase.Late);
                    }
                    else
                    {
                        mod.OnRemoteClientUpdate(PeerUpdateOrder.UpdatePhase.Late);
                    }
                }
            }
        }

        public override void OnDestroy()
        {
            foreach (var mod in modules.Values)
            {
                mod.OnPeerUnload();
            }
            base.OnDestroy();
        }

        #region - Module Methods -
        /// <summary>
        ///     Adds a network module to the manager.
        /// </summary>
        /// <param name="module"></param>
        private void AddModule(PeerModule module)
        {
            if (modules.ContainsKey(module.GetType()))
            {
                if (NetLogFilter.LogDeveloper) { Debug.Log($"{TAG} Module has already been added. {module} || ({Time.time})"); }
            }
            modules.Add(module.GetType(), module);
        }

        /// <summary>
        ///     Adds a network module to the manager and initializes all modules.
        /// </summary>
        /// <param name="module"></param>
        public void AddModuleAndInitialize(PeerModule module)
        {
            AddModule(module);
            InitializeModules();
        }

        /// <summary>
        ///     Checks if the maanger has the module.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public bool HasModule(PeerModule module)
        {
            return modules.ContainsKey(module.GetType());
        }

        /// <summary>
        ///     Initializes all uninitialized modules
        /// </summary>
        /// <returns></returns>
        public bool InitializeModules()
        {
            while (true)
            {
                var changed = false;
                foreach (var mod in modules)
                {
                    // Module is already initialized
                    if (initializedModules.Contains(mod.Key)) { continue; }

                    mod.Value.Initialize(this);
                    initializedModules.Add(mod.Key);
                    changed = true;
                }

                // If nothing else can be initialized
                if (!changed)
                {
                    return !GetUninitializedModules().Any();
                }
            }
        }

        /// <summary>
        ///     Gets the module of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetModule<T>() where T : PeerModule
        {
            modules.TryGetValue(typeof(T), out PeerModule module);
            if (module == null)
            {
                module = modules.Values.FirstOrDefault(m => m is T);
            }
            return module as T;
        }

        /// <summary>
        ///     Gets all initialized modules.
        /// </summary>
        /// <returns></returns>
        private List<PeerModule> GetInitializedModules()
        {
            return modules
                .Where(m => initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }

        /// <summary>
        ///     Gets all unitialized modules.
        /// </summary>
        /// <returns></returns>
        private List<PeerModule> GetUninitializedModules()
        {
            return modules
                .Where(m => !initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }
        #endregion
    }
}
