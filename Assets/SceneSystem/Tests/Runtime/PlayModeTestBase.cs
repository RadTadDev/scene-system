using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RadTadDev.SceneSystem.Tests
{
    /// <summary>
    /// Shared setup for PlayMode tests. Each test gets a fresh system and a fresh <see cref="RuntimeSceneManager" />,
    /// a capturing log sink, and an empty event log. Installers and uninstallers record into <see cref="Events" />
    /// by default. Test scenes left loaded are unloaded after each test.
    /// </summary>
    public abstract class PlayModeTestBase
    {
        protected const string Alpha = "SceneAlpha";
        protected const string Beta = "SceneBeta";
        protected const string Gamma = "SceneGamma";

        protected static readonly string[] None = Array.Empty<string>();
        protected static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Every install, uninstall and hook call, in the order it ran.
        /// </summary>
        protected readonly List<string> Events = new();

        /// <summary>
        /// The system under test. Recreated for each test.
        /// </summary>
        protected SceneSystem Scenes;

        /// <summary>
        /// The manager used for loads. Recreated for each test.
        /// </summary>
        protected RuntimeSceneManager SceneMgr;

        /// <summary>
        /// Captured log output. Recreated for each test.
        /// </summary>
        protected CapturingLogSink Logs;


        [SetUp]
        public void CreateFreshSystem()
        {
            Events.Clear();
            Logs = CapturingLogSink.Install();

            SceneMgr = new RuntimeSceneManager();
            SceneMgr.Available.Add(Alpha);
            SceneMgr.Available.Add(Beta);
            SceneMgr.Available.Add(Gamma);

            Scenes = CreateSystem(null, null);
        }


        [UnityTearDown]
        public IEnumerator UnloadTestScenes()
        {
            CapturingLogSink.Restore();

            var operations = new List<AsyncOperation>();
            foreach (var name in new[] { Alpha, Beta, Gamma })
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.isLoaded)
                {
                    operations.Add(SceneManager.UnloadSceneAsync(scene));
                }
            }

            foreach (var operation in operations)
            {
                if (operation != null)
                {
                    yield return operation;
                }
            }
        }


        /// <summary>
        /// Creates a system. Null installers record into <see cref="Events" />.
        /// </summary>
        protected SceneSystem CreateSystem(Func<Scene, Task> installer, Func<Scene, Task> uninstaller,
            CancellationToken token = default)
        {
            return new SceneSystem(
                installer ?? new Func<Scene, Task>(RecordInstall),
                uninstaller ?? new Func<Scene, Task>(RecordUninstall),
                token);
        }


        protected Task RecordInstall(Scene scene)
        {
            Events.Add("install:" + scene.name);
            return Task.CompletedTask;
        }


        protected Task RecordUninstall(Scene scene)
        {
            Events.Add("uninstall:" + scene.name);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Starts a transition on the current system. A null load manager uses <see cref="SceneMgr" />.
        /// </summary>
        protected SceneTransaction Run(string[] unloads, string[] loads, ISceneManager loadManager = null,
            ISceneManager unloadManager = null, float delay = 0)
        {
            return Scenes.Transition(Refs(unloads), Refs(loads), loadManager ?? SceneMgr, unloadManager, delay);
        }


        /// <summary>
        /// Waits for a transaction to finish, failing the test if it takes longer than <see cref="Timeout" />.
        /// </summary>
        protected static async Task<bool> Await(SceneTransaction transaction)
        {
            var finished = await Task.WhenAny(transaction.Completion, Task.Delay(Timeout));
            Assert.AreSame(transaction.Completion, finished, "Transaction did not finish in time.");
            return transaction.Completion.Result;
        }


        /// <summary>
        /// Loads the named scenes and checks that the transaction succeeds.
        /// </summary>
        protected async Task LoadAsync(params string[] names)
        {
            Assert.IsTrue(await Await(Run(None, names)), "Setup load failed.");
        }


        protected static SceneReference Ref(string name)
        {
            var reference = (SceneReference)Activator.CreateInstance(typeof(SceneReference), true);
            typeof(SceneReference)
                .GetField("_sceneName", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(reference, name);
            return reference;
        }


        protected static SceneReference[] Refs(string[] names)
        {
            return names.Select(Ref).ToArray();
        }


        protected static bool HasRoot(Scene scene, string rootName)
        {
            return scene.GetRootGameObjects().Any(root => root.name == rootName);
        }


        protected static string RootNameOf(string sceneName)
        {
            return RuntimeSceneManager.RootNameFor(sceneName);
        }
    }
}
