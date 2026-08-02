using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>選択対象への色の設定と解除を提供する。</summary>
    internal static class IrodoriMenu
    {
        [MenuItem(IrodoriInfo.AssetMenuRoot + "Set Color…", false, 1100)]
        private static void ShowProjectPopup()
        {
            string[] guids = Selection.assetGUIDs;
            if (guids.Length > 0)
            {
                IrodoriColorPopup.OpenProject(guids);
            }
        }

        [MenuItem(IrodoriInfo.AssetMenuRoot + "Set Color…", true)]
        private static bool ValidateShowProjectPopup()
        {
            return Selection.assetGUIDs.Length > 0;
        }

        [MenuItem(IrodoriInfo.AssetMenuRoot + "Clear Color", false, 1101)]
        private static void ClearProjectColor()
        {
            ClearProject(Selection.assetGUIDs);
        }

        [MenuItem(IrodoriInfo.AssetMenuRoot + "Clear Color", true)]
        private static bool ValidateClearProjectColor()
        {
            return Selection.assetGUIDs.Length > 0;
        }

        [MenuItem(IrodoriInfo.GameObjectMenuRoot + "Set Color…", false, 30)]
        private static void ShowHierarchyPopup(MenuCommand command)
        {
            if (IsDuplicateInvocation(command))
            {
                return;
            }

            GameObject[] objects = Selection.gameObjects;
            if (objects.Length > 0)
            {
                IrodoriColorPopup.OpenHierarchy(objects);
            }
        }

        [MenuItem(IrodoriInfo.GameObjectMenuRoot + "Clear Color", false, 31)]
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

                GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                if (id.identifierType == 0)
                {
                    foundUnstableId = true;
                    continue;
                }

                changed |= Upsert(settings.sceneAssignments, id.ToString(), labelId);

                // NOTE: 子への着色で意図せずアセットを塗らないよう、Prefab インスタンスのルートだけを同期する。
                if (PrefabUtility.GetNearestPrefabInstanceRoot(obj) != obj)
                {
                    continue;
                }

                string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                if (settings.projectAssignments == null)
                {
                    settings.projectAssignments = new List<IrodoriAssignment>();
                }

                changed |= Upsert(settings.projectAssignments, guid, labelId);
            }

            if (foundUnstableId)
            {
                Debug.LogWarning("Irodori Colorizer: シーンを保存してから色を設定してください。");
            }

            SaveAndRefresh(changed);
        }

        internal static void ClearHierarchy(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                return;
            }

            List<IrodoriAssignment> assignments = IrodoriSettings.instance.sceneAssignments;
            if (assignments == null || assignments.Count == 0)
            {
                return;
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

                GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                if (id.identifierType == 0)
                {
                    foundUnstableId = true;
                    continue;
                }

                string key = id.ToString();
                for (int j = assignments.Count - 1; j >= 0; j--)
                {
                    IrodoriAssignment assignment = assignments[j];
                    if (assignment != null && assignment.key == key)
                    {
                        assignments.RemoveAt(j);
                        changed = true;
                    }
                }
            }

            if (foundUnstableId)
            {
                Debug.LogWarning("Irodori Colorizer: シーンを保存してから色を解除してください。");
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

            GlobalObjectId firstId = GlobalObjectId.GetGlobalObjectIdSlow(objects[0]);
            if (firstId.identifierType == 0)
            {
                return null;
            }

            List<IrodoriAssignment> assignments = IrodoriSettings.instance.sceneAssignments;
            string common = FindLabel(assignments, firstId.ToString());
            if (string.IsNullOrEmpty(common))
            {
                return null;
            }

            for (int i = 1; i < objects.Length; i++)
            {
                if (objects[i] == null)
                {
                    return null;
                }

                GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(objects[i]);
                if (id.identifierType == 0 || FindLabel(assignments, id.ToString()) != common)
                {
                    return null;
                }
            }

            return common;
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
