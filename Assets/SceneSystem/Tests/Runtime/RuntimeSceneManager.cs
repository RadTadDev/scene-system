using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RadTadDev.SceneSystem.Tests
{
    /// <summary>
    /// A manager for PlayMode tests that creates empty Unity scenes at runtime instead of loading scene assets.
    /// The scenes are real Unity scenes: they appear in the hierarchy, hold objects, and unload through Unity.
    /// Loading a scene asset needs Build Settings, which these tests cannot set up, so
    /// <see cref="UnitySceneManager" /> is not exercised end to end here.
    /// A name that is not in <see cref="Available" /> fails to load, the way a missing scene would.
    /// </summary>
    public sealed class RuntimeSceneManager : ISceneManager
    {
        /// <summary>
        /// Scene names this manager can load. Any other name throws.
        /// </summary>
        public HashSet<string> Available { get; } = new();

        /// <summary>
        /// Frames each load and unload waits before it completes, so they really are asynchronous.
        /// </summary>
        public int FramesToWait { get; set; } = 2;


        /// <summary>
        /// The name of the root object every loaded scene contains.
        /// </summary>
        public static string RootNameFor(string sceneName)
        {
            return "Root_" + sceneName;
        }


        public async Task<Scene> LoadAsync(SceneReference scene, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            if (!Available.Contains(scene.Name))
            {
                throw new InvalidOperationException($"Scene '{scene.Name}' is not available to this manager.");
            }

            await WaitFrames();

            var loaded = SceneManager.CreateScene(scene.Name);
            var root = new GameObject(RootNameFor(scene.Name));
            SceneManager.MoveGameObjectToScene(root, loaded);
            return loaded;
        }


        public async Task UnloadAsync(SceneReference scene)
        {
            var loaded = SceneManager.GetSceneByName(scene.Name);
            if (!loaded.isLoaded)
            {
                throw new InvalidOperationException($"Scene '{scene.Name}' is not loaded.");
            }

            await WaitFrames();

            var operation = SceneManager.UnloadSceneAsync(loaded);
            if (operation == null)
            {
                throw new InvalidOperationException($"Unity could not unload scene '{scene.Name}'.");
            }

            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }


        public Task OnInstalledAsync(Scene scene)
        {
            return Task.CompletedTask;
        }


        public Task OnUninstalledAsync(Scene scene)
        {
            return Task.CompletedTask;
        }


        private async Task WaitFrames()
        {
            for (var i = 0; i < FramesToWait; i++)
            {
                await Task.Yield();
            }
        }
    }
}
