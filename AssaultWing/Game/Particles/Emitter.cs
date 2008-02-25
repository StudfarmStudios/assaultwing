using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Particles
{
    /// <summary>
    /// The emitter indicates how the particles appear on the world
    /// </summary>
    [LimitedSerialization]
    public abstract class Emitter
    {
        #region Fields

        private Vector3 position;

        #endregion

        #region Properties

        /// <summary>
        /// Position of the center of the emitter.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Method that calculates the initial position and direction of a particle.
        /// </summary>
        /// <param name="position">Initial position of the particle.</param>
        /// <param name="direction">Initial direction of the particle. This vector must be normalized.</param>
        public abstract void EmittPosition(out Vector3 position, out Vector3 direction);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Vector with the direction the system emits the particles at
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="halfAngle"></param>
        /// <returns></returns>
        protected Vector3 EmitDirection(Vector3 direction, float halfAngle)
        {
            float pitch, yaw, roll;

            pitch = RandomHelper.GetRandomFloat() * (halfAngle * 2) - halfAngle;
            yaw = RandomHelper.GetRandomFloat() * (halfAngle * 2) - halfAngle;
            roll = RandomHelper.GetRandomFloat() * (halfAngle * 2) - halfAngle;

            Matrix rotation = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(yaw),
                MathHelper.ToRadians(pitch), MathHelper.ToRadians(roll));

            return Vector3.TransformNormal(direction, rotation);
        }

        #endregion
    }
}
