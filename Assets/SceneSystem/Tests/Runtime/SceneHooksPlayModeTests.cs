using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace RadTadDev.SceneSystem.Tests
{
    /// <summary>
    /// Checks that the manager hooks are awaited by the transaction, using real scenes.
    /// </summary>
    [TestFixture]
    public class SceneHooksPlayModeTests : PlayModeTestBase
    {
        [Test]
        public async Task InstalledHookIsAwaitedBeforeTheNextInstall()
        {
            var manager = new DelegatingSceneManager(SceneMgr)
            {
                OnInstalledHook = async scene =>
                {
                    Events.Add("hook-start:" + scene.name);
                    await Task.Delay(300);
                    Events.Add("hook-end:" + scene.name);
                }
            };

            Assert.IsTrue(await Await(Run(None, new[] { Alpha, Beta }, manager)));

            CollectionAssert.AreEqual(
                new[]
                {
                    "install:" + Alpha, "hook-start:" + Alpha, "hook-end:" + Alpha,
                    "install:" + Beta, "hook-start:" + Beta, "hook-end:" + Beta,
                },
                Events);
        }


        [Test]
        public async Task UninstalledHookRunsWhileSceneIsStillLoaded()
        {
            var manager = new DelegatingSceneManager(SceneMgr);
            Assert.IsTrue(await Await(Run(None, new[] { Alpha }, manager)));
            Events.Clear();

            manager.OnUninstalledHook = async scene =>
            {
                Events.Add("hook-start:loaded=" + SceneManager.GetSceneByName(scene.name).isLoaded);
                await Task.Delay(200);
                Events.Add("hook-end:loaded=" + SceneManager.GetSceneByName(scene.name).isLoaded);
            };

            Assert.IsTrue(await Await(Run(new[] { Alpha }, None, manager)));

            CollectionAssert.AreEqual(
                new[] { "uninstall:" + Alpha, "hook-start:loaded=True", "hook-end:loaded=True" },
                Events);
            Assert.IsFalse(SceneManager.GetSceneByName(Alpha).isLoaded);
        }


        [Test]
        public async Task UninstalledHookFinishesBeforeTheSceneUnloads()
        {
            var manager = new DelegatingSceneManager(SceneMgr);
            Assert.IsTrue(await Await(Run(None, new[] { Alpha }, manager)));

            var unloadedDuringHook = false;
            manager.OnUninstalledHook = async scene =>
            {
                await Task.Delay(200);
                unloadedDuringHook = !SceneManager.GetSceneByName(scene.name).isLoaded;
            };

            Assert.IsTrue(await Await(Run(new[] { Alpha }, None, manager)));

            Assert.IsFalse(unloadedDuringHook);
            Assert.IsFalse(SceneManager.GetSceneByName(Alpha).isLoaded);
        }


        [Test]
        public async Task FailingInstalledHookFailsTheTransaction()
        {
            var manager = new DelegatingSceneManager(SceneMgr)
            {
                OnInstalledHook = scene => throw new System.InvalidOperationException("hook failed")
            };

            Assert.IsFalse(await Await(Run(None, new[] { Alpha, Beta }, manager)));

            CollectionAssert.DoesNotContain(Events, "install:" + Beta);
        }
    }
}
