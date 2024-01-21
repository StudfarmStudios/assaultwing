using System;
using NUnit.Framework;

namespace AW2.Game
{
    [TestFixture]
    public class GobTest
    {
        private const float STEP = 0.1f;

        [Test]
        public void TestDrawRotationOffsetDampingKeepsSign([Range(-Gob.ROTATION_SMOOTHING_CUTOFF, Gob.ROTATION_SMOOTHING_CUTOFF, STEP)] float x)
        {
            Assert.AreEqual(Math.Sign(x), Math.Sign(Gob.DampDrawRotationOffset(x)));
        }

        [Test]
        public void TestDrawRotationOffsetDampingShrinks([Range(-Gob.ROTATION_SMOOTHING_CUTOFF, Gob.ROTATION_SMOOTHING_CUTOFF, STEP)] float x)
        {
            Assert.GreaterOrEqual(Math.Abs(x), Math.Abs(Gob.DampDrawRotationOffset(x)));
        }

        [Test]
        public void TestDrawRotationOffsetDampingDerivativeIsNotTooSteep([Range(-Gob.ROTATION_SMOOTHING_CUTOFF + STEP, Gob.ROTATION_SMOOTHING_CUTOFF, STEP)] float x)
        {
            var delta = Gob.DampDrawRotationOffset(x) - Gob.DampDrawRotationOffset(x - STEP);
            Assert.GreaterOrEqual(1, delta / STEP);
            Assert.LessOrEqual(-1, delta / STEP);
        }
    }
}
