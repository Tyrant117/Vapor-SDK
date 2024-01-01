using System;
using UnityEditor.Search;

namespace Vapor.ObjectLogging
{
    [Serializable]
    public class RichStringLog
    {
        public int Type;
        public string Content;
        public string StackTrace;
        public DateTimeOffset TimeStamp;

        public bool IsMatch(string searchString)
        {
#if UNITY_EDITOR
            return FuzzySearch.FuzzyMatch(searchString, Content);
#else
            return false;
#endif
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Content, TimeStamp);
        }
    }
}
