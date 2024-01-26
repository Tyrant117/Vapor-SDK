using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace VaporXR
{
    /// <summary>
    /// Specifies Interaction Layers to use in XR interactions.
    /// </summary>
    [Serializable]
    public struct InteractionLayerMask : ISerializationCallbackReceiver
    {
        [SerializeField] 
        private uint _bits;

        private int _mask;
        
        /// <summary>
        /// Implicitly converts an InteractionLayerMask to an integer.
        /// </summary>
        /// <param name="mask">The mask to be converted.</param>
        /// <returns>Returns the integer value of the Interaction Layer Mask.</returns>
        public static implicit operator int(InteractionLayerMask mask)
        {
            return mask._mask;
        }
        
        /// <summary>
        /// Implicitly converts an integer to an InteractionLayerMask.
        /// </summary>
        /// <param name="intVal">The mask value.</param>
        /// <returns>Returns the Interaction Layer Mask for the integer value.</returns>
        public static implicit operator InteractionLayerMask(int intVal)
        {
            InteractionLayerMask mask;
            mask._mask = intVal;
            mask._bits = (uint)intVal;
            return mask;
        }
        
        /// <summary>
        /// Converts an interaction layer mask value to an integer value.
        /// </summary>
        /// <returns>Returns the integer value equivalent to this Interaction Layer Mask.</returns>
        public int Value
        {
            get => _mask;
            set
            {
                _mask = value;
                _bits = (uint)value;
            }
        }

        /// <summary>
        /// Given a layer number, returns the name of the Interaction Layer as defined in either a Builtin or a User Layer.
        /// </summary>
        /// <param name="layer">The interaction layer bit index.</param>
        /// <returns>Returns the name of the supplied Interaction Layer value.</returns>
        public static string LayerToName(int layer)
        {
            if (layer is < 0 or >= InteractionLayerSettings.LayerSize)
                return string.Empty;

            return InteractionLayerSettings.Instance.GetLayerNameAt(layer);
        }

        /// <summary>
        /// Given an Interaction Layer name, returns the index as defined by either a Builtin or a User Layer.
        /// </summary>
        /// <param name="layerName">The interaction layer name.</param>
        /// <returns>Returns the index of the supplied Interaction Layer name.</returns>
        public static int NameToLayer(string layerName)
        {
            return InteractionLayerSettings.Instance.GetLayer(layerName);
        }

        /// <summary>
        /// Given a set of Interaction Layer names, returns the equivalent mask for all of them.
        /// </summary>
        /// <param name="layerNames">The interaction layer names to be converted to a mask</param>
        /// <returns>Returns the equivalent mask for all the supplied Interaction Layer names.</returns>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="layerNames"/> is <see langword="null"/>.</exception>
        public static int GetMask(params string[] layerNames)
        {
            if (layerNames == null)
                throw new ArgumentNullException(nameof(layerNames));

            var mask = 0;
            foreach (var name in layerNames)
            {
                var layer = NameToLayer(name);

                if (layer != -1)
                {
                    mask |= 1 << layer;
                }
            }
            return mask;
        }

        /// <summary>
        /// See <see cref="ISerializationCallbackReceiver"/>.
        /// </summary>
        public void OnAfterDeserialize()
        {
            _mask = (int)_bits;
        }

        /// <summary>
        /// See <see cref="ISerializationCallbackReceiver"/>.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }
    }
}
