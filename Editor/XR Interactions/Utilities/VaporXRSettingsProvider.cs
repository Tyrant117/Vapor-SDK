using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.Processors;
using UnityEngine.UIElements;
using UnityEngine.XR;
using VaporEditor;
using VaporEvents;
using VaporKeys;
using VaporXR;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace VaporXREditor
{
    public class VaporXRSettingsProvider : SettingsProvider
    {        

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new VaporXRSettingsProvider("Vapor/XR Settings", SettingsScope.User);
        }

        public VaporXRSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var header = new VisualElement() /*new StyledHeaderBox("Inspector Settings")*/
            {
                style =
                {
                    marginLeft = 3,
                    marginRight = 3
                }
            };

            var generateReaderButton = new Button(CreateReaders)
            {
                text = "Generate Legacy Readers"
            };
            var generateInputButton = new Button(CreateInputActionSet)
            {
                text = "Generate Input Actions"
            };
            var generateInteractionLayers = new Button(CreateInteractionLayers)
            {
                text = "Generate Interaction Layers"
            };
            var generateProviderKeys = new Button(CreateProviderKeys)
            {
                text = "Generate Provider Keys"
            };

            header.Add(generateReaderButton);
            header.Add(generateInputButton);
            header.Add(generateInteractionLayers);
            header.Add(generateProviderKeys);
            rootElement.Add(header);
            base.OnActivate(searchContext, rootElement);
        }

        private static void CreateReaders()
        {
            Debug.Log("<b>[VaporXR]</b> Creating Readers");
            try
            {
                AssetDatabase.StartAssetEditing();
                _CreateSide("Left", XRInputTrackingAggregator.Characteristics.LeftController);
                _CreateSide("Right", XRInputTrackingAggregator.Characteristics.RightController);
                _CreateHmd();
                _CreateHaptics();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            static void _CreateReader<T, U>(string prefix, InputDeviceCharacteristics characteristics, InputFeatureUsage<U> usage) where T : XRInputDeviceValueReader<U> where U : struct
            {
                var assets = AssetDatabase.FindAssets($"{prefix}_{usage.name}", new[] { $"Assets/{FolderSetupUtility.ReadersRelativePath}" });
                if (assets is not { Length: 0 }) return;

                var reader = ScriptableObject.CreateInstance<T>();
                reader.name = $"{prefix}_{usage.name}";
                reader.Characteristics = characteristics;
                reader.Usage = new InputFeatureUsageString<U>(usage);
                AssetDatabase.CreateAsset(reader, $"Assets/{FolderSetupUtility.ReadersRelativePath}/{reader.name}.asset");
            }

            void _CreateSide(string s, InputDeviceCharacteristics inputDeviceCharacteristics)
            {
                _CreateReader<XRInputDeviceVector3ValueReader, Vector3>(s, inputDeviceCharacteristics, CommonUsages.devicePosition);
                _CreateReader<XRInputDeviceQuaternionValueReader, Quaternion>(s, inputDeviceCharacteristics, CommonUsages.deviceRotation);
                _CreateReader<XRInputDeviceFloatValueReader, float>(s, inputDeviceCharacteristics, CommonUsages.grip);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.gripButton);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.isTracked);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.isTracked);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.menuButton);
                _CreateReader<XRInputDeviceVector2ValueReader, Vector2>(s, inputDeviceCharacteristics, CommonUsages.primary2DAxis);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.primary2DAxisClick);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.primary2DAxisTouch);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.primaryButton);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.primaryTouch);
                _CreateReader<XRInputDeviceVector2ValueReader, Vector2>(s, inputDeviceCharacteristics, CommonUsages.secondary2DAxis);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.secondary2DAxisClick);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.secondary2DAxisTouch);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.secondaryButton);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.secondaryTouch);
                _CreateReader<XRInputDeviceInputTrackingStateValueReader, InputTrackingState>(s, inputDeviceCharacteristics, CommonUsages.trackingState);
                _CreateReader<XRInputDeviceFloatValueReader, float>(s, inputDeviceCharacteristics, CommonUsages.trigger);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.triggerButton);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s, inputDeviceCharacteristics, CommonUsages.userPresence);
            }

            void _CreateHmd()
            {
                _CreateReader<XRInputDeviceInputTrackingStateValueReader, InputTrackingState>("Hmd", XRInputTrackingAggregator.Characteristics.Hmd, CommonUsages.trackingState);
                _CreateReader<XRInputDeviceVector3ValueReader, Vector3>("Hmd", XRInputTrackingAggregator.Characteristics.Hmd, CommonUsages.centerEyePosition);
                _CreateReader<XRInputDeviceQuaternionValueReader, Quaternion>("Hmd", XRInputTrackingAggregator.Characteristics.Hmd, CommonUsages.centerEyeRotation);
                _CreateReader<XRInputDeviceBoolValueReader, bool>("Hmd", XRInputTrackingAggregator.Characteristics.Hmd, CommonUsages.isTracked);
                _CreateReader<XRInputDeviceBoolValueReader, bool>("Hmd", XRInputTrackingAggregator.Characteristics.Hmd, CommonUsages.userPresence);
            }

            void _CreateHaptics()
            {
                var assets = AssetDatabase.FindAssets("Left_Controller", new[] { $"Assets/{FolderSetupUtility.ReadersRelativePath}" });
                if (assets == null || assets.Length == 0)
                {
                    var leftHaptic = ScriptableObject.CreateInstance<XRInputDeviceHapticImpulseProvider>();
                    leftHaptic.name = "Left_Controller";
                    leftHaptic.Characteristics = XRInputTrackingAggregator.Characteristics.LeftController;
                    AssetDatabase.CreateAsset(leftHaptic, $"Assets/{FolderSetupUtility.ReadersRelativePath}/{leftHaptic.name}.asset");
                }

                assets = AssetDatabase.FindAssets("Right_Controller", new[] { $"Assets/{FolderSetupUtility.ReadersRelativePath}" });
                if (assets == null || assets.Length == 0)
                {
                    var rightHaptic = ScriptableObject.CreateInstance<XRInputDeviceHapticImpulseProvider>();
                    rightHaptic.name = "Right_Controller";
                    rightHaptic.Characteristics = XRInputTrackingAggregator.Characteristics.RightController;
                    AssetDatabase.CreateAsset(rightHaptic, $"Assets/{FolderSetupUtility.ReadersRelativePath}/{rightHaptic.name}.asset");
                }
            }
        }

        private static void CreateInputActionSet()
        {
            Debug.Log("<b>[VaporXR]</b> Creating Input Actions");
            string fileName = "VXR Default Input Actions";
            try
            {
                AssetDatabase.StartAssetEditing();
                var actions = ScriptableObject.CreateInstance<InputActionAsset>();
                
                _CreateHeadMap(actions);
                _CreateHandMap(actions, "Right Hand", InputUsages.RightHand);
                _CreateHandMap(actions, "Left Hand", InputUsages.LeftHand);
                _CreateUIMap(actions);

                actions.name = fileName;
                // Write JSON to the file
                System.IO.File.WriteAllText($"Assets/{FolderSetupUtility.ReadersRelativePath}/{actions.name}.{InputActionAsset.Extension}", actions.ToJson());
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.ImportAsset($"Assets/{FolderSetupUtility.ReadersRelativePath}/{fileName}.{InputActionAsset.Extension}");
            }

            void _AddButton(InputActionMap map, string name, string binding)
            {
                var action = map.AddAction(name, InputActionType.Button, binding);
                action.wantsInitialStateCheck = true;
            }

            void _AddAxis1D(InputActionMap map, string name, string binding)
            {
                var action = map.AddAction(name, InputActionType.Value, binding);
                action.expectedControlType = "Axis";
            }

            void _AddAxis2D(InputActionMap map, string name, string binding)
            {
                var action = map.AddAction(name, InputActionType.Value);
                action.expectedControlType = "Vector2";
                var b = action.AddBinding(binding);
                b.WithProcessor<StickDeadzoneProcessor>();
            }

            void _CreateHandMap(InputActionAsset actionAsset, string name, string handName)
            {
                var map = actionAsset.AddActionMap(name);
                var rightPos = map.AddAction("Position", InputActionType.Value);
                rightPos.expectedControlType = "Vector3";
                rightPos.AddCompositeBinding("XRVector3Fallback")
                    .With("First", $"<XRController>{{{handName}}}/pointerPosition")
                    .With("Second", $"<XRController>{{{handName}}}/devicePosition")
                    .With("Third", $"<XRHandDevice>{{{handName}}}/devicePosition");
                var last = rightPos.bindings.Last(x => x.isComposite);
                rightPos.ChangeBinding(last).WithName("Vector 3 Fallback");

                var rightRot = map.AddAction("Rotation", InputActionType.Value);
                rightRot.expectedControlType = "Quaternion";
                rightRot.AddCompositeBinding("XRQuaternionFallback")
                    .With("First", $"<XRController>{{{handName}}}/pointerRotation")
                    .With("Second", $"<XRController>{{{handName}}}/deviceRotation")
                    .With("Third", $"<XRHandDevice>{{{handName}}}/deviceRotation");
                last = rightRot.bindings.Last(x => x.isComposite);
                rightRot.ChangeBinding(last).WithName("Quaternion Fallback");

                var rightTracked = map.AddAction("Is Tracked", InputActionType.Button, $"<XRController>{{{handName}}}/isTracked");
                rightTracked.wantsInitialStateCheck = true;

                var rightTrackState = map.AddAction("Tracking State", InputActionType.Value, $"<XRController>{{{handName}}}/trackingState");
                rightTrackState.expectedControlType = "Integer";

                var rightHaptic = map.AddAction("Haptic Device", InputActionType.PassThrough, $"<XRController>{{{handName}}}/*");

                // Buttons
                _AddButton(map, "Primary Button", InputUsages.Format(handName, InputUsages.PrimaryButton));
                _AddButton(map, "Primary Touch", InputUsages.Format(handName, InputUsages.PrimaryTouch));

                _AddButton(map, "Secondary Button", InputUsages.Format(handName, InputUsages.SecondaryButton));
                _AddButton(map, "Secondary Touch", InputUsages.Format(handName, InputUsages.SecondaryTouch));

                _AddButton(map, "Trigger Button", InputUsages.Format(handName, InputUsages.TriggerButton));
                _AddButton(map, "Grip Button", InputUsages.Format(handName, InputUsages.GripButton));

                _AddButton(map, "Menu Button", InputUsages.Format(handName, InputUsages.MenuButton));
                _AddButton(map, "System Button", InputUsages.Format(handName, InputUsages.SystemButton));

                _AddButton(map, "Primary 2D Axis Button", InputUsages.Format(handName, InputUsages.Primary2DAxisClick));
                _AddButton(map, "Primary 2D Axis Touch", InputUsages.Format(handName, InputUsages.Primary2DAxisTouch));

                // Axis 1D
                _AddAxis1D(map, "Trigger Axis", InputUsages.Format(handName, InputUsages.TriggerAxis));
                _AddAxis1D(map, "Grip Axis", InputUsages.Format(handName, InputUsages.GripAxis));

                // Axis 2D
                _AddAxis2D(map, "Joystick", InputUsages.Format(handName, InputUsages.Primary2DAxis));
                _AddAxis2D(map, "Joystick Action 1", InputUsages.Format(handName, InputUsages.Primary2DAxis));
                _AddAxis2D(map, "Joystick Action 2", InputUsages.Format(handName, InputUsages.Primary2DAxis));
                _AddAxis2D(map, "Joystick Action 3", InputUsages.Format(handName, InputUsages.Primary2DAxis));
                _AddAxis2D(map, "Joystick Action 4", InputUsages.Format(handName, InputUsages.Primary2DAxis));
                _AddAxis2D(map, "Joystick Action 5", InputUsages.Format(handName, InputUsages.Primary2DAxis));
                _AddAxis2D(map, "Joystick Action 6", InputUsages.Format(handName, InputUsages.Primary2DAxis));
                _AddAxis2D(map, "Joystick Action 7", InputUsages.Format(handName, InputUsages.Primary2DAxis));
                _AddAxis2D(map, "Joystick Action 8", InputUsages.Format(handName, InputUsages.Primary2DAxis));
            }

            static void _CreateHeadMap(InputActionAsset actionAsset)
            {
                var map = actionAsset.AddActionMap("Head");
                var pos = map.AddAction("Position", InputActionType.Value, "<XRHMD>/centerEyePosition");
                pos.expectedControlType = "Vector3";
                pos.AddBinding("<HandheldARInputDevice>/devicePosition");

                var rot = map.AddAction("Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation");
                rot.AddBinding("<HandheldARInputDevice>/deviceRotation");
                rot.expectedControlType = "Quaternion";

                var tracked = map.AddAction("Is Tracked", InputActionType.Button, "<XRHMD>/isTracked");
                tracked.wantsInitialStateCheck = true;

                var trackState = map.AddAction("Tracking State", InputActionType.Value, "<XRHMD>/trackingState");
                trackState.expectedControlType = "Integer";

                var eyeGazePos = map.AddAction("Eye Gaze Position", InputActionType.Value);
                eyeGazePos.expectedControlType = "Vector3";
                eyeGazePos.AddCompositeBinding("XRVector3Fallback")
                .With("First", "<EyeGaze>/pose/position")
                .With("Second", "<XRHMD>/centerEyePosition")
                .With("Third", "");
                var last = eyeGazePos.bindings.Last(x => x.isComposite);
                eyeGazePos.ChangeBinding(last).WithName("Vector 3 Fallback");

                var eyeGazeRot = map.AddAction("Eye Gaze Rotation", InputActionType.Value);
                eyeGazeRot.expectedControlType = "Quaternion";
                eyeGazeRot.AddCompositeBinding("XRQuaternionFallback")
                .With("First", "<EyeGaze>/pose/rotation")
                .With("Second", "<XRHMD>/centerEyeRotation")
                .With("Third", "");
                last = eyeGazeRot.bindings.Last(x => x.isComposite);
                eyeGazeRot.ChangeBinding(last).WithName("Quaternion Fallback");

                var eyeGazeTracked = map.AddAction("Eye Gaze Is Tracked", InputActionType.Button);
                eyeGazeTracked.wantsInitialStateCheck = true;
                eyeGazeTracked.AddCompositeBinding("XRButtonFallback")
                .With("First", "<EyeGaze>/pose/isTracked")
                .With("Second", "<XRHMD>/isTracked")
                .With("Third", "");
                last = eyeGazeTracked.bindings.Last(x => x.isComposite);
                eyeGazeTracked.ChangeBinding(last).WithName("Button Fallback");

                var eyeGazeTrackState = map.AddAction("Eye Gaze Tracking State", InputActionType.Value);
                eyeGazeTrackState.expectedControlType = "Integer";
                eyeGazeTrackState.AddCompositeBinding("XRIntegerFallback")
                .With("First", "<EyeGaze>/pose/trackingState")
                .With("Second", "<XRHMD>/trackingState")
                .With("Third", "");
                last = eyeGazeTrackState.bindings.Last(x => x.isComposite);
                eyeGazeTrackState.ChangeBinding(last).WithName("Integer Fallback");
            }

            static void _CreateUIMap(InputActionAsset actionAsset)
            {
                var map = actionAsset.AddActionMap("UI");

                var navigate = map.AddAction("Navigate", InputActionType.PassThrough);
                navigate.expectedControlType = "Vector2";
                var pad = navigate.AddCompositeBinding("2DVector(mode=0)")
                    .With("Up", "<Gamepad>/leftStick/up").With("Up", "<Gamepad>/rightStick/up")
                    .With("Down", "<Gamepad>/leftStick/down").With("Down", "<Gamepad>/rightStick/down")
                    .With("Left", "<Gamepad>/leftStick/left").With("Left", "<Gamepad>/rightStick/left")
                    .With("Right", "<Gamepad>/leftStick/right").With("Right", "<Gamepad>/rightStick/right");
                var last = navigate.bindings.Last(x => x.isComposite);
                navigate.ChangeBinding(last).WithName("Gamepad");

                navigate.AddBinding("<Gamepad>/dpad");

                navigate.AddCompositeBinding("2DVector(mode=0)")
                    .With("Up", "<Joystick>/stick/up")
                    .With("Down", "<Joystick>/stick/down")
                    .With("Left", "<Joystick>/stick/left")
                    .With("Right", "<Joystick>/stick/right");
                last = navigate.bindings.Last(x => x.isComposite);
                navigate.ChangeBinding(last).WithName("Joystick");

                navigate.AddCompositeBinding("2DVector(mode=0)")
                    .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
                last = navigate.bindings.Last(x => x.isComposite);
                navigate.ChangeBinding(last).WithName("Keyboard");

                var submit = map.AddAction("Submit", InputActionType.Button, "*/{Submit}");
                var cancel = map.AddAction("Cancel", InputActionType.Button, "*/{Cancel}");

                var point = map.AddAction("Point", InputActionType.PassThrough);
                point.expectedControlType = "Vector2";
                point.wantsInitialStateCheck = true;
                point.AddBinding("<Mouse>/position");
                point.AddBinding("<Pen>/position");
                point.AddBinding("<Touchscreen>/position");

                var click = map.AddAction("Click", InputActionType.PassThrough);
                click.expectedControlType = "Button";
                click.wantsInitialStateCheck = true;
                click.AddBinding("<Mouse>/leftButton");
                click.AddBinding("<Pen>/tip");
                click.AddBinding("<Touchscreen>/Press");

                var scroll = map.AddAction("ScrollWheel", InputActionType.PassThrough, "<Mouse>/scroll");
                scroll.expectedControlType = "Vector2";

                var middleClick = map.AddAction("MiddleClick", InputActionType.PassThrough, "<Mouse>/middleButton");
                middleClick.expectedControlType = "Button";

                var rightClick = map.AddAction("RightClick", InputActionType.PassThrough, "<Mouse>/rightButton");
                rightClick.expectedControlType = "Button";
            }
        }

        private static void CreateInteractionLayers()
        {
            Debug.Log("<b>[VaporXR]</b> Creating Interaction Layers");
            try
            {
                FolderUtility.CreateFolderFromPath($"Assets/{FolderSetupUtility.FolderRelativePath}/Keys");
                var keyValuePairs = new List<KeyGenerator.KeyValuePair> { KeyGenerator.StringToKeyValuePair("Default"), KeyGenerator.StringToKeyValuePair("Teleport") };
                KeyGenerator.FormatKeyFiles($"{FolderSetupUtility.FolderRelativePath}/Keys", "VaporKeyDefinitions", "InteractionLayerKeys", keyValuePairs);

                FolderUtility.CreateFolderFromPath($"Assets/{FolderSetupUtility.FolderRelativePath}/Resources");
                var assets = AssetDatabase.FindAssets($"InteractionLayerSettings", new[] { $"Assets/{FolderSetupUtility.FolderRelativePath}/Resources" });
                if (assets is not { Length: 0 }) return;

                var reader = ScriptableObject.CreateInstance<InteractionLayerSettings>();
                reader.name = $"InteractionLayerSettings";
                AssetDatabase.CreateAsset(reader, $"Assets/{FolderSetupUtility.FolderRelativePath}/Resources/{reader.name}.asset");
            }
            finally
            {

            }
        }

        private static void CreateProviderKeys()
        {
            Debug.Log("<b>[VaporXR]</b> Creating Provider Keys");
            try
            {
                FolderUtility.CreateFolderFromPath($"Assets/{FolderSetupUtility.ProviderKeysRelativePath}");
                _CreateProviderKey("Input Manager");
                _CreateProviderKey("Right Hand");
                _CreateProviderKey("Left Hand");

                KeySo.GenerateKeysOfType<ProviderKeySo>();

                _CreateLayerKey("Default");
                _CreateLayerKey("Teleport");
                KeySo.GenerateKeysOfType<InteractionLayerKeySo>();
            }
            finally
            {

            }

            void _CreateProviderKey(string name)
            {
                var key = ScriptableObject.CreateInstance<ProviderKeySo>();
                key.name = name;
                var assets = AssetDatabase.FindAssets(name, new[] { $"Assets/{FolderSetupUtility.ProviderKeysRelativePath}" });
                if (assets == null || assets.Length == 0)
                {
                    AssetDatabase.CreateAsset(key, $"Assets/{FolderSetupUtility.ProviderKeysRelativePath}/{key.name}.asset");
                }
            }

            void _CreateLayerKey(string layer)
            {
                var key = ScriptableObject.CreateInstance<InteractionLayerKeySo>();
                key.name = layer;
                var assets = AssetDatabase.FindAssets(layer, new[] { $"Assets/{FolderSetupUtility.ProviderKeysRelativePath}" });
                if (assets == null || assets.Length == 0)
                {
                    AssetDatabase.CreateAsset(key, $"Assets/{FolderSetupUtility.ProviderKeysRelativePath}/{key.name}.asset");
                }
            }
        }
    }
}
