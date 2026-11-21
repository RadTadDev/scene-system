using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace RadTadDev.SceneSystem.Tests
{
    /// <summary>
    /// Records every scene operation in the order it starts, and lets a test hold or fail any one of them.
    /// Scenes are preview scenes, which exist in Edit Mode without touching the scenes the user has open. Each one is
    /// remembered by its name so the installer and uninstaller can tell them apart. Fakes share that name table, so a
    /// scene loaded by one fake can be installed through another.
    /// Event keys: "load:A", "install:A", "installed:A", "uninstall:A", "uninstalled:A", "unload:A".
    /// </summary>
    internal sealed class FakeScenes
    {
        private static readonly Dictionary<ulong, string> NamesByHandle = new();

        private readonly List<Scene> _created = new();
        private readonly HashSet<string> _failures = new();
        private readonly Dictionary<string, TaskCompletionSource<bool>> _gates = new();

        /// <summary>
        /// Every operation in the order it started.
        /// </summary>
        public readonly List<string> Events = new();

        /// <summary>
        /// A manager that records into this fake.
        /// </summary>
        public readonly ISceneManager Manager;

        public FakeScenes()
        {
            Manager = new FakeManager(this);
        }

        /// <summary>
        /// An installer that records into this fake.
        /// </summary>
        public Func<Scene, Task> Installer => scene => Step("install:" + NameOf(scene));

        /// <summary>
        /// An uninstaller that records into this fake.
        /// </summary>
        public Func<Scene, Task> Uninstaller => scene => Step("uninstall:" + NameOf(scene));

        /// <summary>
        /// Makes the operation with this key wait until the returned source is completed.
        /// </summary>
        public TaskCompletionSource<bool> Hold(string key)
        {
            var gate = new TaskCompletionSource<bool>();
            _gates[key] = gate;
            return gate;
        }

        /// <summary>
        /// Makes the operation with this key fail.
        /// </summary>
        public void Fail(string key)
        {
            _failures.Add(key);
        }

        /// <summary>
        /// The name a scene was created with by a fake.
        /// </summary>
        public static string NameOf(Scene scene)
        {
            return NamesByHandle.TryGetValue(scene.handle.GetRawData(), out var name) ? name : "?";
        }

        /// <summary>
        /// Closes the scenes this fake created.
        /// </summary>
        public void Cleanup()
        {
            foreach (var scene in _created)
            {
                NamesByHandle.Remove(scene.handle.GetRawData());
                EditorSceneManager.ClosePreviewScene(scene);
            }

            _created.Clear();
        }

        private async Task Step(string key)
        {
            // Recorded when the call is made, not when it finishes.
            Events.Add(key);

            if (_failures.Contains(key))
            {
                throw new InvalidOperationException("Fake failure at " + key);
            }

            if (_gates.TryGetValue(key, out var gate))
            {
                await gate.Task;
            }
        }

        private Scene Create(string name)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            NamesByHandle[scene.handle.GetRawData()] = name;
            _created.Add(scene);
            return scene;
        }

        private sealed class FakeManager : ISceneManager
        {
            private readonly FakeScenes _owner;

            public FakeManager(FakeScenes owner)
            {
                _owner = owner;
            }

            public async Task<Scene> LoadAsync(SceneReference scene, LoadSceneMode mode = LoadSceneMode.Additive)
            {
                await _owner.Step("load:" + scene.Name);
                return _owner.Create(scene.Name);
            }

            public Task UnloadAsync(SceneReference scene)
            {
                return _owner.Step("unload:" + scene.Name);
            }

            public Task OnInstalledAsync(Scene scene)
            {
                return _owner.Step("installed:" + NameOf(scene));
            }

            public Task OnUninstalledAsync(Scene scene)
            {
                return _owner.Step("uninstalled:" + NameOf(scene));
            }
        }
    }
}
