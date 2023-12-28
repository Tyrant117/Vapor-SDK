using System.Text;

namespace Vapor.Utilities
{
    public static class TooltipMarkup
    {
        public const string LangWordStart = "<b><color=#3D8FD6FF>";
        public const string LangWordEnd = "</color></b>";

        public const string InterfaceStart = "<b><color=#B8D7A1FF>";
        public const string InterfaceEnd = "</color></b>";

        public const string ClassStart = "<b><color=#4AC9B0FF>";
        public const string ClassEnd = "</color></b>";

        public const string MethodStart = "<b><color=white>";
        public const string MethodEnd = "</color></b>";

        public static string LangWordMarkup(string langword) => $"{LangWordStart}{langword}{LangWordEnd}";
        public static string InterfaceMarkup(string interfaceName) => $"{InterfaceStart}{interfaceName}{InterfaceEnd}";
        public static string ClassMarkup(string className) => $"{ClassStart}{className}{ClassEnd}";
        public static string MethodMarkup(string methodName) => $"{MethodStart}{methodName}{MethodEnd}";

        public static string FormatMarkupString(string tooltip)
        {
            var sb = new StringBuilder(tooltip);
            sb.Replace("<lw>", LangWordStart);
            sb.Replace("</lw>", LangWordEnd);
            sb.Replace("<itf>", InterfaceStart);
            sb.Replace("</itf>", InterfaceEnd);
            sb.Replace("<cls>", ClassStart);
            sb.Replace("</cls>", ClassEnd);
            sb.Replace("<mth>", MethodStart);
            sb.Replace("</mth>", MethodEnd);
            return sb.ToString();
        }
    }
}
