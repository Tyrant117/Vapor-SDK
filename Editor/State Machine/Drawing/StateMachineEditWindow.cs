using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using VaporGraphToolsEditor;
using VaporStateMachine;

namespace VaporStateMachineEditor
{
    public class StateMachineEditWindow : GraphToolsEditWindow<StateMachineGraphControllerSo>
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int index)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceId);
            //Debug.Log(asset.GetType());
            if (asset.GetType() == typeof(StateMachineGraphControllerSo))
            {
                Open<StateMachineEditWindow>(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)));
                return true;
            }
            return false;
        }

        protected override void CreateEditorView(StateMachineGraphControllerSo asset, string graphName)
        {
            GraphEditorView = new GraphEditorView(this, GraphObject, NodeObjects, graphName)
            {
                viewDataKey = SelectedGuid,
            };
        }
    }
}
