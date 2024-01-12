using System;
using UnityEngine.Assertions;
using System.Diagnostics;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class VerticalGroupAttribute : VaporGroupAttribute
    {
        public override UIGroupType Type => UIGroupType.Vertical;

        public VerticalGroupAttribute(string groupName, int order = 0)
        {
            GroupName = groupName.Replace(" ", "");
            Order = order;
            // Assert.IsFalse(Order == int.MaxValue, "Int.MaxValue is reserved");

            int last = GroupName.LastIndexOf('/');
            ParentName = last != -1 ? GroupName[..last] : "";
        }
    }
}
