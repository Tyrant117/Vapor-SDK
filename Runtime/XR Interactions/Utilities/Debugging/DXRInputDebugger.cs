using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace VaporXR.Utilities.Debugging
{
    [RequireComponent(typeof(VXRInputDeviceUpdateProvider))]
    public class DXRInputDebugger : MonoBehaviour
    {
        private class InputContainer
        {
            public TMP_Text Title;
            public TMP_Text Held;
            public TMP_Text Value;
            public TMP_Text Activated;
            public TMP_Text Deactivated;
        }

        public CompositeButtonInputProvider CompositeButton;

        public List<GameObject> Containers;

        private List<InputContainer> _inputContainers = new();

        private void Awake()
        {
            Assert.IsTrue(CompositeButton.ButtonInputProviders.Count < 5, "Input debugger can only Handle four composites at a time.");

            foreach (var c in Containers)
            {
                _inputContainers.Add(new InputContainer()
                {
                    Title = c.transform.Find("Name/Text").GetComponent<TMP_Text>(),
                    Held = c.transform.Find("Is Held/Value").GetComponent<TMP_Text>(),
                    Value = c.transform.Find("Current Value/Value").GetComponent<TMP_Text>(),
                    Activated = c.transform.Find("Was Activated/Value").GetComponent<TMP_Text>(),
                    Deactivated = c.transform.Find("Was Released/Value").GetComponent<TMP_Text>(),
                });
            }

            for (int i = 0; i < CompositeButton.ButtonInputProviders.Count; i++)
            {
                SetValue(_inputContainers[i], CompositeButton.ButtonInputProviders[i]);
            }
        }

        private void OnEnable()
        {
            var update = GetComponent<VXRInputDeviceUpdateProvider>();
            CompositeButton.BindUpdateSource(update);
            update.RegisterForPostInputUpdate(UpdateValues);
            
        }

        private void OnDisable()
        {
            var update = GetComponent<VXRInputDeviceUpdateProvider>();
            CompositeButton.UnbindUpdateSource();
            update.UnRegisterForInputUpdate(UpdateValues);
        }

        private void UpdateValues()
        {
            for (int i = 0; i < CompositeButton.ButtonInputProviders.Count; i++)
            {
                SetValue(_inputContainers[i], CompositeButton.ButtonInputProviders[i]);
            }
        }

        private void SetValue(InputContainer container, ButtonInputProvider input)
        {
            //Debug.Log(input.Threshold);
            container.Held.text = input.IsHeld.ToString();
            container.Held.color = input.IsHeld ? Color.green : Color.red;

            container.Value.text = input.CurrentValue.ToString("N2");

            container.Activated.text = input.CurrentState.ActivatedThisFrame.ToString();
            container.Activated.color = input.CurrentState.ActivatedThisFrame ? Color.green : Color.red;

            container.Deactivated.text = input.CurrentState.DeactivatedThisFrame.ToString();
            container.Deactivated.color = input.CurrentState.DeactivatedThisFrame ? Color.green : Color.red;            
        }
    }
}
