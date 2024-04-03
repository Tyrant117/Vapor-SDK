using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;

namespace VaporXR.Interaction
{
    /// <summary>
    /// This module maps an <see cref="XRInputButton"/> to the <see cref="Interactor.SelectActive"/> callback.
    /// Can either be extended into other modules or used by itself.
    /// <seealso cref="GrabInteractorModule"/>
    /// </summary>
    public class InputSelectInteractorModule : InteractorModule
    {
        [VerticalGroup("Input"), SerializeField]
        [RichTextTooltip("The input mapping to check if the interactor is selecting")]
        private XRInputButton _selectInput;

        public XRInputButton SelectInput { get => _selectInput; protected set => _selectInput = value; }

        protected void OnEnable()
        {
            _selectInput.Enable();
            Interactor.SelectActive = OnSelectActiveCheck;
        }

        protected void OnDisable()
        {
            _selectInput.Disable();
            Interactor.SelectActive = null;
        }

        protected virtual XRIneractionActiveState OnSelectActiveCheck()
        {
            return new XRIneractionActiveState(_selectInput.IsHeld, _selectInput.State.ActivatedThisFrame, _selectInput.CurrentValue);
        }
    }
}
