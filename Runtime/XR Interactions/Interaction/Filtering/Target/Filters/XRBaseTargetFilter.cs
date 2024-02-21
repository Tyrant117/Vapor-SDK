using System.Collections.Generic;
using UnityEngine;
using VaporXR.Interaction;
using VaporXR.Interaction;

namespace VaporXR
{
    /// <summary>
    /// Abstract base class from which all Target Filter behaviours derive.
    /// Instances of this class can be assigned to an <see cref="XRBaseInteractor"/> using the Inspector
    /// by setting the Starting Target Filter (<see cref="XRBaseInteractor.startingTargetFilter"/>).
    /// This serves as a serializable reference instead of using <see cref="XRBaseInteractor.targetFilter"/>
    /// which is not serialized.
    /// </summary>
    /// <seealso cref="XRTargetFilter"/>
    /// <seealso cref="IXRTargetFilter"/>
    public abstract class XRBaseTargetFilter : MonoBehaviour, IXRTargetFilter
    {
        /// <inheritdoc />
        public virtual bool CanProcess => isActiveAndEnabled;

        /// <inheritdoc />
        public virtual void Link(Interaction.IInteractor interactor)
        {
        }

        /// <inheritdoc />
        public virtual void Unlink(Interaction.IInteractor interactor)
        {
        }

        /// <inheritdoc />
        public abstract void Process(Interaction.IInteractor interactor, List<Interactable> targets, List<Interactable> results);
    }
}
