using NUnit.Framework;

namespace RadTadDev.SceneSystem.Tests
{
    [TestFixture]
    public class SceneReferenceTests
    {
        [Test]
        public void NameIsTheSceneName()
        {
            Assert.AreEqual("Level1", TestScenes.Ref("Level1").Name);
        }

        [Test]
        public void DefaultNameIsMissing()
        {
            Assert.AreEqual("Missing", TestScenes.Blank().Name);
        }

        [Test]
        public void ImplicitConversionGivesTheName()
        {
            string name = TestScenes.Ref("Level1");

            Assert.AreEqual("Level1", name);
        }

        [Test]
        public void ToStringGivesTheName()
        {
            Assert.AreEqual("Level1", TestScenes.Ref("Level1").ToString());
        }

        [Test]
        public void EqualityIsByName()
        {
            var a = TestScenes.Ref("A");
            var otherA = TestScenes.Ref("A");
            var b = TestScenes.Ref("B");

            Assert.IsTrue(a.Equals(otherA));
            Assert.AreEqual(a.GetHashCode(), otherA.GetHashCode());
            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void NotEqualToNullOrOtherTypes()
        {
            var a = TestScenes.Ref("A");

            Assert.IsFalse(a.Equals(null));
            Assert.IsFalse(a.Equals("A"));
        }

        [Test]
        public void EqualityIsSymmetric()
        {
            var a = TestScenes.Ref("A");
            var otherA = TestScenes.Ref("A");

            Assert.AreEqual(a.Equals(otherA), otherA.Equals(a));
        }

        [Test]
        public void EqualityIsCaseSensitive()
        {
            Assert.IsFalse(TestScenes.Ref("Level").Equals(TestScenes.Ref("level")));
        }

        [Test]
        public void WorksAsDictionaryKeyByName()
        {
            var keys = new System.Collections.Generic.Dictionary<SceneReference, int>
            {
                [TestScenes.Ref("A")] = 1
            };

            Assert.AreEqual(1, keys[TestScenes.Ref("A")]);
        }

        [Test]
        public void NameWithSpacesIsKeptExactly()
        {
            Assert.AreEqual("Main Menu 2", TestScenes.Ref("Main Menu 2").Name);
        }
    }
}
