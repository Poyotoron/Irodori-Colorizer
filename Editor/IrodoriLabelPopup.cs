using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>有効なラベルを見出し付きで選ぶポップアップ。</summary>
    internal sealed class IrodoriLabelPopup : EditorWindow
    {
        private const float PopupWidth = 260f;
        private const float MaxPopupHeight = 420f;
        private const float Padding = 8f;
        private const float Gap = 4f;
        private const float HeaderHeight = 18f;
        private const float RowHeight = 20f;
        private const float SwatchHeight = 24f;
        private const float FooterHeight = 82f;
        private const float ScrollBarWidth = 16f;
        private const int BasicColumns = 4;

        private static readonly GUIContent TitleContent = new GUIContent("ラベルを選択");
        private static readonly GUIContent CheckContent = new GUIContent("✔");
        private static readonly GUIContent AddContent = new GUIContent("＋", "入力した色をカスタムラベルとして追加");
        private static readonly GUIContent ClearContent = new GUIContent("ラベルを外す");
        private static readonly GUIContent SettingsContent = new GUIContent("設定…");
        private static readonly GUIContent EmptyContent = new GUIContent(
            "有効なプリセットがありません。設定からプリセットを有効にしてください。");
        private static readonly Color ProHover = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color PersonalHover = new Color(0f, 0f, 0f, 0.08f);

        private string[] _projectGuids;
        private GameObject[] _hierarchyObjects;
        private string _currentLabelId;
        private string _hexInput = "";
        private IReadOnlyList<IrodoriLabelSection> _sections;
        private Dictionary<string, GUIContent> _rowContents;
        private Dictionary<string, GUIContent> _swatchContents;
        private Vector2 _scrollPosition;
        private float _contentHeight;

        internal static void OpenProject(string[] guids)
        {
            var window = CreateInstance<IrodoriLabelPopup>();
            window.titleContent = TitleContent;
            window._projectGuids = guids;
            window._currentLabelId = IrodoriMenu.GetCommonProjectLabel(guids);
            window.Prepare();
            window.ShowAtContextPosition();
        }

        internal static void OpenHierarchy(GameObject[] objects)
        {
            var window = CreateInstance<IrodoriLabelPopup>();
            window.titleContent = TitleContent;
            window._hierarchyObjects = objects;
            window._currentLabelId = IrodoriMenu.GetCommonHierarchyLabel(objects);
            window.Prepare();
            window.ShowAtContextPosition();
        }

        private void Prepare()
        {
            _sections = IrodoriLabelResolver.GetVisibleSections();
            _rowContents = new Dictionary<string, GUIContent>();
            _swatchContents = new Dictionary<string, GUIContent>();

            float height = Padding;
            if (_sections.Count == 0)
            {
                height += 42f;
            }
            else
            {
                for (int i = 0; i < _sections.Count; i++)
                {
                    IrodoriLabelSection section = _sections[i];
                    height += Gap + HeaderHeight;
                    if (section.PresetId == "basic")
                    {
                        int rows = (section.Labels.Length + BasicColumns - 1) / BasicColumns;
                        height += rows * (SwatchHeight + Gap);
                    }
                    else
                    {
                        height += section.Labels.Length * RowHeight;
                    }

                    for (int j = 0; j < section.Labels.Length; j++)
                    {
                        IrodoriLabel label = section.Labels[j];
                        _rowContents[label.Id] = new GUIContent(label.Name);
                        _swatchContents[label.Id] = new GUIContent(string.Empty, label.Name);
                    }
                }
            }

            _contentHeight = height + Padding;
        }

        private void ShowAtContextPosition()
        {
            Vector2 point = IrodoriDrawer.ContextScreenPosition;
            var anchor = new Rect(point.x, point.y, 1f, 1f);
            float requiredHeight = _contentHeight + FooterHeight;
            ShowAsDropDown(anchor, new Vector2(PopupWidth, Mathf.Min(requiredHeight, MaxPopupHeight)));
        }

        private void OnGUI()
        {
            float scrollHeight = position.height - FooterHeight;
            var scrollRect = new Rect(0f, 0f, position.width, scrollHeight);
            // NOTE: 縦バーが出ると表示幅が狭くなるため、その分だけ詰めて横バーを出さない。
            bool needsVerticalScroll = _contentHeight > scrollHeight;
            float contentWidth = position.width - (needsVerticalScroll ? ScrollBarWidth : 0f);
            var contentRect = new Rect(0f, 0f, contentWidth, _contentHeight);
            _scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, contentRect);
            DrawSections(contentRect.width);
            GUI.EndScrollView();

            var separatorRect = new Rect(Padding, scrollHeight + 1f, position.width - Padding * 2f, 1f);
            EditorGUI.DrawRect(separatorRect, EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.16f)
                : new Color(0f, 0f, 0f, 0.16f));
            DrawFooter(scrollHeight + 6f);
        }

        private void DrawSections(float width)
        {
            float y = Padding;
            if (_sections.Count == 0)
            {
                GUI.Label(new Rect(Padding, y, width - Padding * 2f, 42f), EmptyContent, EditorStyles.wordWrappedMiniLabel);
                return;
            }

            for (int i = 0; i < _sections.Count; i++)
            {
                IrodoriLabelSection section = _sections[i];
                y += Gap;
                GUI.Label(new Rect(Padding, y, width - Padding * 2f, HeaderHeight), section.Title, EditorStyles.boldLabel);
                y += HeaderHeight;

                if (section.PresetId == "basic")
                {
                    y = DrawBasicSection(section, y, width);
                }
                else
                {
                    y = DrawListSection(section, y, width);
                }
            }
        }

        private float DrawBasicSection(IrodoriLabelSection section, float y, float width)
        {
            float cellWidth = (width - Padding * 2f - Gap * (BasicColumns - 1)) / BasicColumns;
            Color selectedBorder = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            for (int i = 0; i < section.Labels.Length; i++)
            {
                int column = i % BasicColumns;
                int row = i / BasicColumns;
                var cellRect = new Rect(
                    Padding + column * (cellWidth + Gap),
                    y + row * (SwatchHeight + Gap),
                    cellWidth,
                    SwatchHeight);
                IrodoriLabel label = section.Labels[i];

                EditorGUI.DrawRect(cellRect, label.Color);
                if (label.Id == _currentLabelId)
                {
                    DrawBorder(cellRect, selectedBorder, 2f);
                }

                if (GUI.Button(cellRect, _swatchContents[label.Id], GUIStyle.none))
                {
                    ApplyAndClose(label.Id);
                }
            }

            int rows = (section.Labels.Length + BasicColumns - 1) / BasicColumns;
            return y + rows * (SwatchHeight + Gap);
        }

        private float DrawListSection(IrodoriLabelSection section, float y, float width)
        {
            Color hoverColor = EditorGUIUtility.isProSkin ? ProHover : PersonalHover;
            Vector2 mousePosition = Event.current.mousePosition;
            for (int i = 0; i < section.Labels.Length; i++)
            {
                IrodoriLabel label = section.Labels[i];
                var rowRect = new Rect(Padding, y, width - Padding * 2f, RowHeight);
                if (rowRect.Contains(mousePosition))
                {
                    EditorGUI.DrawRect(rowRect, hoverColor);
                }

                if (label.Id == _currentLabelId)
                {
                    GUI.Label(new Rect(rowRect.x, rowRect.y, 16f, RowHeight), CheckContent, EditorStyles.miniLabel);
                }

                EditorGUI.DrawRect(new Rect(rowRect.x + 18f, rowRect.y + 3f, 20f, 14f), label.Color);
                GUI.Label(new Rect(rowRect.x + 42f, rowRect.y, rowRect.width - 42f, RowHeight),
                    _rowContents[label.Id], EditorStyles.label);

                if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
                {
                    ApplyAndClose(label.Id);
                }

                y += RowHeight;
            }

            return y;
        }

        private void DrawFooter(float top)
        {
            float width = position.width - Padding * 2f;
            var inputRect = new Rect(Padding, top, width - 28f, RowHeight);
            var addRect = new Rect(inputRect.xMax + Gap, top, 24f, RowHeight);
            _hexInput = EditorGUI.TextField(inputRect, _hexInput);
            if (GUI.Button(addRect, AddContent))
            {
                AddCustomAndApply();
            }

            top += RowHeight + Gap;
            if (GUI.Button(new Rect(Padding, top, width, 22f), ClearContent))
            {
                Clear();
                Close();
                GUIUtility.ExitGUI();
            }

            top += 22f + Gap;
            if (GUI.Button(new Rect(Padding, top, width, 22f), SettingsContent))
            {
                SettingsService.OpenProjectSettings(IrodoriInfo.SettingsPath);
                Close();
                GUIUtility.ExitGUI();
            }
        }

        private void AddCustomAndApply()
        {
            string input = _hexInput.Trim();
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
            IrodoriSettings settings = IrodoriSettings.instance;
            IrodoriCustomLabel custom = settings.AddCustomLabel(hex, hex);
            if (custom == null)
            {
                return;
            }

            settings.SaveChanges();
            IrodoriDrawer.Invalidate();
            EditorApplication.RepaintProjectWindow();
            EditorApplication.RepaintHierarchyWindow();
            ApplyAndClose(custom.id);
        }

        private void ApplyAndClose(string labelId)
        {
            Apply(labelId);
            Close();
            GUIUtility.ExitGUI();
        }

        private void Apply(string labelId)
        {
            if (_projectGuids != null)
            {
                IrodoriMenu.ApplyProject(_projectGuids, labelId);
            }
            else if (_hierarchyObjects != null)
            {
                IrodoriMenu.ApplyHierarchy(_hierarchyObjects, labelId);
            }
        }

        private void Clear()
        {
            if (_projectGuids != null)
            {
                IrodoriMenu.ClearProject(_projectGuids);
            }
            else if (_hierarchyObjects != null)
            {
                IrodoriMenu.ClearHierarchy(_hierarchyObjects);
            }
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
    }
}
