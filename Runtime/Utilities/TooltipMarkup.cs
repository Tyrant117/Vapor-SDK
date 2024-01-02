using System.Text;

namespace Vapor.Utilities
{
    /// <summary>
    /// A static class that can be used to add rich text colors to inspector tooltips.
    /// </summary>
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

        private static readonly StringBuilder Sb = new();

        public static string LangWordMarkup(string langWord) => $"{LangWordStart}{langWord}{LangWordEnd}";
        public static string InterfaceMarkup(string interfaceName) => $"{InterfaceStart}{interfaceName}{InterfaceEnd}";
        public static string ClassMarkup(string className) => $"{ClassStart}{className}{ClassEnd}";
        public static string MethodMarkup(string methodName) => $"{MethodStart}{methodName}{MethodEnd}";

        public static string FormatMarkupString(string tooltip)
        {
            Sb.Clear();
            Sb.Replace("<lw>", LangWordStart);
            Sb.Replace("</lw>", LangWordEnd);
            Sb.Replace("<itf>", InterfaceStart);
            Sb.Replace("</itf>", InterfaceEnd);
            Sb.Replace("<cls>", ClassStart);
            Sb.Replace("</cls>", ClassEnd);
            Sb.Replace("<mth>", MethodStart);
            Sb.Replace("</mth>", MethodEnd);
            return Sb.ToString();
        }
    }
}
