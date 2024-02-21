namespace VaporXR.Interaction
{
    public interface IInteractableModule
    {
        Interactable Interactable { get; }

        void Init(Interactable interactable);
        bool IsHoverableBy(Interactor interactor);
        bool IsSelectableBy(Interactor interactor);
        void PostProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase);
        void PreProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase);
    }
}
