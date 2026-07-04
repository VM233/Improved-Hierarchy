#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VMFramework.HierarchyColor
{
    public class HierarchyComponentIcon
    {
        private struct HierarchyObjectStatus
        {
            public bool IsSelected;
            public bool IsHovered;
            public bool IsDropDownHovered;
        }

        private const float HIERARCHY_ICON_WIDTH = 18.5f;
        private const float HIERARCHY_EXPAND_ICON_WIDTH = 11f;
        private const float HIERARCHY_EXPAND_ICON_X_OFFSET = HIERARCHY_EXPAND_ICON_WIDTH + 3f;
        private const float SMALL_ICON_SIZE = 10f;
        private const float SMALL_ICON_OFFSET = 7f;

        internal static int MaxIconNum => HierarchyColorSettings.instance.MaxIconNum;

        internal static int IconSize => HierarchyColorSettings.instance.IconSize;

        private static readonly HashSet<Type> hideTypes = new()
        {
            typeof(Transform), typeof(ParticleSystemRenderer), typeof(CanvasRenderer),
        };

        private static Transform offsetObject = null;
        private static int offset = 0;
        private static readonly HashSet<int> additionalSelectedInstanceIDs = new();
        private static bool hierarchyHasFocus;
        private static EditorWindow hierarchyEditorWindow;
        private static bool isMouseDown;

        [InitializeOnLoadMethod]
        public static void Init()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= HierarchyComponentIconGUI;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HierarchyComponentIconGUI;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        public static void HierarchyComponentIconGUI(EntityId instanceID, Rect rect)
        {
            var tempObj = EditorUtility.EntityIdToObject(instanceID);
            if (!tempObj)
            {
                return;
            }

            rect.width += rect.x;
            rect.x = 0;

            var obj = (GameObject)tempObj;
            DrawMainComponentIcon(obj, rect);

            var components = GetVisibleComponents(obj);

            int iconSize = IconSize;
            int y = 1;
            int iconOffset = obj.transform == offsetObject ? offset : 0;

            for (int i = 0; i + iconOffset < components.Count && i < MaxIconNum; i++)
            {
                Component com = components[i + iconOffset];

                Texture2D texture = GetComponentIcon(com);

                if (texture)
                {
                    GUI.DrawTexture(
                        new Rect(rect.width - (iconSize + 1) * (i + 1), rect.y + y, iconSize, iconSize),
                        texture);
                }
            }

            if (components.Count == MaxIconNum + 1)
            {
                Texture2D texture = GetComponentIcon(components[^1]);
                if (texture)
                {
                    GUI.DrawTexture(
                        new Rect(rect.width - (iconSize + 1) * (components.Count - 1 + 1), rect.y + y,
                            iconSize, iconSize), texture);
                }
            }
            else if (components.Count > MaxIconNum)
            {
                GUIStyle style = new(GUI.skin.label)
                {
                    fontSize = 9, alignment = TextAnchor.MiddleCenter
                };

                if (GUI.Button(
                        new Rect(rect.width - (iconSize + 2) * (MaxIconNum + 1), rect.y + y, 22, iconSize),
                        "...", style))
                {
                    if (offsetObject != obj.transform)
                    {
                        offsetObject = obj.transform;
                        offset = 0;
                    }

                    offset += MaxIconNum;
                    if (offset >= components.Count)
                    {
                        offset = 0;
                    }
                }
            }
        }

        internal static bool TryGetMainIconContent(GameObject obj, out GUIContent content,
            out HierarchyColorSettings.ScriptIconType iconType)
        {
            return TryGetMainIconContent(obj, out content, out iconType, false);
        }

        internal static bool TryGetCustomMainIconContent(GameObject obj, out GUIContent content,
            out HierarchyColorSettings.ScriptIconType iconType)
        {
            return TryGetMainIconContent(obj, out content, out iconType, true);
        }

        private static bool TryGetMainIconContent(GameObject obj, out GUIContent content,
            out HierarchyColorSettings.ScriptIconType iconType, bool customIconOnly)
        {
            content = null;
            iconType = HierarchyColorSettings.ScriptIconType.UnityDefault;

            var settings = HierarchyColorSettings.instance;
            if (!settings.ShowMainComponentIcon || obj == null)
            {
                return false;
            }

            if (settings.OverridePrefabIconType && settings.PrefabIconType ==
                HierarchyColorSettings.ScriptIconType.UnityDefault && IsPrefab(obj))
            {
                return false;
            }

            var components = obj.GetComponents<Component>();
            if (components == null || components.Length == 0)
            {
                return false;
            }

            var contentComponent = GetTopComponent(components);
            content = GetContent(contentComponent != null ? contentComponent.GetType() : typeof(Component),
                contentComponent);
            if (content == null || content.image == null)
            {
                return false;
            }

            iconType = GetMainIconType(components, ref content, ref contentComponent);
            bool usesPrefabOverride = settings.OverridePrefabIconType && IsPrefab(obj);
            if (usesPrefabOverride)
            {
                iconType = settings.PrefabIconType;
            }

            if (iconType == HierarchyColorSettings.ScriptIconType.UnityDefault ||
                content == null || content.image == null)
            {
                return false;
            }

            if (customIconOnly && !usesPrefabOverride && !HasCustomMainIcon(contentComponent, content))
            {
                return false;
            }

            return true;
        }

        internal static List<Component> GetVisibleComponents(GameObject obj)
        {
            var components = new List<Component>();
            foreach (var component in obj.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                if (IsTypeIconRequiredToHide(component.GetType()))
                {
                    continue;
                }

                components.Add(component);
            }

            return components;
        }

        private static void DrawMainComponentIcon(GameObject obj, Rect selectionRect)
        {
            if (!TryGetMainIconContent(obj, out var content, out var iconType))
            {
                return;
            }

            var objectStatus = GetHierarchyObjectStatus(obj, selectionRect);
            UpdateSelectedObjectsList(obj, objectStatus);

            var iconRect = selectionRect;
            if (iconType == HierarchyColorSettings.ScriptIconType.BigIcon)
            {
                ClearOriginalIcon(objectStatus, selectionRect);
            }
            else
            {
                iconRect.width = SMALL_ICON_SIZE;
                iconRect.height = SMALL_ICON_SIZE;
                iconRect.position += new Vector2(SMALL_ICON_OFFSET, SMALL_ICON_OFFSET);
            }

            Color originalColor = GUI.color;
            if (!obj.activeInHierarchy)
            {
                GUI.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
            }

            EditorGUI.LabelField(iconRect, content);
            GUI.color = originalColor;
        }

        private static HierarchyColorSettings.ScriptIconType GetMainIconType(IReadOnlyList<Component> components,
            ref GUIContent content, ref Component contentComponent)
        {
            var settings = HierarchyColorSettings.instance;
            int componentsLength = components.Count;

            if (componentsLength > 2 && !settings.ShowAlwaysFirstScriptIcon)
            {
                if (HasDefaultScriptIcon(content))
                {
                    return settings.ContainsNonUnityScripts;
                }

                if (!HasCustomScripts(components, out var component))
                {
                    return settings.ContainsUnityScriptsOnly;
                }

                contentComponent = component;
                content = component != null
                    ? GetContent(component.GetType(), component)
                    : GetContent(typeof(Component));

                return settings.ContainsNonUnityScripts;
            }

            if (componentsLength == 2 || (componentsLength > 2 && settings.ShowAlwaysFirstScriptIcon))
            {
                if (HasDefaultScriptIcon(content))
                {
                    return settings.ContainsSingleUserScript;
                }

                return settings.ContainsUnityScriptsOnly;
            }

            return settings.ContainsNoScripts;
        }

        private static Component GetTopComponent(IReadOnlyList<Component> components)
        {
            var component = components.Count > 1 ? components[1] : components[0];

            if (component == null)
            {
                return component;
            }

            if (!HierarchyColorSettings.instance.ShowAlwaysFirstScriptIcon &&
                component.GetType() == typeof(CanvasRenderer))
            {
                component = components[^1];
            }

            return component;
        }

        private static GUIContent GetContent(Type type, Component component = null)
        {
            var content = EditorGUIUtility.ObjectContent(component, type);
            content.text = null;
            content.tooltip = HierarchyColorSettings.instance.EnableHierarchyIconTooltips ? type.Name : "";
            return content;
        }

        private static bool HasCustomMainIcon(Component component, GUIContent content)
        {
            if (component == null || content?.image == null)
            {
                return false;
            }

            if (IsNamespaceUnityRelated(component))
            {
                return false;
            }

            return !HasDefaultScriptIcon(content);
        }

        private static bool HasCustomScripts(IReadOnlyList<Component> components, out Component customComponent)
        {
            const int maxComponentChecks = 10;
            const int firstComponentIndex = 1;

            customComponent = null;
            if (components.Count > maxComponentChecks)
            {
                return true;
            }

            int componentsToCheckAmount = Mathf.Min(components.Count, maxComponentChecks + firstComponentIndex);
            for (int i = firstComponentIndex; i < componentsToCheckAmount; i++)
            {
                var component = components[i];
                if (IsNamespaceUnityRelated(component))
                {
                    continue;
                }

                customComponent = component;
                return true;
            }

            return false;
        }

        private static bool HasDefaultScriptIcon(GUIContent content)
        {
            string imageName = content.image != null ? content.image.name : null;
            return !string.IsNullOrEmpty(imageName) && imageName.EndsWith("Script Icon");
        }

        private static bool IsNamespaceUnityRelated(Component component)
        {
            if (component == null)
            {
                return false;
            }

            string namespaceStr = component.GetType().Namespace;
            if (string.IsNullOrEmpty(namespaceStr))
            {
                return false;
            }

            bool isNativeUnityRelated;
            switch (HierarchyColorSettings.instance.UnityScriptDetectionType)
            {
                case HierarchyColorSettings.UnityNativeScriptsDetectionType.Unity:
                    isNativeUnityRelated = namespaceStr.Contains("Unity");
                    break;
                case HierarchyColorSettings.UnityNativeScriptsDetectionType.UnityEngine:
                    isNativeUnityRelated = namespaceStr.Equals(nameof(UnityEngine)) ||
                                           namespaceStr.Equals(nameof(UnityEditor)) ||
                                           namespaceStr.StartsWith(nameof(UnityEngine) + ".") ||
                                           namespaceStr.StartsWith(nameof(UnityEditor) + ".");
                    break;
                default:
                    isNativeUnityRelated = false;
                    break;
            }

            return isNativeUnityRelated || namespaceStr.StartsWith("TMPro");
        }

        private static bool IsPrefab(GameObject obj)
        {
            return PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj) != null;
        }

        internal static Texture2D GetComponentIcon(Component component)
        {
            return AssetPreview.GetMiniThumbnail(component);
        }

        private static bool IsTypeIconRequiredToHide(Type type)
        {
            foreach (var hideType in hideTypes)
            {
                if (type == hideType || type.IsSubclassOf(hideType))
                {
                    return true;
                }
            }

            return false;
        }

        private static void OnEditorUpdate()
        {
            if (hierarchyEditorWindow == null && IsHierarchyWindowFocused())
            {
                hierarchyEditorWindow = EditorWindow.focusedWindow;
            }

            hierarchyHasFocus = EditorWindow.focusedWindow != null &&
                                EditorWindow.focusedWindow == hierarchyEditorWindow;
            additionalSelectedInstanceIDs.Clear();
        }

        private static bool IsHierarchyWindowFocused()
        {
            var focusedWindow = EditorWindow.focusedWindow;
            return focusedWindow != null && focusedWindow.GetType().Name.Contains("HierarchyWindow");
        }

        private static HierarchyObjectStatus GetHierarchyObjectStatus(GameObject obj, Rect selectionRect)
        {
            Rect entireRowRect = selectionRect;
            entireRowRect.x = 0;
            entireRowRect.width = short.MaxValue;

            Rect expandChildrenIconRect = selectionRect;
            expandChildrenIconRect.x -= HIERARCHY_EXPAND_ICON_X_OFFSET;
            expandChildrenIconRect.width = HIERARCHY_EXPAND_ICON_WIDTH;

            return new HierarchyObjectStatus
            {
                IsSelected = Array.IndexOf(Selection.instanceIDs, obj.GetInstanceID()) >= 0,
                IsHovered = Event.current != null && entireRowRect.Contains(Event.current.mousePosition),
                IsDropDownHovered = Event.current != null && expandChildrenIconRect.Contains(Event.current.mousePosition)
            };
        }

        private static void UpdateSelectedObjectsList(GameObject obj, HierarchyObjectStatus objectStatus)
        {
            UpdateMouseEventState();
            int instanceID = obj.GetInstanceID();
            if (objectStatus.IsSelected || (objectStatus.IsDropDownHovered && isMouseDown))
            {
                if (Selection.instanceIDs.Length > 1)
                {
                    additionalSelectedInstanceIDs.Clear();
                }

                additionalSelectedInstanceIDs.Add(instanceID);
            }
            else
            {
                additionalSelectedInstanceIDs.Remove(instanceID);
            }
        }

        private static void ClearOriginalIcon(HierarchyObjectStatus objectStatus, Rect selectionRect)
        {
            int selectedAmount = Selection.instanceIDs.Length > 1
                ? Selection.instanceIDs.Length
                : additionalSelectedInstanceIDs.Count;

            Rect backgroundRect = selectionRect;
            backgroundRect.width = HIERARCHY_ICON_WIDTH;
            EditorGUI.DrawRect(backgroundRect, GetHierarchyBackgroundColor(objectStatus, selectedAmount));
        }

        private static Color GetHierarchyBackgroundColor(HierarchyObjectStatus objectStatus, int selectedAmount)
        {
            if (objectStatus.IsSelected)
            {
                if (isMouseDown && !objectStatus.IsDropDownHovered &&
                    !objectStatus.IsHovered && selectedAmount == 1)
                {
                    return GetHierarchyDefaultColor();
                }

                return hierarchyHasFocus ? GetHierarchySelectedColor() : GetHierarchySelectedUnfocusedColor();
            }

            if (objectStatus.IsHovered)
            {
                return isMouseDown && !objectStatus.IsDropDownHovered
                    ? GetHierarchySelectedColor()
                    : GetHierarchyHoveredColor();
            }

            return GetHierarchyDefaultColor();
        }

        private static Color GetHierarchyDefaultColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.2196f, 0.2196f, 0.2196f)
                : new Color(0.7843f, 0.7843f, 0.7843f);
        }

        private static Color GetHierarchySelectedColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.1725f, 0.3647f, 0.5294f)
                : new Color(0.22745f, 0.447f, 0.6902f);
        }

        private static Color GetHierarchySelectedUnfocusedColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.3f, 0.3f)
                : new Color(0.68f, 0.68f, 0.68f);
        }

        private static Color GetHierarchyHoveredColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.2706f, 0.2706f, 0.2706f)
                : new Color(0.698f, 0.698f, 0.698f);
        }

        private static void UpdateMouseEventState()
        {
            if (Event.current == null)
            {
                return;
            }

            if (Event.current.type == EventType.MouseDown)
            {
                isMouseDown = true;
            }
            else if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.DragExited)
            {
                isMouseDown = false;
            }
        }
    }
}
#endif
