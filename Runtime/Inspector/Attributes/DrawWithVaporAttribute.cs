using System;
using System.Diagnostics;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class DrawWithVaporAttribute : Attribute
    {
        public UIGroupType InlinedGroupType { get; }

        public DrawWithVaporAttribute(UIGroupType inlinedGroupType = UIGroupType.Foldout)
        {
            InlinedGroupType = inlinedGroupType;
        }
    }
}
