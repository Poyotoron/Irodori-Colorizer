using System.Collections.Generic;
using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>解決済みのラベル。</summary>
    internal readonly struct IrodoriLabel
    {
        public readonly string Id;
        public readonly string Name;
        public readonly Color Color;
        public readonly string PresetId;
        public readonly bool IsCustom;

        public IrodoriLabel(string id, string name, Color color, string presetId, bool isCustom)
        {
            Id = id;
            Name = name;
            Color = color;
            PresetId = presetId;
            IsCustom = isCustom;
        }
    }

    /// <summary>内蔵プリセット一件分の定義。</summary>
    internal sealed class IrodoriPreset
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly bool EnabledByDefault;
        public readonly IrodoriLabel[] Labels;

        public IrodoriPreset(
            string id,
            string displayName,
            string description,
            bool enabledByDefault,
            IrodoriLabel[] labels)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            EnabledByDefault = enabledByDefault;
            Labels = labels;
        }
    }

    /// <summary>内蔵プリセットの静的定義を提供する。</summary>
    internal static class IrodoriPresets
    {
        // NOTE: 描画中の割り当てを避けるため、定義は一度だけ構築して共有する。
        internal static readonly IrodoriPreset[] All = BuildAll();

        internal static bool TryGetPreset(string presetId, out IrodoriPreset preset)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == presetId)
                {
                    preset = All[i];
                    return true;
                }
            }

            preset = null;
            return false;
        }

        internal static List<string> GetDefaultEnabledPresetIds()
        {
            var ids = new List<string>();
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].EnabledByDefault)
                {
                    ids.Add(All[i].Id);
                }
            }

            return ids;
        }

        internal static bool TryGetDefinition(string labelId, out IrodoriLabel label)
        {
            for (int i = 0; i < All.Length; i++)
            {
                IrodoriLabel[] labels = All[i].Labels;
                for (int j = 0; j < labels.Length; j++)
                {
                    if (labels[j].Id == labelId)
                    {
                        label = labels[j];
                        return true;
                    }
                }
            }

            label = default;
            return false;
        }

        private static IrodoriPreset[] BuildAll()
        {
            return new[]
            {
                new IrodoriPreset(
                    "vrchat",
                    "VRChat アバター改変",
                    "素体・衣装・ギミックなど改変向けの分類",
                    true,
                    new[]
                    {
                        Make("vrchat", "workspace", "改変作業中", "#E5484D"),
                        Make("vrchat", "avatar", "アバター素体", "#F2711C"),
                        Make("vrchat", "costume", "衣装", "#EE5A8F"),
                        Make("vrchat", "parts", "改変パーツ", "#F5A623"),
                        Make("vrchat", "gimmick", "小物・ギミック", "#46C25E"),
                        Make("vrchat", "system", "システム", "#2FBF9E"),
                        Make("vrchat", "motion", "アニメーション", "#9B59E8"),
                        Make("vrchat", "material", "マテリアル", "#4B7BEC"),
                        Make("vrchat", "texture", "テクスチャ", "#4AB8E8"),
                        Make("vrchat", "shader", "シェーダー", "#6C5CE7"),
                        Make("vrchat", "menu", "メニュー", "#D45BD4"),
                        Make("vrchat", "tool", "ツール", "#8C939B"),
                        Make("vrchat", "readonly", "触るな", "#A9764B"),
                    }),
                new IrodoriPreset(
                    "basic",
                    "ベーシックカラー",
                    "意味を持たない汎用色。名前は色名そのもの",
                    true,
                    new[]
                    {
                        Make("basic", "red", "Red", "#E5484D"),
                        Make("basic", "orange", "Orange", "#F2711C"),
                        Make("basic", "amber", "Amber", "#F5A623"),
                        Make("basic", "yellow", "Yellow", "#E8D44D"),
                        Make("basic", "lime", "Lime", "#A9D64B"),
                        Make("basic", "green", "Green", "#46C25E"),
                        Make("basic", "emerald", "Emerald", "#2FBF9E"),
                        Make("basic", "teal", "Teal", "#35B7C7"),
                        Make("basic", "cyan", "Cyan", "#4AB8E8"),
                        Make("basic", "blue", "Blue", "#4B7BEC"),
                        Make("basic", "indigo", "Indigo", "#6C5CE7"),
                        Make("basic", "violet", "Violet", "#9B59E8"),
                        Make("basic", "magenta", "Magenta", "#D45BD4"),
                        Make("basic", "pink", "Pink", "#EE5A8F"),
                        Make("basic", "brown", "Brown", "#A9764B"),
                        Make("basic", "gray", "Gray", "#8C939B"),
                    }),
                new IrodoriPreset(
                    "unity",
                    "汎用 Unity プロジェクト",
                    "Scenes / Scripts / Prefabs など一般的なフォルダ分類",
                    false,
                    new[]
                    {
                        Make("unity", "scenes", "Scenes", "#46C25E"),
                        Make("unity", "scripts", "Scripts", "#4B7BEC"),
                        Make("unity", "prefabs", "Prefabs", "#4AB8E8"),
                        Make("unity", "materials", "Materials", "#9B59E8"),
                        Make("unity", "textures", "Textures", "#D45BD4"),
                        Make("unity", "models", "Models", "#F2711C"),
                        Make("unity", "animations", "Animations", "#EE5A8F"),
                        Make("unity", "audio", "Audio", "#2FBF9E"),
                        Make("unity", "shaders", "Shaders", "#6C5CE7"),
                        Make("unity", "ui", "UI", "#F5A623"),
                        Make("unity", "editor", "Editor", "#35B7C7"),
                        Make("unity", "thirdparty", "ThirdParty", "#8C939B"),
                    }),
            };
        }

        private static IrodoriLabel Make(string presetId, string key, string name, string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                Debug.LogError("Irodori Colorizer: 内蔵ラベルの色を解析できませんでした: " + hex);
            }

            return new IrodoriLabel("preset." + presetId + "." + key, name, color, presetId, false);
        }
    }
}
