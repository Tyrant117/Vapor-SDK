using UnityEngine;

namespace VaporXR.Interaction
{
    public class VXRInteractorInput : MonoBehaviour
    {
        [SerializeField]
        private Interactor _interactor;
        [SerializeField]
        private ButtonInputProvider _input;

        private void OnEnable()
        {
            _interactor.SelectActive += OnPerformedInput;
        }

        private void OnDisable()
        {
            _interactor.SelectActive -= OnPerformedInput;
        }

        private XRIneractionActiveState OnPerformedInput() => new(_input.IsHeld, _input.CurrentState.ActivatedThisFrame, _input.CurrentValue);
    }
}
