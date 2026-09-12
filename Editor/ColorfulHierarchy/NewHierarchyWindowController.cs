#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMFramework.HierarchyColor
{
    internal static class NewHierarchyWindowController
    {
        private static readonly HashSet<long> scheduledWindowIDs = new();
        private static readonly Dictionary<long, List<VisualElement>> windowRows = new();
        private static Type hierarchyWindowType;
        private static double nextWindowScanTime;

        public static void UpdateWhenDue()
        {
            if (EditorApplication.timeSinceStartup < nextWindowScanTime)
            {
                return;
            }

            nextWindowScanTime = EditorApplication.timeSinceStartup +
                                 NewHierarchyConstants.WindowScanIntervalSeconds;
            ApplyToWindows();
        }

        public static void RepaintAll()
        {
            windowRows.Clear();
            NewHierarchyRowRenderer.ClearCache();

            foreach (var windowObject in FindHierarchyWindows())
            {
                if (windowObject is EditorWindow window)
                {
                    ApplyToWindow(window);
                }
            }
        }

        private static void ApplyToWindows()
        {
            foreach (var windowObject in FindHierarchyWindows())
            {
                if (windowObject is EditorWindow window)
                {
                    ApplyToWindow(window);
                }
            }
        }

        private static UnityEngine.Object[] FindHierarchyWindows()
        {
            hierarchyWindowType ??= FindHierarchyWindowType();
            return hierarchyWindowType == null
                ? Array.Empty<UnityEngine.Object>()
                : Resources.FindObjectsOfTypeAll(hierarchyWindowType);
        }

        private static Type FindHierarchyWindowType()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<EditorWindow>())
            {
                if (type.FullName == NewHierarchyConstants.WindowTypeName)
                {
                    return type;
                }
            }

            return null;
        }

        private static void ApplyToWindow(EditorWindow window)
        {
            if (window == null || window.rootVisualElement == null)
            {
                return;
            }

            long windowID = HierarchyEditorObjectUtility.GetObjectID(window);
            EnsureWindowScheduled(window, windowID);
            RefreshRowsCache(windowID, window.rootVisualElement);
            ApplyToCachedRows(windowID, window.rootVisualElement);
        }

        private static void EnsureWindowScheduled(EditorWindow window, long windowID)
        {
            if (!scheduledWindowIDs.Add(windowID))
            {
                return;
            }

            window.rootVisualElement.schedule.Execute(() =>
            {
                if (window == null || window.rootVisualElement == null)
                {
                    scheduledWindowIDs.Remove(windowID);
                    windowRows.Remove(windowID);
                    return;
                }

                ApplyToCachedRows(windowID, window.rootVisualElement);
            }).Every(NewHierarchyConstants.RefreshIntervalMs);
        }

        private static void RefreshRowsCache(long windowID, VisualElement root)
        {
            windowRows[windowID] = root.Query<VisualElement>(name: NewHierarchyConstants.RowName).ToList();
        }

        private static void ApplyToCachedRows(long windowID, VisualElement root)
        {
            if (!windowRows.TryGetValue(windowID, out var rows) || rows.Count == 0)
            {
                RefreshRowsCache(windowID, root);
                rows = windowRows[windowID];
            }

            for (int i = rows.Count - 1; i >= 0; i--)
            {
                var row = rows[i];
                if (row == null || row.panel == null)
                {
                    if (row != null)
                    {
                        NewHierarchyRowRenderer.RemoveFromCache(row);
                    }

                    rows.RemoveAt(i);
                    continue;
                }

                NewHierarchyRowRenderer.Apply(row);
            }
        }
    }
}
#endif
