using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor;
using Object = UnityEngine.Object;

namespace VaporKeys
{
    public class RuntimeDatabase<T>
    {
        private static readonly Dictionary<int, T> _db;
        public static T Get(int id) => _db[id];
        public static bool TryGet(int id, out T value) => _db.TryGetValue(id, out value);
        public static IEnumerable<T> All() => _db.Values;

        public static void InitKeyDatabase<U>(KeyDatabaseSo<U> db) where U : ScriptableObject, IKey, T
        {
            Debug.Log($"RuntimeDatabase of type:{typeof(T).Name}. Init!");
            _db.Clear();
            foreach (var data in db.Data)
            {
                _db.Add(data.Key, data);
            }
        }

        public static void InitValueDatabase<U>(TypeDatabaseSo<U> db) where U : Object, T
        {
            Debug.Log($"RuntimeDatabase of type:{typeof(T).Name}. Init!");
            _db.Clear();

            foreach (var data in db.Data)
            {
                _db.Add(data.name.GetKeyHashCode(), data);
            }
        }
    }
}
