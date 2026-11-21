using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace RadTadDev.SceneSystem.Tests.Editor
{
    [TestFixture]
    public class SceneNameUniquenessBuildCheckTests
    {
        [SetUp]
        public void SetUp()
        {
            _original = EditorBuildSettings.scenes;
        }

        [TearDown]
        public void TearDown()
        {
            EditorBuildSettings.scenes = _original;
        }

        private EditorBuildSettingsScene[] _original;

        [Test]
        public void ThrowsWhenTwoEnabledScenesShareAName()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/A/Main.unity", true),
                new EditorBuildSettingsScene("Assets/B/Main.unity", true)
            };

            Assert.Throws<BuildFailedException>(() => new SceneNameUniquenessBuildCheck().OnPreprocessBuild(null));
        }

        [Test]
        public void PassesWhenNamesAreUnique()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/A/Main.unity", true),
                new EditorBuildSettingsScene("Assets/B/Other.unity", true)
            };

            Assert.DoesNotThrow(() => new SceneNameUniquenessBuildCheck().OnPreprocessBuild(null));
        }

        [Test]
        public void IgnoresDisabledScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/A/Main.unity", true),
                new EditorBuildSettingsScene("Assets/B/Main.unity", false)
            };

            Assert.DoesNotThrow(() => new SceneNameUniquenessBuildCheck().OnPreprocessBuild(null));
        }

        [Test]
        public void PassesWithNoScenes()
        {
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];

            Assert.DoesNotThrow(() => new SceneNameUniquenessBuildCheck().OnPreprocessBuild(null));
        }
    }
}
