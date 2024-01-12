using System.Collections.Generic;
using UnityEngine;

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
        public virtual bool canProcess => isActiveAndEnabled;

        /// <inheritdoc />
        public virtual void Link(VXRBaseInteractor interactor)
        {
        }

        /// <inheritdoc />
        public virtual void Unlink(VXRBaseInteractor interactor)
        {
        }

        /// <inheritdoc />
        public abstract void Process(VXRBaseInteractor interactor, List<IXRInteractable> targets, List<IXRInteractable> results);
    }
}
