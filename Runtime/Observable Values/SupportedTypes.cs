using System;
using UnityEngine;

namespace VaporObservables
{
    public interface IWrapper
    {
        object ToObject();

        Type ToType();
    }

    internal static class SupportedTypes
    {
        internal static IWrapper ToWrapper(object value)
        {
            var type = value.GetType();
            IWrapper wrapped = type switch
            {
                var t when t == typeof(Vector2) => new Vector2Wrapper(value),
                var t when t == typeof(Vector2Int) => new Vector2IntWrapper(value),
                var t when t == typeof(Vector3) => new Vector3Wrapper(value),
                var t when t == typeof(Vector3Int) => new Vector3IntWrapper(value),
                var t when t == typeof(Color) => new ColorWrapper(value),
                var t when t == typeof(Quaternion) => new QuaternionWrapper(value),
                _ => new FallbackObjectWrapper(value)
            };
            return wrapped;
        }
    }

    [Serializable]
    internal struct FallbackObjectWrapper : IWrapper
    {
        public object Fallback;
        public string StoredType;

        public FallbackObjectWrapper(object value)
        {
            Fallback = value;
            StoredType = value.GetType().AssemblyQualifiedName;
        }

        public readonly object ToObject()
        {
            return Fallback;
        }

        public readonly Type ToType()
        {
            return Type.GetType(StoredType);
        }
    }

    [Serializable]
    internal struct Vector2Wrapper : IWrapper
    {
        public float x;
        public float y;

        public Vector2Wrapper(object value)
        {
            var val = (Vector2)value;
            x = val.x;
            y = val.y;
        }

        public readonly object ToObject()
        {
            return new Vector2(x, y);
        }

        public readonly Type ToType()
        {
            return typeof(Vector2);
        }
    }

    [Serializable]
    internal struct Vector2IntWrapper : IWrapper
    {
        public int x;
        public int y;

        public Vector2IntWrapper(object value)
        {
            var val = (Vector2Int)value;
            x = val.x;
            y = val.y;
        }

        public readonly object ToObject()
        {
            return new Vector2Int(x, y);
        }

        public readonly Type ToType()
        {
            return typeof(Vector2Int);
        }
    }

    [Serializable]
    internal struct Vector3Wrapper : IWrapper
    {
        public float x;
        public float y;
        public float z;

        public Vector3Wrapper(object value)
        {
            var val = (Vector3)value;
            x = val.x;
            y = val.y;
            z = val.z;
        }

        public readonly object ToObject()
        {
            return new Vector3(x, y, z);
        }

        public readonly Type ToType()
        {
            return typeof(Vector3);
        }
    }

    [Serializable]
    internal struct Vector3IntWrapper : IWrapper
    {
        public int x;
        public int y;
        public int z;

        public Vector3IntWrapper(object value)
        {
            var val = (Vector3Int)value;
            x = val.x;
            y = val.y;
            z = val.z;
        }

        public readonly object ToObject()
        {
            return new Vector3Int(x, y, z);
        }

        public readonly Type ToType()
        {
            return typeof(Vector3Int);
        }
    }

    [Serializable]
    internal struct ColorWrapper : IWrapper
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public ColorWrapper(object value)
        {
            var val = (Color)value;
            r = val.r;
            g = val.g;
            b = val.b;
            a = val.a;
        }

        public readonly object ToObject()
        {
            return new Color(r, g, b, a);
        }

        public readonly Type ToType()
        {
            return typeof(Color);
        }
    }

    [Serializable]
    internal struct QuaternionWrapper : IWrapper
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public QuaternionWrapper(object value)
        {
            var val = (Quaternion)value;
            x = val.x;
            y = val.y;
            z = val.z;
            w = val.w;
        }

        public readonly object ToObject()
        {
            return new Quaternion(x, y, z, w);
        }

        public readonly Type ToType()
        {
            return typeof(Quaternion);
        }
    }
}
