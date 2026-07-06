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
        private struct NewHierarchyRowStyleState
        {
            public string ObjectName;
            public string DisplayName;
            public HierarchyColorPreset Preset;
        }

        private const int NEW_HIERARCHY_REFRESH_INTERVAL_MS = 16;
        private const string NEW_HIERARCHY_WINDOW_TYPE_NAME = "Unity.Hierarchy.Editor.HierarchyWindow";
        private const string NEW_HIERARCHY_ROW_NAME = "unity-multi-column-view__row-container";
        private const string NEW_HIERARCHY_ITEM_CONTAINER_TYPE_NAME = "Unity.Hierarchy.HierarchyViewItemContainer";
        private const string NEW_HIERARCHY_NAME_CLASS = "hierarchy-item__name";
        private const string NEW_HIERARCHY_DEFAULT_ICON_CLASS = "hierarchy-item__icon";
        private const string NEW_HIERARCHY_LEFT_CUSTOM_SECTION_CLASS = "hierarchy-item__left-custom-section";
        private const string NEW_HIERARCHY_ICON_ROOT_CLASS = "hierarchy-color-component-icons";
        private const string NEW_HIERARCHY_MAIN_ICON_CLASS = "hierarchy-color-main-component-icon";
        private const string NEW_HIERARCHY_MAIN_ICON_HOST_CLASS = "hierarchy-color-main-component-icon-host";

        private static FieldInfo viewItemField;
        private static FieldInfo nodeField;
        private static FieldInfo handlerField;
        private static MethodInfo getGameObjectMethod;
        private static Type getGameObjectMethodHandlerType;
        private static readonly HashSet<long> scheduledNewHierarchyWindowIDs = new();
        private static readonly Dictionary<VisualElement, NewHierarchyRowStyleState> newHierarchyRowStyleStates = new();

        static ColorfulHierarchy()
        {
#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyWindow;
#else
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindow;
#endif
            EditorApplication.update += ApplyToNewHierarchyWindows;
            EditorApplication.hierarchyChanged += RepaintNewHierarchyWindows;
        }

#if UNITY_6000_4_OR_NEWER
        private static void OnHierarchyWindow(EntityId entityID, Rect selectionRect)
#else
        private static void OnHierarchyWindow(int instanceID, Rect selectionRect)
#endif
        {
            var settings = HierarchyColorSettings.instance;
            if (!settings.EnableHighlight)
            {
                return;
            }

#if UNITY_6000_4_OR_NEWER
            var instance = EditorUtility.EntityIdToObject(entityID);
#else
            var instance = EditorUtility.InstanceIDToObject(instanceID);
#endif

            if (instance == null)
            {
                return;
            }

            foreach (var preset in settings.ColorPresets)
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

        private static void ApplyToNewHierarchyWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null || window.GetType().FullName != NEW_HIERARCHY_WINDOW_TYPE_NAME)
                {
                    continue;
                }

                ApplyToNewHierarchyWindow(window);
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
            if (window == null || window.rootVisualElement == null)
            {
                return;
            }

            EnsureNewHierarchyWindowScheduled(window);
            ApplyToNewHierarchyRows(window.rootVisualElement);
        }

        private static void EnsureNewHierarchyWindowScheduled(EditorWindow window)
        {
            long windowID = GetObjectID(window);
            if (!scheduledNewHierarchyWindowIDs.Add(windowID))
            {
                return;
            }

            window.rootVisualElement.schedule.Execute(() =>
            {
                if (window == null || window.rootVisualElement == null)
                {
                    scheduledNewHierarchyWindowIDs.Remove(windowID);
                    return;
                }

                ApplyToNewHierarchyRows(window.rootVisualElement);
            }).Every(NEW_HIERARCHY_REFRESH_INTERVAL_MS);
        }

        private static long GetObjectID(UnityEngine.Object obj)
        {
#if UNITY_6000_5_OR_NEWER
            return (long)EntityId.ToULong(obj.GetEntityId());
#else
            return obj.GetInstanceID();
#endif
        }

        private static void ApplyToNewHierarchyRows(VisualElement root)
        {
            var rows = new List<VisualElement>(
                FindAll(root, element => element.name == NEW_HIERARCHY_ROW_NAME));
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

            if (!HierarchyColorSettings.instance.EnableHighlight)
            {
                string originalName = gameObject != null ? gameObject.name : label.text;
                if (gameObject == null && newHierarchyRowStyleStates.TryGetValue(row, out var state))
                {
                    originalName = state.ObjectName;
                }

                ClearNewHierarchyRow(row, label, originalName);
                DrawNewHierarchyComponentIcons(row, gameObject);
                return;
            }

            if (gameObject == null)
            {
                if (!TryApplyCachedNewHierarchyRow(row, label))
                {
                    ClearNewHierarchyRow(row, label, label.text);
                }

                return;
            }

            string objectName = gameObject.name;
            if (!TryGetPreset(objectName, out var preset))
            {
                ClearNewHierarchyRow(row, label, objectName);
                DrawNewHierarchyComponentIcons(row, gameObject);
                return;
            }

            ApplyNewHierarchyPreset(row, label, objectName, preset, out var displayName);
            newHierarchyRowStyleStates[row] = new()
            {
                ObjectName = objectName,
                DisplayName = displayName,
                Preset = preset
            };

            DrawNewHierarchyComponentIcons(row, gameObject);
        }

        private static bool TryApplyCachedNewHierarchyRow(VisualElement row, Label label)
        {
            if (!newHierarchyRowStyleStates.TryGetValue(row, out var state))
            {
                return false;
            }

            if (label.text != state.ObjectName && label.text != state.DisplayName)
            {
                return false;
            }

            ApplyNewHierarchyPreset(row, label, state.ObjectName, state.Preset, out _);
            return true;
        }

        private static void ApplyNewHierarchyPreset(VisualElement row, Label label, string objectName,
            HierarchyColorPreset preset, out string displayName)
        {
            displayName = objectName[preset.keyChar.Length..];
            if (preset.autoUpperLetters)
            {
                displayName = displayName.ToUpper();
            }

            row.style.backgroundColor = preset.backgroundColor;
            label.text = displayName;
            label.style.color = preset.textColor;
            label.style.unityFontStyleAndWeight = preset.fontStyle;
            label.style.unityTextAlign = preset.textAlignment;
        }

        private static void ClearNewHierarchyRow(VisualElement row, Label label, string objectName)
        {
            newHierarchyRowStyleStates.Remove(row);
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

            var mainIconComponent = DrawNewHierarchyMainComponentIcon(row, gameObject);

            var customSection = FindFirst(row,
                element => element.ClassListContains(NEW_HIERARCHY_LEFT_CUSTOM_SECTION_CLASS));
            if (customSection == null)
            {
                return;
            }

            DrawNewHierarchyVisibleComponentIcons(customSection, gameObject, mainIconComponent);
        }

        private static Component DrawNewHierarchyMainComponentIcon(VisualElement row, GameObject gameObject)
        {
            var defaultIcon = FindFirst(row,
                element => element.ClassListContains(NEW_HIERARCHY_DEFAULT_ICON_CLASS));
            if (defaultIcon == null)
            {
                return null;
            }

            if (!HierarchyComponentIcon.TryGetMainIconOverrideContent(gameObject, out var content,
                    out var iconType, out var component))
            {
                return null;
            }

            int iconSize = iconType == HierarchyColorSettings.ScriptIconType.SmallIcon
                ? 10
                : HierarchyComponentIcon.IconSize;

            var image = CreateNewHierarchyIconImage(content.image, iconSize);
            image.tooltip = content.tooltip;
            image.name = NEW_HIERARCHY_MAIN_ICON_CLASS;
            image.AddToClassList(NEW_HIERARCHY_MAIN_ICON_CLASS);
            image.style.position = Position.Absolute;
            image.style.left = (HierarchyComponentIcon.IconSize - iconSize) * 0.5f;
            image.style.top = (HierarchyComponentIcon.IconSize - iconSize) * 0.5f;
            image.style.opacity = gameObject.activeInHierarchy ? 1f : 0.5f;

            defaultIcon.AddToClassList(NEW_HIERARCHY_MAIN_ICON_HOST_CLASS);
            defaultIcon.style.unityBackgroundImageTintColor = Color.clear;
            defaultIcon.Add(image);

            return component;
        }

        private static void DrawNewHierarchyVisibleComponentIcons(VisualElement customSection, GameObject gameObject,
            Component mainIconComponent)
        {
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
            iconRoot.style.minHeight = iconSize;
            iconRoot.style.flexShrink = 0;

            int iconCount = 0;
            for (int i = 0; i < components.Count && iconCount < HierarchyComponentIcon.MaxIconNum; i++)
            {
                if (components[i] == mainIconComponent)
                {
                    continue;
                }

                Texture2D texture = HierarchyComponentIcon.GetComponentIcon(components[i]);
                if (texture == null)
                {
                    continue;
                }

                var image = CreateNewHierarchyIconImage(texture, iconSize);
                image.style.marginLeft = 1;
                iconRoot.Add(image);
                iconCount++;
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

            var mainIconRoots = new List<VisualElement>(FindAll(row,
                element => element.ClassListContains(NEW_HIERARCHY_MAIN_ICON_CLASS)));
            foreach (var iconRoot in mainIconRoots)
            {
                iconRoot.RemoveFromHierarchy();
            }

            var mainIconHosts = new List<VisualElement>(FindAll(row,
                element => element.ClassListContains(NEW_HIERARCHY_MAIN_ICON_HOST_CLASS)));
            foreach (var iconHost in mainIconHosts)
            {
                iconHost.RemoveFromClassList(NEW_HIERARCHY_MAIN_ICON_HOST_CLASS);
                iconHost.style.unityBackgroundImageTintColor = StyleKeyword.Null;
            }
        }

        private static Image CreateNewHierarchyIconImage(Texture texture, int iconSize)
        {
            var image = new Image
            {
                image = texture,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };

            image.style.width = iconSize;
            image.style.height = iconSize;
            image.style.minWidth = iconSize;
            image.style.minHeight = iconSize;
            image.style.flexShrink = 0;

            return image;
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
