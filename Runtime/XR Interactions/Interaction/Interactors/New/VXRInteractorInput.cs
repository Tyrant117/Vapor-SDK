using UnityEngine;

namespace VaporXR.Interactors
{
    public class VXRInteractorInput : MonoBehaviour
    {
        [SerializeField]
        private VXRInteractor _interactor;
        [SerializeField]
        private ButtonInputProvider _input;

        private void OnEnable()
        {
            if (_interactor is IVXRSelectInteractor select)
            {
                select.SelectActive += OnPerformedInput;
            }
            else if (_interactor is IVXRHoverInteractor hover)
            {
                //hover.HoverActive += OnPerformedInput;
            }
        }

        private void OnDisable()
        {
            if (_interactor is IVXRSelectInteractor select)
            {
                select.SelectActive -= OnPerformedInput;
            }
            else if (_interactor is IVXRHoverInteractor hover)
            {
                //hover.HoverActive -= OnPerformedInput;
            }
        }

        private XRIneractionActiveState OnPerformedInput() => new(_input.IsHeld, _input.CurrentState.ActivatedThisFrame, _input.CurrentValue);
    }
}
