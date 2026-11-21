using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// A scene manager that uses Unity's scene loading directly.
    /// </summary>
    public class UnitySceneManager : ISceneManager
    {
        /// <inheritdoc />
        public async Task<Scene> LoadAsync(SceneReference scene, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            await SceneManager.LoadSceneAsync(scene, mode).AsTaskAsync();
            return SceneManager.GetSceneByName(scene.Name);
        }

        /// <inheritdoc />
        public Task UnloadAsync(SceneReference scene)
        {
            var operation = SceneManager.UnloadSceneAsync(scene);
            return operation == null
                ? throw new InvalidOperationException($"Unity scene manager failed to unload scene {scene.Name}")
                : operation.AsTaskAsync();
        }

        /// <inheritdoc />
        public Task OnInstalledAsync(Scene scene)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnUninstalledAsync(Scene scene)
        {
            return Task.CompletedTask;
        }
    }
}
