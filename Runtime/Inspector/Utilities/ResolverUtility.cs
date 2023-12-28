using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporInspector
{
    public static class ResolverUtility
    {
        public static bool HasResolver(string resolver, out ResolverType type)
        {
            type = ResolverType.None;
            if (!string.IsNullOrEmpty(resolver))
            {
                var first = resolver[0];
                var hasResolver = first.Equals('$') || first.Equals('@') || first.Equals('%');
                if (hasResolver)
                {
                    type = first.Equals('$') ? ResolverType.Property : first.Equals('@') ? ResolverType.Method : ResolverType.Field;
                }
                return hasResolver;
            }
            else
            {
                return false;
            }
        }

        public static Color GetColor(string colorStringResolver, Color defaultColor, out ResolverType resolver)
        {
            var color = defaultColor;
            resolver = ResolverType.None;
            if (string.IsNullOrEmpty(colorStringResolver))
            {
                return color;
            }
            else
            {
                char first = colorStringResolver[0];
                if (first.Equals('#'))
                {
                    color = ColorUtility.TryParseHtmlString(colorStringResolver, out var htmlColor) ? htmlColor : Color.white;
                }
                else if (HasResolver(colorStringResolver, out var type))
                {
                    resolver = type;
                }
                else
                {
                    var split = colorStringResolver.Split(',');
                    if (split.Length == 4)
                    {
                        color = float.TryParse(split[0], out var r) && float.TryParse(split[1], out var g) && float.TryParse(split[2], out var b) && float.TryParse(split[3], out var a)
                            ? new Color(r, g, b, a)
                            : Color.white;
                    }
                    else if (split.Length == 3)
                    {
                        color = float.TryParse(split[0], out var r) && float.TryParse(split[1], out var g) && float.TryParse(split[2], out var b)
                            ? new Color(r, g, b)
                            : Color.white;
                    }
                    else
                    {
                        color = Color.white;
                    }
                }
                return color;
            }
        }
    }
}
