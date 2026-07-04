#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMFramework.HierarchyColor
{
    [InitializeOnLoad]
    public class ColorfulHierarchy
    {
        private const string NEW_HIERARCHY_WINDOW_TYPE_NAME = "Unity.Hierarchy.Editor.HierarchyWindow";
        private const string NEW_HIERARCHY_ROW_NAME = "unity-multi-column-view__row-container";
        private const string NEW_HIERARCHY_ITEM_CONTAINER_TYPE_NAME = "Unity.Hierarchy.HierarchyViewItemContainer";
        private const string NEW_HIERARCHY_NAME_CLASS = "hierarchy-item__name";
        private const string NEW_HIERARCHY_LEFT_CUSTOM_SECTION_CLASS = "hierarchy-item__left-custom-section";
        private const string NEW_HIERARCHY_ICON_ROOT_CLASS = "hierarchy-color-component-icons";

        private static readonly HashSet<int> scheduledWindowIDs = new();

        private static FieldInfo viewItemField;
        private static FieldInfo nodeField;
        private static FieldInfo handlerField;
        private static MethodInfo getGameObjectMethod;
        private static Type getGameObjectMethodHandlerType;

        static ColorfulHierarchy()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyWindow;
            EditorApplication.update += AttachToNewHierarchyWindows;
            EditorApplication.hierarchyChanged += RepaintNewHierarchyWindows;
        }

        private static void OnHierarchyWindow(EntityId instanceID, Rect selectionRect)
        {
            var instance = EditorUtility.EntityIdToObject(instanceID);

            if (instance == null)
            {
                return;
            }

            foreach (var preset in HierarchyColorSettings.instance.ColorPresets)
            {
                if (string.IsNullOrEmpty(preset.keyChar))
                {
                    continue;
                }

                if (instance.name.TrimStart().StartsWith(preset.keyChar))
                {
                    string newName = instance.name[preset.keyChar.Length..];

                    EditorGUI.DrawRect(selectionRect, preset.backgroundColor);

                    GUIStyle newStyle = new()
                    {
                        alignment = preset.textAlignment,
                        fontStyle = preset.fontStyle,
                        normal = new GUIStyleState()
                        {
                            textColor = preset.textColor,
                        }
                    };

                    if (preset.autoUpperLetters)
                    {
                        newName = newName.ToUpper();
                    }

                    EditorGUI.LabelField(selectionRect, newName, newStyle);
                }
            }
        }

        private static void AttachToNewHierarchyWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null || window.GetType().FullName != NEW_HIERARCHY_WINDOW_TYPE_NAME)
                {
                    continue;
                }

                int windowID = window.GetInstanceID();
                if (!scheduledWindowIDs.Add(windowID))
                {
                    continue;
                }

                window.rootVisualElement.schedule.Execute(() => ApplyToNewHierarchyWindow(window)).Every(200);
            }
        }

        private static void RepaintNewHierarchyWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window != null && window.GetType().FullName == NEW_HIERARCHY_WINDOW_TYPE_NAME)
                {
                    ApplyToNewHierarchyWindow(window);
                }
            }
        }

        private static void ApplyToNewHierarchyWindow(EditorWindow window)
        {
            if (window == null)
            {
                return;
            }

            var rows = new List<VisualElement>(
                FindAll(window.rootVisualElement, element => element.name == NEW_HIERARCHY_ROW_NAME));
            foreach (var row in rows)
            {
                ApplyToNewHierarchyRow(row);
            }
        }

        private static void ApplyToNewHierarchyRow(VisualElement row)
        {
            var gameObject = GetNewHierarchyGameObject(row);
            var label = FindNewHierarchyNameLabel(row);

            if (label == null)
            {
                ClearNewHierarchyRow(row, null, null);
                return;
            }

            string objectName = gameObject != null ? gameObject.name : label.text;
            if (!TryGetPreset(objectName, out var preset))
            {
                ClearNewHierarchyRow(row, label, objectName);
                DrawNewHierarchyComponentIcons(row, gameObject);
                return;
            }

            string displayName = objectName[preset.keyChar.Length..];
            if (preset.autoUpperLetters)
            {
                displayName = displayName.ToUpper();
            }

            row.style.backgroundColor = preset.backgroundColor;
            label.text = displayName;
            label.style.color = preset.textColor;
            label.style.unityFontStyleAndWeight = preset.fontStyle;
            label.style.unityTextAlign = preset.textAlignment;

            DrawNewHierarchyComponentIcons(row, gameObject);
        }

        private static void ClearNewHierarchyRow(VisualElement row, Label label, string objectName)
        {
            row.style.backgroundColor = StyleKeyword.Null;

            if (label != null)
            {
                if (!string.IsNullOrEmpty(objectName))
                {
                    label.text = objectName;
                }

                label.style.color = StyleKeyword.Null;
                label.style.unityFontStyleAndWeight = StyleKeyword.Null;
                label.style.unityTextAlign = StyleKeyword.Null;
            }

            ClearNewHierarchyComponentIcons(row);
        }

        private static bool TryGetPreset(string objectName, out HierarchyColorPreset preset)
        {
            preset = null;
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            foreach (var candidate in HierarchyColorSettings.instance.ColorPresets)
            {
                if (string.IsNullOrEmpty(candidate.keyChar))
                {
                    continue;
                }

                if (objectName.TrimStart().StartsWith(candidate.keyChar))
                {
                    preset = candidate;
                    return true;
                }
            }

            return false;
        }

        private static Label FindNewHierarchyNameLabel(VisualElement row)
        {
            var nameElement = FindFirst(row, element => element.ClassListContains(NEW_HIERARCHY_NAME_CLASS));
            return FindFirst(nameElement, element => element is Label label && !string.IsNullOrEmpty(label.text)) as Label;
        }

        private static GameObject GetNewHierarchyGameObject(VisualElement row)
        {
            try
            {
                var itemContainer = FindFirst(row,
                    element => element.GetType().FullName == NEW_HIERARCHY_ITEM_CONTAINER_TYPE_NAME);
                if (itemContainer == null)
                {
                    return null;
                }

                EnsureNewHierarchyReflection(itemContainer.GetType());

                var viewItem = viewItemField?.GetValue(itemContainer);
                if (viewItem == null)
                {
                    return null;
                }

                var node = nodeField?.GetValue(viewItem);
                var handler = handlerField?.GetValue(viewItem);
                if (node == null || handler == null)
                {
                    return null;
                }

                EnsureNewHierarchyHandlerReflection(handler.GetType());
                if (getGameObjectMethod == null)
                {
                    return null;
                }

                object[] args = { node };
                return getGameObjectMethod.Invoke(handler, args) as GameObject;
            }
            catch
            {
                return null;
            }
        }

        private static void EnsureNewHierarchyReflection(Type itemContainerType)
        {
            viewItemField ??= itemContainerType.GetField("m_ViewItem",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var viewItemType = viewItemField?.FieldType;
            if (viewItemType == null)
            {
                return;
            }

            nodeField ??= viewItemType.GetField("m_Node",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            handlerField ??= viewItemType.GetField("m_Handler",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static void EnsureNewHierarchyHandlerReflection(Type handlerType)
        {
            if (getGameObjectMethodHandlerType == handlerType)
            {
                return;
            }

            getGameObjectMethodHandlerType = handlerType;
            getGameObjectMethod = handlerType.GetMethod("GetGameObject",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static void DrawNewHierarchyComponentIcons(VisualElement row, GameObject gameObject)
        {
            ClearNewHierarchyComponentIcons(row);

            if (gameObject == null)
            {
                return;
            }

            var customSection = FindFirst(row,
                element => element.ClassListContains(NEW_HIERARCHY_LEFT_CUSTOM_SECTION_CLASS));
            if (customSection == null)
            {
                return;
            }

            var components = HierarchyComponentIcon.GetVisibleComponents(gameObject);
            if (components.Count == 0)
            {
                return;
            }

            int iconSize = HierarchyComponentIcon.IconSize;
            var iconRoot = new VisualElement
            {
                name = NEW_HIERARCHY_ICON_ROOT_CLASS,
                pickingMode = PickingMode.Ignore
            };
            iconRoot.AddToClassList(NEW_HIERARCHY_ICON_ROOT_CLASS);
            iconRoot.style.flexDirection = FlexDirection.Row;
            iconRoot.style.marginLeft = 4;
            iconRoot.style.height = iconSize;

            int count = Mathf.Min(components.Count, HierarchyComponentIcon.MaxIconNum);
            for (int i = 0; i < count; i++)
            {
                Texture2D texture = HierarchyComponentIcon.GetComponentIcon(components[i]);
                if (texture == null)
                {
                    continue;
                }

                var image = new Image
                {
                    image = texture,
                    pickingMode = PickingMode.Ignore
                };
                image.style.width = iconSize;
                image.style.height = iconSize;
                image.style.marginLeft = 1;
                iconRoot.Add(image);
            }

            if (iconRoot.childCount > 0)
            {
                customSection.Add(iconRoot);
            }
        }

        private static void ClearNewHierarchyComponentIcons(VisualElement row)
        {
            var iconRoots = new List<VisualElement>(FindAll(row,
                element => element.ClassListContains(NEW_HIERARCHY_ICON_ROOT_CLASS)));
            foreach (var iconRoot in iconRoots)
            {
                iconRoot.RemoveFromHierarchy();
            }
        }

        private static VisualElement FindFirst(VisualElement root, Func<VisualElement, bool> predicate)
        {
            if (root == null)
            {
                return null;
            }

            if (predicate(root))
            {
                return root;
            }

            for (int i = 0; i < root.hierarchy.childCount; i++)
            {
                var result = FindFirst(root.hierarchy.ElementAt(i), predicate);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static IEnumerable<VisualElement> FindAll(VisualElement root, Func<VisualElement, bool> predicate)
        {
            if (root == null)
            {
                yield break;
            }

            if (predicate(root))
            {
                yield return root;
            }

            for (int i = 0; i < root.hierarchy.childCount; i++)
            {
                foreach (var child in FindAll(root.hierarchy.ElementAt(i), predicate))
                {
                    yield return child;
                }
            }
        }
    }
}
#endif
