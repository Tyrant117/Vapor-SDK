using System;

namespace VaporGraphTools
{
    public class SearchableNodeAttribute : Attribute
    {
        public string MenuName { get; }
        public string NodeName { get; }
        public Type TypeCollection { get; }
        public string[] IncludeFlags { get; }

        public SearchableNodeAttribute(string menuName, string nodeName, params string[] includeFlags)
        {
            MenuName = menuName;
            NodeName = nodeName;
            TypeCollection = null;
            IncludeFlags = includeFlags;
        }

        public SearchableNodeAttribute(string menuName, Type typeCollection, params string[] includeFlags)
        {
            MenuName = menuName;
            NodeName = string.Empty;
            TypeCollection = typeCollection;
            IncludeFlags = includeFlags;
        }
    }
}
