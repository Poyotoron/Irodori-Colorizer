using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>対象キーとラベル ID の対応。</summary>
    [Serializable]
    internal sealed class IrodoriAssignment
    {
        public string key;
        public string labelId;
    }

    /// <summary>内蔵ラベル定義に対する利用者の上書き。</summary>
    [Serializable]
    internal sealed class IrodoriLabelOverride
    {
        public string labelId;
        public bool overrideName;
        public string name;
        public bool overrideColor;
        public string colorHex;
        public bool hidden;
    }

    /// <summary>利用者が追加したラベル。</summary>
    [Serializable]
    internal sealed class IrodoriCustomLabel
    {
        public string id;
        public string name;
        public string colorHex;
    }

    // NOTE: ProjectSettings に保存すると、利用者の Assets やビルド成果物を汚さずにチーム共有できる。
    [FilePath("ProjectSettings/IrodoriColorizer.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class IrodoriSettings : ScriptableSingleton<IrodoriSettings>
    {
        public int schemaVersion = 2;

        public bool enabled = true;
        public bool paintProject = true;
        public bool paintHierarchy = true;
        public float fillAlpha = 0.32f;
        public bool paintFullRow = true;
        public bool autoTextColor = true;
        public Color forcedTextColor = Color.white;
        public bool keepSelectionVisible = true;
        public float labelIndent = 18f;

        public List<string> enabledPresets = new List<string> { "vrchat", "basic" };
        public List<IrodoriLabelOverride> overrides = new List<IrodoriLabelOverride>();
        public List<IrodoriCustomLabel> customLabels = new List<IrodoriCustomLabel>();

        public List<IrodoriAssignment> projectAssignments = new List<IrodoriAssignment>();
        public List<IrodoriAssignment> sceneAssignments = new List<IrodoriAssignment>();

        // NOTE: ScriptableSingleton は最初のアクセス時に読み込まれるため、起動時に移行を確定させる。
        [InitializeOnLoadMethod]
        private static void EnsureMigrated()
        {
            IrodoriSettings settings = instance;
            if (settings.schemaVersion > 2)
            {
                return;
            }

            bool changed = false;
            if (settings.enabledPresets == null)
            {
                settings.enabledPresets = IrodoriPresets.GetDefaultEnabledPresetIds();
                changed = true;
            }

            if (settings.overrides == null)
            {
                settings.overrides = new List<IrodoriLabelOverride>();
                changed = true;
            }

            if (settings.customLabels == null)
            {
                settings.customLabels = new List<IrodoriCustomLabel>();
                changed = true;
            }

            if (settings.projectAssignments == null)
            {
                settings.projectAssignments = new List<IrodoriAssignment>();
                changed = true;
            }

            if (settings.sceneAssignments == null)
            {
                settings.sceneAssignments = new List<IrodoriAssignment>();
                changed = true;
            }

            if (settings.schemaVersion < 2)
            {
                // NOTE: 旧設定にはプリセット情報が無いため、従来の色を含む既定構成で開始する。
                settings.enabledPresets = IrodoriPresets.GetDefaultEnabledPresetIds();
                settings.schemaVersion = 2;
                changed = true;
            }

            if (changed)
            {
                settings.SaveChanges();
            }
        }

        internal bool IsPresetEnabled(string presetId)
        {
            return enabledPresets != null && enabledPresets.Contains(presetId);
        }

        internal bool SetPresetEnabled(string presetId, bool value)
        {
            if (string.IsNullOrEmpty(presetId))
            {
                return false;
            }

            if (enabledPresets == null)
            {
                enabledPresets = new List<string>();
            }

            bool enabledNow = enabledPresets.Contains(presetId);
            if (enabledNow == value)
            {
                return false;
            }

            if (value)
            {
                enabledPresets.Add(presetId);
            }
            else
            {
                for (int i = enabledPresets.Count - 1; i >= 0; i--)
                {
                    if (enabledPresets[i] == presetId)
                    {
                        enabledPresets.RemoveAt(i);
                    }
                }
            }

            return true;
        }

        internal IrodoriLabelOverride FindOverride(string labelId)
        {
            if (overrides == null)
            {
                return null;
            }

            for (int i = overrides.Count - 1; i >= 0; i--)
            {
                IrodoriLabelOverride item = overrides[i];
                if (item != null && item.labelId == labelId)
                {
                    return item;
                }
            }

            return null;
        }

        internal IrodoriLabelOverride GetOrCreateOverride(string labelId)
        {
            IrodoriLabelOverride existing = FindOverride(labelId);
            if (existing != null)
            {
                return existing;
            }

            if (overrides == null)
            {
                overrides = new List<IrodoriLabelOverride>();
            }

            var created = new IrodoriLabelOverride { labelId = labelId };
            overrides.Add(created);
            return created;
        }

        internal bool RemoveOverride(string labelId)
        {
            if (overrides == null)
            {
                return false;
            }

            bool removed = false;
            for (int i = overrides.Count - 1; i >= 0; i--)
            {
                IrodoriLabelOverride item = overrides[i];
                if (item != null && item.labelId == labelId)
                {
                    overrides.RemoveAt(i);
                    removed = true;
                }
            }

            return removed;
        }

        internal int ClearOverrides()
        {
            if (overrides == null)
            {
                overrides = new List<IrodoriLabelOverride>();
                return 0;
            }

            int count = overrides.Count;
            overrides.Clear();
            return count;
        }

        internal IrodoriCustomLabel AddCustomLabel(string name, string colorHex)
        {
            if (!ColorUtility.TryParseHtmlString(colorHex, out Color color))
            {
                Debug.LogWarning("Irodori Colorizer: カスタムラベルの色を解析できませんでした。");
                return null;
            }

            string normalizedHex = "#" + ColorUtility.ToHtmlStringRGB(color);
            if (customLabels == null)
            {
                customLabels = new List<IrodoriCustomLabel>();
            }

            for (int attempt = 0; attempt < 8; attempt++)
            {
                string id = "custom." + Guid.NewGuid().ToString("N").Substring(0, 8);
                if (ContainsCustomLabel(id))
                {
                    continue;
                }

                var custom = new IrodoriCustomLabel
                {
                    id = id,
                    name = string.IsNullOrEmpty(name) ? normalizedHex : name,
                    colorHex = normalizedHex,
                };
                customLabels.Add(custom);
                return custom;
            }

            Debug.LogWarning("Irodori Colorizer: カスタムラベルの ID を作成できませんでした。");
            return null;
        }

        internal bool RemoveCustomLabel(string id)
        {
            if (customLabels == null)
            {
                return false;
            }

            for (int i = customLabels.Count - 1; i >= 0; i--)
            {
                IrodoriCustomLabel custom = customLabels[i];
                if (custom != null && custom.id == id)
                {
                    customLabels.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        internal int CountAssignments(string labelId)
        {
            return CountAssignments(projectAssignments, labelId) + CountAssignments(sceneAssignments, labelId);
        }

        private bool ContainsCustomLabel(string id)
        {
            for (int i = 0; i < customLabels.Count; i++)
            {
                if (customLabels[i] != null && customLabels[i].id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountAssignments(List<IrodoriAssignment> assignments, string labelId)
        {
            if (assignments == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < assignments.Count; i++)
            {
                IrodoriAssignment assignment = assignments[i];
                if (assignment != null && assignment.labelId == labelId)
                {
                    count++;
                }
            }

            return count;
        }

        public void SaveChanges()
        {
            Save(true);
        }
    }
}
