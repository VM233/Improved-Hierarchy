#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VMFramework.HierarchyColor
{
    [FilePath("ImprovedHierarchySettings.asset",
        FilePathAttribute.Location.PreferencesFolder)]
    public sealed class HierarchyColorSettings : ScriptableSingleton<HierarchyColorSettings>
    {
        public enum ScriptIconType
        {
            SmallIcon,
            BigIcon,
            UnityDefault
        }

        public enum UnityNativeScriptsDetectionType
        {
            UnityEngine,
            Unity,
            None
        }

        private const int MIN_ICON_NUM = 0;
        private const int MAX_ICON_NUM = 10;
        private const int MIN_ICON_SIZE = 1;
        private const int MAX_ICON_SIZE = 24;

        [SerializeField]
        private List<HierarchyColorPreset> colorPresets = new();

        [SerializeField]
        private bool enableHighlight = true;

        [SerializeField]
        private int maxIconNum = 5;

        [SerializeField]
        private int iconSize = 16;

        [SerializeField]
        private bool showMainComponentIcon = true;

        [SerializeField]
        private bool showAlwaysFirstScriptIcon = false;

        [SerializeField]
        private UnityNativeScriptsDetectionType unityScriptDetectionType = UnityNativeScriptsDetectionType.Unity;

        [SerializeField]
        private ScriptIconType containsUnityScriptsOnly = ScriptIconType.BigIcon;

        [SerializeField]
        private ScriptIconType containsNonUnityScripts = ScriptIconType.SmallIcon;

        [SerializeField]
        private ScriptIconType containsSingleUserScript = ScriptIconType.SmallIcon;

        [SerializeField]
        private ScriptIconType containsNoScripts = ScriptIconType.BigIcon;

        [SerializeField]
        private bool overridePrefabIconType = false;

        [SerializeField]
        private ScriptIconType prefabIconType = ScriptIconType.SmallIcon;

        [SerializeField]
        private bool enableHierarchyIconTooltips = true;

        internal ulong PresentationRevision { get; private set; }

        public IReadOnlyList<HierarchyColorPreset> ColorPresets
        {
            get
            {
                EnsureInitialized();
                return colorPresets;
            }
        }

        public bool EnableHighlight
        {
            get
            {
                EnsureInitialized();
                return enableHighlight;
            }
        }

        public int MaxIconNum
        {
            get
            {
                EnsureInitialized();
                return maxIconNum;
            }
        }

        public int IconSize
        {
            get
            {
                EnsureInitialized();
                return iconSize;
            }
        }

        public bool ShowMainComponentIcon
        {
            get
            {
                EnsureInitialized();
                return showMainComponentIcon;
            }
        }

        public bool ShowAlwaysFirstScriptIcon
        {
            get
            {
                EnsureInitialized();
                return showAlwaysFirstScriptIcon;
            }
        }

        public UnityNativeScriptsDetectionType UnityScriptDetectionType
        {
            get
            {
                EnsureInitialized();
                return unityScriptDetectionType;
            }
        }

        public ScriptIconType ContainsUnityScriptsOnly
        {
            get
            {
                EnsureInitialized();
                return containsUnityScriptsOnly;
            }
        }

        public ScriptIconType ContainsNonUnityScripts
        {
            get
            {
                EnsureInitialized();
                return containsNonUnityScripts;
            }
        }

        public ScriptIconType ContainsSingleUserScript
        {
            get
            {
                EnsureInitialized();
                return containsSingleUserScript;
            }
        }

        public ScriptIconType ContainsNoScripts
        {
            get
            {
                EnsureInitialized();
                return containsNoScripts;
            }
        }

        public bool OverridePrefabIconType
        {
            get
            {
                EnsureInitialized();
                return overridePrefabIconType;
            }
        }

        public ScriptIconType PrefabIconType
        {
            get
            {
                EnsureInitialized();
                return prefabIconType;
            }
        }

        public bool EnableHierarchyIconTooltips
        {
            get
            {
                EnsureInitialized();
                return enableHierarchyIconTooltips;
            }
        }

        private void OnEnable()
        {
            EnsureInitialized();
            PublishPresentationChange();
        }

        private void OnValidate()
        {
            NormalizeValues();
            PublishPresentationChange();
        }

        internal void EnsureInitialized()
        {
            colorPresets ??= new();

            if (colorPresets.Count == 0)
            {
                AddDefaultColorPresets();
            }

            NormalizeValues();
        }

        internal void SaveSettings()
        {
            EnsureInitialized();
            PublishPresentationChange();
            EditorUtility.SetDirty(this);
            Save(true);
        }

        internal void PublishPresentationChange()
        {
            PresentationRevision++;
        }

        private void AddDefaultColorPresets()
        {
            colorPresets.Add(new()
            {
                keyChar = "$",
                textColor = Color.white,
                backgroundColor = new(1f, 0.6470588f, 0f, 1f)
            });

            colorPresets.Add(new()
            {
                keyChar = "^",
                textColor = Color.white,
                backgroundColor = Color.green
            });

            colorPresets.Add(new()
            {
                keyChar = "#",
                textColor = new(0f, 0.7490196f, 1f, 1f),
                backgroundColor = Color.yellow
            });

            colorPresets.Add(new()
            {
                keyChar = "@",
                textColor = new(1f, 0.4117647f, 0.7058824f, 1f),
                backgroundColor = new(0.4980392f, 1f, 0.8313726f, 1f)
            });

            colorPresets.Add(new()
            {
                keyChar = "/",
                textColor = Color.white,
                backgroundColor = Color.magenta
            });

            colorPresets.Add(new()
            {
                keyChar = "%",
                textColor = Color.white,
                backgroundColor = new(0.5f, 0f, 0.5f, 1f)
            });

            colorPresets.Add(new()
            {
                keyChar = "!",
                textColor = Color.white,
                backgroundColor = Color.red
            });

            colorPresets.Add(new()
            {
                keyChar = "&",
                textColor = Color.white,
                backgroundColor = Color.black
            });

            colorPresets.Add(new()
            {
                keyChar = "*",
                textColor = Color.white,
                backgroundColor = new(0f, 1f, 0.617f, 1f)
            });
        }

        private void NormalizeValues()
        {
            maxIconNum = Mathf.Clamp(maxIconNum, MIN_ICON_NUM, MAX_ICON_NUM);
            iconSize = Mathf.Clamp(iconSize, MIN_ICON_SIZE, MAX_ICON_SIZE);
        }
    }
}
#endif
