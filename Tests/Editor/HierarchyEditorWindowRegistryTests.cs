using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VMFramework.HierarchyColor.Tests
{
    public sealed class HierarchyEditorWindowRegistryTests
    {
        [Test]
        public void ReadsUnityOwnedWindowLifetimeWithoutCopyingMembership()
        {
            var registry = HierarchyEditorWindowRegistry.ActiveWindows;
            int before = registry.Count;
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                Assert.That(HierarchyEditorWindowRegistry.ActiveWindows, Is.SameAs(registry));
                Assert.That(registry.Contains(window), Is.True);
                Assert.That(registry.Count, Is.EqualTo(before + 1));
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
            Assert.That(registry.Contains(window), Is.False);
            Assert.That(registry.Count, Is.EqualTo(before));
        }
    }
}
