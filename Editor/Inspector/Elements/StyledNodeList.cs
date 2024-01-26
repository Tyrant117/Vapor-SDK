using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VaporInspector;
using Object = UnityEngine.Object;

namespace VaporInspectorEditor
{
    public class StyledNodeList : Box
    {
        public VaporInspectorNode Node { get; }
        public SerializedProperty ArraySizeProperty { get; private set; }
        public TypeInfo ElementType { get; }
        public Foldout Foldout { get; private set; }
        public Label Label { get; private set; }
        public VisualElement Content { get; private set; }
        public ListView ListView { get; private set; }

        public override VisualElement contentContainer => ListView;

        public StyledNodeList(VaporInspectorNode node)
        {
            Node = node;
            ArraySizeProperty = node.Property.FindPropertyRelative("Array.size");
            StyleBox();
            StyleFoldout();
            hierarchy.Add(ListView);
            var ti = Node.FieldInfo.FieldType.GetTypeInfo();
            ElementType = ti.IsGenericType
                ? ti.GenericTypeArguments[0].GetTypeInfo()
                : ti.GetElementType().GetTypeInfo();
        }

        public void Rebind()
        {
            ArraySizeProperty = Node.Property.FindPropertyRelative("Array.size");
            ListView.itemsSource = null;
            ListView.BindProperty(Node.Property);
            ListView.RefreshItems();
        }

        protected void StyleBox()
        {
            name = $"{Node.Property.propertyPath}_styled-list";
            style.borderBottomColor = ContainerStyles.BorderColor;
            style.borderTopColor = ContainerStyles.BorderColor;
            style.borderRightColor = ContainerStyles.BorderColor;
            style.borderLeftColor = ContainerStyles.BorderColor;
            style.borderBottomLeftRadius = 3;
            style.borderBottomRightRadius = 3;
            style.borderTopLeftRadius = 3;
            style.borderTopRightRadius = 3;
            style.marginTop = 3;
            style.marginBottom = 3;
            style.marginLeft = 0;
            style.marginRight = 0;
        }

        protected void StyleFoldout()
        {
            ListView = new ListView
            {
                name = $"{Node.Property.propertyPath}_styled-list-view",
                style =
                {
                    maxHeight = 251,
                    flexGrow = 1
                },
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showFoldoutHeader = true,
                showAddRemoveFooter = false,
                showBorder = false,
                showBoundCollectionSize = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                makeItem = OnCustomMake,
                bindItem = OnCustomBind,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                selectionType = SelectionType.None,
                itemsSource = null
            };
            ListView.BindProperty(Node.Property);

            Foldout = ListView.Q<Foldout>();
            Foldout.text = Node.Property.displayName;
            Foldout.name = $"{Node.Property.propertyPath}_styled-list-foldout";
            Foldout.viewDataKey = $"styled-list-foldout__vdk_{Node.Property.displayName}";

            var tog = Foldout.Q<Toggle>();
            tog.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
            tog.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Clear", ca =>
                {
                    Node.CleanupResolvers(true);
                    Node.Property.ClearArray();
                    Node.Property.serializedObject.ApplyModifiedProperties();
                    Rebind();
                    //ListView.schedule.Execute(() => ListView.RefreshItems()).ExecuteLater(100);
                });
            }));
            var togStyle = tog.style;
            togStyle.marginTop = 0;
            togStyle.marginLeft = 0;
            togStyle.marginRight = 0;
            togStyle.marginBottom = 0;
            togStyle.backgroundColor = ContainerStyles.BackgroundColor;

            var togContainerStyle = tog.hierarchy[0].style;
            togContainerStyle.marginLeft = 3;
            togContainerStyle.marginTop = 3;
            togContainerStyle.marginBottom = 3;
            togContainerStyle.flexShrink = 1;

            // Label
            Label = Foldout.Q<Toggle>().Q<Label>();
            Label.style.textOverflow = TextOverflow.Ellipsis;

            // tog.Add(new ToolbarSearchField());
            var sizeVal = new ValidatedIntegerField()
            {
                style =
                {
                    minWidth = 51,
                    marginRight = 2,
                    marginTop = 2,
                },
                isDelayed = true,
            };
            sizeVal.BindProperty(ArraySizeProperty);
            sizeVal.ValidateInput += newVal => newVal >= 0;
            sizeVal.RegisterValueChangedCallback(evt =>
            {
                var target = evt.target as IntegerField;
                if(evt.newValue != evt.previousValue)
                {
                    Rebind();
                }
                if (evt.newValue >= 0)
                {
                    return;
                }
                if (target != null) 
                {
                    target.value = 0;
                }
            });
            var valText = sizeVal[0][0];
            valText.style.marginLeft = 0;
            valText.style.paddingLeft = 2;
            tog.Add(sizeVal);
            var minus = new Button(OnRemoveFromList)
            {
                text = "-",
                style =
                {
                    paddingLeft = 5,
                    paddingRight = 5,
                    fontSize = 14,
                    borderBottomWidth = 0,
                    borderLeftWidth = 1,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    borderBottomLeftRadius = 0,
                    borderBottomRightRadius = 0,
                    borderTopLeftRadius = 0,
                    borderTopRightRadius = 0,
                    marginLeft = 0,
                    marginRight = 0,
                    minWidth = 21,
                }
            };
            tog.Add(minus);
            var plus = new Button(OnAddToList)
            {
                text = "+",
                style =
                {
                    paddingLeft = 5,
                    paddingRight = 5,
                    fontSize = 14,
                    borderBottomWidth = 0,
                    borderLeftWidth = 1,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    borderBottomLeftRadius = 0,
                    borderBottomRightRadius = 0,
                    borderTopLeftRadius = 0,
                    borderTopRightRadius = 0,
                    marginLeft = 0,
                    marginRight = 0,
                }
            };
            tog.Add(plus);

            // Content
            Content = Foldout.Q<VisualElement>("unity-content");

            Foldout.value = false;
        }

        private void OnCustomBind(VisualElement toBind, int index)
        {
            // Debug.Log($"Custom Bind {Drawer.Property.displayName} Index: {index}");
            var prop = GetPropertyAtIndex(index);
            if (prop == null)
            {
                return;
            }

            if (ElementType.IsDefined(typeof(DrawWithVaporAttribute)))
            {
                var rootElement = toBind[0];
                // var drawers = rootElement.userData as List<VaporDrawerInfo>;
                var node = rootElement.userData as VaporInspectorNode;
                var foldout = rootElement.Q<StyledFoldout>();
                if (foldout != null)
                {
                    foldout.Label.text = $"Element {index}";
                }

                // ReSharper disable once PossibleNullReferenceException
                node.Rebind(prop);
                //Debug.Log($"Node Property: {node.Target} - {node.Property.propertyPath}");
                // foreach (var drawer in drawers)
                // {
                //     drawer.Rebind(prop);
                // }

                foreach (var bindable in rootElement.Query().Where(x => x is IBindable).ToList())
                {
                    if (bindable is not PropertyField field)
                    {
                        continue;
                    }

                    if (!bindable.name.Contains(Node.Property.name))
                    {
                        continue;
                    }

                    var lastIndex = bindable.name.LastIndexOf('.') + 1;
                    var lastElement = bindable.name[lastIndex..];
                    var propToBind = prop.FindPropertyRelative(lastElement);
                    //Debug.Log($"Binding Property: {propToBind.propertyPath} = {propToBind.boxedValue}");
                    field.BindProperty(propToBind);
                    //DrawerUtility.OnNodePropertyBuilt(field);
                }

                foreach (var b in rootElement.Query<StyledButton>().ToList())
                {
                    DrawerUtility.OnNodeMethodBuilt(b);
                }

                foreach (var list in rootElement.Query<StyledNodeList>().ToList())
                {
                    list.Rebind();
                }
            }
            else
            {
                var field = toBind.Q<PropertyField>();
                field.BindProperty(prop);
                DrawerUtility.OnNodeListPropertyBuilt(field, prop, Node);
            }

            var button = toBind.Q<Button>("styled-list-element-button__delete");
            button.clickable = new Clickable(() => RemoveIndexFromList(index));
        }

        protected VisualElement OnCustomMake()
        {
            // Debug.Log($"Custom Make {Drawer.Property.displayName}");
            var be = new BindableElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            
            var nextProp = GetPropertyAtIndex(ArraySizeProperty.intValue - 1);
            var nodeRoot = new VaporInspectorNode(ElementType, nextProp);
            if (nodeRoot.IsDrawnWithVapor)
            {
                nodeRoot.Draw(be);
                Node.Add(nodeRoot);
            }
            else
            {
                var pf = new PropertyField()
                {
                    userData = Node.Property.serializedObject,
                    style =
                    {
                        flexGrow = 1,
                        marginRight = 3,
                    }
                };
                pf.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("Set To Null", action =>
                    {
                        var data = action.userData as PropertyField;
                        if (data.userData is not SerializedObject so)
                        {
                            return;
                        }

                        var prop = so.FindProperty(data.bindingPath);
                        if (prop == null)
                        {
                            return;
                        }

                        prop.boxedValue = null;
                        prop.serializedObject.ApplyModifiedProperties();
                    }, _ => ElementType.IsSubclassOf(typeof(Object)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden, pf);
                    evt.menu.AppendAction("Reset", action =>
                    {
                        var data = action.userData as PropertyField;
                        if (data.userData is not SerializedObject so)
                        {
                            return;
                        }

                        var prop = so.FindProperty(data.bindingPath);
                        if (prop == null)
                        {
                            return;
                        }

                        var val = Activator.CreateInstance(ElementType.AsType());
                        prop.boxedValue = val;
                        prop.serializedObject.ApplyModifiedProperties();
                    }, _ => !ElementType.IsSubclassOf(typeof(Object)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden, pf);
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Copy", action =>
                    {
                        var data = action.userData as PropertyField;
                        if (data.userData is not SerializedObject so)
                        {
                            return;
                        }

                        var prop = so.FindProperty(data.bindingPath);
                        if (prop == null)
                        {
                            return;
                        }

                        ClipboardUtility.WriteToBuffer(prop.boxedValue);
                    }, _ => DropdownMenuAction.Status.Normal, pf);
                    evt.menu.AppendAction("Paste", action =>
                    {
                        var data = action.userData as PropertyField;
                        if (data.userData is not SerializedObject so)
                        {
                            return;
                        }

                        var prop = so.FindProperty(data.bindingPath);
                        if (prop == null)
                        {
                            return;
                        }

                        ClipboardUtility.ReadFromBuffer(prop, ElementType.AsType());
                    }, actionStatus =>
                    {
                        var data = actionStatus.userData as PropertyField;
                        if (data.userData is not SerializedObject so)
                        {
                            return DropdownMenuAction.Status.Hidden;
                        }

                        var prop = so.FindProperty(data.bindingPath);
                        if (prop == null)
                        {
                            return DropdownMenuAction.Status.Hidden;
                        }

                        var read = ClipboardUtility.CanReadFromBuffer(ElementType.AsType());
                        return read ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                    }, pf);
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Copy Binding Path", action =>
                    {
                        var data = action.userData as PropertyField;
                        if (data.userData is not SerializedObject so)
                        {
                            return;
                        }

                        var prop = so.FindProperty(data.bindingPath);
                        if (prop == null)
                        {
                            return;
                        }

                        EditorGUIUtility.systemCopyBuffer = data.bindingPath;
                    }, _ => DropdownMenuAction.Status.Normal, pf);
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Duplicate Array Element", action =>
                    {
                        var data = action.userData as PropertyField;
                        if (data.userData is not SerializedObject so)
                        {
                            return;
                        }

                        var prop = so.FindProperty(data.bindingPath);
                        if (prop == null)
                        {
                            return;
                        }

                        var matchIndex = -1;
                        for (var i = 0; i < ArraySizeProperty.intValue; i++)
                        {
                            var path = GetPropertyAtIndex(i).propertyPath;
                            // Debug.Log(path);
                            if (path != prop.propertyPath) continue;
                            
                            matchIndex = i;
                            break;
                        }
                        // Debug.Log(matchIndex);
                        if (matchIndex == -1)
                        {
                            return;
                        }
                        Node.Property.InsertArrayElementAtIndex(matchIndex);
                        Node.Property.serializedObject.ApplyModifiedProperties();
                    }, _ => DropdownMenuAction.Status.Normal, pf);
                }));

                be.Add(pf);
            }

            var del = new Button()
            {
                name = "styled-list-element-button__delete",
                text = "x",
                style =
                {
                    paddingLeft = 4,
                    paddingRight = 4,
                    fontSize = 11,
                    marginLeft = 1,
                    marginRight = -2,
                }
            };
            be.Add(del);

            return be;
        }

        #region - Add / Remove -
        private void OnAddToList()
        {
            // Debug.Log($"Added: {Drawer.Property.displayName}");
            ListView.viewController.AddItems(1);
            // Drawer.Property.arraySize++;
            // Drawer.Property.serializedObject.ApplyModifiedProperties();
            // ListView.schedule.Execute(() => { ListView.RefreshItems(); }).ExecuteLater(100L);
        }

        private void OnRemoveFromList()
        {
            if (ArraySizeProperty.intValue <= 0)
            {
                return;
            }
            //var propToRemove = GetPropertyAtIndex(ArraySizeProperty.intValue - 1);
            //Debug.Log($"Removing Prop {propToRemove.propertyPath}");
            //Node.CleanupResolverWithProperty(propToRemove);
            ListView.viewController.RemoveItem(ArraySizeProperty.intValue - 1);
            Rebind();

            // Drawer.Property.arraySize--;
            // Drawer.Property.serializedObject.ApplyModifiedProperties();
            // ListView.schedule.Execute(() => { ListView.RefreshItems(); }).ExecuteLater(100L);
        }

        private void RemoveIndexFromList(int index)
        {
            ListView.viewController.RemoveItem(index);
            Rebind();
            // Node.Property.DeleteArrayElementAtIndex(index);
            // Node.Property.serializedObject.ApplyModifiedProperties();
            // ListView.RefreshItems();
            // ListView.schedule.Execute(() => { ListView.RefreshItems(); }).ExecuteLater(100L);
        }
        #endregion

        #region - Helpers -
        private SerializedProperty GetPropertyAtIndex(int index)
        {
            return index < Node.Property.arraySize && index >= 0
                ? Node.Property.FindPropertyRelative($"Array.data[{index}]")
                : null;
        }
        #endregion
    }
}
