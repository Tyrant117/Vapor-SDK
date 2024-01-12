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

        public const string StructStart = "<b><color=#85C490FF>";
        public const string StructEnd = "</color></b>";

        private static readonly StringBuilder s_Sb = new();

        public static string LangWordMarkup(string langWord) => $"{LangWordStart}{langWord}{LangWordEnd}";
        public static string InterfaceMarkup(string interfaceName) => $"{InterfaceStart}{interfaceName}{InterfaceEnd}";
        public static string ClassMarkup(string className) => $"{ClassStart}{className}{ClassEnd}";
        public static string MethodMarkup(string methodName) => $"{MethodStart}{methodName}{MethodEnd}";
        public static string StructMarkup(string structName) => $"{StructStart}{structName}{StructEnd}";

        public static string FormatMarkupString(string tooltip)
        {
            s_Sb.Clear();
            s_Sb.Append(tooltip);
            s_Sb.Replace("<lw>", LangWordStart);
            s_Sb.Replace("</lw>", LangWordEnd);
            s_Sb.Replace("<itf>", InterfaceStart);
            s_Sb.Replace("</itf>", InterfaceEnd);
            s_Sb.Replace("<cls>", ClassStart);
            s_Sb.Replace("</cls>", ClassEnd);
            s_Sb.Replace("<str>", StructStart);
            s_Sb.Replace("</str>", StructEnd);
            s_Sb.Replace("<mth>", MethodStart);
            s_Sb.Replace("</mth>", MethodEnd);
            return s_Sb.ToString();
        }
    }
}
