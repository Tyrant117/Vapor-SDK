using System;
using System.Diagnostics;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class HideMonoScriptAttribute : Attribute
    {

    }
}
