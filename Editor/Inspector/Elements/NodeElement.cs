using System;
using UnityEngine;
using UnityEngine.UIElements;
using VaporInspector;

namespace VaporInspectorEditor
{
    /// <summary>
    /// The <see cref="VisualElement"/> representation of a <see cref="VaporInspectorNode"/>
    /// </summary>
    public class NodeElement : VisualElement
    {
        public VaporInspectorNode Node { get; }

        public override VisualElement contentContainer { get; }

        public NodeElement(VaporInspectorNode node)
        {
            Node = node;
            userData = Node;
            name = $"Node_[{Node.DrawOrder}]";

            var shouldFlexGrow = Node.Group is { Type: UIGroupType.Horizontal } || (Node.OverrideGroupTypeWithDrawn && Node.DrawnWithGroup == UIGroupType.Horizontal);
            switch (Node.ElementType)
            {
                case VaporInspectorNode.NodeElementType.Root:
                    contentContainer = this;
                    if (Node.IsDrawnWithVapor)
                    {
                        var rootGroup = _InsertDrawWithVaporGroup();
                        Node.OverrideGroupTypeWithDrawn = true;
                        hierarchy.Add(rootGroup);
                        contentContainer = rootGroup.contentContainer;
                        if (Node.IsRootNode)
                        {
                            style.flexGrow = 1;
                        }
                    }
                    break;
                case VaporInspectorNode.NodeElementType.Field:
                    if (!Node.IsDrawnWithVapor)
                    {
                        if (shouldFlexGrow)
                        {
                            var horizontalField = DrawerUtility.DrawVaporFieldWithVerticalLayout(Node);
                            var isFirst = Node.Parent.Children[0] == Node;
                            horizontalField.style.flexGrow = 1;
                            horizontalField.style.marginLeft = isFirst ? 0 : 2;
                            hierarchy.Add(horizontalField);
                            contentContainer = horizontalField[0].contentContainer;
                        }
                        else
                        {
                            var field = DrawerUtility.DrawVaporField(Node);
                            hierarchy.Add(field);
                            contentContainer = field.contentContainer;
                        }
                    }
                    else
                    {
                        var vaporPropertyGroup = _InsertDrawWithVaporGroup();
                        Node.OverrideGroupTypeWithDrawn = true;
                        hierarchy.Add(vaporPropertyGroup);
                        contentContainer = vaporPropertyGroup.contentContainer;
                    }
                    break;
                case VaporInspectorNode.NodeElementType.Property:
                    var property = DrawerUtility.DrawVaporProperty(Node);
                    if (shouldFlexGrow)
                    {
                        property.style.flexGrow = 1;
                    }
                    hierarchy.Add(property);
                    contentContainer = property.contentContainer;
                    break;
                case VaporInspectorNode.NodeElementType.Method:
                    var method = DrawerUtility.DrawVaporMethod(Node);
                    if (shouldFlexGrow)
                    {
                        method.style.flexGrow = 1;
                    }
                    hierarchy.Add(method);
                    contentContainer = method.contentContainer;
                    break;
                case VaporInspectorNode.NodeElementType.Group:
                    var groupContent = DrawerUtility.DrawGroupElement(Node, Node.Group);
                    if (shouldFlexGrow)
                    {
                        groupContent.style.flexGrow = 1;
                    }
                    hierarchy.Add(groupContent);
                    contentContainer = groupContent.contentContainer;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            
            foreach (var child in Node.Children)
            {
                child.Draw(this);
            }

            VisualElement _InsertDrawWithVaporGroup()
            {
                VaporGroupAttribute vaporGroup = Node.DrawnWithGroup switch
                {
                    UIGroupType.Horizontal => new HorizontalGroupAttribute($"{Node.Property.propertyPath}"),
                    UIGroupType.Vertical => new VerticalGroupAttribute($"{Node.Property.propertyPath}"),
                    UIGroupType.Foldout => new FoldoutGroupAttribute($"{Node.Property.propertyPath}", Node.Property.displayName),
                    UIGroupType.Box => new BoxGroupAttribute($"{Node.Property.propertyPath}", Node.Property.displayName),
                    UIGroupType.Tab => new VerticalGroupAttribute($"{Node.Property.propertyPath}"),
                    UIGroupType.Title => new TitleGroupAttribute($"{Node.Property.propertyPath}", Node.Property.displayName),
                    _ => throw new ArgumentOutOfRangeException()
                };
                        
                var vaporPropertyGroup = DrawerUtility.DrawGroupElement(Node, vaporGroup);
                if (shouldFlexGrow)
                {
                    vaporPropertyGroup.style.flexGrow = 1;
                }

                return vaporPropertyGroup;
            }
        }
    }
}
