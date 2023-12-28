using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
// ReSharper disable UnassignedField.Global

namespace VaporInspector
{
    public class SearchableDropdown<T> : DropdownField
    {
        private GenericDropdownMenu _currentMenu;
        private TextField _searchField;
        private ScrollView _scrollView;
        private List<T> _unfilteredChoices;
        private List<T> _filteredChoices;
        public T Value { get; set; }
        public int Index { get; set; } = -1;

        public Action<VisualElement, T,T> ValueChanged;

        public SearchableDropdown(string label, T currentValue) : base(label)
        {
            var propertyInfo = typeof(BasePopupField<string, string>).GetField("createMenuCallback", BindingFlags.Instance | BindingFlags.NonPublic);
            propertyInfo?.SetValue(this, new Func<GenericDropdownMenu>(CreateMenu));
            // ReSharper disable once VirtualMemberCallInConstructor
            value = currentValue.ToString();
            Value = currentValue;
        }

        public void SetChoices(List<T> allChoices)
        {
            // choices = allChoices;
            _unfilteredChoices = allChoices;
            _filteredChoices = new List<T>();
            Index = _unfilteredChoices.FindIndex(arg => arg.ToString() == value);
        }

        private GenericDropdownMenu CreateMenu()
        {
            var menu = new GenericDropdownMenu();
            var menuContainerInfo = menu.GetType().GetProperty("menuContainer", BindingFlags.Instance | BindingFlags.NonPublic);
            var scrollViewContainerInfo = typeof(GenericDropdownMenu).GetProperty("scrollView", BindingFlags.Instance | BindingFlags.NonPublic);
            // ReSharper disable once PossibleNullReferenceException
            var menuContainer = (VisualElement)menuContainerInfo.GetValue(menu);
            // ReSharper disable once PossibleNullReferenceException
            _scrollView = (ScrollView)scrollViewContainerInfo.GetValue(menu);
            _searchField = new TextField()
            {
                style =
                {
                    marginBottom = 3
                }
            };
            _searchField.RegisterValueChangedCallback(_ => Rebuild());
            _searchField.schedule.Execute(() => { _searchField.Focus(); }).ExecuteLater(60L);

            menuContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _scrollView.RegisterCallback<FocusOutEvent>(OnFocusOut, TrickleDown.TrickleDown);
            _scrollView.hierarchy.Insert(0, _searchField);
            _scrollView.style.minHeight = 32;
            _scrollView.style.maxHeight = 180;
            _currentMenu = menu;
            Rebuild(true);
            return _currentMenu;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!_scrollView.ContainsPoint(MousePosition()))
                HideMenu();
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            if (!_scrollView.ContainsPoint(MousePosition()))
                HideMenu();

            evt.StopPropagation();
        }

        private Vector2 MousePosition()
        {
            if (_currentMenu == null) return new Vector2();
            var mpFi = _currentMenu.GetType().GetField("m_MousePosition", BindingFlags.Instance | BindingFlags.NonPublic);
            // ReSharper disable once PossibleNullReferenceException
            return (Vector2)mpFi.GetValue(_currentMenu);
        }

        private void HideMenu()
        {
            if (_currentMenu == null) return;
            var hideMi = _currentMenu.GetType().GetMethod("Hide", BindingFlags.Instance | BindingFlags.NonPublic);
            // ReSharper disable once PossibleNullReferenceException
            hideMi.Invoke(_currentMenu, new object[] { false });
        }

        private void Rebuild(bool first = false)
        {
            // ReSharper disable once PossibleNullReferenceException
            var menuItems = _currentMenu.GetType().GetField("m_Items", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_currentMenu) as IList;
            // ReSharper disable once PossibleNullReferenceException
            menuItems.Clear();
            for (int i = _scrollView.childCount - 1; i >= 0; i--)
            {
                _scrollView[i].RemoveFromHierarchy();
            }


            bool filtered = true;
            var filterWord = _searchField.value;
            _filteredChoices.Clear();
            if (string.IsNullOrEmpty(filterWord) || filterWord.Equals("None"))
            {
                filtered = false;
                _filteredChoices.AddRange(_unfilteredChoices);
            }
            else
            {
                foreach (var choice in _unfilteredChoices.Where(choice => FuzzySearch.FuzzyMatch(filterWord, choice.ToString())))
                {
                    Debug.Log(choice);
                    _filteredChoices.Add(choice);
                }
            }

            var idx = 0;
            var checkedIndex = -1;
            foreach (var choice in _filteredChoices)
            {
                // AddItem(choice, index == idx, true, OnSelect, choice);
                // Debug.Log("Adding " + choice);
                var isChecked = !filtered && Index == idx;
                if (isChecked)
                {
                    checkedIndex = idx;
                }

                _currentMenu.AddItem(choice.ToString(), isChecked, OnSelect, choice);
                idx++;
            }

            if (first && checkedIndex >= 0)
            {
                const int maxDiff = ((180 - 32) / 20 - 1) / 2;
                var diff = Mathf.Min(maxDiff, _scrollView.childCount - 1 - checkedIndex);
                _scrollView.schedule.Execute(() => _scrollView.ScrollTo(_scrollView.contentContainer[checkedIndex + diff])).ExecuteLater(60L);
            }

            var desMin = Mathf.Max(10 + 22 + _filteredChoices.Count * 20, 32);
            _scrollView.style.minHeight = Mathf.Min(180, desMin);
        }

        private void OnSelect(object obj)
        {
            if (obj is not T s) return;

            Index = _unfilteredChoices.FindIndex(arg => arg.ToString() == s.ToString());
            value = s.ToString();
            var oldValue = Value;
            Value = s;
            ValueChanged?.Invoke(this, oldValue, s);
            Debug.Log($"Selected {Index} {value}");
        }

        // private MenuItem AddItem(string itemName, bool isChecked, bool isEnabled, Action<object> action = null, object data = null)
        // {
        //     // if (string.IsNullOrEmpty(itemName) || itemName.EndsWith("/"))
        //     // {
        //     //     this.AddSeparator(itemName);
        //     //     return (MenuItem)null;
        //     // }
        //     //
        //     // for (int index = 0; index < this.m_Items.Count; ++index)
        //     // {
        //     //     if (itemName == this.m_Items[index].name)
        //     //         return (MenuItem)null;
        //     // }
        //
        //     VisualElement child1 = new VisualElement();
        //     child1.AddToClassList(GenericDropdownMenu.itemUssClassName);
        //     child1.SetEnabled(isEnabled);
        //     child1.userData = data;
        //     VisualElement child2 = new VisualElement();
        //     child2.AddToClassList(GenericDropdownMenu.checkmarkUssClassName);
        //     child2.pickingMode = PickingMode.Ignore;
        //     child1.Add(child2);
        //     if (isChecked)
        //         child1.AddPsuedoState((int)ExternalPseudoStates.Checked);
        //     Label child3 = new Label(itemName);
        //     child3.AddToClassList(GenericDropdownMenu.labelUssClassName);
        //     child3.pickingMode = PickingMode.Ignore;
        //     child1.Add((VisualElement)child3);
        //     _scrollView.Add(child1);
        //     MenuItem menuItem = new MenuItem()
        //     {
        //         name = itemName,
        //         element = child1,
        //         actionUserData = action
        //     };
        //     // this.m_Items.Add(menuItem);
        //     return menuItem;
        // }
        //
        // public class MenuItem
        // {
        //     public string name;
        //     public VisualElement element;
        //     public Action<object> actionUserData;
        // }
    }
}
