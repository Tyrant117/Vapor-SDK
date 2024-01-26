using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    [RequireComponent(typeof(XRPokeFilter))]
    public class VXRTouchScreenInteractable : VXRBaseInteractable
    {
        #region Inspector
        [FoldoutGroup("Components"), SerializeField, AutoReference]
        private XRPokeFilter _pokeFilter;
        #endregion

        #region Properties
        public XRPokeFilter PokeFilter => _pokeFilter;
        #endregion
    }
}
