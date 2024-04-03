using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public interface IGraphToolsNode
    {
        List<Port> Ports { get; }

        NodeSo GetNode();
    }
}
