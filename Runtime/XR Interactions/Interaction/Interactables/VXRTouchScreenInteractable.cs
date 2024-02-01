using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    [RequireComponent(typeof(VXRPokeFilter))]
    public class VXRTouchScreenInteractable : VXRBaseInteractable
    {
        #region Inspector
        [FoldoutGroup("Components"), SerializeField, AutoReference]
        private VXRPokeFilter _pokeFilter;
        #endregion

        #region Properties
        public VXRPokeFilter PokeFilter => _pokeFilter;
        #endregion
    }
}
