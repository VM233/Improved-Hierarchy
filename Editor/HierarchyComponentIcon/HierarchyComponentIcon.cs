#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VMFramework.HierarchyColor
{
    public class HierarchyComponentIcon
    {
        internal static int MaxIconNum => HierarchyColorSettings.instance.MaxIconNum;

        internal static int IconSize => HierarchyColorSettings.instance.IconSize;

        private static readonly HashSet<Type> hideTypes = new()
        {
            typeof(Transform), typeof(ParticleSystemRenderer), typeof(CanvasRenderer),
        };

        private static Transform offsetObject = null;
        private static int offset = 0;

        [InitializeOnLoadMethod]
        public static void Init()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HierarchyComponentIconGUI;
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
    }
}
#endif
