using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporStateMachine;

namespace VaporXR
{
    public class HandPosingStateMachine : MonoBehaviour
    {
        private StateMachine _fsm;

        private void Awake()
        {
            _fsm = new StateMachine("Hand");

            var idle = new State("Idle", false);
            var hover = new State("Hover", false);
            var grabbing = new State("Grabbing", false);

            _fsm.AddState(idle);
            _fsm.AddState(hover);
            _fsm.AddState(grabbing);
        }

        private void Start()
        {
            _fsm.Init();
        }

        private void Update()
        {
            _fsm.OnUpdate();
        }
    }
}
