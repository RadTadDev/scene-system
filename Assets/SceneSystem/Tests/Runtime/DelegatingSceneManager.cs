using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace RadTadDev.SceneSystem.Tests
{
    /// <summary>
    /// Wraps another manager so a test can add async work to the install and uninstall hooks. Loads and unloads go
    /// straight to the inner manager.
    /// </summary>
    public sealed class DelegatingSceneManager : ISceneManager
    {
        private readonly ISceneManager _inner;

        public DelegatingSceneManager(ISceneManager inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Runs in place of the inner manager's <c>OnInstalledAsync</c> when set.
        /// </summary>
        public Func<Scene, Task> OnInstalledHook { get; set; }

        /// <summary>
        /// Runs in place of the inner manager's <c>OnUninstalledAsync</c> when set.
        /// </summary>
        public Func<Scene, Task> OnUninstalledHook { get; set; }

        public Task<Scene> LoadAsync(SceneReference scene, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            return _inner.LoadAsync(scene, mode);
        }

        public Task UnloadAsync(SceneReference scene)
        {
            return _inner.UnloadAsync(scene);
        }

        public Task OnInstalledAsync(Scene scene)
        {
            return OnInstalledHook != null ? OnInstalledHook(scene) : _inner.OnInstalledAsync(scene);
        }

        public Task OnUninstalledAsync(Scene scene)
        {
            return OnUninstalledHook != null ? OnUninstalledHook(scene) : _inner.OnUninstalledAsync(scene);
        }
    }
}
