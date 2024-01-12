using System;
using UnityEngine;
using UnityEngine.Serialization;
using Vapor.Utilities;
using VaporInspector;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Base for a behavior that implements a specific type of user locomotion. This behavior communicates with a
    /// <see cref="LocomotionMediator"/> to gain access to the mediator's <see cref="VXRBodyTransformer"/>, which the
    /// provider can use to queue <see cref="IXRBodyTransformation"/>s that move the user.
    /// </summary>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_LocomotionProviders)]
    public abstract class LocomotionProvider : MonoBehaviour
    {
        #region Inspector
        [SerializeField, AutoReference(searchParents: true), FoldoutGroup("Locomotion")] 
        [RichTextTooltip("The behavior that this provider communicates with for access to the mediator's <cls>XRBodyTransformer</cls>.")]
        private LocomotionMediator _mediator;

        [SerializeField, FoldoutGroup("Locomotion")] 
        [RichTextTooltip("The queue order of this provider's transformations of the <cls>VXROrigin</cls>. The lower the value, the earlier the transformations are applied.")]
        private int _transformationPriority;
        #endregion

        #region Properties
        /// <summary>
        /// The behavior that this provider communicates with for access to the mediator's <see cref="VXRBodyTransformer"/>.
        /// If one is not provided, this provider will attempt to locate one during its <see cref="Awake"/> call.
        /// </summary>
        public LocomotionMediator Mediator { get => _mediator; set => _mediator = value; }

        /// <summary>
        /// The queue order of this provider's transformations of the XR Origin. The lower the value, the earlier the
        /// transformations are applied.
        /// </summary>
        /// <seealso cref="TryQueueTransformation(IXRBodyTransformation)"/>
        public int TransformationPriority { get => _transformationPriority; set => _transformationPriority = value; }

        /// <summary>
        /// The current state of locomotion. The <see cref="Mediator"/> determines this state based on the provider's
        /// requests for the <see cref="VXRBodyTransformer"/>.
        /// </summary>
        /// <seealso cref="TryPrepareLocomotion"/>
        /// <seealso cref="CanStartMoving"/>
        /// <seealso cref="TryEndLocomotion"/>
        public LocomotionState LocomotionState => _mediator != null ? _mediator.GetProviderLocomotionState(this) : LocomotionState.Idle;

        /// <summary>
        /// Whether the provider is actively preparing or performing locomotion. This is <see langword="true"/> when
        /// <see cref="LocomotionState"/> is <see cref="LocomotionState.Preparing"/> or <see cref="LocomotionState.Moving"/>,
        /// <see langword="false"/> otherwise.
        /// </summary>
        /// <seealso cref="LocomotionStateExtensions.IsActive"/>
        public bool IsLocomotionActive => LocomotionState.IsActive();

        /// <summary>
        /// Whether the provider has finished preparing for locomotion and is ready to enter the <see cref="LocomotionState.Moving"/> state.
        /// This only applies when <see cref="LocomotionState"/> is <see cref="LocomotionState.Preparing"/>, so there is
        /// no need for this implementation to query <see cref="LocomotionState"/>.
        /// </summary>
        public virtual bool CanStartMoving => true;
        #endregion

        #region Fields
        private VXRBodyTransformer _activeBodyTransformer;
        private bool _anyTransformationsThisFrame;
        #endregion

        #region Events
        /// <summary>
        /// Calls the methods in its invocation list when the provider has entered the <see cref="LocomotionState.Moving"/> state.
        /// </summary>
        /// <seealso cref="LocomotionState"/>
        public event Action<LocomotionProvider> LocomotionStarted;

        /// <summary>
        /// Calls the methods in its invocation list when the provider has entered the <see cref="LocomotionState.Ended"/> state.
        /// </summary>
        /// <seealso cref="LocomotionState"/>
        public event Action<LocomotionProvider> LocomotionEnded;

        /// <summary>
        /// Calls the methods in its invocation list just before the <see cref="VXRBodyTransformer"/> applies this
        /// provider's transformation(s). This is invoked at most once per frame while <see cref="LocomotionState"/> is
        /// <see cref="LocomotionState.Moving"/>, and only if the provider has queued at least one transformation.
        /// </summary>
        /// <remarks>This is invoked before the <see cref="VXRBodyTransformer"/> applies the transformations from other
        /// providers as well.</remarks>
        public event Action<LocomotionProvider> BeforeStepLocomotion;
        #endregion


        protected virtual void Awake()
        {
            if (_mediator == null)
            {
                _mediator = GetComponentInParent<LocomotionMediator>();
                if (_mediator == null)
                {
                    ComponentLocatorUtility<LocomotionMediator>.TryFindComponent(out _mediator);
                }
            }

            if (_mediator != null) return;

            Debug.LogError("Locomotion Provider requires a Locomotion Mediator or Locomotion System (legacy) in the scene.", this);
            enabled = false;
        }

        /// <summary>
        /// Attempts to transition this provider into the <see cref="LocomotionState.Preparing"/> state. This succeeds
        /// if <see cref="IsLocomotionActive"/> was <see langword="false"/> when this was called.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if <see cref="IsLocomotionActive"/> was
        /// <see langword="false"/> when this was called, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// If this succeeds, then the provider can enter the <see cref="LocomotionState.Moving"/> state either by calling
        /// <see cref="TryStartLocomotionImmediately"/> or by waiting for the <see cref="Mediator"/>'s next
        /// <see cref="LocomotionMediator.Update"/> in which the provider's <see cref="CanStartMoving"/>
        /// is <see langword="true"/>. When the provider enters the <see cref="LocomotionState.Moving"/> state, it will
        /// invoke <see cref="LocomotionStarted"/> and gain access to the <see cref="VXRBodyTransformer"/>.
        /// </remarks>
        protected bool TryPrepareLocomotion()
        {
            return _mediator != null && _mediator.TryPrepareLocomotion(this);
        }

        /// <summary>
        /// Attempts to transition this provider into the <see cref="LocomotionState.Moving"/> state. This succeeds if
        /// <see cref="LocomotionState"/> was not already <see cref="LocomotionState.Moving"/> when
        /// this was called.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if <see cref="LocomotionState"/> was not already
        /// <see cref="LocomotionState.Moving"/> when this was called, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// This method bypasses the check for <see cref="CanStartMoving"/>.
        /// After this provider enters the <see cref="LocomotionState.Moving"/> state, it will invoke
        /// <see cref="LocomotionStarted"/> and gain access to the <see cref="VXRBodyTransformer"/>.
        /// </remarks>
        protected bool TryStartLocomotionImmediately()
        {
            return _mediator != null && _mediator.TryStartLocomotion(this);
        }

        /// <summary>
        /// Attempts to transition this provider into the <see cref="LocomotionState.Ended"/> state. This succeeds if
        /// <see cref="IsLocomotionActive"/> was <see langword="true"/> when this was called.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if <see cref="IsLocomotionActive"/> was
        /// <see langword="true"/> when this was called, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// After this provider enters the <see cref="LocomotionState.Ended"/> state, it will invoke
        /// <see cref="LocomotionEnded"/> and lose access to the <see cref="VXRBodyTransformer"/>. Then during the
        /// <see cref="Mediator"/>'s <see cref="LocomotionMediator.Update"/> in the next frame, the provider will enter
        /// the <see cref="LocomotionState.Idle"/> state, unless it has called <see cref="TryPrepareLocomotion"/> or
        /// <see cref="TryStartLocomotionImmediately"/> again.
        /// </remarks>
        protected bool TryEndLocomotion()
        {
            return _mediator != null && _mediator.TryEndLocomotion(this);
        }

        public void OnLocomotionStart(VXRBodyTransformer transformer)
        {
            _activeBodyTransformer = transformer;
            _activeBodyTransformer.BeforeApplyTransformations += OnBeforeTransformationsApplied;
            _anyTransformationsThisFrame = false;
            OnLocomotionStarting();
            LocomotionStarted?.Invoke(this);
        }

        /// <summary>
        /// Called when locomotion enters the <see cref="LocomotionState.Moving"/> state, after the provider gains
        /// access to the <see cref="VXRBodyTransformer"/> and before it invokes <see cref="LocomotionStarted"/>.
        /// </summary>
        protected virtual void OnLocomotionStarting()
        {
        }

        public void OnLocomotionEnd()
        {
            LocomotionEnded?.Invoke(this);
            OnLocomotionEnding();

            if (_activeBodyTransformer != null)
                _activeBodyTransformer.BeforeApplyTransformations -= OnBeforeTransformationsApplied;
            _activeBodyTransformer = null;
        }

        /// <summary>
        /// Called when locomotion enters the <see cref="LocomotionState.Ended"/> state, after the provider invokes
        /// <see cref="LocomotionEnded"/> and before it loses access to the <see cref="VXRBodyTransformer"/>.
        /// </summary>
        protected virtual void OnLocomotionEnding()
        {
        }

        /// <summary>
        /// Attempts to queue a transformation to be applied during the active <see cref="VXRBodyTransformer"/>'s next
        /// <see cref="VXRBodyTransformer.Update"/>. The provider's <see cref="TransformationPriority"/> determines when
        /// the transformation is applied in relation to others. The queue attempt only succeeds if the provider is in
        /// the <see cref="LocomotionState.Moving"/> state.
        /// </summary>
        /// <param name="bodyTransformation">The transformation that will receive a call to
        /// <see cref="IXRBodyTransformation.Apply"/> in the next <see cref="VXRBodyTransformer.Update"/>.</param>
        /// <returns>Returns <see langword="true"/> if the provider has access to the <see cref="VXRBodyTransformer"/>,
        /// <see langword="false"/> otherwise.</returns>
        /// <remarks>This should only be called when <see cref="LocomotionState"/> is <see cref="LocomotionState.Moving"/>,
        /// otherwise this method will do nothing and return <see langword="false"/>.</remarks>
        /// <seealso cref="TryQueueTransformation(IXRBodyTransformation, int)"/>
        protected bool TryQueueTransformation(IXRBodyTransformation bodyTransformation)
        {
            if (!CanQueueTransformation())
                return false;

            _activeBodyTransformer.QueueTransformation(bodyTransformation, _transformationPriority);
            _anyTransformationsThisFrame = true;
            return true;
        }

        /// <summary>
        /// Attempts to queue a transformation to be applied during the active <see cref="VXRBodyTransformer"/>'s next
        /// <see cref="VXRBodyTransformer.Update"/>. The given <paramref name="priority"/> determines when the
        /// transformation is applied in relation to others. The queue attempt only succeeds if the provider is in the
        /// <see cref="LocomotionState.Moving"/> state.
        /// </summary>
        /// <param name="bodyTransformation">The transformation that will receive a call to
        /// <see cref="IXRBodyTransformation.Apply"/> in the next <see cref="VXRBodyTransformer.Update"/>.</param>
        /// <param name="priority">Value that determines when to apply the transformation. Transformations with lower
        /// priority values are applied before those with higher priority values.</param>
        /// <returns>Returns <see langword="true"/> if the provider has access to the <see cref="VXRBodyTransformer"/>,
        /// <see langword="false"/> otherwise.</returns>
        /// <remarks>This should only be called when <see cref="LocomotionState"/> is <see cref="LocomotionState.Moving"/>,
        /// otherwise this method will do nothing and return <see langword="false"/>.</remarks>
        /// <seealso cref="TryQueueTransformation(IXRBodyTransformation)"/>
        protected bool TryQueueTransformation(IXRBodyTransformation bodyTransformation, int priority)
        {
            if (!CanQueueTransformation())
                return false;

            _activeBodyTransformer.QueueTransformation(bodyTransformation, priority);
            _anyTransformationsThisFrame = true;
            return true;
        }

        private bool CanQueueTransformation()
        {
            if (_activeBodyTransformer == null)
            {
                if (LocomotionState == LocomotionState.Moving)
                {
                    Debug.LogError("Cannot queue transformation because reference to active XR Body Transformer " +
                                   "is null, even though Locomotion Provider is in Moving state. This should not happen.", this);
                }

                return false;
            }

            return true;
        }

        private void OnBeforeTransformationsApplied(VXRBodyTransformer bodyTransformer)
        {
            if (_anyTransformationsThisFrame)
                BeforeStepLocomotion?.Invoke(this);

            _anyTransformationsThisFrame = false;
        }
    }
}
