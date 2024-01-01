using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public interface IClientIdentity
    {
        uint NetID { get; set; }
        Peer Peer { get; }
        bool IsPeer => Peer != null;
    }
}
