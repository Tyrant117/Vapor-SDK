using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VaporInspectorEditor
{
    public class VaporPropertyField : VisualElement
    {
        public object Value { get; private set; }

        public VaporPropertyField(Type type)
        {
            //List<FieldInfo> fieldInfo = new();
            //List<MethodInfo> methodInfo = new();
            //var targetType = type;
            //Stack<Type> typeStack = new();
            //Value = Activator.CreateInstance(type);
            //while (targetType != null)
            //{
            //    typeStack.Push(targetType);
            //    targetType = targetType.BaseType;
            //}

            //while (typeStack.TryPop(out var type))
            //{
            //    fieldInfo.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            //    methodInfo.AddRange(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            //}

            //foreach (var field in fieldInfo.Where(VaporInspectorNode.FieldSearchPredicate))
            //{
            //    var property = Property != null ? Property.FindPropertyRelative(field.Name) : Root.FindProperty(field.Name);
            //    if (property == null)
            //    {
            //        continue;
            //    }

            //    var pf = new PropertyField()

            //    var node = new VaporInspectorNode(this, field, field);
            //    children.Add(node);
            //}

            //foreach (var method in methodInfo.Where(VaporInspectorNode.MethodSearchPredicate))
            //{
            //    var path = Property != null ? $"{Property.propertyPath}_m_{method.Name}" : $"m_{method.Name}";
            //    var node = new VaporInspectorNode(this, path, method);
            //    children.Add(node);
            //}
        }

        private void ConfigureField(FieldInfo fieldInfo)
        {

        }
    }
}
