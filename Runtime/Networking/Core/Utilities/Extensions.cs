using System;

namespace VaporNetcode
{
    public static class Extensions
    {
        public static string ToHexString(this ArraySegment<byte> segment) => BitConverter.ToString(segment.Array, segment.Offset, segment.Count);

        // string.GetHashCode is not guaranteed to be the same on all
        // machines, but we need one that is the same on all machines.
        // NOTE: Do not call this from hot path because it's slow O(N) for long method names.
        // - As of 2012-02-16 There are 2 design-time callers (weaver) and 1 runtime caller that caches.
        public static int GetStableHashCode(this string text)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in text)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }
    }
}
