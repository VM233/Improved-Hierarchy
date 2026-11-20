#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace VMFramework.HierarchyColor
{
    internal static class NewHierarchyWindowController
    {
        private sealed class WindowPresentation
        {
            private readonly EditorWindow window;
            private readonly VisualElement root;
            private readonly IVisualElementScheduledItem scheduled;
            private readonly List<VisualElement> rows = new();
            private readonly UQueryState<VisualElement> rowQuery;

            internal WindowPresentation(EditorWindow window)
            {
                this.window = window;
                root = window.rootVisualElement;
                rowQuery = root.Query<VisualElement>(name: NewHierarchyConstants.RowName).Build();
                root.RegisterCallback<DetachFromPanelEvent>(OnDetached);
                scheduled = root.schedule.Execute(ApplyScheduledRows).Every(NewHierarchyConstants.RefreshIntervalMs);
            }

            internal void RefreshRows(bool hierarchyChanged)
            {
                rows.Clear();
                rowQuery.ToList(rows);
                ApplyRows(hierarchyChanged);
            }

            private void ApplyScheduledRows() => ApplyRows(false);
            private void ApplyRows(bool hierarchyChanged)
            {
                foreach (var row in rows)
                    if (row.panel != null) NewHierarchyRowRenderer.Apply(row,hierarchyChanged);
            }

            private void OnDetached(DetachFromPanelEvent evt)
            {
                if (evt.target != root) return;
                scheduled.Pause();
                root.UnregisterCallback<DetachFromPanelEvent>(OnDetached);
                foreach (var row in rows) NewHierarchyRowRenderer.RemoveFromCache(row);
                rows.Clear();
                windows.Remove(window);
            }
        }

        private static readonly Dictionary<EditorWindow, WindowPresentation> windows = new();
        private static double nextWindowScanTime;

        public static void UpdateWhenDue()
        {
            if (EditorApplication.timeSinceStartup < nextWindowScanTime) return;
            nextWindowScanTime = EditorApplication.timeSinceStartup + NewHierarchyConstants.WindowScanIntervalSeconds;
            ApplyToWindows(false);
        }

        public static void RepaintAll()
        {
            ApplyToWindows(true);
        }

        private static void ApplyToWindows(bool hierarchyChanged)
        {
            var active = HierarchyEditorWindowRegistry.ActiveWindows;
            for (int i = 0; i < active.Count; i++)
            {
                var window = active[i];
                if (window.GetType().FullName != NewHierarchyConstants.WindowTypeName) continue;
                if (window.rootVisualElement.panel == null) continue;
                if (!windows.TryGetValue(window, out var presentation))
                {
                    presentation = new WindowPresentation(window);
                    windows.Add(window, presentation);
                }
                presentation.RefreshRows(hierarchyChanged);
            }
        }

    }
}
#endif
