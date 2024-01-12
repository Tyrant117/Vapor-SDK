using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VaporInspectorEditor
{
    public static class ClipboardUtility
    {
        public static object CopyBuffer { get; set; }

        public static void WriteToBuffer(VaporDrawerInfo drawerInfo)
        {
            CopyBuffer = drawerInfo.Property.boxedValue;
            // Debug.Log($"Write: {CopyBuffer.GetType()} - {CopyBuffer}");
        }

        public static void WriteToBuffer(VaporInspectorNode node)
        {
            CopyBuffer = node.Property.boxedValue;
        }

        public static void WriteToBuffer(object copyTarget)
        {
            CopyBuffer = copyTarget;
            // Debug.Log($"Write: {CopyBuffer.GetType()} - {CopyBuffer}");
        }

        public static bool CanReadFromBuffer(VaporDrawerInfo drawerInfo)
        {
            return CopyBuffer != null && (CopyBuffer.GetType() == drawerInfo.FieldInfo.FieldType || CopyBuffer.GetType().IsSubclassOf(drawerInfo.FieldInfo.FieldType));
        }
        
        public static bool CanReadFromBuffer(VaporInspectorNode node)
        {
            return CopyBuffer != null && (CopyBuffer.GetType() == node.FieldInfo.FieldType || CopyBuffer.GetType().IsSubclassOf(node.FieldInfo.FieldType));
        }
        
        public static bool CanReadFromBuffer(Type type)
        {
            return CopyBuffer != null && (CopyBuffer.GetType() == type || CopyBuffer.GetType().IsSubclassOf(type));
        }

        public static void ReadFromBuffer(VaporDrawerInfo drawerInfo)
        {
            // Debug.Log($"Read: {CopyBuffer.GetType()} - {drawerInfo.FieldInfo.FieldType}");
            var isSubclassOrType = (CopyBuffer.GetType() == drawerInfo.FieldInfo.FieldType || CopyBuffer.GetType().IsSubclassOf(drawerInfo.FieldInfo.FieldType));
            if (!isSubclassOrType)
            {
                return;
            }
            drawerInfo.Property.boxedValue = CopyBuffer;
            drawerInfo.Property.serializedObject.ApplyModifiedProperties();
        }
        
        public static void ReadFromBuffer(VaporInspectorNode node)
        {
            // Debug.Log($"Read: {CopyBuffer.GetType()} - {drawerInfo.FieldInfo.FieldType}");
            var isSubclassOrType = (CopyBuffer.GetType() == node.FieldInfo.FieldType || CopyBuffer.GetType().IsSubclassOf(node.FieldInfo.FieldType));
            if (!isSubclassOrType)
            {
                return;
            }
            node.Property.boxedValue = CopyBuffer;
            node.Property.serializedObject.ApplyModifiedProperties();
        }
        
        public static void ReadFromBuffer(SerializedProperty property, Type type)
        {
            // Debug.Log($"Read: {CopyBuffer.GetType()} - {drawerInfo.FieldInfo.FieldType}");
            var isSubclassOrType = CopyBuffer.GetType() == type || CopyBuffer.GetType().IsSubclassOf(type);
            if (!isSubclassOrType)
            {
                return;
            }
            property.boxedValue = CopyBuffer;
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
