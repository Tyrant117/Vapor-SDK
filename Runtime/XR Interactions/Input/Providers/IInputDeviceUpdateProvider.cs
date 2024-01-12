using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    public interface IInputDeviceUpdateProvider
    {
        event Action InputUpdated;
        event Action PostInputUpdated;

        void RegisterForInputUpdate(Action callback);
        void UnRegisterForInputUpdate(Action callback);
        void RegisterForPostInputUpdate(Action callback);
        void UnRegisterForPostInputUpdate(Action callback);
    }
}
