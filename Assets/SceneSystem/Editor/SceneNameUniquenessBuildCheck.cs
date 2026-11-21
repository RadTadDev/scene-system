using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// Checks that scene names are unique.
    /// </summary>
    public class SceneNameUniquenessBuildCheck : IPreprocessBuildWithReport
    {
        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            var seen = new Dictionary<string, string>();
            var duplicates = new List<string>();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled)
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(scene.path);

                if (seen.TryGetValue(name, out var existingPath))
                {
                    duplicates.Add($"'{name}' — '{existingPath}' and '{scene.path}'");
                }
                else
                {
                    seen[name] = scene.path;
                }
            }

            if (duplicates.Count > 0)
            {
                throw new BuildFailedException(
                    $"Duplicate scene names in Build Settings: {string.Join(", ", duplicates)}");
            }
        }
    }
}
