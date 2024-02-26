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
        public const string ProviderKeysRelativePath = "Vapor/Keys/XR";
        
        //private const string VaporXRReadersCreated = "_vaporXRReadersCreated";
        //private const string VaporXRInteractionLayersCreated = "_vaporXRInteractionLayersCreated";

        //public static bool AreVaporXRReadersCreated
        //{
        //    get => EditorPrefs.GetBool(PlayerSettings.productName + VaporXRReadersCreated, false);
        //    set => EditorPrefs.SetBool(PlayerSettings.productName + VaporXRReadersCreated, value);
        //}

        //public static bool AreInteractionLayersCreated
        //{
        //    get => EditorPrefs.GetBool(PlayerSettings.productName + VaporXRInteractionLayersCreated, false);
        //    set => EditorPrefs.SetBool(PlayerSettings.productName + VaporXRInteractionLayersCreated, value);
        //}
        
        [InitializeOnLoadMethod]
        private static void SetupFolders()
        {
            FolderUtility.CreateFolderFromPath($"Assets/{FolderRelativePath}");
            FolderUtility.CreateFolderFromPath($"Assets/{ReadersRelativePath}");
            //CreateReaders();
            //CreateInteractionLayers();
        }

        private static void CreateReaders()
        {
            //if(AreVaporXRReadersCreated)
            //{
            //    Debug.Log("<b>[VaporXR]</b> Readers Already Created");
            //    return;
            //}

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
                //AreVaporXRReadersCreated = true;
                AssetDatabase.StopAssetEditing();
            }

            static void _CreateReader<T, U>(string prefix, InputDeviceCharacteristics characteristics, InputFeatureUsage<U> usage) where T : XRInputDeviceValueReader<U> where U : struct
            {
                var assets = AssetDatabase.FindAssets($"{prefix}_{usage.name}", new[] { $"Assets/{ReadersRelativePath}" });
                if(assets is not { Length: 0 }) return;
                
                var reader = ScriptableObject.CreateInstance<T>();
                reader.name = $"{prefix}_{usage.name}";
                reader.Characteristics = characteristics;
                reader.Usage = new InputFeatureUsageString<U>(usage);
                AssetDatabase.CreateAsset(reader, $"Assets/{ReadersRelativePath}/{reader.name}.asset");
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
                _CreateReader<XRInputDeviceVector3ValueReader, Vector3>("Hmd",XRInputTrackingAggregator.Characteristics.Hmd, CommonUsages.centerEyePosition);
                _CreateReader<XRInputDeviceQuaternionValueReader, Quaternion>("Hmd",XRInputTrackingAggregator.Characteristics.Hmd, CommonUsages.centerEyeRotation);
                _CreateReader<XRInputDeviceBoolValueReader, bool>("Hmd",XRInputTrackingAggregator.Characteristics.Hmd, CommonUsages.isTracked);
                _CreateReader<XRInputDeviceBoolValueReader, bool>("Hmd",XRInputTrackingAggregator.Characteristics.Hmd, CommonUsages.userPresence);
            }
            
            void _CreateHaptics()
            {
                var assets = AssetDatabase.FindAssets("Left_Controller", new[] { $"Assets/{ReadersRelativePath}" });
                if (assets == null || assets.Length == 0)
                {
                    var leftHaptic = ScriptableObject.CreateInstance<XRInputDeviceHapticImpulseProvider>();
                    leftHaptic.name = "Left_Controller";
                    leftHaptic.Characteristics = XRInputTrackingAggregator.Characteristics.LeftController;
                    AssetDatabase.CreateAsset(leftHaptic, $"Assets/{ReadersRelativePath}/{leftHaptic.name}.asset");
                }

                assets = AssetDatabase.FindAssets("Right_Controller", new[] { $"Assets/{ReadersRelativePath}" });
                if (assets == null || assets.Length == 0)
                {
                    var rightHaptic = ScriptableObject.CreateInstance<XRInputDeviceHapticImpulseProvider>();
                    rightHaptic.name = "Right_Controller";
                    rightHaptic.Characteristics = XRInputTrackingAggregator.Characteristics.RightController;
                    AssetDatabase.CreateAsset(rightHaptic, $"Assets/{ReadersRelativePath}/{rightHaptic.name}.asset");
                }
            }
        }

        private static void CreateInteractionLayers()
        {
            //if(AreInteractionLayersCreated)
            //{
            //    Debug.Log("<b>[VaporXR]</b> Interaction Layers Already Created");
            //    return;
            //}

            Debug.Log("<b>[VaporXR]</b> Creating Interaction Layers");

            try
            {
                FolderUtility.CreateFolderFromPath($"Assets/{FolderRelativePath}/Keys");
                var keyValuePairs = new List<KeyGenerator.KeyValuePair> { KeyGenerator.StringToKeyValuePair("Default") };
                KeyGenerator.FormatKeyFiles($"{FolderRelativePath}/Keys", "VaporKeyDefinitions", "InteractionLayerKeys", keyValuePairs);

                FolderUtility.CreateFolderFromPath($"Assets/{FolderRelativePath}/Resources");
                var assets = AssetDatabase.FindAssets($"InteractionLayerSettings", new[] { $"Assets/{FolderRelativePath}/Resources" });
                if (assets is not { Length: 0 }) return;

                var reader = ScriptableObject.CreateInstance<InteractionLayerSettings>();
                reader.name = $"InteractionLayerSettings";
                AssetDatabase.CreateAsset(reader, $"Assets/{FolderRelativePath}/Resources/{reader.name}.asset");
            }
            finally
            {
                //AreInteractionLayersCreated = true;
            }
        }
    }
}
