using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace RadTadDev.SceneSystem.Tests
{
    [TestFixture]
    public class UnitySceneManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            // Unity logs an error when it cannot unload a scene. The test expects that.
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void OnInstalledAsyncCompletesImmediately()
        {
            var manager = new UnitySceneManager();

            Assert.IsTrue(manager.OnInstalledAsync(default).IsCompletedSuccessfully);
        }

        [Test]
        public void OnUninstalledAsyncCompletesImmediately()
        {
            var manager = new UnitySceneManager();

            Assert.IsTrue(manager.OnUninstalledAsync(default).IsCompletedSuccessfully);
        }

        [Test]
        public void UnloadThrowsForSceneThatIsNotLoaded()
        {
            var manager = new UnitySceneManager();

            Assert.Throws<InvalidOperationException>(() => manager.UnloadAsync(TestScenes.Ref("NotLoadedScene_Tests")));
        }
    }
}
