using System;

namespace VaporGraphTools
{
    /// <summary>
    /// Implement this interface on classes that need deliver external values into a <see cref="MathNodeSo.Evaluate(IExternalValueGetter, int)"/>
    /// </summary>
    public interface IExternalValueGetter
    {
        /// <summary>
        /// Returns the current value for the provided key
        /// </summary>
        /// <param name="keyType"></param>
        /// <param name="valueKey">The key that maps to an external value</param>
        /// <param name="fromId">The id to pass to the getter to determine extra processing information, -1 means no extra information.</param>
        object GetExternalValueAsBoxed(int valueKey, params int[] filters);

        float GetExternalValueAsFloat(int valueKey, params int[] filters);
        int GetExternalValueAsInt(int valueKey, params int[] filters);
        bool GetExternalValueAsBool(int valueKey, params int[] filters);

        DynamicValue GetExposedValue(string exposedPropertyKey, Type propertyType);

        DynamicValue GetCachedEventArg(int eventKey);
        void FireEvent(int eventKey, ArgVector args);
    }
}
