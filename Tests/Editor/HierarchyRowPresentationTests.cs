using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace VMFramework.HierarchyColor.Tests
{
    public sealed class HierarchyRowPresentationTests
    {
        private GameObject gameObject;
        private VisualElement row;
        private VisualElement iconHost;
        private Label label;
        private string savedSettings;

        [SetUp]
        public void SetUp()
        {
            var settings = HierarchyColorSettings.instance;
            savedSettings = EditorJsonUtility.ToJson(settings);
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("enableHighlight").boolValue = true;
            serialized.FindProperty("maxIconNum").intValue = 5;
            serialized.FindProperty("showMainComponentIcon").boolValue = true;
            serialized.FindProperty("containsUnityScriptsOnly").enumValueIndex = 1;
            serialized.FindProperty("containsNoScripts").enumValueIndex = 1;
            var presets = serialized.FindProperty("colorPresets");
            presets.arraySize = 1;
            presets.GetArrayElementAtIndex(0).FindPropertyRelative("keyChar").stringValue = "@";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            settings.PublishPresentationChange();
            gameObject = new GameObject("Camera Object", typeof(Camera), typeof(BoxCollider));
            row = new VisualElement();
            iconHost = new VisualElement();
            iconHost.AddToClassList(NewHierarchyConstants.DefaultIconClass);
            row.Add(iconHost);
            var icons = new VisualElement();
            icons.AddToClassList(NewHierarchyConstants.LeftCustomSectionClass);
            row.Add(icons);
            label = new Label(gameObject.name);
            label.AddToClassList(NewHierarchyConstants.NameClass);
            row.Add(label);
        }

        [TearDown]
        public void TearDown()
        {
            NewHierarchyRowRenderer.RemoveFromCache(row);
            Object.DestroyImmediate(gameObject);
            var settings = HierarchyColorSettings.instance;
            EditorJsonUtility.FromJsonOverwrite(savedSettings, settings);
            settings.PublishPresentationChange();
        }

        private VisualElement Draw()
        {
            NewHierarchyRowRenderer.Apply(row, gameObject, label);
            var icons = row.Q(className: NewHierarchyConstants.IconRootClass);
            Assert.That(icons, Is.Not.Null, "The fixture must render actual component icons.");
            return icons;
        }

        [Test]
        public void RebindingUnchangedRowPreservesGeneratedIcons()
        {
            var icons = Draw();
            for (int index = 0; index < 10; index++)
            {
                label.text = gameObject.name;
                NewHierarchyRowRenderer.Apply(row, gameObject, label);
                Assert.That(row.Q(className: NewHierarchyConstants.IconRootClass), Is.SameAs(icons));
            }
        }

        [Test]
        public void ComponentAndActiveChangesRebuildOnlyTheChangedPresentation()
        {
            var before = Draw();
            var body = gameObject.AddComponent<Rigidbody>();
            var added = Draw();
            Assert.That(added, Is.Not.SameAs(before));
            Assert.That(before.parent, Is.Null);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            VisualElement reordered;
            try
            {
                Assert.That(UnityEditorInternal.ComponentUtility.MoveComponentUp(body), Is.True);
                reordered = Draw();
                Assert.That(reordered, Is.Not.SameAs(added));
            }
            finally
            {
                // Unity Test Runner reverts Undo after TearDown. Consume this record while its object lives.
                Undo.RevertAllDownToGroup(undoGroup);
            }
            Object.DestroyImmediate(body);
            var removed = Draw();
            Assert.That(removed, Is.Not.SameAs(reordered));
            gameObject.SetActive(false);
            var disabled = Draw();
            Assert.That(disabled, Is.Not.SameAs(removed));
            Assert.That(iconHost.Q<Image>().style.opacity.value, Is.EqualTo(0.5f));
        }

        [Test]
        public void EverySettingsPublicationInvalidatesTheRow()
        {
            var first = Draw();
            var settings = HierarchyColorSettings.instance;
            var serialized = new SerializedObject(settings);
            var option = serialized.FindProperty("showAlwaysFirstScriptIcon");
            option.boolValue = !option.boolValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            settings.PublishPresentationChange();
            Assert.That(Draw(), Is.Not.SameAs(first));
        }

        [Test]
        public void PrefixWhitespaceAndNativeLabelRebindKeepTheDisplayName()
        {
            var preset = HierarchyColorSettings.instance.ColorPresets[0];
            gameObject.name = "  " + preset.keyChar + "Row Name";
            var icons = Draw();
            string expected = preset.autoUpperLetters ? "Row Name".ToUpper() : "Row Name";
            Assert.That(label.text, Is.EqualTo(expected));
            label.text = gameObject.name;
            NewHierarchyRowRenderer.Apply(row, gameObject, label);
            Assert.That(label.text, Is.EqualTo(expected));
            Assert.That(row.Q(className: NewHierarchyConstants.IconRootClass), Is.SameAs(icons));
        }

        [Test]
        public void RecycledRowAdoptsNewObjectAndDoesNotOverwriteSceneLabel()
        {
            var first = Draw();
            var second = new GameObject("Other Object", typeof(Light), typeof(BoxCollider));
            try
            {
                NewHierarchyRowRenderer.Apply(row, second, label);
                Assert.That(label.text, Is.EqualTo(second.name));
                Assert.That(first.parent, Is.Null);
                label.text = "Scene Name";
                NewHierarchyRowRenderer.Apply(row, null, label);
                Assert.That(label.text, Is.EqualTo("Scene Name"));
                Assert.That(row.Q(className: NewHierarchyConstants.IconRootClass), Is.Null);
                Assert.That(iconHost.ClassListContains(NewHierarchyConstants.MainIconHostClass), Is.False);
                Assert.That(row.style.backgroundColor.keyword, Is.EqualTo(StyleKeyword.Null));
                Draw();
            }
            finally { Object.DestroyImmediate(second); }
        }

        [Test]
        public void UnchangedPassDoesNotAllocateIconsOrComponentArrays()
        {
            var icons = Draw();
            for (int index = 0; index < 10; index++) NewHierarchyRowRenderer.Apply(row, gameObject, label);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 100; index++) NewHierarchyRowRenderer.Apply(row, gameObject, label);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(bytes, Is.LessThanOrEqualTo(8192),
                $"One hundred unchanged passes allocated {bytes} bytes, including native object names.");
            Assert.That(row.Q(className: NewHierarchyConstants.IconRootClass), Is.SameAs(icons));
        }

        [UnityTest]
        public IEnumerator DetachReleasesIconsAndReadoptionBuildsAFreshPresentation()
        {
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                window.Show();
                yield return null;
                window.rootVisualElement.Add(row);
                Assert.That(row.panel, Is.Not.Null);
                var icons = Draw();
                row.RemoveFromHierarchy();
                Assert.That(icons.parent, Is.Null);
                Assert.That(iconHost.ClassListContains(NewHierarchyConstants.MainIconHostClass), Is.False);
                window.rootVisualElement.Add(row);
                Assert.That(Draw(), Is.Not.SameAs(icons));
            }
            finally { window.Close(); }
        }
    }
}
