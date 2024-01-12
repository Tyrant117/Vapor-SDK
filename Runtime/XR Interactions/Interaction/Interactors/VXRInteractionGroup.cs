using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Serialization;
using Vapor.Utilities;
using VaporEvents;
using VaporInspector;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace VaporXR
{
    /// <summary>
    /// Behaviour implementation of <see cref="IXRInteractionGroup"/>. An Interaction Group hooks into the interaction system
    /// (via <see cref="VXRInteractionManager"/>) and enforces that only one <see cref="IXRGroupMember"/> within the Group
    /// can interact at a time. Each Group member must be either an <see cref="VXRBaseInteractor"/> or an <see cref="IXRInteractionGroup"/>.
    /// </summary>
    /// <remarks>
    /// The member prioritized for interaction in any given frame is whichever member was interacting the previous frame
    /// if it can select in the current frame. If there is no such member, then the interacting member is whichever one
    /// in the ordered list of members interacts first.
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_InteractionGroups)]
    public class VXRInteractionGroup : MonoBehaviour, IXRInteractionOverrideGroup, IXRGroupMember
    {
        /// <summary>
        /// These correspond to the default names of the Interaction Groups in the sample XR Rig.
        /// </summary>
        public static class GroupNames
        {
            /// <summary> Left controller and hand interactors </summary>
            public static readonly string k_Left = "Left";
            /// <summary> Right controller and hand interactors </summary>
            public static readonly string k_Right = "Right";
            /// <summary> Head/eye interactors </summary>
            public static readonly string k_Center = "Center";
        }

        [Serializable, DrawWithVapor(UIGroupType.Vertical)]
        public class GroupMemberAndOverridesPair
        {
            [RequireInterface(typeof(IXRGroupMember))]
            public Object groupMember;

            [RequireInterface(typeof(IXRGroupMember))]
            public List<Object> overrideGroupMembers = new();
        }
        
        #region Inspector
        [SerializeField]
        [RichTextTooltip("The name of the interaction group, which can be used to retrieve it from the Interaction Manager.")]
        private string _groupName;
        
        [SerializeField]
        [RichTextTooltip("The XR Interaction Manager that this Interaction Group will communicate with (will find one if not set manually).")]
        private VXRInteractionManager _interactionManager;
        
        [SerializeField]
        [RichTextTooltip("Ordered list of Interactors or Interaction Groups that are registered with the Group on Awake.")]
        [RequireInterface(typeof(IXRGroupMember))]
        private List<Object> _startingGroupMembers = new();
        
        [SerializeField]
        [RichTextTooltip("Configuration for each Group Member of which other Members are able to override its interaction " +
                         "when they attempt to select, despite the difference in priority order.")]
        private List<GroupMemberAndOverridesPair> _startingInteractionOverridesMap = new();
        #endregion

        #region Properties
        /// <inheritdoc />
        public string groupName => _groupName;
        
        VXRInteractionManager m_RegisteredInteractionManager;/// <summary>
        /// The <see cref="VXRInteractionManager"/> that this Interaction Group will communicate with (will find one if <see langword="null"/>).
        /// </summary>
        public VXRInteractionManager interactionManager
        {
            get => _interactionManager;
            set
            {
                _interactionManager = value;
                if (Application.isPlaying && isActiveAndEnabled)
                    RegisterWithInteractionManager();
            }
        }
        
        /// <inheritdoc />
        public IXRInteractionGroup ContainingGroup { get; private set; }
        
        /// <summary>
        /// Ordered list of Interactors or Interaction Groups that are registered with the Group on Awake.
        /// All objects in this list should implement the <see cref="IXRGroupMember"/> interface and either the
        /// <see cref="VXRBaseInteractor"/> interface or the <see cref="IXRInteractionGroup"/> interface.
        /// </summary>
        /// <remarks>
        /// There are separate methods to access and modify the Group members used after Awake.
        /// </remarks>
        /// <seealso cref="AddGroupMember"/>
        /// <seealso cref="MoveGroupMemberTo"/>
        /// <seealso cref="RemoveGroupMember"/>
        /// <seealso cref="ClearGroupMembers"/>
        /// <seealso cref="ContainsGroupMember"/>
        /// <seealso cref="GetGroupMembers"/>
        public List<Object> startingGroupMembers
        {
            get => _startingGroupMembers;
            set
            {
                _startingGroupMembers = value;
                RemoveMissingMembersFromStartingOverridesMap();
            }
        }
        
        /// <inheritdoc />
        public VXRBaseInteractor activeInteractor { get; private set; }
        
        /// <inheritdoc />
        public VXRBaseInteractor focusInteractor { get; private set; }
        
        /// <inheritdoc />
        public IXRFocusInteractable focusInteractable { get; private set; }
        
        // Used by custom editor to check if we can edit the starting configuration
        public bool isRegisteredWithInteractionManager => m_RegisteredInteractionManager != null;
        public bool hasRegisteredStartingMembers { get; private set; }
        #endregion

        #region Fields
        private readonly RegistrationList<IXRGroupMember> _groupMembers = new RegistrationList<IXRGroupMember>();
        private readonly List<IXRGroupMember> _tempGroupMembers = new List<IXRGroupMember>();
        private bool _isProcessingGroupMembers;
        
        /// <summary>
        /// Mapping of each group member to a set of other members that can override its interaction via selection.
        /// </summary>
        private readonly Dictionary<IXRGroupMember, HashSet<IXRGroupMember>> _interactionOverridesMap = new();

        private readonly List<IXRInteractable> _validTargets = new();

        private static readonly List<IXRSelectInteractable> s_InteractablesSelected = new();
        private static readonly List<IXRHoverInteractable> s_InteractablesHovered = new();
        #endregion

        #region Events
        /// <inheritdoc />
        public event Action<InteractionGroupRegisteredEventArgs> registered;

        /// <inheritdoc />
        public event Action<InteractionGroupUnregisteredEventArgs> unregistered;
        #endregion
        
        protected virtual void Awake()
        {
            // Setup Interaction Manager
            FindCreateInteractionManager();

            // Starting member interactors will be re-registered with the manager below when they are added to the group.
            // Make sure the group is registered first.
            RegisterWithInteractionManager();

            // It is more efficient to add than move, but if there are existing items
            // use move to ensure the correct order dictated by the starting lists.
            if (_groupMembers.FlushedCount > 0)
            {
                var index = 0;
                foreach (var obj in _startingGroupMembers)
                {
                    if (obj != null && obj is IXRGroupMember groupMember)
                        MoveGroupMemberTo(groupMember, index++);
                }
            }
            else
            {
                foreach (var obj in _startingGroupMembers)
                {
                    if (obj != null && obj is IXRGroupMember groupMember)
                        AddGroupMember(groupMember);
                }
            }

            if (string.IsNullOrWhiteSpace(_groupName))
                _groupName = gameObject.name;

            RemoveMissingMembersFromStartingOverridesMap();
            foreach (var groupMemberAndOverridesPair in _startingInteractionOverridesMap)
            {
                var groupMemberObj = groupMemberAndOverridesPair.groupMember;
                if (groupMemberObj == null || !(groupMemberObj is IXRGroupMember groupMember))
                    continue;

                foreach (var overrideGroupMemberObj in groupMemberAndOverridesPair.overrideGroupMembers)
                {
                    if (overrideGroupMemberObj != null && overrideGroupMemberObj is IXRGroupMember overrideGroupMember)
                        AddInteractionOverrideForGroupMember(groupMember, overrideGroupMember);
                }
            }

            hasRegisteredStartingMembers = true;
        }

        public void RemoveMissingMembersFromStartingOverridesMap()
        {
            for (var i = _startingInteractionOverridesMap.Count - 1; i >= 0; i--)
            {
                var groupMemberAndOverrides = _startingInteractionOverridesMap[i];
                if (!_startingGroupMembers.Contains(groupMemberAndOverrides.groupMember))
                {
                    _startingInteractionOverridesMap.RemoveAt(i);
                }
                else
                {
                    var overrides = groupMemberAndOverrides.overrideGroupMembers;
                    for (var j = overrides.Count - 1; j >= 0; j--)
                    {
                        if (!_startingGroupMembers.Contains(overrides[j]))
                            overrides.RemoveAt(j);
                    }
                }
            }
        }

        protected virtual void OnEnable()
        {
            FindCreateInteractionManager();
            RegisterWithInteractionManager();
        }

        protected virtual void OnDisable()
        {
            UnregisterWithInteractionManager();
        }

        protected virtual void OnDestroy()
        {
            hasRegisteredStartingMembers = false;
            _interactionOverridesMap.Clear();
            ClearGroupMembers();
        }

        /// <summary>
        /// Adds <paramref name="overrideGroupMember"/> to the list of Group members that are to be added as
        /// interaction overrides for <paramref name="sourceGroupMember"/> on Awake. Both objects must already be
        /// included in the <see cref="startingGroupMembers"/> list. The override object should implement either the
        /// <see cref="VXRBaseInteractor"/> interface or the <see cref="IXRInteractionOverrideGroup"/> interface.
        /// </summary>
        /// <param name="sourceGroupMember">The Group member whose interaction can be potentially overridden by
        /// <paramref name="overrideGroupMember"/>.</param>
        /// <param name="overrideGroupMember">The Group member to add as a possible interaction override.</param>
        /// <remarks>
        /// Use <see cref="AddInteractionOverrideForGroupMember"/> to add to the interaction overrides used after Awake.
        /// </remarks>
        /// <seealso cref="RemoveStartingInteractionOverride"/>
        /// <seealso cref="AddInteractionOverrideForGroupMember"/>
        public void AddStartingInteractionOverride(Object sourceGroupMember, Object overrideGroupMember)
        {
            if (sourceGroupMember == null)
            {
                Debug.LogError($"{nameof(sourceGroupMember)} cannot be null.");
                return;
            }

            if (overrideGroupMember == null)
            {
                Debug.LogError($"{nameof(overrideGroupMember)} cannot be null.");
                return;
            }

            if (!_startingGroupMembers.Contains(sourceGroupMember))
            {
                Debug.LogError($"Cannot add starting override group member for source member {sourceGroupMember} " +
                    $"because {sourceGroupMember} is not included in the starting group members.", this);

                return;
            }

            if (!_startingGroupMembers.Contains(overrideGroupMember))
            {
                Debug.LogError($"Cannot add override group member {overrideGroupMember} for source member " +
                    $"because {overrideGroupMember} is not included in the starting group members.", this);

                return;
            }

            if (TryGetStartingGroupMemberAndOverridesPair(sourceGroupMember, out var groupMemberAndOverrides))
            {
                groupMemberAndOverrides.overrideGroupMembers.Add(overrideGroupMember);
            }
            else
            {
                _startingInteractionOverridesMap.Add(new GroupMemberAndOverridesPair
                {
                    groupMember = sourceGroupMember,
                    overrideGroupMembers = new List<Object> { overrideGroupMember }
                });
            }
        }

        /// <summary>
        /// Removes <paramref name="overrideGroupMember"/> from the list of Group members that are to be added as
        /// interaction overrides for <paramref name="sourceGroupMember"/> on Awake.
        /// </summary>
        /// <param name="sourceGroupMember">The Group member whose interaction can no longer be overridden by
        /// <paramref name="overrideGroupMember"/>.</param>
        /// <param name="overrideGroupMember">The Group member to remove as a possible interaction override.</param>
        /// <returns>
        /// Returns <see langword="true"/> if <paramref name="overrideGroupMember"/> was removed from the list of
        /// potential overrides for <paramref name="sourceGroupMember"/>. Otherwise, returns <see langword="false"/>
        /// if <paramref name="overrideGroupMember"/> was not part of the list.
        /// </returns>
        /// <remarks>
        /// Use <see cref="RemoveInteractionOverrideForGroupMember"/> to remove from the interaction overrides used after Awake.
        /// </remarks>
        /// <seealso cref="AddStartingInteractionOverride"/>
        /// <seealso cref="RemoveInteractionOverrideForGroupMember"/>
        public bool RemoveStartingInteractionOverride(Object sourceGroupMember, Object overrideGroupMember)
        {
            if (sourceGroupMember == null)
            {
                Debug.LogError($"{nameof(sourceGroupMember)} cannot be null.");
                return false;
            }

            return TryGetStartingGroupMemberAndOverridesPair(sourceGroupMember, out var groupMemberAndOverrides) &&
                groupMemberAndOverrides.overrideGroupMembers.Remove(overrideGroupMember);
        }

        bool TryGetStartingGroupMemberAndOverridesPair(Object sourceGroupMember,
            out GroupMemberAndOverridesPair groupMemberAndOverrides)
        {
            if (sourceGroupMember == null)
            {
                groupMemberAndOverrides = null;
                return false;
            }

            foreach (var pair in _startingInteractionOverridesMap)
            {
                if (pair.groupMember != sourceGroupMember)
                    continue;

                groupMemberAndOverrides = pair;
                return true;
            }

            groupMemberAndOverrides = null;
            return false;
        }

        /// <inheritdoc />
        void IXRInteractionGroup.OnRegistered(InteractionGroupRegisteredEventArgs args)
        {
            if (args.manager != _interactionManager)
                Debug.LogWarning($"An Interaction Group was registered with an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_interactionManager}\" but was registered with \"{args.manager}\".", this);

            m_RegisteredInteractionManager = args.manager;

            _groupMembers.Flush();
            _isProcessingGroupMembers = true;
            foreach (var groupMember in _groupMembers.RegisteredSnapshot)
            {
                if (!_groupMembers.IsStillRegistered(groupMember))
                    continue;

                if (groupMember.ContainingGroup == null)
                    RegisterAsGroupMember(groupMember);
            }

            _isProcessingGroupMembers = false;

            registered?.Invoke(args);
        }

        /// <inheritdoc />
        void IXRInteractionGroup.OnBeforeUnregistered()
        {
            _groupMembers.Flush();
            _isProcessingGroupMembers = true;
            foreach (var groupMember in _groupMembers.RegisteredSnapshot)
            {
                if (!_groupMembers.IsStillRegistered(groupMember))
                    continue;

                RegisterAsNonGroupMember(groupMember);
            }

            _isProcessingGroupMembers = false;
        }

        /// <inheritdoc />
        void IXRInteractionGroup.OnUnregistered(InteractionGroupUnregisteredEventArgs args)
        {
            if (args.manager != m_RegisteredInteractionManager)
                Debug.LogWarning($"An Interaction Group was unregistered from an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{m_RegisteredInteractionManager}\" but was unregistered from \"{args.manager}\".", this);

            m_RegisteredInteractionManager = null;
            unregistered?.Invoke(args);
        }

        /// <inheritdoc />
        public void AddGroupMember(IXRGroupMember groupMember)
        {
            if (groupMember == null)
                throw new ArgumentNullException(nameof(groupMember));

            if (!ValidateAddGroupMember(groupMember))
                return;

            if (_isProcessingGroupMembers)
                Debug.LogWarning($"{groupMember} added while {name} is processing Group members. It won't be processed until the next process.", this);

            if (_groupMembers.Register(groupMember))
                RegisterAsGroupMember(groupMember);
        }

        /// <inheritdoc />
        public void MoveGroupMemberTo(IXRGroupMember groupMember, int newIndex)
        {
            if (groupMember == null)
                throw new ArgumentNullException(nameof(groupMember));

            if (!ValidateAddGroupMember(groupMember))
                return;

            // BaseRegistrationList<T> does not yet support reordering with pending registration changes.
            if (_isProcessingGroupMembers)
            {
                Debug.LogError($"Cannot move {groupMember} while {name} is processing Group members.", this);
                return;
            }

            _groupMembers.Flush();
            if (_groupMembers.MoveItemImmediately(groupMember, newIndex) && groupMember.ContainingGroup == null)
                RegisterAsGroupMember(groupMember);
        }

        private bool ValidateAddGroupMember(IXRGroupMember groupMember)
        {
            if (!(groupMember is VXRBaseInteractor || groupMember is IXRInteractionGroup))
            {
                Debug.LogError($"Group member {groupMember} must be either an Interactor or an Interaction Group.", this);
                return false;
            }

            if (groupMember.ContainingGroup != null && !ReferenceEquals(groupMember.ContainingGroup, this))
            {
                Debug.LogError($"Cannot add/move {groupMember} because it is already part of a Group. Remove the member from the Group first.", this);
                return false;
            }

            if (groupMember is IXRInteractionGroup subGroup && subGroup.HasDependencyOnGroup(this))
            {
                Debug.LogError($"Cannot add/move {groupMember} because this would create a circular dependency of groups.", this);
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public bool RemoveGroupMember(IXRGroupMember groupMember)
        {
            if (_groupMembers.Unregister(groupMember))
            {
                // Reset active interactor if it was part of the member that was removed
                if (activeInteractor != null && GroupMemberIsOrContainsInteractor(groupMember, activeInteractor))
                    activeInteractor = null;

                _interactionOverridesMap.Remove(groupMember);
                RegisterAsNonGroupMember(groupMember);
                return true;
            }

            return false;
        }

        private bool GroupMemberIsOrContainsInteractor(IXRGroupMember groupMember, VXRBaseInteractor interactor)
        {
            if (ReferenceEquals(groupMember, interactor))
                return true;

            if (!(groupMember is IXRInteractionGroup memberGroup))
                return false;

            memberGroup.GetGroupMembers(_tempGroupMembers);
            foreach (var subGroupMember in _tempGroupMembers)
            {
                if (GroupMemberIsOrContainsInteractor(subGroupMember, interactor))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void ClearGroupMembers()
        {
            _groupMembers.Flush();
            for (var index = _groupMembers.FlushedCount - 1; index >= 0; --index)
            {
                var groupMember = _groupMembers.GetRegisteredItemAt(index);
                RemoveGroupMember(groupMember);
            }
        }

        /// <inheritdoc />
        public bool ContainsGroupMember(IXRGroupMember groupMember)
        {
            return _groupMembers.IsRegistered(groupMember);
        }

        /// <inheritdoc />
        public void GetGroupMembers(List<IXRGroupMember> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            _groupMembers.GetRegisteredItems(results);
        }

        /// <inheritdoc />
        public bool HasDependencyOnGroup(IXRInteractionGroup group)
        {
            if (ReferenceEquals(group, this))
                return true;

            GetGroupMembers(_tempGroupMembers);
            foreach (var groupMember in _tempGroupMembers)
            {
                if (groupMember is IXRInteractionGroup subGroup && subGroup.HasDependencyOnGroup(group))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void AddInteractionOverrideForGroupMember(IXRGroupMember sourceGroupMember, IXRGroupMember overrideGroupMember)
        {
            if (sourceGroupMember == null)
            {
                Debug.LogError($"{nameof(sourceGroupMember)} cannot be null.");
                return;
            }

            if (overrideGroupMember == null)
            {
                Debug.LogError($"{nameof(overrideGroupMember)} cannot be null.");
                return;
            }

            if (!(overrideGroupMember is VXRBaseInteractor || overrideGroupMember is IXRInteractionOverrideGroup))
            {
                Debug.LogError($"Override group member {overrideGroupMember} must implement either " +
                    $"{nameof(VXRBaseInteractor)} or {nameof(IXRInteractionOverrideGroup)}.", this);

                return;
            }

            if (!ContainsGroupMember(sourceGroupMember))
            {
                Debug.LogError($"Cannot add override group member for source member {sourceGroupMember} because {sourceGroupMember} " +
                    "is not registered with the Group. Call AddGroupMember first.", this);

                return;
            }

            if (!ContainsGroupMember(overrideGroupMember))
            {
                Debug.LogError($"Cannot add override group member {overrideGroupMember} for source member because {overrideGroupMember} " +
                    "is not registered with the Group. Call AddGroupMember first.", this);

                return;
            }

            if (GroupMemberIsPartOfOverrideChain(overrideGroupMember, sourceGroupMember))
            {
                Debug.LogError($"Cannot add {overrideGroupMember} as an override group member for {sourceGroupMember} " +
                    "because this would create a loop of group member overrides.", this);

                return;
            }

            if (_interactionOverridesMap.TryGetValue(sourceGroupMember, out var overrides))
                overrides.Add(overrideGroupMember);
            else
                _interactionOverridesMap[sourceGroupMember] = new HashSet<IXRGroupMember> { overrideGroupMember };
        }

        /// <inheritdoc />
        public bool GroupMemberIsPartOfOverrideChain(IXRGroupMember sourceGroupMember, IXRGroupMember potentialOverrideGroupMember)
        {
            if (ReferenceEquals(potentialOverrideGroupMember, sourceGroupMember))
                return true;

            if (!_interactionOverridesMap.TryGetValue(sourceGroupMember, out var overrides))
                return false;

            foreach (var nextOverride in overrides)
            {
                if (GroupMemberIsPartOfOverrideChain(nextOverride, potentialOverrideGroupMember))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool RemoveInteractionOverrideForGroupMember(IXRGroupMember sourceGroupMember, IXRGroupMember overrideGroupMember)
        {
            if (sourceGroupMember == null)
            {
                Debug.LogError($"{nameof(sourceGroupMember)} cannot be null.");
                return false;
            }

            if (!ContainsGroupMember(sourceGroupMember))
            {
                Debug.LogError($"Cannot remove override group member for source member {sourceGroupMember} because {sourceGroupMember} " +
                    "is not registered with the Group.", this);

                return false;
            }

            return _interactionOverridesMap.TryGetValue(sourceGroupMember, out var overrides) && overrides.Remove(overrideGroupMember);
        }

        /// <inheritdoc />
        public bool ClearInteractionOverridesForGroupMember(IXRGroupMember sourceGroupMember)
        {
            if (sourceGroupMember == null)
            {
                Debug.LogError($"{nameof(sourceGroupMember)} cannot be null.");
                return false;
            }

            if (!ContainsGroupMember(sourceGroupMember))
            {
                Debug.LogError($"Cannot clear override group members for source member {sourceGroupMember} because {sourceGroupMember} " +
                    "is not registered with the Group.", this);

                return false;
            }

            if (!_interactionOverridesMap.TryGetValue(sourceGroupMember, out var overrides))
                return false;

            overrides.Clear();
            return true;

        }

        /// <inheritdoc />
        public void GetInteractionOverridesForGroupMember(IXRGroupMember sourceGroupMember, HashSet<IXRGroupMember> results)
        {
            if (sourceGroupMember == null)
            {
                Debug.LogError($"{nameof(sourceGroupMember)} cannot be null.");
                return;
            }

            if (results == null)
            {
                Debug.LogError($"{nameof(results)} cannot be null.");
                return;
            }

            if (!ContainsGroupMember(sourceGroupMember))
            {
                Debug.LogError($"Cannot get override group members for source member {sourceGroupMember} because {sourceGroupMember} " +
                    "is not registered with the Group.", this);

                return;
            }

            results.Clear();
            if (_interactionOverridesMap.TryGetValue(sourceGroupMember, out var overrides))
                results.UnionWith(overrides);
        }

        private void FindCreateInteractionManager()
        {
            if (_interactionManager != null)
                return;

            _interactionManager = SingletonBus.Get<VXRInteractionManager>();
        }

        private void RegisterWithInteractionManager()
        {
            if (m_RegisteredInteractionManager == _interactionManager)
                return;

            UnregisterWithInteractionManager();

            if (_interactionManager != null)
            {
                _interactionManager.RegisterInteractionGroup(this);
            }
        }

        private void UnregisterWithInteractionManager()
        {
            if (m_RegisteredInteractionManager == null)
                return;

            m_RegisteredInteractionManager.UnregisterInteractionGroup(this);
        }

        private void RegisterAsGroupMember(IXRGroupMember groupMember)
        {
            if (m_RegisteredInteractionManager == null)
                return;

            groupMember.OnRegisteringAsGroupMember(this);
            ReRegisterGroupMemberWithInteractionManager(groupMember);
        }

        private void RegisterAsNonGroupMember(IXRGroupMember groupMember)
        {
            if (m_RegisteredInteractionManager == null)
                return;

            groupMember.OnRegisteringAsNonGroupMember();
            ReRegisterGroupMemberWithInteractionManager(groupMember);
        }

        private void ReRegisterGroupMemberWithInteractionManager(IXRGroupMember groupMember)
        {
            if (m_RegisteredInteractionManager == null)
                return;

            // Re-register the interactor or group so the manager can update its status as part of a group
            switch (groupMember)
            {
                case VXRBaseInteractor interactor:
                    if (m_RegisteredInteractionManager.IsRegistered(interactor))
                    {
                        m_RegisteredInteractionManager.UnregisterInteractor(interactor);
                        m_RegisteredInteractionManager.RegisterInteractor(interactor);
                    }
                    break;
                case IXRInteractionGroup group:
                    if (m_RegisteredInteractionManager.IsRegistered(group))
                    {
                        m_RegisteredInteractionManager.UnregisterInteractionGroup(group);
                        m_RegisteredInteractionManager.RegisterInteractionGroup(group);
                    }
                    break;
                default:
                    Debug.LogError($"Group member {groupMember} must be either an Interactor or an Interaction Group.", this);
                    break;
            }
        }

        /// <inheritdoc />
        void IXRInteractionGroup.PreprocessGroupMembers(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            // Flush once at the start of the update phase, and this is the first method invoked by the manager
            _groupMembers.Flush();

            _isProcessingGroupMembers = true;
            foreach (var groupMember in _groupMembers.RegisteredSnapshot)
            {
                if (!_groupMembers.IsStillRegistered(groupMember))
                    continue;

                switch (groupMember)
                {
                    case VXRBaseInteractor interactor:
                        if (!m_RegisteredInteractionManager.IsRegistered(interactor))
                            continue;

                        interactor.PreprocessInteractor(updatePhase);
                        break;
                    case IXRInteractionGroup group:
                        if (!m_RegisteredInteractionManager.IsRegistered(group))
                            continue;

                        group.PreprocessGroupMembers(updatePhase);
                        break;
                }
            }

            _isProcessingGroupMembers = false;
        }

        /// <inheritdoc />
        void IXRInteractionGroup.ProcessGroupMembers(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            _isProcessingGroupMembers = true;
            foreach (var groupMember in _groupMembers.RegisteredSnapshot)
            {
                if (!_groupMembers.IsStillRegistered(groupMember))
                    continue;

                switch (groupMember)
                {
                    case VXRBaseInteractor interactor:
                        if (!m_RegisteredInteractionManager.IsRegistered(interactor))
                            continue;

                        interactor.ProcessInteractor(updatePhase);
                        break;
                    case IXRInteractionGroup group:
                        if (!m_RegisteredInteractionManager.IsRegistered(group))
                            continue;

                        group.ProcessGroupMembers(updatePhase);
                        break;
                }
            }

            _isProcessingGroupMembers = false;
        }

        /// <inheritdoc />
        void IXRInteractionGroup.UpdateGroupMemberInteractions()
        {
            // Prioritize previous active interactor if it can select this frame
            VXRBaseInteractor prePrioritizedInteractor = null;
            if (activeInteractor != null && m_RegisteredInteractionManager.IsRegistered(activeInteractor) &&
                activeInteractor is VXRBaseInteractor activeSelectInteractor &&
                CanStartOrContinueAnySelect(activeSelectInteractor))
            {
                prePrioritizedInteractor = activeInteractor;
            }

            ((IXRInteractionGroup)this).UpdateGroupMemberInteractions(prePrioritizedInteractor, out var interactorThatPerformedInteraction);
            activeInteractor = interactorThatPerformedInteraction;
        }

        bool CanStartOrContinueAnySelect(VXRBaseInteractor selectInteractor)
        {
            if (selectInteractor.KeepSelectedTargetValid)
            {
                foreach (var interactable in selectInteractor.InteractablesSelected)
                {
                    if (m_RegisteredInteractionManager.CanSelect(selectInteractor, interactable))
                        return true;
                }
            }

            m_RegisteredInteractionManager.GetValidTargets(selectInteractor, _validTargets);
            foreach (var target in _validTargets)
            {
                if (!(target is IXRSelectInteractable selectInteractable))
                    continue;

                if (m_RegisteredInteractionManager.CanSelect(selectInteractor, selectInteractable))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        void IXRInteractionGroup.UpdateGroupMemberInteractions(VXRBaseInteractor prePrioritizedInteractor, out VXRBaseInteractor interactorThatPerformedInteraction)
        {
            if (((IXRInteractionOverrideGroup)this).ShouldOverrideActiveInteraction(out var overridingInteractor))
                prePrioritizedInteractor = overridingInteractor;

            interactorThatPerformedInteraction = null;
            _isProcessingGroupMembers = true;
            foreach (var groupMember in _groupMembers.RegisteredSnapshot)
            {
                if (!_groupMembers.IsStillRegistered(groupMember))
                    continue;

                switch (groupMember)
                {
                    case VXRBaseInteractor interactor:
                        if (!m_RegisteredInteractionManager.IsRegistered(interactor))
                            continue;

                        var preventInteraction = prePrioritizedInteractor != null && interactor != prePrioritizedInteractor;
                        UpdateInteractorInteractions(interactor, preventInteraction, out var performedInteraction);
                        if (performedInteraction)
                        {
                            interactorThatPerformedInteraction = interactor;
                            prePrioritizedInteractor = interactor;
                        }

                        break;
                    case IXRInteractionGroup group:
                        if (!m_RegisteredInteractionManager.IsRegistered(group))
                            continue;

                        group.UpdateGroupMemberInteractions(prePrioritizedInteractor, out var interactorInSubGroupThatPerformedInteraction);
                        if (interactorInSubGroupThatPerformedInteraction != null)
                        {
                            interactorThatPerformedInteraction = interactorInSubGroupThatPerformedInteraction;
                            prePrioritizedInteractor = interactorInSubGroupThatPerformedInteraction;
                        }

                        break;
                }
            }

            _isProcessingGroupMembers = false;
            activeInteractor = interactorThatPerformedInteraction;
        }

        /// <inheritdoc />
        bool IXRInteractionOverrideGroup.ShouldOverrideActiveInteraction(out VXRBaseInteractor overridingInteractor)
        {
            overridingInteractor = null;
            if (activeInteractor == null ||
                !TryGetOverridesForContainedInteractor(activeInteractor, out var activeMemberOverrides))
            {
                return false;
            }

            // Iterate through group members rather than the overrides set so we can ensure that priority is respected
            var shouldOverride = false;
            _isProcessingGroupMembers = true;
            foreach (var groupMember in _groupMembers.RegisteredSnapshot)
            {
                if (!_groupMembers.IsStillRegistered(groupMember) || !activeMemberOverrides.Contains(groupMember))
                    continue;

                if (ShouldGroupMemberOverrideInteraction(activeInteractor, groupMember, out overridingInteractor))
                {
                    shouldOverride = true;
                    break;
                }
            }

            _isProcessingGroupMembers = false;
            return shouldOverride;
        }

        /// <summary>
        /// Tries to find the set of overrides for <paramref name="interactor"/> or overrides for the member Group that
        /// contains <paramref name="interactor"/> if <paramref name="interactor"/> is nested.
        /// </summary>
        /// <param name="interactor">The contained interactor to check against.</param>
        /// <param name="overrideGroupMembers">The set of override Group members for <paramref name="interactor"/> or
        /// overrides for the member Group that contains <paramref name="interactor"/>.</param>
        /// <returns>
        /// Returns <see langword="true"/> if <paramref name="interactor"/> has overrides or a member Group
        /// containing <paramref name="interactor"/> has overrides, <see langword="false"/> otherwise.
        /// </returns>
        bool TryGetOverridesForContainedInteractor(VXRBaseInteractor interactor, out HashSet<IXRGroupMember> overrideGroupMembers)
        {
            overrideGroupMembers = null;
            if (!(interactor is IXRGroupMember interactorAsGroupMember))
            {
                Debug.LogError($"Interactor {interactor} must be a {nameof(IXRGroupMember)}.", this);
                return false;
            }

            // If the interactor is nested, bubble up to find the top-level member Group that contains the interactor.
            var nextContainingGroup = interactorAsGroupMember.ContainingGroup;
            var groupMemberForInteractor = interactorAsGroupMember;
            while (nextContainingGroup != null && !ReferenceEquals(nextContainingGroup, this))
            {
                if (nextContainingGroup is IXRGroupMember groupMemberGroup)
                {
                    nextContainingGroup = groupMemberGroup.ContainingGroup;
                    groupMemberForInteractor = groupMemberGroup;
                }
                else
                {
                    nextContainingGroup = null;
                }
            }

            if (nextContainingGroup == null)
            {
                Debug.LogError($"Interactor {interactor} must be contained by this group or one of its sub-groups.", this);
                return false;
            }

            return _interactionOverridesMap.TryGetValue(groupMemberForInteractor, out overrideGroupMembers);
        }

        /// <inheritdoc />
        bool IXRInteractionOverrideGroup.ShouldAnyMemberOverrideInteraction(VXRBaseInteractor interactingInteractor,
            out VXRBaseInteractor overridingInteractor)
        {
            overridingInteractor = null;
            var shouldOverride = false;
            _isProcessingGroupMembers = true;
            foreach (var groupMember in _groupMembers.RegisteredSnapshot)
            {
                if (!_groupMembers.IsStillRegistered(groupMember))
                    continue;

                if (ShouldGroupMemberOverrideInteraction(interactingInteractor, groupMember, out overridingInteractor))
                {
                    shouldOverride = true;
                    break;
                }
            }

            _isProcessingGroupMembers = false;
            return shouldOverride;
        }

        private bool ShouldGroupMemberOverrideInteraction(VXRBaseInteractor interactingInteractor,
            IXRGroupMember overrideGroupMember, out VXRBaseInteractor overridingInteractor)
        {
            overridingInteractor = null;
            switch (overrideGroupMember)
            {
                case VXRBaseInteractor interactor:
                    if (!m_RegisteredInteractionManager.IsRegistered(interactor))
                        return false;

                    if (ShouldInteractorOverrideInteraction(interactingInteractor, interactor))
                    {
                        overridingInteractor = interactor;
                        return true;
                    }

                    break;
                case IXRInteractionOverrideGroup group:
                    if (!m_RegisteredInteractionManager.IsRegistered(group))
                        return false;

                    if (group.ShouldAnyMemberOverrideInteraction(interactingInteractor, out overridingInteractor))
                        return true;

                    break;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given <paramref name="overridingInteractor"/> should override the active interaction of
        /// <paramref name="interactingInteractor"/> - that is, whether <paramref name="overridingInteractor"/> can
        /// select any interactable that <paramref name="interactingInteractor"/> is interacting with.
        /// </summary>
        /// <param name="interactingInteractor">The interactor that is currently interacting with at least one interactable.</param>
        /// <param name="overridingInteractor">The interactor that is capable of overriding the interaction of <paramref name="interactingInteractor"/>.</param>
        /// <returns>True if <paramref name="overridingInteractor"/> should override the active interaction of
        /// <paramref name="interactingInteractor"/>, false otherwise.</returns>
        private bool ShouldInteractorOverrideInteraction(VXRBaseInteractor interactingInteractor, VXRBaseInteractor overridingInteractor)
        {
            var interactingSelectInteractor = interactingInteractor as VXRBaseInteractor;
            var interactingHoverInteractor = interactingInteractor as VXRBaseInteractor;
            m_RegisteredInteractionManager.GetValidTargets(overridingInteractor, _validTargets);
            foreach (var target in _validTargets)
            {
                if (!(target is IXRSelectInteractable selectInteractable) ||
                    !m_RegisteredInteractionManager.CanSelect(overridingInteractor, selectInteractable))
                {
                    continue;
                }

                if (interactingSelectInteractor != null && interactingSelectInteractor.IsSelecting(selectInteractable))
                    return true;

                if (interactingHoverInteractor != null && target is IXRHoverInteractable hoverInteractable &&
                    interactingHoverInteractor.IsHovering(hoverInteractable))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateInteractorInteractions(VXRBaseInteractor interactor, bool preventInteraction, out bool performedInteraction)
        {
            performedInteraction = false;

            using (VXRInteractionManager.s_GetValidTargetsMarker.Auto())
                m_RegisteredInteractionManager.GetValidTargets(interactor, _validTargets);
            
            var selectInteractor = interactor;
            var hoverInteractor = interactor;

            if (selectInteractor != null)
            {
                using (VXRInteractionManager.s_EvaluateInvalidSelectionsMarker.Auto())
                {
                    if (preventInteraction)
                        ClearAllInteractorSelections(selectInteractor);
                    else
                        m_RegisteredInteractionManager.ClearInteractorSelection(selectInteractor, _validTargets);
                }
            }

            if (hoverInteractor != null)
            {
                using (VXRInteractionManager.s_EvaluateInvalidHoversMarker.Auto())
                {
                    if (preventInteraction)
                        ClearAllInteractorHovers(hoverInteractor);
                    else
                        m_RegisteredInteractionManager.ClearInteractorHover(hoverInteractor, _validTargets);
                }
            }

            if (preventInteraction)
                return;

            if (selectInteractor != null)
            {
                using (VXRInteractionManager.s_EvaluateValidSelectionsMarker.Auto())
                    m_RegisteredInteractionManager.InteractorSelectValidTargets(selectInteractor, _validTargets);

                // Alternatively check if the base interactor is interacting with UGUI
                // TODO move this api call to IUIInteractor for XRI 3.0
                if (selectInteractor.HasSelection || (interactor is VXRBaseInteractor baseInteractor && baseInteractor.IsInteractingWithUI))
                    performedInteraction = true;
            }

            if (hoverInteractor != null)
            {
                using (VXRInteractionManager.s_EvaluateValidHoversMarker.Auto())
                    m_RegisteredInteractionManager.InteractorHoverValidTargets(hoverInteractor, _validTargets);

                if (hoverInteractor.HasHover)
                    performedInteraction = true;
            }
        }

        private void ClearAllInteractorSelections(VXRBaseInteractor selectInteractor)
        {
            if (selectInteractor.InteractablesSelected.Count == 0)
                return;

            s_InteractablesSelected.Clear();
            s_InteractablesSelected.AddRange(selectInteractor.InteractablesSelected);
            for (var i = s_InteractablesSelected.Count - 1; i >= 0; --i)
            {
                var interactable = s_InteractablesSelected[i];
                m_RegisteredInteractionManager.SelectExit(selectInteractor, interactable);
            }
        }

        private void ClearAllInteractorHovers(VXRBaseInteractor hoverInteractor)
        {
            if (hoverInteractor.InteractablesHovered.Count == 0)
                return;

            s_InteractablesHovered.Clear();
            s_InteractablesHovered.AddRange(hoverInteractor.InteractablesHovered);
            for (var i = s_InteractablesHovered.Count - 1; i >= 0; --i)
            {
                var interactable = s_InteractablesHovered[i];
                m_RegisteredInteractionManager.HoverExit(hoverInteractor, interactable);
            }
        }

        /// <inheritdoc />
        public void OnFocusEntering(FocusEnterEventArgs args)
        {
            focusInteractable = args.interactableObject;
            focusInteractor = args.interactorObject;
        }

        /// <inheritdoc />
        public void OnFocusExiting(FocusExitEventArgs args)
        {
            if (focusInteractable == args.interactableObject)
            {
                focusInteractable = null;
                focusInteractor = null;
            }
        }

        /// <inheritdoc />
        void IXRGroupMember.OnRegisteringAsGroupMember(IXRInteractionGroup group)
        {
            if (ContainingGroup != null)
            {
                Debug.LogError($"{name} is already part of a Group. Remove the member from the Group first.", this);
                return;
            }

            if (!group.ContainsGroupMember(this))
            {
                Debug.LogError($"{nameof(IXRGroupMember.OnRegisteringAsGroupMember)} was called but the Group does not contain {name}. " +
                               "Add the member to the Group rather than calling this method directly.", this);
                return;
            }

            ContainingGroup = group;
        }

        /// <inheritdoc />
        void IXRGroupMember.OnRegisteringAsNonGroupMember()
        {
            ContainingGroup = null;
        }
    }
}
