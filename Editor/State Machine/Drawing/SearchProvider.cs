using System;
using UnityEngine;
using VaporGraphTools;
using VaporGraphToolsEditor;
using VaporStateMachine;

namespace VaporStateMachineEditor
{
    public class SearchProvider : GraphToolsSearchProvider<StateMachineGraphControllerSo>
    {
        protected override void FilterNewNode(Type nodeType, SearchableNodeAttribute attribute)
        {
            if (string.IsNullOrEmpty(attribute.MenuName)) { return; }
            Elements.Add(new SearchContextElement(nodeType, attribute.MenuName));
        }      
    }
}
