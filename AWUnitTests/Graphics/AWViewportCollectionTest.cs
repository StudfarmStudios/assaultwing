using NUnit.Framework;

namespace AW2.Graphics
{
    [TestFixture]
    public class AWViewportCollectionTest
    {
        [Test]
        public void AspectRatioComparison()
        {
            Assert.AreEqual(0, AWViewportCollection.CompareAspectRatios(1.0f, 1.0f));
            Assert.AreEqual(0, AWViewportCollection.CompareAspectRatios(0.5f, 0.5f));
            Assert.AreEqual(0, AWViewportCollection.CompareAspectRatios(2.0f, 2.0f));

            Assert.AreEqual(1, AWViewportCollection.CompareAspectRatios(0.5f, 1.0f));
            Assert.AreEqual(-1, AWViewportCollection.CompareAspectRatios(1.0f, 2.0f));
            Assert.AreEqual(-1, AWViewportCollection.CompareAspectRatios(1.0f, 0.5f));
            Assert.AreEqual(1, AWViewportCollection.CompareAspectRatios(2.0f, 1.0f));

            Assert.AreEqual(-1, AWViewportCollection.CompareAspectRatios(0.5f, float.MaxValue));
            Assert.AreEqual(1, AWViewportCollection.CompareAspectRatios(float.MaxValue, 0.5f));
            Assert.AreEqual(1, AWViewportCollection.CompareAspectRatios(float.Epsilon, 2.0f));
            Assert.AreEqual(-1, AWViewportCollection.CompareAspectRatios(2.0f, float.Epsilon));

            Assert.AreEqual(1, AWViewportCollection.CompareAspectRatios(0.9f, 1.1f));
            Assert.AreEqual(-1, AWViewportCollection.CompareAspectRatios(0.9f, 1.2f));
        }
    }
}
