using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VaporInspector;

namespace VaporInspectorEditor
{
    public class VaporGroupNode
    {
        public VaporGroupNode Parent { get; }
        public List<VaporGroupNode> Children { get; }
        public List<(int, int, VisualElement)> ContainerContent { get; }
        public UIGroupType GroupType { get; }
        public string GroupName { get; }
        public int GroupOrder { get; }
        public VisualElement Container { get; }
        public bool IsRootNode { get; }
        public bool ShouldDraw => Children.Count > 0 || ContainerContent.Count > 0;

        public VaporGroupNode(VaporGroupNode parent, VaporGroupAttribute groupAttribute, VisualElement container)
        {
            Parent = parent;
            Children = new List<VaporGroupNode>();
            ContainerContent = new List<(int, int, VisualElement)>();
            if (groupAttribute != null)
            {
                GroupType = groupAttribute.Type;
                GroupName = groupAttribute.GroupName;
                GroupOrder = groupAttribute.Order;
            }
            Container = container;
            IsRootNode = Parent == null;
        }

        public VaporGroupNode(VaporGroupNode parent, UIGroupType type, string groupName, int order, VisualElement container)
        {
            Parent = parent;
            Children = new List<VaporGroupNode>();
            ContainerContent = new List<(int, int, VisualElement)>();
            GroupType = type;
            GroupName = groupName;
            GroupOrder = order;
            Container = container;
            IsRootNode = Parent == null;
        }

        public void AddChild(VaporGroupAttribute groupAttribute, VisualElement container)
        {
            var childNode = new VaporGroupNode(this, groupAttribute, container);
            Debug.Log($"Child Added [{Container.name}]: {childNode.Container.name} - {childNode.GroupOrder}");
            Children.Add(childNode);
        }

        public void AddChild(UIGroupType type, string groupName, int order, VisualElement container)
        {
            var childNode = new VaporGroupNode(this, type, groupName, order, container);
            Debug.Log($"Child Added [{Container.name}]: {childNode.Container.name} - {childNode.GroupOrder}");
            Children.Add(childNode);
        }

        public void AddContent(VaporDrawerInfo info)
        {
            VisualElement ve = GroupType switch
            {
                UIGroupType.Horizontal => _PopulateHorizontalGroup(info),
                UIGroupType.Vertical => _PopulateVerticalGroup(info),
                UIGroupType.Foldout => _PopulateFoldout(info),
                UIGroupType.Box => _PopulateBox(info),
                UIGroupType.Tab => _PopulateTabGroup(info),
                UIGroupType.Title => _PopulateTitleGroup(info),
                _ => null
            };
            int addOrder = ContainerContent.Count;
            Debug.Log($"Content Added [{Container.name}]: {ve.name} - {info.UpdatedOrder} - {addOrder}");
            ContainerContent.Add(new(info.UpdatedOrder, addOrder, ve));

            VisualElement _PopulateHorizontalGroup(VaporDrawerInfo drawer)
            {
                bool isFirst = ContainerContent.Count == 0;
                var drawn = DrawerUtility.DrawVaporElementWithVerticalLayout(drawer, drawer.Path);
                drawn.style.flexGrow = 1;
                drawn.style.marginLeft = isFirst ? 0 : 2;
                return drawn;
            }

            VisualElement _PopulateVerticalGroup(VaporDrawerInfo drawer)
            {
                var formatWithVerticalLayout = false;
                var parentNode = Parent;
                while (parentNode != null)
                {
                    if (parentNode.IsRootNode) { break; }

                    if (Parent.GroupType == UIGroupType.Horizontal)
                    {
                        formatWithVerticalLayout = true;
                    }
                    parentNode = parentNode.Parent;
                }

                // var isFirst = ContainerContent.Count == 0;
                if (formatWithVerticalLayout)
                {
                    var drawn = DrawerUtility.DrawVaporElementWithVerticalLayout(drawer, drawer.Path);
                    drawn.style.marginTop = 1;
                    return drawn;
                }
                else
                {
                    var drawn = DrawerUtility.DrawVaporElement(drawer, drawer.Path);
                    drawn.style.marginTop = 1;
                    return drawn;
                }
            }

            VisualElement _PopulateFoldout(VaporDrawerInfo drawer)
            {
                // var isFirst = ContainerContent.Count == 0;
                var drawn = DrawerUtility.DrawVaporElement(drawer, drawer.Path);
                return drawn;
            }

            VisualElement _PopulateBox(VaporDrawerInfo drawer)
            {
                // var isFirst = ContainerContent.Count == 0;
                var drawn = DrawerUtility.DrawVaporElement(drawer, drawer.Path);
                return drawn;
            }

            VisualElement _PopulateTabGroup(VaporDrawerInfo drawer)
            {
                // var isFirst = ContainerContent.Count == 0;
                var drawn = DrawerUtility.DrawVaporElement(drawer, drawer.Path);
                return drawn;
            }

            VisualElement _PopulateTitleGroup(VaporDrawerInfo drawer)
            {
                // var isFirst = ContainerContent.Count == 0;
                var drawn = DrawerUtility.DrawVaporElement(drawer, drawer.Path);
                return drawn;
            }
        }

        public void BuildContent()
        {
            var sorted = ContainerContent.OrderBy(x => x.Item1).ThenBy(x => x.Item2);
            foreach (var next in sorted)
            {
                Debug.Log($"Built [{Container.name}]: {next.Item3.name} - {next.Item1} - {next.Item2}");
                Container.Add(next.Item3);
            }
        }
    }
}
