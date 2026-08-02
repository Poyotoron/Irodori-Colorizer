using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>Project と Hierarchy の行描画を共通化する。</summary>
    [InitializeOnLoad]
    internal static class IrodoriDrawer
    {
        private static readonly Color ProBackground = new Color(0.219f, 0.219f, 0.219f);
        private static readonly Color PersonalBackground = new Color(0.760f, 0.760f, 0.760f);
        private static readonly Color DarkText = new Color(0.06f, 0.06f, 0.06f);

        private static Dictionary<string, string> _projectMap;
        private static GUIStyle _labelStyle;
        private static GUIStyle _gridLabelStyle;
        private static Color _lastTextColor;
        private static Color _lastGridTextColor;
        private static bool _hasTextColor;
        private static bool _hasGridTextColor;

        internal static Vector2 ContextScreenPosition { get; private set; }

        static IrodoriDrawer()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectItem;
            EditorApplication.projectChanged += Invalidate;
        }

        private static Dictionary<string, string> ProjectMap
        {
            get
            {
                if (_projectMap == null)
                {
                    _projectMap = BuildProjectMap();
                }

                return _projectMap;
            }
        }

        /// <summary>設定変更後に参照キャッシュを破棄する。</summary>
        public static void Invalidate()
        {
            _projectMap = null;
            IrodoriHierarchy.Invalidate();
        }

        private static Dictionary<string, string> BuildProjectMap()
        {
            List<IrodoriAssignment> assignments = IrodoriSettings.instance.projectAssignments;
            int capacity = assignments != null ? assignments.Count : 0;
            var map = new Dictionary<string, string>(capacity, StringComparer.Ordinal);

            if (assignments == null)
            {
                return map;
            }

            for (int i = 0; i < assignments.Count; i++)
            {
                IrodoriAssignment assignment = assignments[i];
                if (assignment == null || string.IsNullOrEmpty(assignment.key) || string.IsNullOrEmpty(assignment.labelId))
                {
                    continue;
                }

                map[assignment.key] = assignment.labelId;
            }

            return map;
        }

        private static void EnsureStyles()
        {
            if (_labelStyle != null)
            {
                return;
            }

            // NOTE: EditorStyles は起動直後には未初期化のため、GUI が有効な最初の描画時に一度だけ複製する。
            _labelStyle = new GUIStyle(EditorStyles.label);
            _gridLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
            };
            _hasTextColor = false;
            _hasGridTextColor = false;
        }

        private static void OnProjectItem(string guid, Rect selectionRect)
        {
            CaptureContextClick(selectionRect);

            IrodoriSettings settings = IrodoriSettings.instance;
            // NOTE: このコールバックは表示行ごとに繰り返されるため、無効時は参照処理にも入らない。
            if (!settings.enabled || !settings.paintProject)
            {
                return;
            }

            if (ProjectMap.Count == 0 || !ProjectMap.TryGetValue(guid, out string labelId))
            {
                return;
            }

            if (!IrodoriPalette.TryResolve(labelId, out IrodoriLabel label))
            {
                return;
            }

            bool isGrid = selectionRect.height > 20f;
            bool selected = Array.IndexOf(Selection.assetGUIDs, guid) >= 0;
            DrawProjectRow(selectionRect, label.Color, isGrid, selected, guid);
        }

        internal static void CaptureContextClick(Rect selectionRect)
        {
            Event current = Event.current;
            if (current == null || current.type != EventType.ContextClick || !selectionRect.Contains(current.mousePosition))
            {
                return;
            }

            // NOTE: 標準の右クリックメニューを残すためイベントは消費せず、表示座標だけを保存する。
            ContextScreenPosition = GUIUtility.GUIToScreenPoint(current.mousePosition);
        }

        private static void DrawProjectRow(Rect selectionRect, Color labelColor, bool isGrid, bool selected, string guid)
        {
            IrodoriSettings settings = IrodoriSettings.instance;
            Color blended = Blend(labelColor, settings.fillAlpha);

            if (isGrid)
            {
                DrawProjectGridCell(selectionRect, blended, selected, guid, settings);
                return;
            }

            if (selected && settings.keepSelectionVisible)
            {
                DrawSelectionBar(selectionRect, labelColor, 1f);
                return;
            }

            DrawFill(selectionRect, blended, settings.paintFullRow);

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Texture icon = AssetDatabase.GetCachedIcon(path);
            if (icon != null)
            {
                GUI.DrawTexture(GetIconRect(selectionRect), icon, ScaleMode.ScaleToFit, true);
            }

            string name = Path.GetFileNameWithoutExtension(path);
            DrawName(selectionRect, name, GetTextColor(blended, settings), 1f, settings.labelIndent);
        }

        private static void DrawProjectGridCell(
            Rect selectionRect,
            Color blended,
            bool selected,
            string guid,
            IrodoriSettings settings)
        {
            if (selected && settings.keepSelectionVisible)
            {
                var stripe = selectionRect;
                stripe.yMin = stripe.yMax - 3f;
                EditorGUI.DrawRect(stripe, blended);
                return;
            }

            EditorGUI.DrawRect(selectionRect, blended);

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path);
            Texture preview = obj != null ? AssetPreview.GetAssetPreview(obj) : null;
            if (preview == null)
            {
                preview = AssetDatabase.GetCachedIcon(path);
            }

            const float labelHeight = 18f;
            if (preview != null)
            {
                var previewRect = selectionRect;
                previewRect.xMin += 4f;
                previewRect.xMax -= 4f;
                previewRect.yMin += 2f;
                previewRect.yMax -= labelHeight;
                if (previewRect.width > 0f && previewRect.height > 0f)
                {
                    GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit, true);
                }
            }

            var labelRect = selectionRect;
            labelRect.xMin += 2f;
            labelRect.xMax -= 2f;
            labelRect.yMin = labelRect.yMax - labelHeight;
            string name = Path.GetFileNameWithoutExtension(path);
            DrawGridName(labelRect, name, GetTextColor(blended, settings));
        }

        internal static void DrawHierarchyRow(Rect selectionRect, Color labelColor, bool selected, bool inactive, GameObject obj)
        {
            IrodoriSettings settings = IrodoriSettings.instance;
            float visualAlpha = inactive ? 0.5f : 1f;
            Color blended = Blend(labelColor, settings.fillAlpha * visualAlpha);

            if (selected && settings.keepSelectionVisible)
            {
                DrawSelectionBar(selectionRect, labelColor, visualAlpha);
                return;
            }

            DrawFill(selectionRect, blended, settings.paintFullRow);

            Texture icon = AssetPreview.GetMiniThumbnail(obj);
            if (icon != null)
            {
                Color previousColor = GUI.color;
                var iconColor = Color.white;
                iconColor.a = visualAlpha;
                GUI.color = iconColor;
                GUI.DrawTexture(GetIconRect(selectionRect), icon, ScaleMode.ScaleToFit, true);
                GUI.color = previousColor;
            }

            DrawName(selectionRect, obj.name, GetTextColor(blended, settings), visualAlpha, settings.labelIndent);
        }

        private static void DrawFill(Rect selectionRect, Color color, bool paintFullRow)
        {
            var fill = selectionRect;
            if (paintFullRow)
            {
                // NOTE: 左端まで塗ると折りたたみ矢印が潰れるため、元の行より 2px 手前で止める。
                fill.xMin = selectionRect.x - 2f;
            }

            EditorGUI.DrawRect(fill, color);
        }

        private static void DrawSelectionBar(Rect selectionRect, Color color, float alpha)
        {
            var bar = selectionRect;
            bar.xMin = selectionRect.x - 2f;
            bar.width = 3f;
            color.a = alpha;
            EditorGUI.DrawRect(bar, color);
        }

        private static Rect GetIconRect(Rect selectionRect)
        {
            return new Rect(selectionRect.x, selectionRect.y + (selectionRect.height - 16f) * 0.5f, 16f, 16f);
        }

        private static void DrawName(Rect selectionRect, string name, Color textColor, float alpha, float labelIndent)
        {
            textColor.a *= alpha;
            var labelRect = selectionRect;
            labelRect.xMin = selectionRect.x + labelIndent;

            EnsureStyles();
            SetTextColor(textColor);
            GUI.Label(labelRect, name, _labelStyle);
        }

        private static void SetTextColor(Color color)
        {
            if (_hasTextColor && _lastTextColor == color)
            {
                return;
            }

            _labelStyle.normal.textColor = color;
            _labelStyle.onNormal.textColor = color;
            _labelStyle.focused.textColor = color;
            _labelStyle.onFocused.textColor = color;
            _lastTextColor = color;
            _hasTextColor = true;
        }

        private static void DrawGridName(Rect labelRect, string name, Color textColor)
        {
            EnsureStyles();
            SetGridTextColor(textColor);
            GUI.Label(labelRect, name, _gridLabelStyle);
        }

        private static void SetGridTextColor(Color color)
        {
            if (_hasGridTextColor && _lastGridTextColor == color)
            {
                return;
            }

            _gridLabelStyle.normal.textColor = color;
            _gridLabelStyle.onNormal.textColor = color;
            _gridLabelStyle.focused.textColor = color;
            _gridLabelStyle.onFocused.textColor = color;
            _lastGridTextColor = color;
            _hasGridTextColor = true;
        }

        private static Color Blend(Color labelColor, float alpha)
        {
            Color background = EditorGUIUtility.isProSkin ? ProBackground : PersonalBackground;
            Color blended = Color.Lerp(background, labelColor, Mathf.Clamp01(alpha));
            blended.a = 1f;
            return blended;
        }

        private static Color GetTextColor(Color blended, IrodoriSettings settings)
        {
            if (!settings.autoTextColor)
            {
                return settings.forcedTextColor;
            }

            float luma = 0.299f * blended.r + 0.587f * blended.g + 0.114f * blended.b;
            return luma > 0.55f ? DarkText : Color.white;
        }
    }
}
