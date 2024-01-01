using UnityEngine;

namespace VaporNetcode
{
    [System.Serializable]
    public class ClientModule
    {
        /// <summary>
        ///     Called by master server when module should be started
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        ///     Called when the manager updates all the modules.
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        ///     Called when the manager unloads all the modules.
        /// </summary>
        public virtual void Unload() { }
    }
}