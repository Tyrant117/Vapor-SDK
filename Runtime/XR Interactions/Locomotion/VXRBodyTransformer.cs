using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Utilities;
using Object = UnityEngine.Object;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Behavior that manages user locomotion via transformation of an <see cref="XROrigin.Origin"/>. This behavior
    /// applies queued <see cref="IXRBodyTransformation"/>s every <see cref="Update"/>.
    /// </summary>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_XRBodyTransformer)]
    public class VXRBodyTransformer : MonoBehaviour
    {
        private struct OrderedTransformation
        {
            public IXRBodyTransformation Transformation;
            public int Priority;
        }

        #region Inspector
        [SerializeField, AutoReference(searchParents: true)] [Tooltip("The XR Origin to transform (will find one if None).")]
        private XROrigin _xrOrigin;

        [SerializeField]
        [RequireInterface(typeof(IXRBodyPositionEvaluator))]
        [Tooltip("Object that determines the position of the user's body. If set to None, this behavior will estimate " +
                 "the position to be the camera position projected onto the XZ plane of the XR Origin.")]
        private Object _bodyPositionEvaluatorObject;

        [SerializeField] [RequireInterface(typeof(IConstrainedXRBodyManipulator))] [Tooltip("Object used to perform movement that is constrained by collision (optional, may be None).")]
        private Object _constrainedBodyManipulatorObject;

        [SerializeField]
        [Tooltip("When enabled and if a Constrained Manipulator is not already assigned, this behavior will use the XR " +
                 "Origin's Character Controller to perform constrained movement, if one exists on the XR Origin's base GameObject.")]
        private bool _useCharacterControllerIfExists = true;
        #endregion

        #region Properties
        /// <summary>
        /// The XR Origin whose <see cref="XROrigin.Origin"/> to transform (will find one if <see langword="null"/>).
        /// </summary>
        /// <remarks>
        /// Setting this property at runtime also re-links the <see cref="ConstrainedBodyManipulator"/> to the new origin.
        /// </remarks>
        public XROrigin XROrigin
        {
            get => _xrOrigin;
            set
            {
                _xrOrigin = value;
                if (Application.isPlaying)
                    InitializeMovableBody();
            }
        }

        private IXRBodyPositionEvaluator _bodyPositionEvaluator;
        /// <summary>
        /// Object supplied to transformations that determines the position of the user's body. If <see langword="null"/>
        /// on <see cref="OnEnable"/>, this will be set to a shared instance of <see cref="UnderCameraBodyPositionEvaluator"/>.
        /// </summary>
        /// <remarks>
        /// Setting this property at runtime also re-links the <see cref="ConstrainedBodyManipulator"/> to the new evaluator.
        /// </remarks>
        public IXRBodyPositionEvaluator BodyPositionEvaluator
        {
            get => _bodyPositionEvaluator;
            set
            {
                _bodyPositionEvaluator = value;
                if (Application.isPlaying)
                    InitializeMovableBody();
            }
        }

        private IConstrainedXRBodyManipulator _constrainedBodyManipulator;
        /// <summary>
        /// Object supplied to transformations that can be used to perform movement that is constrained by collision
        /// (optional, may be <see langword="null"/>).
        /// </summary>
        /// <remarks>
        /// Setting this property at runtime unlinks the previous manipulator from the body and links the new manipulator
        /// to the body.
        /// </remarks>
        public IConstrainedXRBodyManipulator ConstrainedBodyManipulator
        {
            get => _constrainedBodyManipulator;
            set
            {
                _constrainedBodyManipulator = value;
                if (_movableBody != null)
                {
                    _movableBody.UnlinkConstrainedManipulator();
                    if (_constrainedBodyManipulator != null)
                        _movableBody.LinkConstrainedManipulator(_constrainedBodyManipulator);
                }
            }
        }

        /// <summary>
        /// If <see langword="true"/> and if a <see cref="ConstrainedBodyManipulator"/> is not already assigned, this
        /// behavior will check in <see cref="OnEnable"/> if the <see cref="XROrigin.Origin"/> has a
        /// <see cref="CharacterController"/>. If so, it will set <see cref="ConstrainedBodyManipulator"/> to a shared
        /// instance of <see cref="CharacterControllerBodyManipulator"/>, so that the Character Controller is used
        /// to perform constrained movement.
        /// </summary>
        public bool UseCharacterControllerIfExists { get => _useCharacterControllerIfExists; set => _useCharacterControllerIfExists = value; }
        #endregion

        #region Fields
        private bool _usingDynamicBodyPositionEvaluator;
        private bool _usingDynamicConstrainedBodyManipulator;

        private XRMovableBody _movableBody;

        private readonly LinkedList<OrderedTransformation> _transformationsQueue = new LinkedList<OrderedTransformation>();
        #endregion

        #region Events
        /// <summary>
        /// Calls the methods in its invocation list every <see cref="Update"/> before transformations are applied.
        /// </summary>
        public event Action<VXRBodyTransformer> BeforeApplyTransformations;
        #endregion

        protected virtual void OnEnable()
        {
            if (_xrOrigin == null)
            {
                if (!ComponentLocatorUtility<XROrigin>.TryFindComponent(out _xrOrigin))
                {
                    Debug.LogError("XR Body Transformer requires an XR Origin in the scene.", this);
                    enabled = false;
                    return;
                }
            }

            _bodyPositionEvaluator = _bodyPositionEvaluatorObject as IXRBodyPositionEvaluator;
            if (_bodyPositionEvaluator == null)
            {
                _usingDynamicBodyPositionEvaluator = true;
                _bodyPositionEvaluator = ScriptableSingletonCache<UnderCameraBodyPositionEvaluator>.GetInstance(this);
            }

            _constrainedBodyManipulator = _constrainedBodyManipulatorObject as IConstrainedXRBodyManipulator;
            if (_constrainedBodyManipulator == null && _useCharacterControllerIfExists)
            {
                if (_xrOrigin.Origin.TryGetComponent<CharacterController>(out _))
                {
                    _usingDynamicConstrainedBodyManipulator = true;
                    _constrainedBodyManipulator =
                        ScriptableSingletonCache<CharacterControllerBodyManipulator>.GetInstance(this);
                }
            }

            InitializeMovableBody();
        }

        protected virtual void OnDisable()
        {
            _movableBody?.UnlinkConstrainedManipulator();
            _movableBody = null;

            if (_usingDynamicBodyPositionEvaluator)
            {
                ScriptableSingletonCache<UnderCameraBodyPositionEvaluator>.ReleaseInstance(this);
                _usingDynamicBodyPositionEvaluator = false;
            }

            if (_usingDynamicConstrainedBodyManipulator)
            {
                ScriptableSingletonCache<CharacterControllerBodyManipulator>.ReleaseInstance(this);
                _usingDynamicConstrainedBodyManipulator = false;
            }
        }

        protected virtual void Update()
        {
            BeforeApplyTransformations?.Invoke(this);
            while (_transformationsQueue.Count > 0)
            {
                _transformationsQueue.First.Value.Transformation.Apply(_movableBody);
                _transformationsQueue.RemoveFirst();
            }
        }

        private void InitializeMovableBody()
        {
            _movableBody = new XRMovableBody(_xrOrigin, _bodyPositionEvaluator);
            if (_constrainedBodyManipulator != null)
            {
                _movableBody.LinkConstrainedManipulator(_constrainedBodyManipulator);
            }
        }

        /// <summary>
        /// Queues a transformation to be applied during the next <see cref="Update"/>. Transformations are applied
        /// sequentially based on ascending <paramref name="priority"/>. Transformations with the same priority are
        /// applied in the order they were queued. Each transformation is removed from the queue after it is applied.
        /// </summary>
        /// <param name="transformation">The transformation that will receive a call to
        /// <see cref="IXRBodyTransformation.Apply"/> in the next <see cref="Update"/>.</param>
        /// <param name="priority">Value that determines when to apply the transformation. Transformations with lower
        /// priority values are applied before those with higher priority values.</param>
        public void QueueTransformation(IXRBodyTransformation transformation, int priority = 0)
        {
            var orderedTransformation = new OrderedTransformation
            {
                Transformation = transformation,
                Priority = priority,
            };

            var node = _transformationsQueue.First;
            if (node == null || node.Value.Priority > priority)
            {
                _transformationsQueue.AddFirst(orderedTransformation);
                return;
            }

            while (node.Next != null && node.Next.Value.Priority <= priority)
            {
                node = node.Next;
            }

            _transformationsQueue.AddAfter(node, orderedTransformation);
        }
    }
}