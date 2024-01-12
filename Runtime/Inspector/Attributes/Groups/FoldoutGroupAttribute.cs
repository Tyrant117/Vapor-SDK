using System;
using System.Diagnostics;
using UnityEngine.Assertions;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class FoldoutGroupAttribute : VaporGroupAttribute
    {
        public string Header { get; }
        public override UIGroupType Type => UIGroupType.Foldout;

        public FoldoutGroupAttribute(string groupName, string header = "", int order = 0)
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
