using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// Moves particles in a fashion that mimics physical laws.
    /// </summary>
    /// Physical updater uses the following:
    /// - Particle.timeout is the time of death of the particle
    [LimitedSerialization]
    public class PhysicalUpdater : ParticleUpdater
    {
        #region PhysicalUpdater fields

        /// <summary>
        /// The range of lifetimes of particles, in seconds.
        /// </summary>
        [TypeParameter]
        ExpectedValue particleAge;

        /// <summary>
        /// Acceleration of particles in the initial emission direction,
        /// in meters per second squared.
        /// </summary>
        [TypeParameter, ShallowCopy]
        PengParameter acceleration;

        /// <summary>
        /// Particle rotation speed, in radians per second.
        /// </summary>
        [TypeParameter, ShallowCopy]
        PengParameter rotationSpeed;

        /// <summary>
        /// Scale of particles.
        /// </summary>
        [TypeParameter, ShallowCopy]
        PengParameter scale;

        /// <summary>
        /// Alpha of particles. 0 is transparent, 1 is opaque.
        /// </summary>
        [TypeParameter, ShallowCopy]
        PengParameter alpha;

        /// <summary>
        /// Amount of drag of particles. Drag is the decrement of velocity in one second
        /// relative to original velocity (e.g. applying a drag of 0.2 to a velocity of
        /// 100 for one second will result in a velocity of 80, given that there are no
        /// other forces affecting the velocity).
        /// </summary>
        [TypeParameter]
        float drag;

        #endregion PhysicalUpdater fields

        /// <summary>
        /// Creates an uninitialised physical updater.
        /// </summary>
        /// This constructor is only for serialisation.
        public PhysicalUpdater()
        {
            particleAge = new ExpectedValue(5, 2);
            acceleration = new CurveLerp();
            rotationSpeed = new CurveLerp();
            scale = new CurveLerp();
            alpha = new CurveLerp();
            drag = 0;
        }

        /// <summary>
        /// Updates the state of a particle and returns if it died or not.
        /// </summary>
        /// The given particle should belong to the peng that owns this particle updater.
        /// <param name="particle">The particle to update.</param>
        /// <returns><c>true</c> if the particle died and should be removed,
        /// <c>false</c> otherwise.</returns>
        public bool Update(Particle particle)
        {
            // Note: This method is run potentially very often. It must be kept quick.

            var now = AssaultWingCore.Instance.DataEngine.ArenaTotalTime;

            // Initialise custom particle fields
            if (particle.Timeout == TimeSpan.Zero)
                particle.Timeout = now + TimeSpan.FromSeconds(particleAge.GetRandomValue());

            // Kill a timed out particle
            if (particle.Timeout <= now) return true;

            // Update particle fields
            float elapsedSeconds = (float)AssaultWingCore.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            float lifePos = (now - particle.BirthTime).Ticks / (float)(particle.Timeout - particle.BirthTime).Ticks;
            particle.LayerDepth = lifePos;

            float accelerationValue = acceleration.GetValue(lifePos, particle.PengInput, particle.Random);
            float rotationSpeedValue = rotationSpeed.GetValue(lifePos, particle.PengInput, particle.Random);
            particle.Scale = scale.GetValue(lifePos, particle.PengInput, particle.Random);
            particle.Alpha = alpha.GetValue(lifePos, particle.PengInput, particle.Random);

            particle.Pos += particle.Move * elapsedSeconds;
            particle.Move += particle.DirectionVector * accelerationValue * elapsedSeconds;
            particle.Rotation += rotationSpeedValue * elapsedSeconds;
            particle.Move *= (float)Math.Pow(1 - drag, elapsedSeconds);
            return false;
        }

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public void MakeConsistent(Type limitationAttribute)
        {
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                // Make sure there's no null references.
                if (acceleration == null)
                    throw new Exception("Serialization error: PhysicalUpdater acceleration not defined");
                if (rotationSpeed == null)
                    throw new Exception("Serialization error: PhysicalUpdater rotationSpeed not defined");
                if (scale == null)
                    throw new Exception("Serialization error: PhysicalUpdater scale not defined");
                if (alpha == null)
                    throw new Exception("Serialization error: PhysicalUpdater alpha not defined");
            }
        }

        #endregion
    }
}
