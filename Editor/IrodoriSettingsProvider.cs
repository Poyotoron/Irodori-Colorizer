using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>表示とラベルの設定を Project Settings に公開する。</summary>
    internal static class IrodoriSettingsProvider
    {
        private const string LabelListFoldoutKey = "IrodoriColorizer.LabelListFoldout";
        private const float ColorFieldWidth = 46f;
        private const float HexFieldWidth = 66f;
        private const float SmallButtonWidth = 20f;
        private const float ElementGap = 2f;

        private static readonly GUIContent PresetsHeading = new GUIContent("プリセット");
        private static readonly GUIContent LabelListHeading = new GUIContent("ラベル一覧");
        private static readonly GUIContent CustomHeading = new GUIContent("カスタム");
        private static readonly GUIContent VisibleContent = new GUIContent("◉", "このラベルを非表示にする");
        private static readonly GUIContent HiddenContent = new GUIContent("◎", "このラベルを再表示する");
        private static readonly GUIContent ResetContent = new GUIContent("↺", "既定値に戻す");
        private static readonly GUIContent DeleteContent = new GUIContent("×", "このラベルを削除する");
        private static readonly GUIContent ResetAllContent = new GUIContent("すべてのオーバーライドをリセット");
        private static readonly GUIContent AddContent = new GUIContent("追加");
        private static readonly Color FallbackCustomColor = BuildFallbackCustomColor();
        private static readonly GUIContent[] PresetContents = BuildPresetContents();
        private static readonly GUIContent[] PresetHeadingContents = BuildPresetHeadingContents();
        private static readonly Dictionary<string, string> HexCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, Color> HexColorCache = new Dictionary<string, Color>();

        private static int _staleAssignmentCount = -1;
        private static int _unknownAssignmentCount = -1;
        private static string _staleAssignmentMessage;
        private static string _unknownAssignmentMessage;
        private static string _newCustomHex = "";
        private static string _newCustomName = "";

        [SettingsProvider]
        private static SettingsProvider Create()
        {
            var provider = new SettingsProvider(IrodoriInfo.SettingsPath, SettingsScope.Project)
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
                    "preset",
                    "label",
                }),
            };
            provider.activateHandler = (searchContext, rootElement) => InvalidateAssignmentCounts();
            return provider;
        }

        internal static void InvalidateAssignmentCounts()
        {
            _staleAssignmentCount = -1;
            _unknownAssignmentCount = -1;
            _staleAssignmentMessage = null;
            _unknownAssignmentMessage = null;
        }

        private static void OnGUI(string searchContext)
        {
            IrodoriSettings settings = IrodoriSettings.instance;
            bool changed = false;

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

            EditorGUILayout.Space();
            DrawPresets(settings, ref changed);
            changed |= EditorGUI.EndChangeCheck();

            EnsureAssignmentCounts(settings);
            if (_staleAssignmentCount > 0)
            {
                EditorGUILayout.HelpBox(_staleAssignmentMessage, MessageType.Info);
            }

            if (_unknownAssignmentCount > 0)
            {
                EditorGUILayout.HelpBox(_unknownAssignmentMessage, MessageType.Info);
            }

            EditorGUILayout.Space();
            bool foldout = SessionState.GetBool(LabelListFoldoutKey, true);
            EditorGUI.BeginChangeCheck();
            foldout = EditorGUILayout.Foldout(foldout, LabelListHeading, true);
            if (EditorGUI.EndChangeCheck())
            {
                SessionState.SetBool(LabelListFoldoutKey, foldout);
            }

            if (foldout)
            {
                EditorGUI.BeginChangeCheck();
                DrawLabelList(settings, ref changed);
                changed |= EditorGUI.EndChangeCheck();
            }

            if (changed)
            {
                SaveAndRefresh(settings);
            }
        }

        private static void DrawPresets(IrodoriSettings settings, ref bool changed)
        {
            EditorGUILayout.LabelField(PresetsHeading, EditorStyles.boldLabel);
            for (int i = 0; i < IrodoriPresets.All.Length; i++)
            {
                IrodoriPreset preset = IrodoriPresets.All[i];
                bool current = settings.IsPresetEnabled(preset.Id);
                bool next = EditorGUILayout.ToggleLeft(PresetContents[i], current);
                if (next != current && settings.SetPresetEnabled(preset.Id, next))
                {
                    changed = true;
                    InvalidateAssignmentCounts();
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(preset.Description, EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
        }

        private static void DrawLabelList(IrodoriSettings settings, ref bool changed)
        {
            for (int i = 0; i < IrodoriPresets.All.Length; i++)
            {
                IrodoriPreset preset = IrodoriPresets.All[i];
                if (!settings.IsPresetEnabled(preset.Id))
                {
                    continue;
                }

                EditorGUILayout.LabelField(PresetHeadingContents[i], EditorStyles.boldLabel);
                for (int j = 0; j < preset.Labels.Length; j++)
                {
                    DrawBuiltInLabel(settings, preset.Labels[j], ref changed);
                }

                EditorGUILayout.Space(2f);
            }

            if (settings.customLabels != null && settings.customLabels.Count > 0)
            {
                EditorGUILayout.LabelField(CustomHeading, EditorStyles.boldLabel);
                for (int i = 0; i < settings.customLabels.Count; i++)
                {
                    IrodoriCustomLabel custom = settings.customLabels[i];
                    if (custom != null)
                    {
                        DrawCustomLabel(settings, custom, ref changed);
                    }
                }

                EditorGUILayout.Space(2f);
            }

            int overrideCount = settings.overrides != null ? settings.overrides.Count : 0;
            using (new EditorGUI.DisabledScope(overrideCount == 0))
            {
                if (GUILayout.Button(ResetAllContent) && EditorUtility.DisplayDialog(
                        "オーバーライドのリセット",
                        "内蔵ラベルの色・名前・非表示設定をすべて既定値に戻します。",
                        "リセット",
                        "キャンセル"))
                {
                    settings.ClearOverrides();
                    SaveAndRefresh(settings);
                    GUIUtility.ExitGUI();
                }
            }

            DrawCustomAddRow(settings, ref changed);
        }

        private static void DrawBuiltInLabel(IrodoriSettings settings, IrodoriLabel definition, ref bool changed)
        {
            IrodoriLabelOverride labelOverride = settings.FindOverride(definition.Id);
            bool hidden = labelOverride != null && labelOverride.hidden;
            string name = labelOverride != null && labelOverride.overrideName && !string.IsNullOrEmpty(labelOverride.name)
                ? labelOverride.name
                : definition.Name;
            Color color = definition.Color;
            Color overrideColor;
            if (labelOverride != null && labelOverride.overrideColor &&
                ColorUtility.TryParseHtmlString(labelOverride.colorHex, out overrideColor))
            {
                color = overrideColor;
            }

            GetRowRects(out Rect colorRect, out Rect nameRect, out Rect hexRect,
                out Rect visibleRect, out Rect resetRect, out Rect deleteRect);

            using (new EditorGUI.DisabledScope(hidden))
            {
                Color nextColor = EditorGUI.ColorField(colorRect, GUIContent.none, color, true, false, false);
                if (nextColor != color)
                {
                    labelOverride = settings.GetOrCreateOverride(definition.Id);
                    labelOverride.overrideColor = true;
                    labelOverride.colorHex = "#" + ColorUtility.ToHtmlStringRGB(nextColor);
                    color = nextColor;
                    changed = true;
                }

                string nextName = EditorGUI.TextField(nameRect, name);
                if (nextName != name)
                {
                    labelOverride = settings.GetOrCreateOverride(definition.Id);
                    labelOverride.overrideName = !string.IsNullOrEmpty(nextName);
                    labelOverride.name = labelOverride.overrideName ? nextName : null;
                    changed = true;
                }

                EditorGUI.SelectableLabel(hexRect, GetHex(definition.Id, color), EditorStyles.miniLabel);
            }

            if (GUI.Button(visibleRect, hidden ? HiddenContent : VisibleContent))
            {
                labelOverride = settings.GetOrCreateOverride(definition.Id);
                labelOverride.hidden = !hidden;
                changed = true;
            }

            using (new EditorGUI.DisabledScope(labelOverride == null))
            {
                if (GUI.Button(resetRect, ResetContent))
                {
                    changed |= settings.RemoveOverride(definition.Id);
                    labelOverride = null;
                }
            }

            using (new EditorGUI.DisabledScope(true))
            {
                GUI.Button(deleteRect, DeleteContent);
            }

            if (labelOverride != null && !labelOverride.overrideName && !labelOverride.overrideColor && !labelOverride.hidden)
            {
                changed |= settings.RemoveOverride(definition.Id);
            }
        }

        private static void DrawCustomLabel(IrodoriSettings settings, IrodoriCustomLabel custom, ref bool changed)
        {
            // NOTE: 壊れた色指定が残ると一覧にも描画にも出ないため、開いた時点で既定色へ直す。
            if (!ColorUtility.TryParseHtmlString(custom.colorHex, out Color color))
            {
                color = FallbackCustomColor;
                custom.colorHex = "#8C939B";
                changed = true;
            }

            GetRowRects(out Rect colorRect, out Rect nameRect, out Rect hexRect,
                out Rect visibleRect, out Rect resetRect, out Rect deleteRect);

            Color nextColor = EditorGUI.ColorField(colorRect, GUIContent.none, color, true, false, false);
            if (nextColor != color)
            {
                custom.colorHex = "#" + ColorUtility.ToHtmlStringRGB(nextColor);
                color = nextColor;
                changed = true;
            }

            string nextName = EditorGUI.TextField(nameRect, custom.name);
            if (nextName != custom.name)
            {
                custom.name = nextName;
                changed = true;
            }

            EditorGUI.SelectableLabel(hexRect, GetHex(custom.id, color), EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                GUI.Button(visibleRect, VisibleContent);
                GUI.Button(resetRect, ResetContent);
            }

            if (GUI.Button(deleteRect, DeleteContent) && EditorUtility.DisplayDialog(
                    "ラベルの削除",
                    "このラベルを削除します。割り当ては削除されませんが、どのラベルにも解決されなくなります。",
                    "削除",
                    "キャンセル"))
            {
                settings.RemoveCustomLabel(custom.id);
                SaveAndRefresh(settings);
                GUIUtility.ExitGUI();
            }
        }

        private static void DrawCustomAddRow(IrodoriSettings settings, ref bool changed)
        {
            Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            var hexRect = new Rect(row.x, row.y, 90f, row.height);
            var buttonRect = new Rect(row.xMax - 50f, row.y, 50f, row.height);
            var nameRect = new Rect(hexRect.xMax + ElementGap, row.y,
                buttonRect.x - hexRect.xMax - ElementGap * 2f, row.height);
            _newCustomHex = EditorGUI.TextField(hexRect, _newCustomHex);
            _newCustomName = EditorGUI.TextField(nameRect, _newCustomName);
            if (!GUI.Button(buttonRect, AddContent))
            {
                return;
            }

            string input = _newCustomHex.Trim();
            if (!input.StartsWith("#"))
            {
                input = "#" + input;
            }

            if (!ColorUtility.TryParseHtmlString(input, out Color color))
            {
                Debug.LogWarning("Irodori Colorizer: Hex カラーコードを解析できませんでした。");
                return;
            }

            string hex = "#" + ColorUtility.ToHtmlStringRGB(color);
            string name = string.IsNullOrEmpty(_newCustomName) ? hex : _newCustomName;
            if (settings.AddCustomLabel(name, hex) == null)
            {
                return;
            }

            _newCustomHex = "";
            _newCustomName = "";
            changed = true;
        }

        private static void GetRowRects(
            out Rect colorRect,
            out Rect nameRect,
            out Rect hexRect,
            out Rect visibleRect,
            out Rect resetRect,
            out Rect deleteRect)
        {
            Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            float fixedWidth = ColorFieldWidth + HexFieldWidth + SmallButtonWidth * 3f + ElementGap * 5f;
            float nameWidth = Mathf.Max(40f, row.width - fixedWidth);
            float x = row.x;
            colorRect = new Rect(x, row.y, ColorFieldWidth, row.height);
            x = colorRect.xMax + ElementGap;
            nameRect = new Rect(x, row.y, nameWidth, row.height);
            x = nameRect.xMax + ElementGap;
            hexRect = new Rect(x, row.y, HexFieldWidth, row.height);
            x = hexRect.xMax + ElementGap;
            visibleRect = new Rect(x, row.y, SmallButtonWidth, row.height);
            x = visibleRect.xMax + ElementGap;
            resetRect = new Rect(x, row.y, SmallButtonWidth, row.height);
            x = resetRect.xMax + ElementGap;
            deleteRect = new Rect(x, row.y, SmallButtonWidth, row.height);
        }

        private static string GetHex(string id, Color color)
        {
            if (!HexColorCache.TryGetValue(id, out Color cachedColor) || cachedColor != color)
            {
                HexColorCache[id] = color;
                HexCache[id] = "#" + ColorUtility.ToHtmlStringRGB(color);
            }

            return HexCache[id];
        }

        private static void EnsureAssignmentCounts(IrodoriSettings settings)
        {
            if (_staleAssignmentCount >= 0 && _unknownAssignmentCount >= 0)
            {
                return;
            }

            var customIds = new HashSet<string>();
            if (settings.customLabels != null)
            {
                for (int i = 0; i < settings.customLabels.Count; i++)
                {
                    IrodoriCustomLabel custom = settings.customLabels[i];
                    if (custom != null && !string.IsNullOrEmpty(custom.id))
                    {
                        customIds.Add(custom.id);
                    }
                }
            }

            _staleAssignmentCount = 0;
            _unknownAssignmentCount = 0;
            CountUnavailableAssignments(settings.projectAssignments, settings, customIds);
            CountUnavailableAssignments(settings.sceneAssignments, settings, customIds);
            _staleAssignmentMessage = "無効なプリセットに紐づく割り当てが " + _staleAssignmentCount +
                " 件あります。プリセットを有効にすると復元されます。";
            _unknownAssignmentMessage = "解決できないラベル ID の割り当てが " + _unknownAssignmentCount + " 件あります。";
        }

        private static void CountUnavailableAssignments(
            List<IrodoriAssignment> assignments,
            IrodoriSettings settings,
            HashSet<string> customIds)
        {
            if (assignments == null)
            {
                return;
            }

            for (int i = 0; i < assignments.Count; i++)
            {
                IrodoriAssignment assignment = assignments[i];
                if (assignment == null || string.IsNullOrEmpty(assignment.labelId))
                {
                    continue;
                }

                if (IrodoriPresets.TryGetDefinition(assignment.labelId, out IrodoriLabel definition))
                {
                    if (!settings.IsPresetEnabled(definition.PresetId))
                    {
                        _staleAssignmentCount++;
                    }
                }
                else if (!customIds.Contains(assignment.labelId))
                {
                    _unknownAssignmentCount++;
                }
            }
        }

        private static GUIContent[] BuildPresetContents()
        {
            var contents = new GUIContent[IrodoriPresets.All.Length];
            for (int i = 0; i < contents.Length; i++)
            {
                IrodoriPreset preset = IrodoriPresets.All[i];
                contents[i] = new GUIContent(
                    preset.DisplayName + " (" + preset.Labels.Length + ")",
                    preset.Description);
            }

            return contents;
        }

        private static Color BuildFallbackCustomColor()
        {
            ColorUtility.TryParseHtmlString("#8C939B", out Color color);
            return color;
        }

        private static GUIContent[] BuildPresetHeadingContents()
        {
            var contents = new GUIContent[IrodoriPresets.All.Length];
            for (int i = 0; i < contents.Length; i++)
            {
                contents[i] = new GUIContent(IrodoriPresets.All[i].DisplayName);
            }

            return contents;
        }

        private static void SaveAndRefresh(IrodoriSettings settings)
        {
            settings.SaveChanges();
            IrodoriDrawer.Invalidate();
            EditorApplication.RepaintProjectWindow();
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
