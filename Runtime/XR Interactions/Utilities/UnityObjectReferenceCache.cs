
using UnityEngine;

namespace VaporXR.Utilities
{
    /// <summary>
    /// A cache for a Unity Object reference that is used to avoid the overhead of the Unity Object alive check
    /// every time the reference is accessed after the first time.
    /// </summary>
    /// <typeparam name="T">The type of the serialized field.</typeparam>
    public class UnityObjectReferenceCache<T> where T : Object
    {
        private T _capturedField;
        private T _fieldOrNull;

        /// <summary>
        /// A fast but unsafe method for getting the field without doing a Unity Object alive check after the first time.
        /// The purpose is to avoid the overhead of the Unity Object alive check when it is known that the reference is alive,
        /// but does not have the rigor of detecting when the Object is deleted or destroyed after the first check, which should be very rare.
        /// This will handle the user modifying the field in the Inspector window by invalidating the cached version.
        /// </summary>
        /// <param name="field">The serialized field to get.</param>
        /// <param name="fieldOrNull">The Unity Object or actual <see langword="null"/>.</param>
        /// <returns>Returns <see langword="true"/> if the reference is not null. Otherwise, returns <see langword="false"/>.</returns>
        public bool TryGet(T field, out T fieldOrNull)
        {
            if (ReferenceEquals(_capturedField, field))
            {
                fieldOrNull = _fieldOrNull;
#pragma warning disable UNT0029 // bypass Unity Object alive check
                return _fieldOrNull is not null;
#pragma warning restore UNT0029
            }

            _capturedField = field;
            if (field != null)
            {
                _fieldOrNull = field;
                fieldOrNull = field;
                return true;
            }

            _fieldOrNull = null;
            fieldOrNull = null;
            return false;
        }
    }

    /// <summary>
    /// A cache for a serialized Unity Object reference that represents an interface type.
    /// </summary>
    /// <typeparam name="TInterface">Interface that the reference Unity Object should implement.</typeparam>
    /// <typeparam name="TObject">Serialized field type, usually Unity <see cref="Object"/>.</typeparam>
    /// <seealso cref="RequireInterfaceAttribute"/>
    public class UnityObjectReferenceCache<TInterface, TObject> where TInterface : class where TObject : Object
    {
        private TObject _capturedObject;
        private TInterface _interface;

        /// <summary>
        /// Gets the interface-typed Object reference.
        /// </summary>
        /// <param name="field">The serialized field to get.</param>
        /// <returns>Returns the interface-typed Object reference, which may be <see langword="null"/>.</returns>
        public TInterface Get(TObject field)
        {
            if (ReferenceEquals(_capturedObject, field))
            {
                return _interface;
            }

            _capturedObject = field;
            _interface = field as TInterface;

            return _interface;
        }

        /// <summary>
        /// Sets the Object reference to the interface-typed reference.
        /// </summary>
        /// <param name="field">The serialized field to set.</param>
        /// <param name="value">The interface-typed value.</param>
        // ReSharper disable once RedundantAssignment -- ref field is used to update the serialized field with the new value
        public void Set(ref TObject field, TInterface value)
        {
            field = value as TObject;
            _capturedObject = field;
            _interface = value;
        }
    }
}
