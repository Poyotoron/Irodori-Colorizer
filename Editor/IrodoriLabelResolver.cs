using System;
using System.Collections.Generic;
using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>見出し付きでラベルを表示する一区画。</summary>
    internal sealed class IrodoriLabelSection
    {
        public readonly string PresetId;
        public readonly string Title;
        public readonly IrodoriLabel[] Labels;

        public IrodoriLabelSection(string presetId, string title, IrodoriLabel[] labels)
        {
            PresetId = presetId;
            Title = title;
            Labels = labels;
        }
    }

    /// <summary>内蔵定義と利用者の設定を合成し、有効なラベルを解決する。</summary>
    internal static class IrodoriLabelResolver
    {
        private static List<IrodoriLabel> _visibleLabels;
        private static List<IrodoriLabelSection> _visibleSections;
        private static Dictionary<string, IrodoriLabel> _labelMap;
        private static bool _dirty = true;

        internal static IReadOnlyList<IrodoriLabel> GetVisibleLabels()
        {
            EnsureBuilt();
            return _visibleLabels;
        }

        internal static IReadOnlyList<IrodoriLabelSection> GetVisibleSections()
        {
            EnsureBuilt();
            return _visibleSections;
        }

        internal static bool TryResolve(string labelId, out IrodoriLabel label)
        {
            EnsureBuilt();
            if (!string.IsNullOrEmpty(labelId) && _labelMap.TryGetValue(labelId, out label))
            {
                return true;
            }

            label = default;
            return false;
        }

        internal static void Invalidate()
        {
            _dirty = true;
        }

        private static void EnsureBuilt()
        {
            if (!_dirty)
            {
                return;
            }

            Rebuild();
        }

        private static void Rebuild()
        {
            _dirty = false;
            var labels = new List<IrodoriLabel>();
            var sections = new List<IrodoriLabelSection>();
            var map = new Dictionary<string, IrodoriLabel>(StringComparer.Ordinal);
            IrodoriSettings settings = IrodoriSettings.instance;

            for (int i = 0; i < IrodoriPresets.All.Length; i++)
            {
                IrodoriPreset preset = IrodoriPresets.All[i];
                if (!settings.IsPresetEnabled(preset.Id))
                {
                    continue;
                }

                var sectionLabels = new List<IrodoriLabel>(preset.Labels.Length);
                for (int j = 0; j < preset.Labels.Length; j++)
                {
                    IrodoriLabel definition = preset.Labels[j];
                    IrodoriLabelOverride labelOverride = settings.FindOverride(definition.Id);
                    if (labelOverride != null && labelOverride.hidden)
                    {
                        continue;
                    }

                    string name = definition.Name;
                    Color color = definition.Color;
                    if (labelOverride != null)
                    {
                        if (labelOverride.overrideName && !string.IsNullOrEmpty(labelOverride.name))
                        {
                            name = labelOverride.name;
                        }

                        Color overrideColor;
                        if (labelOverride.overrideColor &&
                            ColorUtility.TryParseHtmlString(labelOverride.colorHex, out overrideColor))
                        {
                            color = overrideColor;
                        }
                    }

                    var resolved = new IrodoriLabel(definition.Id, name, color, preset.Id, false);
                    sectionLabels.Add(resolved);
                    labels.Add(resolved);
                    map[resolved.Id] = resolved;
                }

                if (sectionLabels.Count > 0)
                {
                    sections.Add(new IrodoriLabelSection(preset.Id, preset.DisplayName, sectionLabels.ToArray()));
                }
            }

            var customSection = new List<IrodoriLabel>();
            if (settings.customLabels != null)
            {
                for (int i = 0; i < settings.customLabels.Count; i++)
                {
                    IrodoriCustomLabel custom = settings.customLabels[i];
                    if (custom == null || string.IsNullOrEmpty(custom.id))
                    {
                        continue;
                    }

                    if (!ColorUtility.TryParseHtmlString(custom.colorHex, out Color color))
                    {
                        Debug.LogWarning("Irodori Colorizer: カスタムラベルの色を解析できないため除外しました: " + custom.id);
                        continue;
                    }

                    string name = string.IsNullOrEmpty(custom.name) ? custom.colorHex : custom.name;
                    var resolved = new IrodoriLabel(custom.id, name, color, null, true);
                    customSection.Add(resolved);
                    labels.Add(resolved);
                    map[resolved.Id] = resolved;
                }
            }

            if (customSection.Count > 0)
            {
                sections.Add(new IrodoriLabelSection(null, "カスタム", customSection.ToArray()));
            }

            _visibleLabels = labels;
            _visibleSections = sections;
            _labelMap = map;
        }
    }
}
