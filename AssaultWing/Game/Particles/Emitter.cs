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

        private Vector2 position;

        #endregion

        #region Properties

        /// <summary>
        /// Position of the center of the emitter.
        /// </summary>
        public Vector2 Position
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
        /// <param name="direction">Initial direction of the particle. The returned vector is normalized.</param>
        /// <param name="directionAngle">Initial direction angle of the particle.</param>
        public abstract void EmittPosition(out Vector2 position, out Vector2 direction, out float directionAngle);

        #endregion
    }
}
