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
    public class PhysicalUpdater
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

        private float _dragMultiplier;
        private float _elapsedSeconds;

        public ExpectedValue ParticleAge { get { return _particleAge; } }

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

        public void Activate()
        {
            // Precalculate some values to speed up Update()
            _elapsedSeconds = (float)AssaultWingCore.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            _dragMultiplier = (float)Math.Pow(1 - _drag, _elapsedSeconds);
        }

        public bool Update(Particle particle)
        {
            // Note: This method is run potentially very often. It must be kept quick.

            var lifePos = (AssaultWingCore.Instance.DataEngine.ArenaTotalTime - particle.BirthTime).Ticks
                / (float)(particle.Timeout - particle.BirthTime).Ticks;
            if (lifePos >= 1) return true;
            UpdateVisualProperties(particle, lifePos);
            UpdatePhysicalProperties(particle, lifePos);
            particle.UpdateCounter++;
            return false;
        }

        private void UpdateVisualProperties(Particle particle, float lifePos)
        {
            particle.LayerDepth = lifePos;
            particle.Scale = _scale.GetValue(lifePos, particle.PengInput, particle.Random);
            // Optimisation: PengParameter.GetValue may be somewhat slow, call it only sometimes.
            if ((particle.UpdateCounter & 0x03) == 0) particle.Alpha = _alpha.GetValue(lifePos, particle.PengInput, particle.Random);
        }

        private void UpdatePhysicalProperties(Particle particle, float lifePos)
        {
            // Optimisation: PengParameter.GetValue may be somewhat slow, call it only sometimes.
            if ((particle.UpdateCounter & 0x0f) == 0) particle.LastAcceleration = _acceleration.GetValue(lifePos, particle.PengInput, particle.Random);
            if ((particle.UpdateCounter & 0x07) == 0) particle.LastRotationSpeed = _rotationSpeed.GetValue(lifePos, particle.PengInput, particle.Random);
            particle.Pos += particle.Move * _elapsedSeconds;
            particle.Move += particle.DirectionVector * particle.LastAcceleration * _elapsedSeconds;
            particle.Rotation += particle.LastRotationSpeed * _elapsedSeconds;
            particle.Move *= _dragMultiplier;
        }
    }
}
