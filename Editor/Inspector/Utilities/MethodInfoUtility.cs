using System.Reflection;

namespace VaporInspectorEditor
{
    public static class MethodInfoUtility
    {
        public static bool HasAttributeOnMethod<T>(MethodInfo method)
        {
            return method.IsDefined(typeof(T), true);
        }

        public static T GetMethodAttribute<T>(MethodInfo method) where T : class
        {
            T[] attributes = (T[])method.GetCustomAttributes(typeof(T), true);
            return (attributes.Length > 0) ? attributes[0] : null;
        }

        public static bool TryGetMethodAttribute<T>(MethodInfo method, out T attribute) where T : class
        {
            T[] attributes = (T[])method.GetCustomAttributes(typeof(T), true);
            if ((attributes.Length > 0))
            {
                attribute = (T)attributes[0];
                return true;
            }
            else
            {
                attribute = null;
                return false;
            }
        }
    }
}
