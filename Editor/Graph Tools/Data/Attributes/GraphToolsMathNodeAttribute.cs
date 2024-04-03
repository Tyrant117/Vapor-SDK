using System;

namespace VaporGraphToolsEditor.Math
{
    public class GraphToolsMathNodeAttribute : Attribute
    {
        public string MenuName { get; }

        public GraphToolsMathNodeAttribute(string menuName)
        {
            MenuName = menuName;
        }
    }
}
