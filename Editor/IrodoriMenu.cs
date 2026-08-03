using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>選択対象へのラベルの設定と解除を提供する。</summary>
    internal static class IrodoriMenu
    {
        [MenuItem(IrodoriInfo.AssetMenuRoot + "Set Label…", false, 1100)]
        private static void ShowProjectPopup()
        {
            string[] guids = Selection.assetGUIDs;
            if (guids.Length > 0)
            {
                IrodoriLabelPopup.OpenProject(guids);
            }
        }

        [MenuItem(IrodoriInfo.AssetMenuRoot + "Set Label…", true)]
        private static bool ValidateShowProjectPopup()
        {
            return Selection.assetGUIDs.Length > 0;
        }

        [MenuItem(IrodoriInfo.AssetMenuRoot + "Clear Label", false, 1101)]
        private static void ClearProjectColor()
        {
            ClearProject(Selection.assetGUIDs);
        }

        [MenuItem(IrodoriInfo.AssetMenuRoot + "Clear Label", true)]
        private static bool ValidateClearProjectColor()
        {
            return Selection.assetGUIDs.Length > 0;
        }

        [MenuItem(IrodoriInfo.GameObjectMenuRoot + "Set Label…", false, 30)]
        private static void ShowHierarchyPopup(MenuCommand command)
        {
            if (IsDuplicateInvocation(command))
            {
                return;
            }

            GameObject[] objects = Selection.gameObjects;
            if (objects.Length > 0)
            {
                IrodoriLabelPopup.OpenHierarchy(objects);
            }
        }

        [MenuItem(IrodoriInfo.GameObjectMenuRoot + "Clear Label", false, 31)]
        private static void ClearHierarchyColor(MenuCommand command)
        {
            if (IsDuplicateInvocation(command))
            {
                return;
            }

            ClearHierarchy(Selection.gameObjects);
        }

        private static bool IsDuplicateInvocation(MenuCommand command)
        {
            Object[] objects = Selection.objects;
            return objects.Length > 1 && command.context != null && command.context != objects[0];
        }

        internal static void ApplyProject(string[] guids, string labelId)
        {
            if (guids == null || guids.Length == 0)
            {
                return;
            }

            IrodoriSettings settings = IrodoriSettings.instance;
            if (settings.projectAssignments == null)
            {
                settings.projectAssignments = new List<IrodoriAssignment>();
            }

            bool changed = false;
            for (int i = 0; i < guids.Length; i++)
            {
                if (!string.IsNullOrEmpty(guids[i]))
                {
                    changed |= Upsert(settings.projectAssignments, guids[i], labelId);
                }
            }

            SaveAndRefresh(changed);
        }

        internal static void ClearProject(string[] guids)
        {
            if (guids == null || guids.Length == 0)
            {
                return;
            }

            List<IrodoriAssignment> assignments = IrodoriSettings.instance.projectAssignments;
            if (assignments == null || assignments.Count == 0)
            {
                return;
            }

            bool changed = false;
            for (int i = assignments.Count - 1; i >= 0; i--)
            {
                IrodoriAssignment assignment = assignments[i];
                if (assignment != null && System.Array.IndexOf(guids, assignment.key) >= 0)
                {
                    assignments.RemoveAt(i);
                    changed = true;
                }
            }

            SaveAndRefresh(changed);
        }

        internal static void ApplyHierarchy(GameObject[] objects, string labelId)
        {
            if (objects == null || objects.Length == 0)
            {
                return;
            }

            IrodoriSettings settings = IrodoriSettings.instance;
            if (settings.sceneAssignments == null)
            {
                settings.sceneAssignments = new List<IrodoriAssignment>();
            }

            bool changed = false;
            bool foundUnstableId = false;
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj == null)
                {
                    continue;
                }

                if (TryGetPrefabAssetGuid(obj, null, out string guid))
                {
                    if (settings.projectAssignments == null)
                    {
                        settings.projectAssignments = new List<IrodoriAssignment>();
                    }

                    changed |= Upsert(settings.projectAssignments, guid, labelId);

                    if (settings.sceneAssignments.Count > 0)
                    {
                        GlobalObjectId prefabId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                        if (prefabId.identifierType != 0)
                        {
                            changed |= RemoveAssignment(settings.sceneAssignments, prefabId.ToString());
                        }
                    }

                    continue;
                }

                GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                if (id.identifierType == 0)
                {
                    foundUnstableId = true;
                    continue;
                }

                changed |= Upsert(settings.sceneAssignments, id.ToString(), labelId);
            }

            if (foundUnstableId)
            {
                Debug.LogWarning("Irodori Colorizer: シーンを保存してからラベルを設定してください。");
            }

            SaveAndRefresh(changed);
        }

        internal static void ClearHierarchy(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                return;
            }

            IrodoriSettings settings = IrodoriSettings.instance;
            bool changed = false;
            bool foundUnstableId = false;
            var assetGuids = new HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj == null)
                {
                    continue;
                }

                GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                if (id.identifierType != 0 && RemoveAssignment(settings.sceneAssignments, id.ToString()))
                {
                    changed = true;
                    continue;
                }

                bool isPrefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(obj) == obj;
                if (isPrefabRoot &&
                    TryGetPrefabAssetGuid(obj, null, out string guid) &&
                    !string.IsNullOrEmpty(FindLabel(settings.projectAssignments, guid)))
                {
                    assetGuids.Add(guid);
                }
                else if (id.identifierType == 0 && !isPrefabRoot)
                {
                    foundUnstableId = true;
                }
            }

            if (assetGuids.Count > 0 && EditorUtility.DisplayDialog(
                    "ラベルの解除",
                    "この色は元の Prefab アセットに付いています。解除すると Project の表示と、シーン上の他のインスタンスからも色が消えます。",
                    "解除",
                    "キャンセル"))
            {
                foreach (string guid in assetGuids)
                {
                    changed |= RemoveAssignment(settings.projectAssignments, guid);
                }
            }

            if (foundUnstableId)
            {
                Debug.LogWarning("Irodori Colorizer: シーンを保存してからラベルを解除してください。");
            }

            SaveAndRefresh(changed);
        }

        internal static string GetCommonProjectLabel(string[] guids)
        {
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            List<IrodoriAssignment> assignments = IrodoriSettings.instance.projectAssignments;
            string common = FindLabel(assignments, guids[0]);
            if (string.IsNullOrEmpty(common))
            {
                return null;
            }

            for (int i = 1; i < guids.Length; i++)
            {
                if (FindLabel(assignments, guids[i]) != common)
                {
                    return null;
                }
            }

            return common;
        }

        internal static string GetCommonHierarchyLabel(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0 || objects[0] == null)
            {
                return null;
            }

            if (!TryGetHierarchyLabelId(objects[0], out string common))
            {
                return null;
            }

            for (int i = 1; i < objects.Length; i++)
            {
                if (objects[i] == null)
                {
                    return null;
                }

                if (!TryGetHierarchyLabelId(objects[i], out string labelId) || labelId != common)
                {
                    return null;
                }
            }

            return common;
        }

        internal static bool TryGetHierarchyLabelId(GameObject obj, out string labelId)
        {
            labelId = null;
            if (obj == null)
            {
                return false;
            }

            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            string sceneKey = id.identifierType != 0 ? id.ToString() : null;
            IrodoriSettings settings = IrodoriSettings.instance;
            bool hasProjectAssignments = settings.projectAssignments != null && settings.projectAssignments.Count > 0;
            return TryGetHierarchyLabelId(
                obj,
                sceneKey,
                null,
                null,
                hasProjectAssignments,
                out labelId);
        }

        internal static bool TryGetHierarchyLabelId(
            GameObject obj,
            string sceneKey,
            Dictionary<string, string> sceneAssignmentMap,
            Dictionary<string, string> assetGuidCache,
            bool hasProjectAssignments,
            out string labelId)
        {
            labelId = null;
            if (!string.IsNullOrEmpty(sceneKey))
            {
                bool foundSceneLabel = sceneAssignmentMap != null
                    ? sceneAssignmentMap.TryGetValue(sceneKey, out labelId)
                    : TryFindLabel(IrodoriSettings.instance.sceneAssignments, sceneKey, out labelId);
                if (foundSceneLabel)
                {
                    return true;
                }
            }

            if (!hasProjectAssignments || !TryGetPrefabAssetGuid(obj, assetGuidCache, out string guid))
            {
                labelId = null;
                return false;
            }

            return IrodoriDrawer.TryGetProjectLabelId(guid, out labelId);
        }

        private static bool TryGetPrefabAssetGuid(
            GameObject obj,
            Dictionary<string, string> assetGuidCache,
            out string guid)
        {
            guid = null;
            // NOTE: 入れ子の Prefab は各ルートが自身の元アセットの色を示すため、最も近いルートで判定する。
            if (obj == null || PrefabUtility.GetNearestPrefabInstanceRoot(obj) != obj)
            {
                return false;
            }

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (assetGuidCache != null && assetGuidCache.TryGetValue(assetPath, out guid))
            {
                return !string.IsNullOrEmpty(guid);
            }

            guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (assetGuidCache != null)
            {
                assetGuidCache[assetPath] = guid;
            }

            return !string.IsNullOrEmpty(guid);
        }

        private static bool Upsert(List<IrodoriAssignment> assignments, string key, string labelId)
        {
            int foundIndex = -1;
            bool changed = false;
            for (int i = assignments.Count - 1; i >= 0; i--)
            {
                IrodoriAssignment assignment = assignments[i];
                if (assignment == null || assignment.key != key)
                {
                    continue;
                }

                if (foundIndex < 0)
                {
                    foundIndex = i;
                    if (assignment.labelId != labelId)
                    {
                        assignment.labelId = labelId;
                        changed = true;
                    }
                }
                else
                {
                    assignments.RemoveAt(i);
                    foundIndex--;
                    changed = true;
                }
            }

            if (foundIndex >= 0)
            {
                return changed;
            }

            assignments.Add(new IrodoriAssignment { key = key, labelId = labelId });
            return true;
        }

        private static string FindLabel(List<IrodoriAssignment> assignments, string key)
        {
            if (assignments == null)
            {
                return null;
            }

            for (int i = assignments.Count - 1; i >= 0; i--)
            {
                IrodoriAssignment assignment = assignments[i];
                if (assignment != null && assignment.key == key)
                {
                    return assignment.labelId;
                }
            }

            return null;
        }

        private static bool TryFindLabel(
            List<IrodoriAssignment> assignments,
            string key,
            out string labelId)
        {
            labelId = FindLabel(assignments, key);
            return !string.IsNullOrEmpty(labelId);
        }

        private static bool RemoveAssignment(List<IrodoriAssignment> assignments, string key)
        {
            if (assignments == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            bool removed = false;
            for (int i = assignments.Count - 1; i >= 0; i--)
            {
                IrodoriAssignment assignment = assignments[i];
                if (assignment != null && assignment.key == key)
                {
                    assignments.RemoveAt(i);
                    removed = true;
                }
            }

            return removed;
        }

        private static void SaveAndRefresh(bool changed)
        {
            if (!changed)
            {
                return;
            }

            IrodoriSettings.instance.SaveChanges();
            IrodoriDrawer.Invalidate();
            EditorApplication.RepaintProjectWindow();
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
