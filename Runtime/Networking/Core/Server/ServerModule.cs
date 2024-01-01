using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    [Serializable]
    public class ServerModule
    {
        /// <summary>
        ///     Called by master server when module should be started
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        ///     Called when the manager updates all the modules.
        /// </summary>
        public virtual void Update(float deltaTime) { }

        /// <summary>
        ///     Called when the manager unloads all the modules.
        /// </summary>
        public virtual void Unload() { }
    }
}