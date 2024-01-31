using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;
using VaporEditor;
using VaporKeys;
using VaporXR;

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
                text = "Generate Readers"
            };
            var generateInteractionLayers = new Button(CreateInteractionLayers)
            {
                text = "Generate Interaction Layers"
            };

            header.Add(generateReaderButton);
            header.Add(generateInteractionLayers);
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

        private static void CreateInteractionLayers()
        {
            Debug.Log("<b>[VaporXR]</b> Creating Interaction Layers");
            try
            {
                FolderUtility.CreateFolderFromPath($"Assets/{FolderSetupUtility.FolderRelativePath}/Keys");
                var keyValuePairs = new List<KeyGenerator.KeyValuePair> { KeyGenerator.StringToKeyValuePair("Default") };
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
    }
}
