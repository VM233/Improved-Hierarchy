#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMFramework.HierarchyColor
{
    internal static class NewHierarchyRowRenderer
    {
        private sealed class RowStyleState
        {
            public GameObject Object;
            public string ObjectName;
            public string DisplayName;
            public ulong SettingsRevision;
            public bool ActiveInHierarchy;
            public Label Label;
            public readonly List<Component> Components = new();
        }

        private static readonly Dictionary<VisualElement, RowStyleState> rowStyleStates = new();
        private static readonly List<Component> componentScratch = new();

        public static void RemoveFromCache(VisualElement row)
        {
            if (!rowStyleStates.Remove(row, out var state)) return;
            row.UnregisterCallback<DetachFromPanelEvent>(OnRowDetached);
            ClearStyle(row, state.Label, state.Object == null ? null : state.Object.name);
            NewHierarchyIconRenderer.Clear(row);
        }

        private static void OnRowDetached(DetachFromPanelEvent evt)
        {
            if (evt.target == evt.currentTarget) RemoveFromCache((VisualElement)evt.currentTarget);
        }

        public static void Apply(VisualElement row)
        {
            Apply(row, NewHierarchyReflection.GetGameObject(row), FindNameLabel(row));
        }

        internal static void Apply(VisualElement row, GameObject gameObject, Label label)
        {
            if (gameObject == null || label == null)
            {
                // A native scene/unbound row keeps its own label, never the previous object's name.
                string nativeName = label?.text;
                RemoveFromCache(row);
                if (label != null) label.text = nativeName;
                return;
            }

            string objectName = gameObject.name;
            var settings = HierarchyColorSettings.instance;
            gameObject.GetComponents(componentScratch);
            if (rowStyleStates.TryGetValue(row, out var state) &&
                state.Object == gameObject && state.Label == label && state.ObjectName == objectName &&
                state.SettingsRevision == settings.PresentationRevision &&
                state.ActiveInHierarchy == gameObject.activeInHierarchy &&
                ComponentsMatch(state.Components, componentScratch))
            {
                if (label.text != state.DisplayName) label.text = state.DisplayName;
                componentScratch.Clear();
                return;
            }

            if (state == null)
            {
                state = new RowStyleState();
                rowStyleStates.Add(row, state);
                row.RegisterCallback<DetachFromPanelEvent>(OnRowDetached);
            }

            string displayName = objectName;
            if (settings.EnableHighlight && HierarchyNameUtility.TryGetPreset(objectName, out var preset))
                ApplyPreset(row, label, objectName, preset, out displayName);
            else
                ClearStyle(row, label, objectName);

            NewHierarchyIconRenderer.Draw(row, gameObject);
            state.Object = gameObject;
            state.ObjectName = objectName;
            state.DisplayName = displayName;
            state.SettingsRevision = settings.PresentationRevision;
            state.ActiveInHierarchy = gameObject.activeInHierarchy;
            state.Label = label;
            state.Components.Clear();
            state.Components.AddRange(componentScratch);
            componentScratch.Clear();
        }

        private static bool ComponentsMatch(List<Component> previous, List<Component> current)
        {
            if (previous.Count != current.Count) return false;
            for (int index = 0; index < current.Count; index++)
                if (!ReferenceEquals(previous[index], current[index])) return false;
            return true;
        }

        private static void ApplyPreset(VisualElement row, Label label, string objectName,
            HierarchyColorPreset preset, out string displayName)
        {
            int start = HierarchyNameUtility.GetStartIndexIgnoringWhitespace(objectName) + preset.keyChar.Length;
            displayName = objectName[start..];
            if (preset.autoUpperLetters) displayName = displayName.ToUpper();
            row.style.backgroundColor = preset.backgroundColor;
            label.text = displayName;
            label.style.color = preset.textColor;
            label.style.unityFontStyleAndWeight = preset.fontStyle;
            label.style.unityTextAlign = preset.textAlignment;
        }

        private static void ClearStyle(VisualElement row, Label label, string objectName)
        {
            row.style.backgroundColor = StyleKeyword.Null;
            if (label == null) return;
            if (objectName != null) label.text = objectName;
            label.style.color = StyleKeyword.Null;
            label.style.unityFontStyleAndWeight = StyleKeyword.Null;
            label.style.unityTextAlign = StyleKeyword.Null;
        }

        private static Label FindNameLabel(VisualElement row)
        {
            var nameElement = VisualElementSearchUtility.FindFirst(row,
                element => element.ClassListContains(NewHierarchyConstants.NameClass));
            return VisualElementSearchUtility.FindFirst(nameElement, element => element is Label) as Label;
        }
    }
}
#endif
