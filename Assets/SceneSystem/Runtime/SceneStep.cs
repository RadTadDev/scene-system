using UnityEngine.SceneManagement;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// One scene inside a transaction, with the manager that loads or unloads it. Once the scene is loaded,
    /// <see cref="LoadedScene" /> holds its handle.
    /// </summary>
    internal struct SceneStep
    {
        /// <summary>
        /// The scene.
        /// </summary>
        public readonly SceneReference Scene;

        /// <summary>
        /// The manager used for this step.
        /// </summary>
        public ISceneManager Manager { get; private set; }

        /// <summary>
        /// The loaded scene handle. Default until the scene has loaded.
        /// </summary>
        public readonly Scene LoadedScene;

        /// <summary>
        /// Creates a new <see cref="SceneStep" />.
        /// </summary>
        /// <param name="scene">The scene this step handles.</param>
        /// <param name="manager">The manager that handles this step.</param>
        /// <param name="loadedScene">The loaded scene handle, if the scene is already loaded.</param>
        public SceneStep(SceneReference scene, ISceneManager manager, Scene loadedScene = default)
        {
            Scene = scene;
            Manager = manager;
            LoadedScene = loadedScene;
        }
    }
}
