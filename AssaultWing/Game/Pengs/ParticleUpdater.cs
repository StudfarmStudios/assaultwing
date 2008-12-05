using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// Updates particles.
    /// </summary>
    /// A particle updater is part of a peng.
    /// <see cref="AW2.Game.Gobs.Peng"/>
    public interface ParticleUpdater
    {
        /// <summary>
        /// Updates the state of a particle and returns if it died or not.
        /// </summary>
        /// The given particle should belong to the peng that owns this particle updater.
        /// <param name="particle">The particle to update.</param>
        /// <returns><c>true</c> if the particle died and should be removed,
        /// <c>false</c> otherwise.</returns>
        bool Update(Particle particle);
    }

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
            TimeSpan now = AssaultWing.Instance.GameTime.TotalGameTime;
            PhysicsEngine physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));

            // Initialise custom particle fields.
            if (particle.timeout == TimeSpan.Zero)
                particle.timeout = now + TimeSpan.FromSeconds(particleAge.GetRandomValue());

            // Kill a timed out particle.
            if (particle.timeout <= now) return true;

            // Update particle fields.
            float lifePos = (now - particle.birthTime).Ticks / (float)(particle.timeout - particle.birthTime).Ticks;
            particle.layerDepth = lifePos;
            particle.move += physics.ApplyChange(acceleration.GetValue(lifePos, particle.pengInput, particle.random))
                * new Vector2((float)Math.Cos(particle.direction), (float)Math.Sin(particle.direction));
            particle.pos += physics.ApplyChange(particle.move);
            particle.rotation += physics.ApplyChange(rotationSpeed.GetValue(lifePos, particle.pengInput, particle.random));
            particle.scale = scale.GetValue(lifePos, particle.pengInput, particle.random);
            particle.alpha = alpha.GetValue(lifePos, particle.pengInput, particle.random);
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
