using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using VaporInspector;

namespace VaporInspectorEditor
{
    public abstract class BaseVaporInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var inspector = new VisualElement();
            inspector.Add(DrawScript());
            var graph = new VaporInspectorNode(serializedObject, serializedObject.targetObject);
            graph.Draw(inspector);
            return inspector;
        }

        protected VisualElement DrawScript()
        {
            var script = new PropertyField(serializedObject.FindProperty("m_Script"));
            var hide = target.GetType().IsDefined(typeof(HideMonoScriptAttribute));
            script.style.display = hide ? DisplayStyle.None : DisplayStyle.Flex;
            script.SetEnabled(false);
            return script;
        }
    }
}
