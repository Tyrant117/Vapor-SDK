using System;
using UnityEngine.Assertions;
using System.Diagnostics;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method , AllowMultiple = true, Inherited = true)]
    public class BoxGroupAttribute : VaporGroupAttribute
    {
        public string Header { get; }
        public override UIGroupType Type => UIGroupType.Box;

        public BoxGroupAttribute(string groupName, string header = "", int order = 0)
        {
            GroupName = groupName.Replace(" ", "");
            Header = string.Empty == header ? groupName : header;
            Order = order;
            // Assert.IsFalse(Order == int.MaxValue, "Int.MaxValue is reserved");

            var last = GroupName.LastIndexOf('/');
            ParentName = last != -1 ? GroupName[..last] : "";
        }
    }
}
