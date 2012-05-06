using NUnit.Framework;

namespace SquishIt.S3.Tests
{
    [TestFixture]
    public class KeyBuilderTest
    {
        [Test]
        public void ReturnToRelative()
        {
            var root = @"C:\fake\dir\";
            var file = @"another\file.js";
            var expected = @"another/file.js";

            var builder = new KeyBuilder(root, "");
            Assert.AreEqual(expected, builder.GetKeyFor(root + file));
        }

        [Test]
        public void ReturnToRelative_Injects_Virtual_Directory()
        {
            var root = @"C:\fake\dir\";
            var file = @"another\file.js";
            var vdir = "/this";
            var expected = @"this/another/file.js";

            var builder = new KeyBuilder(root, vdir);
            Assert.AreEqual(expected, builder.GetKeyFor(root + file));
        }

        [Test]
        public void ReturnToRelative_Only_Replaces_First_Occurrence_Of_Root()
        {
            var root = @"test/";
            var file = @"another/andthen/test/again.js";
            var expected = @"another/andthen/test/again.js";

            var builder = new KeyBuilder(root, "");
            Assert.AreEqual(expected, builder.GetKeyFor(root + file));
        }
    }
}
