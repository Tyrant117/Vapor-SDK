using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    public static class ControllerHardwareOffsets
    {
        public static Pose Oculus = new(new Vector3(0, -0.0672724098f, 0.07348077f), Quaternion.Euler(60, 0, 0));// new (new Vector3(-0.046329204f, 0.00984701794f, 0.087644957f), Quaternion.Euler(46.9339638f, 27.8914871f, 41.7441025f)); // new (new Vector3(-.001f, -.005f, -.05f), Quaternion.Euler(40, 0, 0));
        public static Pose Wmr = new(new Vector3(-.0175f, .01f, -.0725f), Quaternion.Euler(40, 0, 0));
        public static Pose ReverbG2 = new(new Vector3(-.0175f, .01f, -.0725f), Quaternion.Euler(40, 0, 0));
        public static Pose Cosmos = new(new Vector3(-.0001f, -.0005f, -.05f), Quaternion.Euler(40, 0, 0));
        public static Pose Vive = new(new Vector3(-.001f, -.005f, -.05f), Quaternion.Euler(40, 0, 0));
        public static Pose Knuckles = new(new Vector3(-.01f, -.02f, -.075f), Quaternion.Euler(60, 0, 0));

        public static Pose GetDeviceOffset(VXRTrackedPoseDriver.ControllerType type)
        {
            return type switch
            {
                VXRTrackedPoseDriver.ControllerType.Oculus => Oculus,
                VXRTrackedPoseDriver.ControllerType.WMR => Wmr,
                VXRTrackedPoseDriver.ControllerType.Vive => Vive,
                VXRTrackedPoseDriver.ControllerType.Knuckles => Knuckles,
                VXRTrackedPoseDriver.ControllerType.Cosmos => Cosmos,
                VXRTrackedPoseDriver.ControllerType.ReverbG2 => ReverbG2,
                _ => Pose.identity,
            };
        }
    }
}
