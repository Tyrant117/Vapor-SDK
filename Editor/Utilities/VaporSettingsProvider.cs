using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.UIElements;

namespace VaporEditor
{
    public class VaporSettingsProvider : SettingsProvider
    {
        private const string EnableVaporXR = "enableVaporXR";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new VaporSettingsProvider("Vapor/Modules", SettingsScope.User);
        }

        public static bool VaporInspectorsEnabled
        {
            get => EditorPrefs.GetBool(EnableVaporXR, false);
            set => EditorPrefs.SetBool(EnableVaporXR, value);
        }

        public VaporSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var header = new VisualElement() /*new StyledHeaderBox("Inspector Settings")*/
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
            enableTog.SetValueWithoutNotify(VaporInspectorsEnabled);
            enableTog.RegisterValueChangedCallback(x =>
            {
                VaporInspectorsEnabled = x.newValue;
                DefineVaporXREnabled();
            });

            DefineVaporXREnabled();
            header.Add(enableTog);
            rootElement.Add(header);
            base.OnActivate(searchContext, rootElement);
        }

        private static void DefineVaporXREnabled()
        {
            var enabled = VaporInspectorsEnabled;
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
