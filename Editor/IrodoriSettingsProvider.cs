using System.Collections.Generic;
using UnityEditor;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>表示オプションを Project Settings に公開する。</summary>
    internal static class IrodoriSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider Create()
        {
            return new SettingsProvider(IrodoriInfo.SettingsPath, SettingsScope.Project)
            {
                label = IrodoriInfo.DisplayName,
                guiHandler = OnGUI,
                keywords = new HashSet<string>(new[]
                {
                    "irodori",
                    "colorizer",
                    "project",
                    "hierarchy",
                    "color",
                }),
            };
        }

        private static void OnGUI(string searchContext)
        {
            IrodoriSettings settings = IrodoriSettings.instance;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("表示", EditorStyles.boldLabel);
            settings.enabled = EditorGUILayout.Toggle("有効", settings.enabled);
            using (new EditorGUI.DisabledScope(!settings.enabled))
            {
                settings.paintProject = EditorGUILayout.Toggle("Project ウィンドウ", settings.paintProject);
                settings.paintHierarchy = EditorGUILayout.Toggle("Hierarchy ウィンドウ", settings.paintHierarchy);
                settings.paintFullRow = EditorGUILayout.Toggle("行全体を塗る", settings.paintFullRow);
                settings.fillAlpha = EditorGUILayout.Slider("塗りの濃さ", settings.fillAlpha, 0.05f, 1f);
                settings.keepSelectionVisible = EditorGUILayout.Toggle("選択行は塗らない", settings.keepSelectionVisible);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("ラベル表示", EditorStyles.boldLabel);
                settings.autoTextColor = EditorGUILayout.Toggle("文字色を自動判定", settings.autoTextColor);
                using (new EditorGUI.DisabledScope(settings.autoTextColor))
                {
                    settings.forcedTextColor = EditorGUILayout.ColorField("文字色（固定）", settings.forcedTextColor);
                }

                settings.labelIndent = EditorGUILayout.Slider("ラベル位置", settings.labelIndent, 12f, 26f);
            }

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            settings.SaveChanges();
            IrodoriDrawer.Invalidate();
            EditorApplication.RepaintProjectWindow();
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
