#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_6000_6_OR_NEWER
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
#else
using System;
using System.Reflection;
#endif

namespace VMFramework.HierarchyColor
{
    internal static class NewHierarchyReflection
    {
#if UNITY_6000_6_OR_NEWER
        public static GameObject GetGameObject(VisualElement row)
        {
            var item = VisualElementSearchUtility.FindFirst(row,
                element => element is HierarchyViewItem) as HierarchyViewItem;
            return item?.View != null && item.Handler is HierarchyGameObjectHandler handler
                ? handler.GetGameObject(in item.Node) : null;
        }
#else
        private static FieldInfo viewItemField;
        private static FieldInfo nodeField;
        private static FieldInfo handlerField;
        private static MethodInfo getGameObjectMethod;
        private static readonly object[] arguments = new object[1];

        public static GameObject GetGameObject(VisualElement row)
        {
            var container = VisualElementSearchUtility.FindFirst(row,
                element => element.GetType().FullName == NewHierarchyConstants.ItemContainerTypeName);
            if (container == null) return null;
            viewItemField ??= RequiredField(container.GetType(), "m_ViewItem");
            var item = viewItemField.GetValue(container);
            if (item == null) return null;
            nodeField ??= RequiredField(item.GetType(), "m_Node");
            handlerField ??= RequiredField(item.GetType(), "m_Handler");
            var handler = handlerField.GetValue(item);
            // Scene and other node handlers deliberately have no GameObject projection.
            if (handler == null || handler.GetType().FullName != "Unity.Hierarchy.Editor.HierarchyGameObjectHandler")
                return null;
            getGameObjectMethod ??= handler.GetType().GetMethod("GetGameObject",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(handler.GetType().FullName, "GetGameObject");
            arguments[0] = nodeField.GetValue(item);
            return (GameObject)getGameObjectMethod.Invoke(handler, arguments);
        }

        private static FieldInfo RequiredField(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                   ?? throw new MissingFieldException(type.FullName, name);
        }
#endif
    }
}
#endif
