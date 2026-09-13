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
            private List<VisualElement> rows = new();

            internal WindowPresentation(EditorWindow window)
            {
                this.window = window;
                root = window.rootVisualElement;
                root.RegisterCallback<DetachFromPanelEvent>(OnDetached);
                scheduled = root.schedule.Execute(ApplyRows).Every(NewHierarchyConstants.RefreshIntervalMs);
            }

            internal void RefreshRows()
            {
                rows = root.Query<VisualElement>(name: NewHierarchyConstants.RowName).ToList();
                ApplyRows();
            }

            private void ApplyRows()
            {
                foreach (var row in rows)
                    if (row.panel != null) NewHierarchyRowRenderer.Apply(row);
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
            ApplyToWindows();
        }

        public static void RepaintAll()
        {
            ApplyToWindows();
        }

        private static void ApplyToWindows()
        {
            foreach (var window in HierarchyEditorWindowRegistry.ActiveWindows)
            {
                if (window.GetType().FullName != NewHierarchyConstants.WindowTypeName) continue;
                if (window.rootVisualElement.panel == null) continue;
                if (!windows.TryGetValue(window, out var presentation))
                {
                    presentation = new WindowPresentation(window);
                    windows.Add(window, presentation);
                }
                presentation.RefreshRows();
            }
        }

    }
}
#endif
