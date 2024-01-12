using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Vapor.Utilities
{
    /// <summary>
    /// Use this class to maintain a registration of items (like Interactors or Interactables). This maintains
    /// a synchronized list that stays constant until buffered registration status changes are
    /// explicitly committed.
    /// </summary>
    /// <typeparam name="T">The type of object to register.</typeparam>
    /// <remarks>
    /// Items like Interactors and Interactables may be registered or unregistered (such as from an Interaction Manager)
    /// at any time, including when processing those items. This class can be used to manage those registration changes.
    /// For consistency with the functionality of Unity components which do not have
    /// Update called the same frame in which they are enabled, disabled, or destroyed,
    /// this class will maintain multiple lists to achieve that desired result with processing
    /// the items, these lists are pooled and reused between instances.
    /// </remarks>
    public abstract class BaseRegistrationList<T>
    {
        /// <summary>
        /// Reusable list of buffered items (used to avoid unnecessary allocations when items to be added or removed
        /// need to be monitored).
        /// </summary>
        private static readonly LinkedPool<List<T>> BufferedListPool = new(() => new List<T>(), actionOnRelease: list => list.Clear(), collectionCheck: false);

        /// <summary>
        /// A snapshot of registered items that should potentially be processed this update phase of the current frame.
        /// The count of items shall only change upon a call to <see cref="Flush"/>.
        /// </summary>
        /// <remarks>
        /// Items being in this collection does not imply that the item is currently registered.
        /// <br />
        /// Logically this should be a <see cref="IReadOnlyList{T}"/> but is kept as a <see cref="List{T}"/>
        /// to avoid allocations when iterating. Use <see cref="Register"/> and <see cref="Unregister"/>
        /// instead of directly changing this list.
        /// </remarks>
        public List<T> RegisteredSnapshot { get; } = new List<T>();

        /// <summary>
        /// Gets the number of registered items including the effective result of buffered registration changes.
        /// </summary>
        public int FlushedCount => RegisteredSnapshot.Count - BufferedRemoveCount + BufferedAddCount;

        /// <summary>
        /// List with buffered items to be added when calling <see cref="Flush"/>.
        /// The count of items shall only change upon a call to <see cref="AddToBufferedAdd"/>,
        /// <see cref="RemoveFromBufferedAdd"/> or <see cref="ClearBufferedAdd"/>.
        /// This list can be <see langword="null"/>, use <see cref="BufferedAddCount"/> to check if there are elements
        /// before directly accessing it. A new list is pooled from <see cref="BufferedListPool"/> when needed.
        /// </summary>
        /// <remarks>
        /// Logically this should be a <see cref="IReadOnlyList{T}"/> but is kept as a <see cref="List{T}"/> to avoid
        /// allocations when iterating.
        /// </remarks>
        protected List<T> BufferedAdd;

        /// <summary>
        /// List with buffered items to be removed when calling <see cref="Flush"/>.
        /// The count of items shall only change upon a call to <see cref="AddToBufferedRemove"/>,
        /// <see cref="RemoveFromBufferedRemove"/> or <see cref="ClearBufferedRemove"/>.
        /// This list can be <see langword="null"/>, use <see cref="BufferedRemoveCount"/> to check if there are elements
        /// before directly accessing it. A new list is pooled from <see cref="BufferedListPool"/> when needed.
        /// </summary>
        /// <remarks>
        /// Logically this should be a <see cref="IReadOnlyList{T}"/> but is kept as a <see cref="List{T}"/> to avoid
        /// allocations when iterating.
        /// </remarks>
        protected List<T> BufferedRemove;

        /// <summary>
        /// The number of buffered items to be added when calling <see cref="Flush"/>.
        /// </summary>
        protected int BufferedAddCount => BufferedAdd?.Count ?? 0;

        /// <summary>
        /// The number of buffered items to be removed when calling <see cref="Flush"/>.
        /// </summary>
        protected int BufferedRemoveCount => BufferedRemove?.Count ?? 0;

        /// <summary>
        /// Adds the given item to the <see cref="BufferedAdd"/> list.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <remarks>
        /// Gets a new list from the <see cref="BufferedListPool"/> if needed.
        /// </remarks>
        protected void AddToBufferedAdd(T item)
        {
            BufferedAdd ??= BufferedListPool.Get();
            BufferedAdd.Add(item);
        }

        /// <summary>
        /// Removes the given item from the <see cref="BufferedAdd"/> list.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        /// <returns>Returns <see langword="true"/> if the item was successfully removed. Otherwise, returns <see langword="false"/>.</returns>
        protected bool RemoveFromBufferedAdd(T item) => BufferedAdd != null && BufferedAdd.Remove(item);

        /// <summary>
        /// Removes all items from the <see cref="BufferedAdd"/> and returns this list to the pool (<see cref="BufferedListPool"/>).
        /// </summary>
        protected void ClearBufferedAdd()
        {
            if (BufferedAdd == null)
                return;

            BufferedListPool.Release(BufferedAdd);
            BufferedAdd = null;
        }

        /// <summary>
        /// Adds the given item to the <see cref="BufferedRemove"/> list.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <remarks>
        /// Gets a new list from the <see cref="BufferedListPool"/> if needed.
        /// </remarks>
        protected void AddToBufferedRemove(T item)
        {
            BufferedRemove ??= BufferedListPool.Get();
            BufferedRemove.Add(item);
        }

        /// <summary>
        /// Removes the given item from the <see cref="BufferedRemove"/> list.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        /// <returns>Returns <see langword="true"/> if the item was successfully removed. Otherwise, returns <see langword="false"/>.</returns>
        protected bool RemoveFromBufferedRemove(T item) => BufferedRemove != null && BufferedRemove.Remove(item);

        /// <summary>
        /// Removes all items from the <see cref="BufferedRemove"/> and returns tis list to the pool (<see cref="BufferedListPool"/>).
        /// </summary>
        protected void ClearBufferedRemove()
        {
            if (BufferedRemove == null)
                return;

            BufferedListPool.Release(BufferedRemove);
            BufferedRemove = null;
        }

        /// <summary>
        /// Checks the registration status of <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The item to query.</param>
        /// <returns>Returns <see langword="true"/> if registered. Otherwise, returns <see langword="false"/>.</returns>
        /// <remarks>
        /// This includes pending changes that have not yet been pushed to <see cref="RegisteredSnapshot"/>.
        /// </remarks>
        /// <seealso cref="IsStillRegistered"/>
        public abstract bool IsRegistered(T item);

        /// <summary>
        /// Faster variant of <see cref="IsRegistered"/> that assumes that the <paramref name="item"/> is in the snapshot.
        /// It short circuits the check when there are no pending changes to unregister, which is usually the case.
        /// </summary>
        /// <param name="item">The item to query.</param>
        /// <returns>Returns <see langword="true"/> if registered</returns>
        /// <remarks>
        /// This includes pending changes that have not yet been pushed to <see cref="RegisteredSnapshot"/>.
        /// Use this method instead of <see cref="IsRegistered"/> when iterating over <see cref="RegisteredSnapshot"/>
        /// for improved performance.
        /// </remarks>
        /// <seealso cref="IsRegistered"/>
        public abstract bool IsStillRegistered(T item);

        /// <summary>
        /// Register <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The item to register.</param>
        /// <returns>Returns <see langword="true"/> if a change in registration status occurred. Otherwise, returns <see langword="false"/>.</returns>
        public abstract bool Register(T item);

        /// <summary>
        /// Unregister <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The item to unregister.</param>
        /// <returns>Returns <see langword="true"/> if a change in registration status occurred. Otherwise, returns <see langword="false"/>.</returns>
        public abstract bool Unregister(T item);

        /// <summary>
        /// Flush pending registration changes into <see cref="RegisteredSnapshot"/>.
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Return all registered items into List <paramref name="results"/> in the order they were registered.
        /// </summary>
        /// <param name="results">List to receive registered items.</param>
        /// <remarks>
        /// Clears <paramref name="results"/> before adding to it.
        /// </remarks>
        public abstract void GetRegisteredItems(List<T> results);

        /// <summary>
        /// Returns the registered item at <paramref name="index"/> based on the order they were registered.
        /// </summary>
        /// <param name="index">Index of the item to return. Must be smaller than <see cref="FlushedCount"/> and not negative.</param>
        /// <returns>Returns the item at the given index.</returns>
        public abstract T GetRegisteredItemAt(int index);

        /// <summary>
        /// Moves the given item in the registration list. Takes effect immediately without calling <see cref="Flush"/>.
        /// If the item is not in the registration list, this can be used to insert the item at the specified index.
        /// </summary>
        /// <param name="item">The item to move or register.</param>
        /// <param name="newIndex">New index of the item.</param>
        /// <returns>Returns <see langword="true"/> if the item was registered as a result of this method, otherwise returns <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Throws when there are pending registration changes that have not been flushed.</exception>
        public bool MoveItemImmediately(T item, int newIndex)
        {
            if (BufferedRemoveCount != 0 || BufferedAddCount != 0)
                throw new InvalidOperationException("Cannot move item when there are pending registration changes that have not been flushed.");

            var currentIndex = RegisteredSnapshot.IndexOf(item);
            if (currentIndex == newIndex)
                return false;

            if (currentIndex >= 0)
                RegisteredSnapshot.RemoveAt(currentIndex);

            RegisteredSnapshot.Insert(newIndex, item);
            OnItemMovedImmediately(item, newIndex);
            return currentIndex < 0;
        }

        /// <summary>
        /// Called after the given item has been inserted at or moved to the specified index.
        /// </summary>
        /// <param name="item">The item that was moved or registered.</param>
        /// <param name="newIndex">New index of the item.</param>
        protected virtual void OnItemMovedImmediately(T item, int newIndex)
        {
        }

        /// <summary>
        /// Unregister all currently registered items. Starts from the last registered item and proceeds backward
        /// until the first registered item is unregistered.
        /// </summary>
        public void UnregisterAll()
        {
            using (BufferedListPool.Get(out var registeredItems))
            {
                GetRegisteredItems(registeredItems);
                for (var i = registeredItems.Count - 1; i >= 0; --i)
                    Unregister(registeredItems[i]);
            }
        }

        protected static void EnsureCapacity(List<T> list, int capacity)
        {
            if (list.Capacity < capacity)
                list.Capacity = capacity;
        }
    }

    /// <inheritdoc />
    public class RegistrationList<T> : BaseRegistrationList<T>
    {
        private readonly HashSet<T> _unorderedBufferedAdd = new HashSet<T>();
        private readonly HashSet<T> _unorderedBufferedRemove = new HashSet<T>();
        private readonly HashSet<T> _unorderedRegisteredSnapshot = new HashSet<T>();
        private readonly HashSet<T> _unorderedRegisteredItems = new HashSet<T>();

        /// <inheritdoc />
        public override bool IsRegistered(T item) => _unorderedRegisteredItems.Contains(item);

        /// <inheritdoc />
        public override bool IsStillRegistered(T item) => _unorderedBufferedRemove.Count == 0 || !_unorderedBufferedRemove.Contains(item);

        /// <inheritdoc />
        public override bool Register(T item)
        {
            if (_unorderedBufferedAdd.Count > 0 && _unorderedBufferedAdd.Contains(item))
                return false;

            var snapshotContainsItem = _unorderedRegisteredSnapshot.Contains(item);
            if ((_unorderedBufferedRemove.Count > 0 && _unorderedBufferedRemove.Remove(item)) || !snapshotContainsItem)
            {
                RemoveFromBufferedRemove(item);
                _unorderedRegisteredItems.Add(item);
                if (!snapshotContainsItem)
                {
                    AddToBufferedAdd(item);
                    _unorderedBufferedAdd.Add(item);
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override bool Unregister(T item)
        {
            if (_unorderedBufferedRemove.Count > 0 && _unorderedBufferedRemove.Contains(item))
                return false;

            if (_unorderedBufferedAdd.Count > 0 && _unorderedBufferedAdd.Remove(item))
            {
                RemoveFromBufferedAdd(item);
                _unorderedRegisteredItems.Remove(item);
                return true;
            }

            if (_unorderedRegisteredSnapshot.Contains(item))
            {
                AddToBufferedRemove(item);
                _unorderedBufferedRemove.Add(item);
                _unorderedRegisteredItems.Remove(item);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override void Flush()
        {
            // This method is called multiple times each frame,
            // so additional explicit Count checks are done for
            // performance.
            if (BufferedRemoveCount > 0)
            {
                foreach (var item in BufferedRemove)
                {
                    RegisteredSnapshot.Remove(item);
                    _unorderedRegisteredSnapshot.Remove(item);
                }

                ClearBufferedRemove();
                _unorderedBufferedRemove.Clear();
            }

            if (BufferedAddCount > 0)
            {
                foreach (var item in BufferedAdd)
                {
                    if (!_unorderedRegisteredSnapshot.Contains(item))
                    {
                        RegisteredSnapshot.Add(item);
                        _unorderedRegisteredSnapshot.Add(item);
                    }
                }

                ClearBufferedAdd();
                _unorderedBufferedAdd.Clear();
            }
        }

        /// <inheritdoc />
        public override void GetRegisteredItems(List<T> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            EnsureCapacity(results, FlushedCount);
            foreach (var item in RegisteredSnapshot)
            {
                if (_unorderedBufferedRemove.Count > 0 && _unorderedBufferedRemove.Contains(item))
                    continue;

                results.Add(item);
            }

            if (BufferedAddCount > 0)
                results.AddRange(BufferedAdd);
        }

        /// <inheritdoc />
        public override T GetRegisteredItemAt(int index)
        {
            if (index < 0 || index >= FlushedCount)
                throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the registration collection.");

            if (BufferedRemoveCount == 0 && BufferedAddCount == 0)
                return RegisteredSnapshot[index];

            if (index >= RegisteredSnapshot.Count - BufferedRemoveCount)
                return BufferedAdd[index - (RegisteredSnapshot.Count - BufferedRemoveCount)];

            var effectiveIndex = 0;
            foreach (var item in RegisteredSnapshot)
            {
                if (_unorderedBufferedRemove.Contains(item))
                    continue;

                if (effectiveIndex == index)
                    return RegisteredSnapshot[index];

                ++effectiveIndex;
            }

            // Unreachable code
            throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the registration collection.");
        }

        /// <inheritdoc />
        protected override void OnItemMovedImmediately(T item, int newIndex)
        {
            base.OnItemMovedImmediately(item, newIndex);
            _unorderedRegisteredItems.Add(item);
            _unorderedRegisteredSnapshot.Add(item);
        }
    }
}
