using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace RadTadDev.SceneSystem.Tests
{
    /// <summary>
    /// Runs real transitions with runtime-created Unity scenes and checks the results in the Unity scene hierarchy,
    /// not just in the system's own bookkeeping.
    /// </summary>
    [TestFixture]
    public class SceneLoadingPlayModeTests : PlayModeTestBase
    {
        // ---- Loading ----

        [Test]
        public async Task LoadingASceneMakesItLoadedInUnity()
        {
            Assert.IsTrue(await Await(Run(None, new[] { Alpha })));

            var scene = SceneManager.GetSceneByName(Alpha);
            Assert.IsTrue(scene.IsValid());
            Assert.IsTrue(scene.isLoaded);
            Assert.IsTrue(Scenes.IsLoaded(Ref(Alpha)));
        }

        [Test]
        public async Task LoadedSceneContainsItsRootObject()
        {
            await LoadAsync(Alpha);

            Assert.IsTrue(HasRoot(SceneManager.GetSceneByName(Alpha), RootNameOf(Alpha)));
        }

        [Test]
        public async Task LoadingIsAdditive()
        {
            await LoadAsync(Alpha);
            var countBefore = SceneManager.sceneCount;

            await LoadAsync(Beta);

            Assert.AreEqual(countBefore + 1, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(Alpha).isLoaded);
            Assert.IsTrue(SceneManager.GetSceneByName(Beta).isLoaded);
        }

        [Test]
        public async Task LoadingManyScenesInOneTransactionLoadsAllOfThem()
        {
            Assert.IsTrue(await Await(Run(None, new[] { Alpha, Beta, Gamma })));

            Assert.IsTrue(SceneManager.GetSceneByName(Alpha).isLoaded);
            Assert.IsTrue(SceneManager.GetSceneByName(Beta).isLoaded);
            Assert.IsTrue(SceneManager.GetSceneByName(Gamma).isLoaded);
        }

        [Test]
        public async Task LoadingAMissingSceneFailsTheTransaction()
        {
            Assert.IsFalse(await Await(Run(None, new[] { "SceneThatDoesNotExist" })));

            Assert.IsFalse(Scenes.IsLoaded(Ref("SceneThatDoesNotExist")));
            Assert.IsFalse(Scenes.IsBusy);
            CollectionAssert.IsEmpty(Events);
        }

        [Test]
        public async Task SystemRecoversAfterAFailedRealLoad()
        {
            Assert.IsFalse(await Await(Run(None, new[] { "SceneThatDoesNotExist" })));

            Assert.IsTrue(await Await(Run(None, new[] { Alpha })));
            Assert.IsTrue(SceneManager.GetSceneByName(Alpha).isLoaded);
        }

        // ---- Install ----

        [Test]
        public async Task InstallerReceivesALoadedSceneThatHasItsContents()
        {
            var observed = new List<string>();
            Scenes = CreateSystem(scene =>
            {
                observed.Add($"{scene.name}:{scene.isLoaded}:{HasRoot(scene, RootNameOf(scene.name))}");
                return Task.CompletedTask;
            }, null);

            Assert.IsTrue(await Await(Run(None, new[] { Alpha })));

            CollectionAssert.AreEqual(new[] { $"{Alpha}:True:True" }, observed);
        }

        [Test]
        public async Task InstallerSceneHandleIsTheSceneUnityReports()
        {
            var matches = new List<bool>();
            Scenes = CreateSystem(scene =>
            {
                matches.Add(scene.Equals(SceneManager.GetSceneByName(scene.name)));
                return Task.CompletedTask;
            }, null);

            Assert.IsTrue(await Await(Run(None, new[] { Alpha })));

            CollectionAssert.AreEqual(new[] { true }, matches);
        }

        [Test]
        public async Task InstallsRunInDeclaredOrderWhenInstallersAreAsync()
        {
            Scenes = CreateSystem(async scene =>
            {
                // Alpha takes longest, so it would finish last if installs overlapped.
                if (scene.name == Alpha)
                {
                    await Task.Delay(300);
                }

                Events.Add("installed:" + scene.name);
            }, null);

            Assert.IsTrue(await Await(Run(None, new[] { Alpha, Beta })));

            CollectionAssert.AreEqual(new[] { "installed:" + Alpha, "installed:" + Beta }, Events);
        }

        [Test]
        public async Task InstallsNeverRunAtTheSameTime()
        {
            var active = 0;
            var maxActive = 0;
            Scenes = CreateSystem(async scene =>
            {
                active++;
                maxActive = Math.Max(maxActive, active);
                await Task.Delay(100);
                active--;
            }, null);

            Assert.IsTrue(await Await(Run(None, new[] { Alpha, Beta, Gamma })));

            Assert.AreEqual(1, maxActive);
        }

        [Test]
        public async Task InstallerExceptionFailsTheTransactionAndLeavesSceneLoaded()
        {
            // Documents the current contract: there is no rollback.
            Scenes = CreateSystem(scene => throw new InvalidOperationException("install failed"), null);

            Assert.IsFalse(await Await(Run(None, new[] { Alpha })));

            Assert.IsTrue(SceneManager.GetSceneByName(Alpha).isLoaded);
        }

        [Test]
        public async Task InstallerExceptionStopsLaterInstalls()
        {
            Scenes = CreateSystem(scene =>
            {
                if (scene.name == Alpha)
                {
                    throw new InvalidOperationException("install failed");
                }

                Events.Add("install:" + scene.name);
                return Task.CompletedTask;
            }, null);

            Assert.IsFalse(await Await(Run(None, new[] { Alpha, Beta })));

            CollectionAssert.DoesNotContain(Events, "install:" + Beta);
        }

        // ---- Uninstall and unload ----

        [Test]
        public async Task UninstallerSeesSceneStillLoadedWithItsContents()
        {
            var observed = new List<string>();
            Scenes = CreateSystem(null, scene =>
            {
                observed.Add($"{scene.name}:{scene.isLoaded}:{HasRoot(scene, RootNameOf(scene.name))}");
                return Task.CompletedTask;
            });
            await LoadAsync(Alpha);

            Assert.IsTrue(await Await(Run(new[] { Alpha }, None)));

            CollectionAssert.AreEqual(new[] { $"{Alpha}:True:True" }, observed);
        }

        [Test]
        public async Task UnloadRemovesSceneFromUnity()
        {
            await LoadAsync(Alpha);

            Assert.IsTrue(await Await(Run(new[] { Alpha }, None)));

            Assert.IsFalse(SceneManager.GetSceneByName(Alpha).isLoaded);
            Assert.IsFalse(Scenes.IsLoaded(Ref(Alpha)));
        }

        [Test]
        public async Task UnloadingOneSceneKeepsTheOthersLoaded()
        {
            await LoadAsync(Alpha, Beta);

            Assert.IsTrue(await Await(Run(new[] { Alpha }, None)));

            Assert.IsFalse(SceneManager.GetSceneByName(Alpha).isLoaded);
            Assert.IsTrue(SceneManager.GetSceneByName(Beta).isLoaded);
        }

        [Test]
        public async Task UninstallRunsBeforeUnloadInUnity()
        {
            Scenes = CreateSystem(null, scene =>
            {
                Events.Add("uninstall:" + scene.name + ":loaded=" + scene.isLoaded);
                return Task.CompletedTask;
            });
            await LoadAsync(Alpha);
            Events.Clear();

            Assert.IsTrue(await Await(Run(new[] { Alpha }, None)));

            Assert.IsFalse(SceneManager.GetSceneByName(Alpha).isLoaded);
            CollectionAssert.AreEqual(new[] { "uninstall:" + Alpha + ":loaded=True" }, Events);
        }

        [Test]
        public async Task SwappingScenesInOneTransactionUnloadsOldAndLoadsNew()
        {
            await LoadAsync(Alpha);
            Events.Clear();

            Assert.IsTrue(await Await(Run(new[] { Alpha }, new[] { Beta })));

            Assert.IsFalse(SceneManager.GetSceneByName(Alpha).isLoaded);
            Assert.IsTrue(SceneManager.GetSceneByName(Beta).isLoaded);
            CollectionAssert.AreEqual(new[] { "uninstall:" + Alpha, "install:" + Beta }, Events);
        }

        [Test]
        public async Task ReloadingTheSameSceneInstallsItAgain()
        {
            await LoadAsync(Alpha);
            Assert.IsTrue(await Await(Run(new[] { Alpha }, None)));
            Events.Clear();

            await LoadAsync(Alpha);

            Assert.IsTrue(SceneManager.GetSceneByName(Alpha).isLoaded);
            CollectionAssert.AreEqual(new[] { "install:" + Alpha }, Events);
        }

        [Test]
        public async Task UnloadingEverySceneLeavesNoTestSceneLoaded()
        {
            await LoadAsync(Alpha, Beta, Gamma);

            Assert.IsTrue(await Await(Run(new[] { Alpha, Beta, Gamma }, None)));

            Assert.IsFalse(SceneManager.GetSceneByName(Alpha).isLoaded);
            Assert.IsFalse(SceneManager.GetSceneByName(Beta).isLoaded);
            Assert.IsFalse(SceneManager.GetSceneByName(Gamma).isLoaded);
        }

        // ---- Progress and timing ----

        [Test]
        public async Task ProgressReachesOneAfterRealLoads()
        {
            var progress = new List<float>();
            var transaction = Run(None, new[] { Alpha, Beta });
            transaction.OnProgressChanged += progress.Add;

            Assert.IsTrue(await Await(transaction));

            Assert.AreEqual(1f, transaction.Progress);
            Assert.AreEqual(2, progress.Count);
            Assert.AreEqual(1f, progress[progress.Count - 1]);
        }

        [Test]
        public async Task DelayedTransitionDoesNotLoadBeforeItsDelay()
        {
            var transaction = Run(None, new[] { Alpha }, delay: 0.3f);

            Assert.IsFalse(SceneManager.GetSceneByName(Alpha).isLoaded);
            Assert.IsTrue(Scenes.IsBusy);

            var watch = Stopwatch.StartNew();
            Assert.IsTrue(await Await(transaction));

            Assert.GreaterOrEqual(watch.Elapsed.TotalSeconds, 0.1);
            Assert.IsTrue(SceneManager.GetSceneByName(Alpha).isLoaded);
        }

        [Test]
        public async Task AppClosingCancelsDelayedRealTransition()
        {
            using var cancellation = new CancellationTokenSource();
            Scenes = CreateSystem(null, null, cancellation.Token);

            var transaction = Run(None, new[] { Alpha }, delay: 5f);
            cancellation.Cancel();

            Assert.IsFalse(await Await(transaction));
            Assert.IsFalse(SceneManager.GetSceneByName(Alpha).isLoaded);
            CollectionAssert.IsEmpty(Events);
        }
    }
}
