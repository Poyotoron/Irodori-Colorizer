using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Poyo.IrodoriColorizer.Editor
{
    /// <summary>対象キーとラベル ID の対応。</summary>
    [Serializable]
    internal sealed class IrodoriAssignment
    {
        public string key;
        public string labelId;
    }

    // NOTE: ProjectSettings に保存すると、利用者の Assets やビルド成果物を汚さずにチーム共有できる。
    [FilePath("ProjectSettings/IrodoriColorizer.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class IrodoriSettings : ScriptableSingleton<IrodoriSettings>
    {
        public int schemaVersion = 1;

        public bool enabled = true;
        public bool paintProject = true;
        public bool paintHierarchy = true;
        public float fillAlpha = 0.32f;
        public bool paintFullRow = true;
        public bool autoTextColor = true;
        public Color forcedTextColor = Color.white;
        public bool keepSelectionVisible = true;
        public float labelIndent = 18f;

        public List<IrodoriAssignment> projectAssignments = new List<IrodoriAssignment>();
        public List<IrodoriAssignment> sceneAssignments = new List<IrodoriAssignment>();

        public void SaveChanges()
        {
            Save(true);
        }
    }
}
