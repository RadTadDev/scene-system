using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// Loads, unloads, and finalizes scenes. The scene system picks the manager for each transaction.
    /// </summary>
    public interface ISceneManager
    {
        /// <summary>
        /// Loads a scene.
        /// </summary>
        /// <param name="scene">The scene to load.</param>
        /// <param name="mode">The mode to load the scene in.</param>
        /// <returns>The loaded scene.</returns>
        public Task<Scene> LoadAsync(SceneReference scene, LoadSceneMode mode = LoadSceneMode.Additive);

        /// <summary>
        /// Unloads a scene.
        /// </summary>
        /// <param name="scene">The scene to unload.</param>
        public Task UnloadAsync(SceneReference scene);

        /// <summary>
        /// Called after a loaded scene has been installed. A manager can use this to wait for work
        /// beyond the local installation.
        /// </summary>
        /// <param name="scene">The scene that was installed.</param>
        public Task OnInstalledAsync(Scene scene);

        /// <summary>
        /// Called after a scene has been uninstalled and before it is unloaded. A manager can use this to
        /// wait for work beyond the local uninstallation.
        /// </summary>
        /// <param name="scene">The scene that was uninstalled.</param>
        public Task OnUninstalledAsync(Scene scene);
    }
}
