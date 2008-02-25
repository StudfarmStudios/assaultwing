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

        public override void EmittPosition(out Microsoft.Xna.Framework.Vector3 position, out Microsoft.Xna.Framework.Vector3 direction)
        {
            position = new Vector3(0f); // dot emitter always creates particles in it's center
            Matrix rot = new Matrix();
            Matrix.CreateRotationZ(this.direction + (RandomHelper.GetRandomFloat() - 0.5f) * jitter, out rot);
            direction = Vector3.TransformNormal(new Vector3(1f,0f,0f), rot);
            // Log.Write("emitted with: " + direction + " " + position);
        }
    }
}
