#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VMFramework.HierarchyColor
{
    public static class HierarchyColorSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Improved Hierarchy", SettingsScope.Project)
            {
                label = "Improved Hierarchy",
                keywords = new HashSet<string>
                {
                    "Hierarchy",
                    "Improved",
                    "Color",
                    "Icon",
                    "Component"
                },
                guiHandler = _ =>
                {
                    var settings = HierarchyColorSettings.instance;
                    settings.EnsureInitialized();

                    var serializedSettings = new SerializedObject(settings);
                    serializedSettings.Update();

                    EditorGUILayout.LabelField("Color Presets", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("colorPresets"), true);
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Component Icons", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("maxIconNum"),
                        new GUIContent("Max Icon Num"));
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("iconSize"),
                        new GUIContent("Icon Size"));
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Main Component Icon", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("showMainComponentIcon"),
                        new GUIContent("Enabled"));
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("showAlwaysFirstScriptIcon"),
                        new GUIContent("Always Show First Script Icon"));
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("unityScriptDetectionType"),
                        new GUIContent("Unity Native Script Keyword"));
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("containsUnityScriptsOnly"),
                        new GUIContent("Contains Unity Scripts Only"));
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("containsNonUnityScripts"),
                        new GUIContent("Contains Non-Unity Scripts"));
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("containsSingleUserScript"),
                        new GUIContent("Contains Single User Script Only"));
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("containsNoScripts"),
                        new GUIContent("Contains No Scripts"));
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("overridePrefabIconType"),
                        new GUIContent("Override Prefab Icons"));
                    using (new EditorGUI.DisabledScope(!serializedSettings
                               .FindProperty("overridePrefabIconType").boolValue))
                    {
                        EditorGUILayout.PropertyField(serializedSettings.FindProperty("prefabIconType"),
                            new GUIContent("Is A Prefab"));
                    }

                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableHierarchyIconTooltips"),
                        new GUIContent("Enable Hierarchy Icon Tooltips"));

                    if (serializedSettings.ApplyModifiedProperties())
                    {
                        settings.SaveSettings();
                        EditorApplication.RepaintHierarchyWindow();
                    }
                }
            };
        }
    }
}
#endif
