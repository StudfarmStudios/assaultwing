using Microsoft.Xna.Framework;
using AW2.Helpers;

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
            RandomHelper.GetRandomCirclePoint(radius, out position, out direction, out directionAngle);
        }
    }
}
