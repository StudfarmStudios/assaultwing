using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Particles
{
    [LimitedSerialization]
    class DotEmitter : Emitter
    {
        # region Fields
        [RuntimeState]
        private float direction = 0f;
        [TypeParameter]
        private float jitter = 0.3f;
        # endregion

        # region Properties
        /// <summary>
        /// How much does the particle direction differ at most from the given default direction
        /// </summary>
        public float Jitter
        {
            get { return jitter; }
            set { jitter = value; }
        }

        public float Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        # endregion

        public override void EmittPosition(out Vector2 position, out Vector2 direction, out float directionAngle)
        {
            position = Vector2.Zero; // dot emitter always creates particles in its center
            directionAngle = this.direction + (RandomHelper.GetRandomFloat() - 0.5f) * jitter;
            Matrix rot = Matrix.CreateRotationZ(directionAngle);
            direction = Vector2.TransformNormal(Vector2.UnitX, rot);
        }
    }
}
