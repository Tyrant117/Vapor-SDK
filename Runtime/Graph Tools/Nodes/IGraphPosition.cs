using UnityEngine;

namespace VaporGraphTools
{
    public interface IGraphPosition
    {
        /// <summary>
        /// Gets or sets the position of the node in a graph.
        /// </summary>
        Rect Position { get; set; }
    }
}
