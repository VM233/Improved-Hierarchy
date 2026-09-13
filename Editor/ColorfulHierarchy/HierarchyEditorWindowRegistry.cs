#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace VMFramework.HierarchyColor
{
    internal static class HierarchyEditorWindowRegistry
    {
        private static readonly Func<List<EditorWindow>> readActiveWindows = CreateReader();

        internal static IReadOnlyList<EditorWindow> ActiveWindows => readActiveWindows();

        private static Func<List<EditorWindow>> CreateReader()
        {
            var property = typeof(EditorWindow).GetProperty("activeEditorWindows",
                BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new MissingMemberException(typeof(EditorWindow).FullName, "activeEditorWindows");
            var getter = property.GetGetMethod(true)
                ?? throw new MissingMethodException(typeof(EditorWindow).FullName, "get_activeEditorWindows");
            return (Func<List<EditorWindow>>)Delegate.CreateDelegate(typeof(Func<List<EditorWindow>>), getter);
        }
    }
}
#endif
