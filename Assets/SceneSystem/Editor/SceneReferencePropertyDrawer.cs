using System;
using UnityEditor;
using UnityEngine;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// Draws a scene reference.
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferencePropertyDrawer : PropertyDrawer
    {
        private const string AssetFieldName = "_sceneAsset";
        private const string NamedFieldName = "_sceneName";

        // Must match the default of SceneReference._sceneName.
        private const string MissingName = "Missing";

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var sceneAsset = property.FindPropertyRelative(AssetFieldName);
            var sceneName = property.FindPropertyRelative(NamedFieldName);

            if (sceneAsset == null)
            {
                throw new NullReferenceException(
                    $"No property '{AssetFieldName}' was found in type {nameof(SceneReference)}");
            }

            if (sceneName == null)
            {
                throw new NullReferenceException(
                    $"No property '{NamedFieldName}' was found in type {nameof(SceneReference)}");
            }

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            sceneAsset.objectReferenceValue =
                EditorGUI.ObjectField(position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);

            sceneName.stringValue = sceneAsset.objectReferenceValue != null
                ? sceneAsset.objectReferenceValue.name
                : MissingName;

            EditorGUI.EndProperty();
        }
    }
}
