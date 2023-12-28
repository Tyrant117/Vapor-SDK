using System;
using UnityEngine.Assertions;
using System.Diagnostics;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TabGroupAttribute : VaporGroupAttribute
    {
        public string TabName { get; }
        public override UIGroupType Type => UIGroupType.Tab;

        public TabGroupAttribute(string groupName, string tabName, int order = 0)
        {
            GroupName = groupName;
            TabName = tabName;
            Order = order;
            Assert.IsFalse(Order == int.MaxValue, "Int.MaxValue is reserved");

            int last = GroupName.LastIndexOf('/');
            ParentName = last != -1 ? GroupName[..last] : "";
        }
    }
}
