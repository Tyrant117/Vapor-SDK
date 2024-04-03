using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VaporGraphTools
{
	public abstract class MathNodeSo : NodeSo, IEvaluatorNode<float>
    {
        public virtual float GetValue(int portIndex) => 0;
		public abstract float Evaluate(IExternalValueGetter externalValues, int portIndex);
    }
}
