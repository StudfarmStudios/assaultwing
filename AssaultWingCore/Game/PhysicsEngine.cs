using System;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Helpers;

namespace AW2.Game
{
    /// <summary>
    /// A physics engine.
    /// </summary>
    public class PhysicsEngine : AWGameComponent
    {
        public PhysicsEngine(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        #region Public interface

        /// <summary>
        /// Applies drag to a gob. Drag is a force that manipulates the gob's
        /// movement closer to that of the medium. Drag constant measures the
        /// amount of this manipulation, 0 meaning no drag and 1 meaning
        /// absolute drag where the gob cannot escape the flow of the medium.
        /// Practical values are very small, under 0.1.
        /// </summary>
        /// Drag is the force that resists movement in a medium.
        /// <param name="gob">The gob to apply drag to.</param>
        /// <param name="flow">Direction and speed of flow of medium
        /// at the gob's location.</param>
        /// <param name="drag">Drag constant for the medium and the gob.</param>
        public void ApplyDrag(Gob gob, Vector2 flow, float drag)
        {
            gob.Move = (1 - drag) * (gob.Move - flow) + flow;
        }

        /// <summary>
        /// Applies the given force to the given gob.
        /// </summary>
        /// Note that the larger the mass of the gob is, the more force is needed to give it
        /// a good push.
        /// <param name="gob">The gob to apply the force to.</param>
        /// <param name="force">The force to apply, measured in Newtons.</param>
        public void ApplyForce(Gob gob, Vector2 force)
        {
            gob.Move += force / gob.Mass * (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
        }

        /// <summary>
        /// Applies the given force to a gob, preventing gob speed from
        /// growing beyond a limit.
        /// </summary>
        /// Note that the larger the mass of the gob is, the more force is needed to give it
        /// a good push. Although the gob's speed cannot grow beyond <b>maxSpeed</b>,
        /// it can still maintain its value even if it's larger than <b>maxSpeed</b>.
        /// <param name="gob">The gob to apply the force to.</param>
        /// <param name="force">The force to apply, measured in Newtons.</param>
        /// <param name="maxSpeed">The speed limit beyond which the gob's speed cannot grow.</param>
        public void ApplyLimitedForce(Gob gob, Vector2 force, float maxSpeed, TimeSpan duration)
        {
            float oldSpeed = gob.Move.Length();
            gob.Move += force / gob.Mass * (float)duration.TotalSeconds;
            float speedLimit = MathHelper.Max(maxSpeed, oldSpeed);
            gob.Move = gob.Move.Clamp(0, speedLimit);
        }

        /// <summary>
        /// Applies the given momentum to the given gob.
        /// </summary>
        /// Note that the larger the mass of the gob is, the more momentum is needed to give it
        /// a good push.
        /// <param name="gob">The gob to apply the momentum to.</param>
        /// <param name="momentum">The momentum to apply, measured in Newton seconds.</param>
        public void ApplyMomentum(Gob gob, Vector2 momentum)
        {
            gob.Move += momentum / gob.Mass;
        }

        /// <summary>
        /// Returns the scalar amount that represents how much the given scalar change speed
        /// affects during the current frame.
        /// </summary>
        /// <param name="changePerSecond">The speed of change per second.</param>
        /// <returns>The amount of change during the current frame.</returns>
        public float ApplyChange(float changePerSecond, TimeSpan duration)
        {
            return changePerSecond * (float)duration.TotalSeconds;
        }

        /// <summary>
        /// Returns the vector that represents how much the given vector change speed
        /// affects during the current frame.
        /// </summary>
        /// <param name="changePerSecond">The vector of change per second.</param>
        /// <returns>The vector of change during the current frame.</returns>
        public Vector2 ApplyChange(Vector2 changePerSecond, TimeSpan duration)
        {
            return changePerSecond * (float)duration.TotalSeconds;
        }

        #endregion Public interface
    }
}
