using System;
using System.Collections.Generic;
using Vapor.Utilities;

namespace Vapor.Utilities
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    /// <typeparam name="T"><inheritdoc /></typeparam>
    /// <remarks>
    /// <inheritdoc />
    /// <para>
    /// This is a variation of <see cref="RegistrationList{T}"/> that can be used when changes in registration
    /// is done infrequently. This class is also smaller in size since it does not have <see cref="HashSet{T}"/> fields
    /// which are more useful when the number of registered items is large and when registration changes happens
    /// more frequently.
    /// </para>
    /// </remarks>
    /// <seealso cref="RegistrationList{T}"/>
    public class SmallRegistrationList<T> : BaseRegistrationList<T>
    {
        private bool _bufferChanges = true;

        /// <summary>
        /// Whether this list should buffer changes, the default value is <see langword="true"/>.
        /// Assign a <see langword="false"/> value to make all changes take effect immediately. This is useful when
        /// changes should be buffered only when the list is being processed.
        /// </summary>
        /// <remarks>
        /// When assign a value, and if needed, this property automatically calls <see cref="Flush"/> to guarantee the
        /// order of buffered changes.
        /// </remarks>
        public bool BufferChanges
        {
            get => _bufferChanges;
            set
            {
                if (_bufferChanges && value == false)
                {
                    _bufferChanges = false;
                    Flush();
                }
                else
                {
                    _bufferChanges = value;
                }
            }
        }

        /// <inheritdoc />
        public override bool IsRegistered(T item) => (BufferedAddCount > 0 && BufferedAdd.Contains(item)) ||
            (RegisteredSnapshot.Count > 0 && RegisteredSnapshot.Contains(item) && IsStillRegistered(item));

        /// <inheritdoc />
        public override bool IsStillRegistered(T item) => BufferedRemoveCount == 0 || !BufferedRemove.Contains(item);

        /// <inheritdoc />
        public override bool Register(T item)
        {
            if (!BufferChanges)
            {
                if (RegisteredSnapshot.Contains(item))
                    return false;

                RegisteredSnapshot.Add(item);
                return true;
            }

            if (BufferedAddCount > 0 && BufferedAdd.Contains(item))
                return false;

            var snapshotContainsItem = RegisteredSnapshot.Contains(item);
            if ((BufferedRemoveCount > 0 && RemoveFromBufferedRemove(item)) || !snapshotContainsItem)
            {
                if (!snapshotContainsItem)
                    AddToBufferedAdd(item);

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override bool Unregister(T item)
        {
            if (!BufferChanges)
                return RegisteredSnapshot.Remove(item);

            if (BufferedRemoveCount > 0 && BufferedRemove.Contains(item))
                return false;

            if (BufferedAddCount > 0 && RemoveFromBufferedAdd(item))
                return true;

            if (RegisteredSnapshot.Contains(item))
            {
                AddToBufferedRemove(item);
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
                }

                ClearBufferedRemove();
            }

            if (BufferedAddCount > 0)
            {
                foreach (var item in BufferedAdd)
                {
                    if (!RegisteredSnapshot.Contains(item))
                    {
                        RegisteredSnapshot.Add(item);
                    }
                }

                ClearBufferedAdd();
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
                if (BufferedRemoveCount > 0 && BufferedRemove.Contains(item))
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
                if (BufferedRemoveCount > 0 && BufferedRemove.Contains(item))
                    continue;

                if (effectiveIndex == index)
                    return RegisteredSnapshot[index];

                ++effectiveIndex;
            }

            // Unreachable code
            throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the registration collection.");
        }
    }
}
