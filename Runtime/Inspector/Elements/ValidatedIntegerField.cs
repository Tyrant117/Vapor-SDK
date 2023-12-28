using System;
using UnityEngine.UIElements;

namespace VaporInspector
{
    public class ValidatedIntegerField : IntegerField
    {
        public Func<int, bool> ValidateInput;
        
        public override int value
        {
            get => base.value;
            set
            {
                var valid = true;
                if (ValidateInput != null)
                {
                    valid = ValidateInput.Invoke(value);
                }

                if (valid)
                {
                    base.value = value;
                }
            }
        }
    }
}
