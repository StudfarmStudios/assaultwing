using System;
using System.Collections.Generic;
using System.Text;
using AW2.Helpers;
using Microsoft.Xna.Framework;

namespace AW2.Game.Particles
{
    [LimitedSerialization]
    class ExplodeEmitter : Emitter
    {
        # region Fields

        [TypeParameter]
        private float radius = 20f;
        # endregion

        # region Properties
        /// <summary>
        /// To what size of an area the particles are created to.
        /// </summary>
        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }

        # endregion

        public override void EmittPosition(out Vector2 position, out Vector2 direction, out float directionAngle)
        {
            // Randomise position with an even distribution over the circle defined by 'radius' and the origin.
            directionAngle = RandomHelper.GetRandomFloat(0, MathHelper.TwoPi);
            float distance = radius * (float)Math.Sqrt(RandomHelper.globalRandomGenerator.NextDouble());
            direction = new Vector2(
                (float)Math.Cos(directionAngle),
                (float)Math.Sin(directionAngle));
            position = direction * distance;
        }
    }
}
