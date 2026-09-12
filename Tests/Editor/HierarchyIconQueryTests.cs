using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMFramework.HierarchyColor.Tests
{
    public sealed class HierarchyIconQueryTests
    {
        [Test]
        public void ClearRemovesOwnedIconsAndRestoresHosts()
        {
            var row = new VisualElement();
            var componentIcons = new VisualElement();
            componentIcons.AddToClassList(NewHierarchyConstants.IconRootClass);
            componentIcons.Add(new Image());
            row.Add(componentIcons);
            var host = new VisualElement();
            host.AddToClassList(NewHierarchyConstants.MainIconHostClass);
            host.style.unityBackgroundImageTintColor = Color.clear;
            var icon = new Image();
            icon.AddToClassList(NewHierarchyConstants.MainIconClass);
            host.Add(icon);
            row.Add(host);
            var unrelated = new Label("Unrelated content");
            unrelated.AddToClassList(NewHierarchyConstants.MainIconClass + "-other");
            row.Add(unrelated);

            NewHierarchyIconRenderer.Clear(row);

            Assert.That(componentIcons.parent, Is.Null);
            Assert.That(icon.parent, Is.Null);
            Assert.That(host.parent, Is.SameAs(row));
            Assert.That(host.ClassListContains(NewHierarchyConstants.MainIconHostClass), Is.False);
            Assert.That(host.style.unityBackgroundImageTintColor.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(unrelated.parent, Is.SameAs(row));
            Assert.That(unrelated.text, Is.EqualTo("Unrelated content"));
            NewHierarchyIconRenderer.Clear(row);
            Assert.That(row.childCount, Is.EqualTo(2));
        }

        [Test]
        public void ClearIncludesRootAndNestedMatches()
        {
            var parent = new VisualElement();
            var row = new VisualElement();
            row.AddToClassList(NewHierarchyConstants.IconRootClass);
            parent.Add(row);
            var child = new VisualElement();
            child.AddToClassList(NewHierarchyConstants.IconRootClass);
            row.Add(child);

            NewHierarchyIconRenderer.Clear(row);

            Assert.That(row.parent, Is.Null);
            Assert.That(child.parent, Is.Null);
            Assert.That(parent.childCount, Is.Zero);
        }

        [Test]
        public void EmptyMatchCleanupDoesNotAllocatePerDescendant()
        {
            var small = CreateTree(32);
            var large = CreateTree(1551);
            NewHierarchyIconRenderer.Clear(small);
            NewHierarchyIconRenderer.Clear(large);
            long smallBytes = MeasureCleanup(small);
            long largeBytes = MeasureCleanup(large);
            Assert.That(largeBytes, Is.LessThanOrEqualTo(smallBytes + 4096),
                $"Ten cleanups allocated {smallBytes} bytes for 32 nodes and {largeBytes} bytes for 1551 nodes.");
        }

        private static VisualElement CreateTree(int count)
        {
            var root = new VisualElement();
            var branch = root;
            for (int index = 1; index < count; index++)
            {
                var child = new VisualElement();
                if (index % 50 == 1)
                {
                    root.Add(child);
                    branch = child;
                }
                else branch.Add(child);
            }
            return root;
        }

        private static long MeasureCleanup(VisualElement root)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10; index++) NewHierarchyIconRenderer.Clear(root);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
    }
}
