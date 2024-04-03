namespace VaporGraphTools
{
    public interface IArgsEvaluatorNode
    {
        ArgVector GetArgs(IExternalValueGetter getter);
    }

    //public interface IArgsEvaluatorNode<T> : IArgsEvaluatorNode
    //{
    //    T GetArgs(IExternalValueGetter getter);
    //}

    public readonly struct ArgVector
    {
        public readonly int SenderId;
        public readonly int FromId;
        public readonly DynamicValue Args;

        public ArgVector(int senderID, int fromID, DynamicValue args)
        {
            SenderId = senderID;
            FromId = fromID;
            Args = args;
        }

        public bool IsValid() => SenderId != 0;
    }

    public interface IEvaluatorNode<T>
    {
        T GetValue(int portIndex);
        T Evaluate(IExternalValueGetter getter, int portIndex);
    }

    //public interface IIntEvaluatorNode
    //{
    //    int Evaluate(IExternalValueGetter getter, int portIndex);
    //}

    //public interface IFloatEvaluatorNode
    //{
    //    float Evaluate(IExternalValueGetter getter, int portIndex);
    //}

    //public interface IBoolEvaluatorNode
    //{
    //    bool Evaluate(IExternalValueGetter getter, int portIndex);
    //}

    //public interface IDoubleEvaluatorNode
    //{
    //    double Evaluate(IExternalValueGetter getter);
    //}

    //public interface INumberEvaluatorNode
    //{
    //    double Evaluate(IExternalValueGetter getter);
    //}
}
