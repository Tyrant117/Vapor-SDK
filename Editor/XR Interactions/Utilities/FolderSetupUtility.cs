using UnityEditor;
using VaporEditor;

namespace VaporXREditor
{
    internal static class FolderSetupUtility
    {
        public const string FolderRelativePath = "Vapor/XR";
        
        [InitializeOnLoadMethod]
        private static void SetupFolders()
        {
            FolderUtility.CreateFolderFromPath($"Assets/{FolderRelativePath}");
        }
    }
}
