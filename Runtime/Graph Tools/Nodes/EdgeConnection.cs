using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporGraphTools
{
    [Serializable]
    public class EdgeConnection
    {
        // Implicit equality operator for comparing EdgeConnection with int
        public static bool operator ==(EdgeConnection edge, int portIndex)
        {
            return edge.PortIndex == portIndex;
        }

        // Implicit inequality operator for comparing EdgeConnection with int
        public static bool operator !=(EdgeConnection edge, int portIndex)
        {
            return edge.PortIndex != portIndex;
        }

        // Implicit equality operator for comparing EdgeConnection with string
        public static bool operator ==(EdgeConnection edge, string guid)
        {
            return edge.Guid == guid;
        }

        // Implicit inequality operator for comparing EdgeConnection with string
        public static bool operator !=(EdgeConnection edge, string guid)
        {
            return edge.Guid != guid;
        }

        public int PortIndex;
        public int ConnectedPortIndex;
        public string Guid;


        public EdgeConnection(int portIndex, string guid)
        {
            PortIndex = portIndex;
            Guid = guid;
        }

        public EdgeConnection(int inPortIndex, int outPortIndex, string guid)
        {
            PortIndex = inPortIndex;
            ConnectedPortIndex = outPortIndex;
            Guid = guid;
        }

        public override bool Equals(object obj)
        {
            return obj is EdgeConnection connection &&
                   PortIndex == connection.PortIndex &&
                   Guid == connection.Guid;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PortIndex, Guid);
        }
    }
}
