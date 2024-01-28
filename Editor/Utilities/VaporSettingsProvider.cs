using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.UIElements;

namespace VaporEditor
{
    public class VaporSettingsProvider : SettingsProvider
    {
        private const string EnableVaporXR = "_enableVaporXR";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {           
            return new VaporSettingsProvider("Vapor/Modules", SettingsScope.User);
        }

        public static bool VaporXREnabled
        {
            get => EditorPrefs.GetBool(PlayerSettings.productName + EnableVaporXR, false);
            set => EditorPrefs.SetBool(PlayerSettings.productName + EnableVaporXR, value);
        }

        public VaporSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var header = new VisualElement()
            {
                style =
                {
                    marginLeft = 3,
                    marginRight = 3
                }
            };

            var enableTog = new Toggle("Enable Vapor XR")
            {
                tooltip = "Enables the VAPOR_XR define allowing for XR interactions."
            };
            enableTog.SetValueWithoutNotify(VaporXREnabled);
            enableTog.RegisterValueChangedCallback(x =>
            {
                VaporXREnabled = x.newValue;
                DefineVaporXREnabled();
            });

            DefineVaporXREnabled();
            header.Add(enableTog);
            rootElement.Add(header);
            base.OnActivate(searchContext, rootElement);
        }

        private static void DefineVaporXREnabled()
        {
            var enabled = VaporXREnabled;
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), out var defines);
            if (enabled)
            {
                if (defines.Contains("VAPOR_XR")) return;
                
                ArrayUtility.Add(ref defines, "VAPOR_XR");
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), defines);
            }
            else
            {
                if (!defines.Contains("VAPOR_XR")) return;
                
                ArrayUtility.Remove(ref defines, "VAPOR_XR");
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), defines);
            }
        }
    }
}
