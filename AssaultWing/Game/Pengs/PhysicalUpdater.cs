using System;
using System.Collections.Generic;
using AW2.Core;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// Moves particles in a fashion that mimics physical laws.
    /// </summary>
    /// Physical updater uses the following:
    /// - Particle.timeout is the time of death of the particle
    [LimitedSerialization]
    public class PhysicalUpdater : ParticleUpdater, IConsistencyCheckable
    {
        /// <summary>
        /// The range of lifetimes of particles, in seconds.
        /// </summary>
        [TypeParameter]
        private ExpectedValue _particleAge;

        /// <summary>
        /// Acceleration of particles in the initial emission direction,
        /// in meters per second squared.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private PengParameter _acceleration;

        /// <summary>
        /// Particle rotation speed, in radians per second.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private PengParameter _rotationSpeed;

        /// <summary>
        /// Scale of particles.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private PengParameter _scale;

        /// <summary>
        /// Alpha of particles. 0 is transparent, 1 is opaque.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private PengParameter _alpha;

        /// <summary>
        /// Amount of drag of particles. Drag is the decrement of velocity in one second
        /// relative to original velocity (e.g. applying a drag of 0.2 to a velocity of
        /// 100 for one second will result in a velocity of 80, given that there are no
        /// other forces affecting the velocity).
        /// </summary>
        [TypeParameter]
        private float _drag;

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public PhysicalUpdater()
        {
            _particleAge = new ExpectedValue(5, 2);
            _acceleration = new CurveLerp();
            _rotationSpeed = new CurveLerp();
            _scale = new CurveLerp();
            _alpha = new CurveLerp();
            _drag = 0;
        }

        public bool Update(Particle particle)
        {
            // Note: This method is run potentially very often. It must be kept quick.

            var now = AssaultWingCore.Instance.DataEngine.ArenaTotalTime;

            // Initialise custom particle fields
            if (particle.Timeout == TimeSpan.Zero)
                particle.Timeout = now + TimeSpan.FromSeconds(_particleAge.GetRandomValue());

            // Kill a timed out particle
            if (particle.Timeout <= now) return true;

            var lifePos = (now - particle.BirthTime).Ticks / (float)(particle.Timeout - particle.BirthTime).Ticks;
            UpdateVisualProperties(particle, lifePos);
            UpdatePhysicalProperties(particle, lifePos);
            return false;
        }

        private void UpdateVisualProperties(Particle particle, float lifePos)
        {
            particle.LayerDepth = lifePos;
            particle.Scale = _scale.GetValue(lifePos, particle.PengInput, particle.Random);
            particle.Alpha = _alpha.GetValue(lifePos, particle.PengInput, particle.Random);
        }

        private void UpdatePhysicalProperties(Particle particle, float lifePos)
        {
            var elapsedSeconds = (float)AssaultWingCore.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            var accelerationValue = _acceleration.GetValue(lifePos, particle.PengInput, particle.Random);
            var rotationSpeedValue = _rotationSpeed.GetValue(lifePos, particle.PengInput, particle.Random);
            particle.Pos += particle.Move * elapsedSeconds;
            particle.Move += particle.DirectionVector * accelerationValue * elapsedSeconds;
            particle.Rotation += rotationSpeedValue * elapsedSeconds;
            particle.Move *= (float)Math.Pow(1 - _drag, elapsedSeconds);
        }

        public void MakeConsistent(Type limitationAttribute)
        {
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
            }
        }
    }
}
