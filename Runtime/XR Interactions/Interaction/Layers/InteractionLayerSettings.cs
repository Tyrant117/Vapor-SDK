using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VaporXR
{
    /// <summary>
    /// Configuration class for interaction layers.
    /// Stores all interaction layers.
    /// </summary>
    public class InteractionLayerSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        private const string DefaultLayerName = "Default";

        public const int LayerSize = 32;
        public const int BuiltInLayerSize = 1;

        [InitializeOnLoadMethod]
        private static void ResetInstance()
        {
            s_Instance = null;
        }
        

        private static InteractionLayerSettings s_Instance;
        public static InteractionLayerSettings Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = Resources.Load<InteractionLayerSettings>("InteractionLayerSettings");
                }

                return s_Instance;
            }
        }

        [SerializeField]
        private string[] _layerNames;

        /// <summary>
        /// Check if the interaction layer name at the supplied index is empty.
        /// </summary>
        /// <param name="index">The index of the target interaction layer.</param>
        /// <returns>Returns <see langword="true"/> if the target interaction layer is empty.</returns>
        public bool IsLayerEmpty(int index)
        {
            return string.IsNullOrEmpty(_layerNames[index]);
        }

        /// <summary>
        /// Sets the interaction layer name at the supplied index.
        /// </summary>
        /// <param name="index">The index of the target interaction layer.</param>
        /// <param name="layerName">The name of the target interaction layer.</param>
        public void SetLayerNameAt(int index, string layerName)
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Interaction Layer");
#endif
            _layerNames[index] = layerName;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Gets the interaction layer name at the supplied index.
        /// </summary>
        /// <param name="index">The index of the target interaction layer.</param>
        /// <returns>Returns the target interaction layer name.</returns>
        public string GetLayerNameAt(int index)
        {
            return _layerNames[index];
        }

        /// <summary>
        /// Gets the value (or bit index) of the supplied interaction layer name.
        /// </summary>
        /// <param name="layerName">The name of the interaction layer to search for its value.</param>
        /// <returns>Returns the interaction layer value.</returns>
        public int GetLayer(string layerName)
        {
            for (var i = 0; i < _layerNames.Length; i++)
            {
                if (string.Equals(layerName, _layerNames[i]))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Fills in the supplied lists with the interaction layer name and its correspondent value in the same index.
        /// </summary>
        /// <param name="names">The list to fill in with interaction layer names.</param>
        /// <param name="values">The list to fill in with interaction layer values.</param>
        public void GetLayerNamesAndValues(List<string> names, List<int> values)
        {
            if (_layerNames == null)
            {
                return;
            }
            
            for (var i = 0; i < _layerNames.Length; i++)
            {
                var layerName = _layerNames[i];
                if (string.IsNullOrEmpty(layerName))
                    continue;

                names.Add(layerName);
                values.Add(i);
            }
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            _layerNames ??= new string[LayerSize];

            if (_layerNames.Length != LayerSize)
                Array.Resize(ref _layerNames, LayerSize);

            if (!string.Equals(_layerNames[0], DefaultLayerName))
                _layerNames[0] = DefaultLayerName;
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
        }
    }
}
