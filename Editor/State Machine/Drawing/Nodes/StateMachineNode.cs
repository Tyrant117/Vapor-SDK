using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphToolsEditor;
using VaporStateMachine;

namespace VaporStateMachineEditor
{
    public class PortTypes
    {
        public class FlowPort { }
    }

    public class StateMachineNode : GraphToolsNode<StateMachineGraphControllerSo, StateNodeSo>
    {
        private Port _inPort;
        private Port _outPort;

        public StateMachineNode(GraphEditorView view, StateNodeSo node)
        {
            View = view;
            Node = node;

            m_CollapseButton.RemoveFromHierarchy();
            CreateTitle();

            CreateFlowInPort();
            CreateFlowOutPort();

            RefreshExpandedState();
        }

        private void CreateTitle()
        {
            title = "Default";
            var label = titleContainer.Q<Label>();
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginRight = 6;
            label.style.marginLeft = 6;
        }

        private void CreateFlowInPort()
        {
            _inPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(PortTypes.FlowPort));
            _inPort.portName = "In";
            _inPort.tooltip = "The flow input";
            Ports.Add(_inPort);
            inputContainer.Add(_inPort);
        }

        private void CreateFlowOutPort()
        {
            _outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(PortTypes.FlowPort));
            _outPort.portName = "Out";
            _outPort.tooltip = "The flow output";
            Ports.Add(_outPort);
            outputContainer.Add(_outPort);
        }
    }
}
