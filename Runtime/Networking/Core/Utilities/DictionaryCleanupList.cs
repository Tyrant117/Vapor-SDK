using System.Collections.Generic;

namespace VaporNetcode
{
    /// <summary>
    /// This class is used to cleanup unused dictionary keys while still being an O(n) removal operation.
    /// The idea is to use this for cleanup, when cleanup needs to be done conistently on an update schedule.
    /// T is the key to the dictionary.
    /// </summary>
    public class DictionaryCleanupList<T,V>
    {
        private int _pointer;
        public T[] Content;

        public DictionaryCleanupList(int capacity)
        {
            _pointer = 0;
            Content = new T[capacity];
        }

        public void Add(T key)
        {
            if (_pointer == Content.Length)
            {
                var buffer = new T[Content.Length * 2];
                Content.CopyTo(buffer, 0);
                Content = buffer;
                Content[_pointer] = key;
            }
            else
            {
                Content[_pointer] = key;
            }
            _pointer++;
        }

        public void RemoveAll(Dictionary<T, V> db)
        {
            while (_pointer > 0)
            {
                _pointer--;
                db.Remove(Content[_pointer]);
            }
        }
    }
}