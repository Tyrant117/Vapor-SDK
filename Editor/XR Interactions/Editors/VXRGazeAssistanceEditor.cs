using UnityEditor;
using VaporInspectorEditor;
using VaporXR;

namespace VaporXREditor
{
#if VAPOR_INSPECTOR
	[CanEditMultipleObjects]
	[CustomEditor(typeof(VXRGazeAssistance), true)]
	public class VXRGazeAssistanceEditor : BaseVaporInspector
	{
	}
#endif
}