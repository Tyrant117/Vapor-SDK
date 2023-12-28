using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.UIElements;

namespace VaporInspectorEditor
{
    public class VaporInspectorsSettingsProvider : SettingsProvider
    {
        [InitializeOnLoadMethod]
        private static void InitDefines()
        {
            DefineVaporEnabled();
        }

        private const string EnableVaporInspectors = "enableVaporInspectors";
        // private const string EnableExplicitImplementation = "enableExplicitImplementation";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new VaporInspectorsSettingsProvider("Vapor/Inspector Settings", SettingsScope.User);
        }

        public static bool VaporInspectorsEnabled
        {
            get => EditorPrefs.GetBool(EnableVaporInspectors, true);
            set => EditorPrefs.SetBool(EnableVaporInspectors, value);
        }

        public VaporInspectorsSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
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

            var enableTog = new Toggle("Enable Vapor Inspectors")
            {
                tooltip = "Enables the VAPOR_INSPECTOR define allowing for drawing to be done."
            };
            enableTog.SetValueWithoutNotify(VaporInspectorsEnabled);
            enableTog.RegisterValueChangedCallback(x =>
            {
                VaporInspectorsEnabled = x.newValue;
                DefineVaporEnabled();
            });

            DefineVaporEnabled();
            header.Add(enableTog);
            rootElement.Add(header);
            base.OnActivate(searchContext, rootElement);
        }

        private static void DefineVaporEnabled()
        {
            var enabled = VaporInspectorsEnabled;
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), out var defines);
            if (enabled)
            {
                if (defines.Contains("VAPOR_INSPECTOR")) return;
                
                ArrayUtility.Add(ref defines, "VAPOR_INSPECTOR");
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), defines);
            }
            else
            {
                if (!defines.Contains("VAPOR_INSPECTOR")) return;
                
                ArrayUtility.Remove(ref defines, "VAPOR_INSPECTOR");
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), defines);
            }
        }
    }
}
