using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporGraphTools
{
    public abstract class LogicNodeSo : NodeSo, IEvaluatorNode<bool>
    {
        public virtual bool GetValue(int portIndex) { return false; }
        public abstract bool Evaluate(IExternalValueGetter getter, int portIndex);
    }
}
