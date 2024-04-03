using System;

namespace VaporGraphTools.Math
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MathNodeParamAttribute : Attribute
    {
        public string ParamName { get; }

        public MathNodeParamAttribute(string paramName)
        {
            ParamName = paramName;
        }        
    }
}
