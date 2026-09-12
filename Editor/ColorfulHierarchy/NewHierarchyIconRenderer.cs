#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace VMFramework.HierarchyColor
{
    internal static class NewHierarchyIconRenderer
    {
        public static void Draw(VisualElement row, GameObject gameObject)
        {
            Clear(row);

            if (gameObject == null)
            {
                return;
            }

            var mainIconComponent = DrawMainComponentIcon(row, gameObject);
            if (HierarchyComponentIcon.MaxIconNum <= 0)
            {
                return;
            }

            var customSection = VisualElementSearchUtility.FindFirst(row,
                element => element.ClassListContains(NewHierarchyConstants.LeftCustomSectionClass));
            if (customSection == null)
            {
                return;
            }

            DrawVisibleComponentIcons(customSection, gameObject, mainIconComponent);
        }

        public static void Clear(VisualElement row)
        {
            var iconRoots = row.Query<VisualElement>(className: NewHierarchyConstants.IconRootClass).ToList();
            foreach (var iconRoot in iconRoots)
            {
                iconRoot.RemoveFromHierarchy();
            }

            var mainIconRoots = row.Query<VisualElement>(className: NewHierarchyConstants.MainIconClass).ToList();
            foreach (var iconRoot in mainIconRoots)
            {
                iconRoot.RemoveFromHierarchy();
            }

            var mainIconHosts = row.Query<VisualElement>(className: NewHierarchyConstants.MainIconHostClass).ToList();
            foreach (var iconHost in mainIconHosts)
            {
                iconHost.RemoveFromClassList(NewHierarchyConstants.MainIconHostClass);
                iconHost.style.unityBackgroundImageTintColor = StyleKeyword.Null;
            }
        }

        private static Component DrawMainComponentIcon(VisualElement row, GameObject gameObject)
        {
            var defaultIcon = VisualElementSearchUtility.FindFirst(row,
                element => element.ClassListContains(NewHierarchyConstants.DefaultIconClass));
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

            var image = CreateIconImage(content.image, iconSize);
            image.tooltip = content.tooltip;
            image.name = NewHierarchyConstants.MainIconClass;
            image.AddToClassList(NewHierarchyConstants.MainIconClass);
            image.style.position = Position.Absolute;
            image.style.left = (HierarchyComponentIcon.IconSize - iconSize) * 0.5f;
            image.style.top = (HierarchyComponentIcon.IconSize - iconSize) * 0.5f;
            image.style.opacity = gameObject.activeInHierarchy ? 1f : 0.5f;

            defaultIcon.AddToClassList(NewHierarchyConstants.MainIconHostClass);
            defaultIcon.style.unityBackgroundImageTintColor = Color.clear;
            defaultIcon.Add(image);

            return component;
        }

        private static void DrawVisibleComponentIcons(VisualElement customSection, GameObject gameObject,
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
                name = NewHierarchyConstants.IconRootClass,
                pickingMode = PickingMode.Ignore
            };
            iconRoot.AddToClassList(NewHierarchyConstants.IconRootClass);
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

                var image = CreateIconImage(texture, iconSize);
                image.style.marginLeft = 1;
                iconRoot.Add(image);
                iconCount++;
            }

            if (iconRoot.childCount > 0)
            {
                customSection.Add(iconRoot);
            }
        }

        private static Image CreateIconImage(Texture texture, int iconSize)
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
    }
}
#endif
