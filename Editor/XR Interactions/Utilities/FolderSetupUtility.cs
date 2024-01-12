using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using VaporEditor;
using VaporKeys;
using VaporXR;

namespace VaporXREditor
{
    internal static class FolderSetupUtility
    {
        public const string FolderRelativePath = "Vapor/XR";
        public const string ReadersRelativePath = "Vapor/XR/Readers";
        
        private const string VaporXRReadersCreated = "vaporXRReadersCreated";

        public static bool AreVaporXRReadersCreated
        {
            get => EditorPrefs.GetBool(VaporXRReadersCreated, false);
            set => EditorPrefs.SetBool(VaporXRReadersCreated, value);
        }
        
        [InitializeOnLoadMethod]
        private static void SetupFolders()
        {
            FolderUtility.CreateFolderFromPath($"Assets/{FolderRelativePath}");
            FolderUtility.CreateFolderFromPath($"Assets/{ReadersRelativePath}");
            CreateReaders();
            CreateInteractionLayers();
        }

        private static void CreateReaders()
        {
            if(AreVaporXRReadersCreated) return;

            Debug.Log("<b>[VaporXR]</b> Creating Readers");
            try
            {
                AssetDatabase.StartAssetEditing();
                _CreateSide("Left", InputDeviceCharacteristics.Left);
                _CreateSide("Right", InputDeviceCharacteristics.Right);
                _CreateHmd();
                _CreateHaptics();
            }
            finally
            {
                AreVaporXRReadersCreated = true;
                AssetDatabase.StopAssetEditing();
            }

            static void _CreateReader<T, U>(string prefix, InputDeviceCharacteristics characteristics, InputFeatureUsage<U> usage) where T : XRInputDeviceValueReader<U> where U : struct
            {
                var assets = AssetDatabase.FindAssets($"{prefix}_{usage.name}", new[] { $"Assets/{ReadersRelativePath}" });
                if(assets is not { Length: 0 }) return;
                
                var reader = ScriptableObject.CreateInstance<T>();
                reader.name = $"{prefix}_{usage.name}";
                reader.characteristics = characteristics;
                reader.usage = new InputFeatureUsageString<U>(usage);
                AssetDatabase.CreateAsset(reader, $"Assets/{ReadersRelativePath}/{reader.name}.asset");
            }

            void _CreateSide(string s, InputDeviceCharacteristics inputDeviceCharacteristics)
            {
                _CreateReader<XRInputDeviceVector3ValueReader, Vector3>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.devicePosition);
                _CreateReader<XRInputDeviceQuaternionValueReader, Quaternion>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.deviceRotation);
                _CreateReader<XRInputDeviceFloatValueReader, float>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.grip);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.gripButton);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.isTracked);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.isTracked);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.menuButton);
                _CreateReader<XRInputDeviceVector2ValueReader, Vector2>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.primary2DAxis);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.primary2DAxisClick);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.primary2DAxisTouch);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.primaryButton);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.primaryTouch);
                _CreateReader<XRInputDeviceVector2ValueReader, Vector2>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.secondary2DAxis);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.secondary2DAxisClick);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.secondary2DAxisTouch);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.secondaryButton);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.secondaryTouch);
                _CreateReader<XRInputDeviceInputTrackingStateValueReader, InputTrackingState>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.trackingState);
                _CreateReader<XRInputDeviceFloatValueReader, float>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.trigger);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.triggerButton);
                _CreateReader<XRInputDeviceBoolValueReader, bool>(s,
                    InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | inputDeviceCharacteristics,
                    CommonUsages.userPresence);
                
            }

            void _CreateHmd()
            {
                _CreateReader<XRInputDeviceInputTrackingStateValueReader, InputTrackingState>("Hmd",
                    InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice,
                    CommonUsages.trackingState);
                _CreateReader<XRInputDeviceVector3ValueReader, Vector3>("Hmd",
                    InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice,
                    CommonUsages.centerEyePosition);
                _CreateReader<XRInputDeviceQuaternionValueReader, Quaternion>("Hmd",
                    InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice,
                    CommonUsages.centerEyeRotation);
                _CreateReader<XRInputDeviceBoolValueReader, bool>("Hmd",
                    InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice,
                    CommonUsages.isTracked);
                _CreateReader<XRInputDeviceBoolValueReader, bool>("Hmd",
                    InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice,
                    CommonUsages.userPresence);
            }
            
            void _CreateHaptics()
            {
                var assets = AssetDatabase.FindAssets("Left_Controller", new[] { $"Assets/{ReadersRelativePath}" });
                if (assets == null || assets.Length == 0)
                {
                    var leftHaptic = ScriptableObject.CreateInstance<XRInputDeviceHapticImpulseProvider>();
                    leftHaptic.name = "Left_Controller";
                    leftHaptic.Characteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller |
                                                 InputDeviceCharacteristics.Left;
                    AssetDatabase.CreateAsset(leftHaptic, $"Assets/{ReadersRelativePath}/{leftHaptic.name}.asset");
                }

                assets = AssetDatabase.FindAssets("Right_Controller", new[] { $"Assets/{ReadersRelativePath}" });
                if (assets == null || assets.Length == 0)
                {
                    var rightHaptic = ScriptableObject.CreateInstance<XRInputDeviceHapticImpulseProvider>();
                    rightHaptic.name = "Right_Controller";
                    rightHaptic.Characteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller |
                                                  InputDeviceCharacteristics.Right;
                    AssetDatabase.CreateAsset(rightHaptic, $"Assets/{ReadersRelativePath}/{rightHaptic.name}.asset");
                }
            }
        }

        private static void CreateInteractionLayers()
        {
            FolderUtility.CreateFolderFromPath($"Assets/{FolderRelativePath}/Keys");
            var keyValuePairs = new List<KeyGenerator.KeyValuePair> { KeyGenerator.StringToKeyValuePair("Default") };
            KeyGenerator.FormatKeyFiles($"{FolderRelativePath}/Keys", "VaporKeyDefinitions", "InteractionLayerKeys", keyValuePairs);
        }
    }
}
