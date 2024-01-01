using System.Runtime.CompilerServices;
using UnityEngine;

namespace VaporNetcode
{
    public static class Convienence
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPointInScreen(Vector2 point) =>
            0 <= point.x && point.x < Screen.width &&
            0 <= point.y && point.y < Screen.height;
    }
}
