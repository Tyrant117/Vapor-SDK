using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public interface IResponseMessage : INetMessage
    {
        /// <summary>
        ///     Message status code
        /// </summary>
        ResponseStatus Status { get; set; }
    }
}
