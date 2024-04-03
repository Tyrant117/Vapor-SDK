using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public class GraphToolsTokenPropertyNode<T, U, Z> : GraphToolsTokenNode<T, U> where T : ScriptableObject where U : ValueNodeSo<Z> where Z : struct
    {
        static readonly Texture2D exposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        private readonly Type _outPortType;
        private Port _outPort;

        public GraphToolsTokenPropertyNode(U node, Type outPortType) : base(null, null)
        {
            Node = node;
            _outPortType = outPortType;

            name = "PropertyTokenView";
            icon = exposedIcon;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphToolsTokenView"));

            this.Q<Label>("title-label").RemoveFromHierarchy();
            topContainer.Insert(2, CreatePropertyDrawer());

            CreateFlowOutPort();

            RefreshExpandedState();
        }

        protected virtual VisualElement CreatePropertyDrawer()
        {
            return new VisualElement();
        }

        private void CreateFlowOutPort()
        {
            _outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, _outPortType);
            _outPort.portName = "Out";
            _outPort.tooltip = "The flow output";
            _outPort.Q<Label>().style.display = DisplayStyle.None;
            Ports.Add(_outPort);
            outputContainer.Add(_outPort);

            if (_outPort != null)
            {
                var pill = this.Q<Pill>("pill");
                pill.right = _outPort;
            }
        }
    }
}
