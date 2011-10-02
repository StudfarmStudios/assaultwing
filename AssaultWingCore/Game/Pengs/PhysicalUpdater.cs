using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PhysicalUpdater : IConsistencyCheckable
    {
        private class ParticleValues
        {
            public int AgeInFrames { get; set; }
            public float[] Alpha { get; set; }
            public float[] Scale { get; set; }
            public float[] RotationSpeed { get; set; }
            public float[] Acceleration { get; set; }
        }

        private const int PRECALC_COUNT = 10;
        private List<ParticleValues> _precalculatedValues;

        /// <summary>
        /// The range of lifetimes of particles, in seconds.
        /// </summary>
        [TypeParameter]
        private PengParameter _particleAge;

        /// <summary>
        /// Acceleration of particles in the initial emission direction,
        /// in meters per second squared.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private PengParameter _acceleration;

        /// <summary>
        /// Scales the input parameter before adding it to the acceleration base value.
        /// </summary>
        [TypeParameter]
        private float _accelerationInputScale;

        /// <summary>
        /// Particle rotation speed, in radians per second.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private PengParameter _rotationSpeed;

        /// <summary>
        /// Scales the input parameter before adding it to the rotationSpeed base value.
        /// </summary>
        [TypeParameter]
        private float _rotationSpeedInputScale;

        /// <summary>
        /// Scale of particles.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private PengParameter _scale;

        /// <summary>
        /// Scales the input parameter before adding it to the scale base value.
        /// </summary>
        [TypeParameter]
        private float _scaleInputScale;

        /// <summary>
        /// Alpha of particles. 0 is transparent, 1 is opaque.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private PengParameter _alpha;

        /// <summary>
        /// Scales the input parameter before adding it to the alpha base value.
        /// </summary>
        [TypeParameter]
        private float _alphaInputScale;

        /// <summary>
        /// Amount of drag of particles. Drag is the decrement of velocity in one second
        /// relative to original velocity (e.g. applying a drag of 0.2 to a velocity of
        /// 100 for one second will result in a velocity of 80, given that there are no
        /// other forces affecting the velocity).
        /// </summary>
        [TypeParameter]
        private float _drag;

        [TypeParameter]
        private bool _areParticlesImmortal;

        private float _dragMultiplier;
        private float _elapsedSeconds;

        public bool AreParticlesImmortal { get { return _areParticlesImmortal; } }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public PhysicalUpdater()
        {
            _particleAge = new ExpectedValue();
            _acceleration = new SimpleCurve();
            _accelerationInputScale = 0;
            _rotationSpeed = new SimpleCurve();
            _rotationSpeedInputScale = 0;
            _scale = new SimpleCurve();
            _scaleInputScale = 0;
            _alpha = new SimpleCurve();
            _alphaInputScale = 0;
            _drag = 0;
            _areParticlesImmortal = false;
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
            var precalcIndex = Math.Abs(particle.Random) % PRECALC_COUNT;
            var values = _precalculatedValues[precalcIndex];
            if (particle.AgeInFrames >= values.AgeInFrames)
            {
                if (_areParticlesImmortal)
                    particle.AgeInFrames -= values.AgeInFrames;
                else
                    return true;
            }
            UpdateVisualProperties(particle, values);
            UpdatePhysicalProperties(particle, values);
            particle.AgeInFrames++;
            return false;
        }

        private void UpdateVisualProperties(Particle particle, ParticleValues values)
        {
            particle.LayerDepth = particle.AgeInFrames / (float)values.AgeInFrames;
            particle.Scale = values.Scale[particle.AgeInFrames] + particle.PengInput * _scaleInputScale;
            particle.Alpha = values.Alpha[particle.AgeInFrames] + particle.PengInput * _alphaInputScale;
        }

        private void UpdatePhysicalProperties(Particle particle, ParticleValues values)
        {
            var acceleration = values.Acceleration[particle.AgeInFrames] + particle.PengInput * _accelerationInputScale;
            var rotationSpeed = values.RotationSpeed[particle.AgeInFrames] + particle.PengInput * _rotationSpeedInputScale;
            particle.Pos += particle.Move * _elapsedSeconds;
            particle.Move += particle.DirectionVector * acceleration * _elapsedSeconds;
            particle.Rotation += rotationSpeed * _elapsedSeconds;
            particle.Move *= _dragMultiplier;
        }

        public void MakeConsistent(Type limitationAttribute)
        {
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                Func<int, int, PengParameter, float[]> getValues = (ageInFrames, random, curve) =>
                    Enumerable.Range(0, ageInFrames + 1)
                        .Select(x => curve.GetValue(x / (float)ageInFrames, random))
                        .ToArray();
                var precalcs =
                    from dummy in Enumerable.Range(0, PRECALC_COUNT)
                    let random = RandomHelper.GetRandomInt()
                    let ageInFrames = (int)Math.Ceiling(_particleAge.GetValue(0, random) * AssaultWingCore.Instance.TargetFPS)
                    select new ParticleValues
                    {
                        AgeInFrames = ageInFrames,
                        Alpha = getValues(ageInFrames, random, _alpha),
                        Scale = getValues(ageInFrames, random, _scale),
                        RotationSpeed = getValues(ageInFrames, random, _rotationSpeed),
                        Acceleration = getValues(ageInFrames, random, _acceleration),
                    };
                _precalculatedValues = precalcs.ToList();
            }
        }
    }
}
