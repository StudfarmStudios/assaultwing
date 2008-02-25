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

        public override void EmittPosition(out Vector3 position, out Vector3 direction)
        {
            float angle = 2 * (float)Math.PI * RandomHelper.GetRandomFloat();
            float distance = RandomHelper.GetRandomFloat() * radius;
            position = new Vector3((float)Math.Cos(angle) * distance, (float)Math.Sin(angle) * distance, 0f); // dot emitter always creates particles in it's center
            Matrix rot = new Matrix();
            Matrix.CreateRotationZ(angle, out rot);
            direction = Vector3.TransformNormal(new Vector3(1f, 0f, 0f), rot);
        }
    }
}
