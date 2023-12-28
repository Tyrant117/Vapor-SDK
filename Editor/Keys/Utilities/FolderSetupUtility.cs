using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VaporEditor;

namespace VaporKeysEditor
{
    internal static class FolderSetupUtility
    {
        public const string EditorNamespace = "VaporKeysEditor";
        public const string FolderRelativePath = "Vapor/Keys";
        public const string KeysRelativePath = FolderRelativePath + "/Keys";
        public const string ConfigRelativePath = FolderRelativePath + "/Config";
        
        [InitializeOnLoadMethod]
        private static void SetupFolders()
        {
            FolderUtility.CreateFolderFromPath($"Assets/{FolderRelativePath}");
            FolderUtility.CreateFolderFromPath($"Assets/{KeysRelativePath}");
            FolderUtility.CreateFolderFromPath($"Assets/{ConfigRelativePath}");

            FolderUtility.CreateAssemblyDefinition($"Assets/{FolderRelativePath}", "VaporKeyDefinitions", "VaporKeyDefinitions", new[] { "CarbonFiberGames.Vapor.Keys" }, false);
        }
    }
}
