#if UNITY_EDITOR
using System;
using UnityEngine.UIElements;

namespace VMFramework.HierarchyColor
{
    internal static class VisualElementSearchUtility
    {
        public static VisualElement FindFirst(VisualElement root, Func<VisualElement, bool> predicate)
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

    }
}
#endif
