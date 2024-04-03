using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporGraphTools
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class NodeResultAttribute : Attribute
    {
        public string ParamName { get; }
        public int PortIndex { get; }
        public bool MultiPort { get; }
        public Type[] PortTypes { get; }

        public NodeResultAttribute(string paramName, int portIndex, params Type[] portTypes)
        {
            ParamName = paramName;
            PortIndex = portIndex;
            MultiPort = false;
            PortTypes = portTypes;
        }

        public NodeResultAttribute(string paramName, int portIndex, bool multiPort, params Type[] portTypes)
        {
            ParamName = paramName;
            PortIndex = portIndex;
            MultiPort = multiPort;
            PortTypes = portTypes;
        }
    }
}
