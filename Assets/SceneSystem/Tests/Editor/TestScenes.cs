using System;
using System.Linq;
using System.Reflection;

namespace RadTadDev.SceneSystem.Tests
{
    /// <summary>
    /// Builds scene references for tests. The constructor of <see cref="SceneReference" /> is private and only
    /// Unity's serializer normally creates it, so tests set the name through reflection.
    /// </summary>
    internal static class TestScenes
    {
        /// <summary>
        /// Creates a reference with the given name.
        /// </summary>
        public static SceneReference Ref(string name)
        {
            var reference = Blank();
            typeof(SceneReference)
                .GetField("_sceneName", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(reference, name);
            return reference;
        }

        /// <summary>
        /// Creates a reference with no name set, so it keeps the field's default value.
        /// </summary>
        public static SceneReference Blank()
        {
            return (SceneReference)Activator.CreateInstance(typeof(SceneReference), true);
        }

        /// <summary>
        /// Creates references for each name, in order.
        /// </summary>
        public static SceneReference[] Refs(params string[] names)
        {
            return names.Select(Ref).ToArray();
        }
    }
}
