using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// A serializable scene reference that works in the Editor and in builds.
    /// </summary>
    [Serializable]
    public class SceneReference
    {
        [SerializeField] private Object _sceneAsset;

        [SerializeField] private string _sceneName = "Missing";

        // Disallow inline construction
        private SceneReference() { }

        /// <summary>
        /// The name of the scene.
        /// </summary>
        public string Name => _sceneName;

        /// <summary>
        /// Treats this scene reference as a scene name string.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <returns>The scene name.</returns>
        public static implicit operator string(SceneReference reference)
        {
            return reference.Name;
        }

        /// <summary>
        /// Tests for equality with another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a scene reference and has the same name.</returns>
        public override bool Equals(object obj)
        {
            return obj is SceneReference sceneReference && Name == sceneReference.Name;
        }

        /// <summary>
        /// Tests for equality against another <see cref="SceneReference" />.
        /// </summary>
        /// <param name="other">The other scene reference.</param>
        /// <returns>True if the other scene reference has the same name.</returns>
        protected bool Equals(SceneReference other)
        {
            return _sceneName == other._sceneName;
        }

        /// <summary>
        /// Gets a hashcode for this reference.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return _sceneName.GetHashCode();
        }

        /// <summary>
        /// Converts this reference to its scene name.
        /// </summary>
        /// <returns>The scene name.</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
