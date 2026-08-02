using UnityEditor;
using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>固定パレットから色を選ぶ簡易ポップアップ。</summary>
    internal sealed class IrodoriColorPopup : EditorWindow
    {
        private const float PopupWidth = 260f;
        private const float PopupHeight = 244f;
        private const float Padding = 10f;
        private const float Gap = 4f;
        private const float HeaderHeight = 20f;
        private const float CellHeight = 44f;
        private const int Columns = 4;

        private static readonly GUIContent TitleContent = new GUIContent("色を選択");
        private static readonly GUIContent[] SwatchContents = BuildSwatchContents();
        private static readonly Color ProCellBackground = new Color(0.26f, 0.26f, 0.26f);
        private static readonly Color PersonalCellBackground = new Color(0.82f, 0.82f, 0.82f);
        private static readonly Color HoverBorder = new Color(0.35f, 0.65f, 1f);

        private string[] _projectGuids;
        private GameObject[] _hierarchyObjects;
        private string _currentLabelId;

        internal static void OpenProject(string[] guids)
        {
            var window = CreateInstance<IrodoriColorPopup>();
            window.titleContent = TitleContent;
            window._projectGuids = guids;
            window._currentLabelId = IrodoriMenu.GetCommonProjectLabel(guids);
            window.ShowAtContextPosition();
        }

        internal static void OpenHierarchy(GameObject[] objects)
        {
            var window = CreateInstance<IrodoriColorPopup>();
            window.titleContent = TitleContent;
            window._hierarchyObjects = objects;
            window._currentLabelId = IrodoriMenu.GetCommonHierarchyLabel(objects);
            window.ShowAtContextPosition();
        }

        private static GUIContent[] BuildSwatchContents()
        {
            var contents = new GUIContent[IrodoriPalette.Labels.Length];
            for (int i = 0; i < contents.Length; i++)
            {
                contents[i] = new GUIContent(string.Empty, IrodoriPalette.Labels[i].Name);
            }

            return contents;
        }

        private void ShowAtContextPosition()
        {
            Vector2 point = IrodoriDrawer.ContextScreenPosition;
            var anchor = new Rect(point.x, point.y, 1f, 1f);
            ShowAsDropDown(anchor, new Vector2(PopupWidth, PopupHeight));
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(Padding, Padding - 2f, PopupWidth - Padding * 2f, HeaderHeight), TitleContent, EditorStyles.boldLabel);

            float gridTop = Padding + HeaderHeight;
            float cellWidth = (PopupWidth - Padding * 2f - Gap * (Columns - 1)) / Columns;
            Color cellBackground = EditorGUIUtility.isProSkin ? ProCellBackground : PersonalCellBackground;
            Color selectedBorder = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            Vector2 mousePosition = Event.current.mousePosition;

            for (int i = 0; i < IrodoriPalette.Labels.Length; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var cellRect = new Rect(
                    Padding + column * (cellWidth + Gap),
                    gridTop + row * (CellHeight + Gap),
                    cellWidth,
                    CellHeight);

                bool selected = IrodoriPalette.Labels[i].Id == _currentLabelId;
                bool hovered = cellRect.Contains(mousePosition);
                EditorGUI.DrawRect(cellRect, selected ? selectedBorder : hovered ? HoverBorder : cellBackground);

                float border = selected ? 3f : hovered ? 2f : 1f;
                var innerRect = new Rect(
                    cellRect.x + border,
                    cellRect.y + border,
                    cellRect.width - border * 2f,
                    cellRect.height - border * 2f);
                EditorGUI.DrawRect(innerRect, cellBackground);

                var swatchRect = new Rect(innerRect.x + 4f, innerRect.y + 4f, innerRect.width - 8f, innerRect.height - 8f);
                EditorGUI.DrawRect(swatchRect, IrodoriPalette.Labels[i].Color);

                if (GUI.Button(cellRect, SwatchContents[i], GUIStyle.none))
                {
                    Apply(IrodoriPalette.Labels[i].Id);
                    Close();
                    GUIUtility.ExitGUI();
                }
            }

            int rows = (IrodoriPalette.Labels.Length + Columns - 1) / Columns;
            float buttonTop = gridTop + rows * (CellHeight + Gap) + 2f;
            var clearRect = new Rect(Padding, buttonTop, PopupWidth - Padding * 2f, 24f);
            if (GUI.Button(clearRect, "色を外す"))
            {
                Clear();
                Close();
                GUIUtility.ExitGUI();
            }
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
    }
}
