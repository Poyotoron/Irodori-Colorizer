using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    internal readonly struct IrodoriLabel
    {
        public readonly string Id;
        public readonly string Name;
        public readonly Color Color;

        public IrodoriLabel(string id, string name, Color color)
        {
            Id = id;
            Name = name;
            Color = color;
        }
    }

    /// <summary>固定パレットとラベル ID の解決を提供する。</summary>
    internal static class IrodoriPalette
    {
        // NOTE: 描画中の割り当てを避けるため、パレットは一度だけ構築して共有する。
        internal static readonly IrodoriLabel[] Labels = BuildLabels();

        private static IrodoriLabel[] BuildLabels()
        {
            return new[]
            {
                Make("preset.basic.red", "Red", "#E5484D"),
                Make("preset.basic.orange", "Orange", "#F2711C"),
                Make("preset.basic.amber", "Amber", "#F5A623"),
                Make("preset.basic.yellow", "Yellow", "#E8D44D"),
                Make("preset.basic.lime", "Lime", "#A9D64B"),
                Make("preset.basic.green", "Green", "#46C25E"),
                Make("preset.basic.emerald", "Emerald", "#2FBF9E"),
                Make("preset.basic.teal", "Teal", "#35B7C7"),
                Make("preset.basic.cyan", "Cyan", "#4AB8E8"),
                Make("preset.basic.blue", "Blue", "#4B7BEC"),
                Make("preset.basic.indigo", "Indigo", "#6C5CE7"),
                Make("preset.basic.violet", "Violet", "#9B59E8"),
                Make("preset.basic.magenta", "Magenta", "#D45BD4"),
                Make("preset.basic.pink", "Pink", "#EE5A8F"),
                Make("preset.basic.brown", "Brown", "#A9764B"),
                Make("preset.basic.gray", "Gray", "#8C939B"),
            };
        }

        private static IrodoriLabel Make(string id, string name, string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return new IrodoriLabel(id, name, color);
        }

        /// <summary>未知のラベル ID は解決せず、描画対象から除外する。</summary>
        internal static bool TryResolve(string labelId, out IrodoriLabel label)
        {
            for (int i = 0; i < Labels.Length; i++)
            {
                if (Labels[i].Id == labelId)
                {
                    label = Labels[i];
                    return true;
                }
            }

            label = default;
            return false;
        }
    }
}
