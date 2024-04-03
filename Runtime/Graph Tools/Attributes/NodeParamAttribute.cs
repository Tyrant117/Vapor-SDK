using System;
using System.Collections.Generic;

namespace VaporGraphTools
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class NodeParamAttribute : Attribute
    {
        public string ParamName { get; }
        public int PortIndex { get; }
        public bool Required { get; }
        public bool MultiPort { get; }
        public Type PortType { get; }
        public Type[] PortTypes { get; }

        public NodeParamAttribute(string paramName, int portIndex, bool required, params Type[] portTypes)
        {
            ParamName = paramName;
            PortIndex = portIndex;
            Required = required;
            MultiPort = false;
            PortTypes = portTypes;
        }

        public NodeParamAttribute(string paramName, int portIndex, bool required, bool multiPort, params Type[] portTypes)
        {
            ParamName = paramName;
            PortIndex = portIndex;
            Required = required;
            MultiPort = multiPort;
            PortTypes = portTypes;
        }
    }
}
