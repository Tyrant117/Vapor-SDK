using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace VaporInspectorEditor
{
    public static class VaporInspectorToolbarMenu
    {
        [MenuItem("Tools/Vapor/Inspector/Open Settings", false, 0)]
        private static void OpenInspectorSettings()
        {
            SettingsService.OpenUserPreferences("Vapor/Inspector Settings");
        }

        [MenuItem("Tools/Vapor/Inspector/Create Inspectors From Selection", false, 1)]
        private static void CreateInspectorsFromSelection()
        {
            AssetDatabase.StartAssetEditing();
            var items = Selection.objects;
            foreach (var item in items)
            {
                if (item is not MonoScript script) continue;

                var type = script.GetClass();
                if (type.IsSubclassOf(typeof(Object)))
                {
                    _CreateEditorClassFile(type.Name);
                }
                else
                {
                    _CreatePropertyDrawerClassFile(type.Name);
                }
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return;

            static void _CreateEditorClassFile(string className)
            {
                StringBuilder sb = new();
                
                sb.Append("//\t* THIS SCRIPT IS AUTO-GENERATED *\n");
                sb.Append("using UnityEditor;\n");
                sb.Append($"using {FolderSetupUtility.EditorNamespace};\n");

                sb.Append($"namespace {FolderSetupUtility.EditorNamespace}\n");
                sb.Append("{\n");
                sb.Append("#if VAPOR_INSPECTOR\n");
                sb.Append("\t[CanEditMultipleObjects]\n" +
                          $"\t[CustomEditor(typeof({className}), true)]\n");
                sb.Append($"\tpublic class {className}Editor : BaseVaporInspector\n");
                sb.Append("\t{\n");
                
                sb.Append("\t}\n");
                sb.Append("#endif\n");
                sb.Append("}");

                System.IO.File.WriteAllText($"{Application.dataPath}/{FolderSetupUtility.EditorRelativePath}/{className}Editor.cs", sb.ToString());
            }
            
            static void _CreatePropertyDrawerClassFile(string className)
            {
                StringBuilder sb = new();
                
                sb.Append("//\t* THIS SCRIPT IS AUTO-GENERATED *\n");
                sb.Append("using UnityEditor;\n");
                sb.Append($"using {FolderSetupUtility.EditorNamespace};\n");

                sb.Append($"namespace {FolderSetupUtility.EditorNamespace}\n");
                sb.Append("{\n");
                sb.Append("#if VAPOR_INSPECTOR\n");
                sb.Append($"\t[CustomPropertyDrawer(typeof({className}), true)]\n");
                sb.Append($"\tpublic class {className}Drawer : PropertyDrawer\n");
                sb.Append("\t{\n");
                
                sb.Append("\t}\n");
                sb.Append("#endif\n");
                sb.Append("}");

                System.IO.File.WriteAllText($"{Application.dataPath}/{FolderSetupUtility.PropertyDrawerRelativePath}/{className}Drawer.cs", sb.ToString());
            }
        }
    }
}
