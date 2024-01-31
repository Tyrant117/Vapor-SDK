using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace VaporNetcodeForGo
{
    /// <summary>
    /// This module is used to sync data from the server only to the owner's client.
    /// It should be used to sync information that the player needs to know about his client that other players don't need to know.
    /// </summary>
    public class PlayerEntityModule : NetworkBehaviour
    {


        [Rpc(SendTo.Owner)]
        private void SyncEntityDataRpc()
        {

        }
    }
}
