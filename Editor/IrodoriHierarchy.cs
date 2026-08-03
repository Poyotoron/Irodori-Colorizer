using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>Hierarchy 固有の GlobalObjectId キャッシュと描画フック。</summary>
    [InitializeOnLoad]
    internal static class IrodoriHierarchy
    {
        private static readonly Dictionary<int, IrodoriLabel> Map = new Dictionary<int, IrodoriLabel>();
        private static bool _dirty = true;

        static IrodoriHierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItem;
            EditorApplication.hierarchyChanged += MarkDirty;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        }

        internal static void Invalidate()
        {
            _dirty = true;
        }

        private static void MarkDirty()
        {
            _dirty = true;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            _dirty = true;
        }

        private static void OnSceneClosed(Scene scene)
        {
            _dirty = true;
        }

        private static void OnPrefabStageOpened(PrefabStage stage)
        {
            _dirty = true;
        }

        private static void OnPrefabStageClosing(PrefabStage stage)
        {
            _dirty = true;
        }

        private static void OnHierarchyItem(int instanceID, Rect selectionRect)
        {
            IrodoriDrawer.CaptureContextClick(selectionRect);

            IrodoriSettings settings = IrodoriSettings.instance;
            // NOTE: Hierarchy は行数が多いため、無効時はキャッシュの確認すら行わない。
            if (!settings.enabled || !settings.paintHierarchy)
            {
                return;
            }

            if (_dirty)
            {
                Rebuild();
            }

            if (Map.Count == 0 || !Map.TryGetValue(instanceID, out IrodoriLabel label))
            {
                return;
            }

            // NOTE: シーン見出しは GameObject ではないため除外する。将来は EntityId 系 API への置換が必要になる。
            var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null)
            {
                return;
            }

            bool selected = System.Array.IndexOf(Selection.instanceIDs, instanceID) >= 0;
            bool inactive = !obj.activeInHierarchy;
            IrodoriDrawer.DrawHierarchyRow(selectionRect, label.Color, selected, inactive, obj);
        }

        private static void Rebuild()
        {
            _dirty = false;
            Map.Clear();

            IrodoriSettings settings = IrodoriSettings.instance;
            List<IrodoriAssignment> assignments = settings.sceneAssignments;
            int sceneAssignmentCount = assignments != null ? assignments.Count : 0;
            bool hasProjectAssignments = settings.projectAssignments != null && settings.projectAssignments.Count > 0;
            if (sceneAssignmentCount == 0 && !hasProjectAssignments)
            {
                return;
            }

            var assignmentMap = new Dictionary<string, string>(sceneAssignmentCount, System.StringComparer.Ordinal);
            for (int i = 0; i < sceneAssignmentCount; i++)
            {
                IrodoriAssignment assignment = assignments[i];
                if (assignment == null || string.IsNullOrEmpty(assignment.key) || string.IsNullOrEmpty(assignment.labelId))
                {
                    continue;
                }

                assignmentMap[assignment.key] = assignment.labelId;
            }

            if (assignmentMap.Count == 0 && !hasProjectAssignments)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            // NOTE: 新しい Unity では旧検索 API が非推奨のため、非アクティブを含む無ソート検索を使う。
            GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            GameObject[] objects = Object.FindObjectsOfType<GameObject>(true);
#endif
            var ids = new GlobalObjectId[objects.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(objects, ids);
            Dictionary<string, string> assetGuidCache = hasProjectAssignments
                ? new Dictionary<string, string>(System.StringComparer.Ordinal)
                : null;

            for (int i = 0; i < objects.Length; i++)
            {
                string sceneKey = ids[i].identifierType != 0 ? ids[i].ToString() : null;
                if (!IrodoriMenu.TryGetHierarchyLabelId(
                        objects[i],
                        sceneKey,
                        assignmentMap,
                        assetGuidCache,
                        hasProjectAssignments,
                        out string labelId))
                {
                    continue;
                }

                if (!IrodoriLabelResolver.TryResolve(labelId, out IrodoriLabel label))
                {
                    continue;
                }

                // NOTE: Unity 2022.3 との互換性を保つため InstanceID を使うが、将来は EntityId 系 API への置換が必要になる。
                Map[objects[i].GetInstanceID()] = label;
            }
        }
    }
}
