using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporStateMachine
{
    public class StateMachineExceptions
    {
        public static string StateMachineNotInitialized => $"The State machine has not been initialized.\n" +
                $"Call SetDefaultState, Init(), or OnEnter to initialize.";

        public static string StateNotFound(string state)
        {
            return $"The State <b>[{state}]</b> does not exist.\n" +
                "Check for typos in the State names.\n" +
                "Ensure the State is in the State machine.";
        }

        public static string ActionTypeMismatch(Type type, Delegate action)
        {
            return $"The expected argument type ({type}) does not match the type of the added action ({action}).";
        }

        public static string NoDefaultStateFound => $"The State machine does not have any states before OnEnter was called.";
    }
}
