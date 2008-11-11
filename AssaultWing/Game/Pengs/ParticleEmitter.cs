using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// Creates particles and gobs.
    /// </summary>
    /// A particle emitter is part of a peng.
    /// <see cref="AW2.Game.Gobs.Peng"/>
    public abstract class ParticleEmitter
    {
        #region ParticleEmitter fields

        /// <summary>
        /// Names of textures of particles to emit.
        /// </summary>
        [TypeParameter]
        protected string[] textureNames;

        /// <summary>
        /// Names of types of gobs to emit.
        /// </summary>
        [TypeParameter]
        protected string[] gobTypeNames;

        /// <summary>
        /// The peng where this emitter belongs to.
        /// </summary>
        protected Peng peng;

        #endregion ParticleEmitter fields

        #region ParticleEmitter properties

        /// <summary>
        /// Names of textures of particles to emit.
        /// </summary>
        public string[] TextureNames { get { return textureNames; } }

        /// <summary>
        /// Names of types of gobs to emit.
        /// </summary>
        public string[] GobTypeNames { get { return gobTypeNames; } }

        /// <summary>
        /// The peng where this emitter belongs to.
        /// </summary>
        public Peng Peng { get { return peng; } set { peng = value; } }

        /// <summary>
        /// <c>true</c> if emitting has finished for good
        /// <c>false</c> otherwise.
        /// </summary>
        public abstract bool Finished { get; }

        #endregion ParticleEmitter properties

        /// <summary>
        /// Returns created particles, adds created gobs to <c>DataEngine</c>.
        /// </summary>
        /// <returns>Created particles, or <c>null</c> if no particles were created.</returns>
        public abstract ICollection<Particle> Emit();

        /// <summary>
        /// Creates an uninitialised particle emitter.
        /// </summary>
        /// This constructor is for serialisation.
        public ParticleEmitter()
        {
            textureNames = new string[] { "dummytexture" };
            gobTypeNames = new string[] { "dummygob" };
        }
    }

    /// <summary>
    /// Particle emitter spilling stuff radially outwards from a circle sector 
    /// (or a full circle) in a radius from its center.
    /// </summary>
    /// The center is located at the origin of the peng's coordinate system.
    [LimitedSerialization]
    public class SprayEmitter : ParticleEmitter, IConsistencyCheckable
    {
        /// <summary>
        /// Type of initial particle facing.
        /// </summary>
        enum FacingType
        {
            /// <summary>
            /// Particles face the average emission direction.
            /// </summary>
            Directed,

            /// <summary>
            /// Particles face away from the emission center.
            /// </summary>
            Radial,

            /// <summary>
            /// Particles face in random directions.
            /// </summary>
            Random,
        }

        #region SprayEmitter fields

        /// <summary>
        /// Radius of emission circle.
        /// </summary>
        [TypeParameter]
        float radius;

        /// <summary>
        /// Half width of emission sector, in radians.
        /// </summary>
        /// Setting spray angle to pi (3.14159...) will spray particles
        /// in a full circle.
        [TypeParameter]
        float sprayAngle;

        /// <summary>
        /// Type of particle facing at emission.
        /// </summary>
        [TypeParameter]
        FacingType facingType;

        /// <summary>
        /// Initial magnitude of particle velocity, in meters per second.
        /// </summary>
        /// The 'age' argument of this peng parameter will always be set to zero.
        /// The direction of particle velocity will be away from the emission center.
        [TypeParameter]
        PengParameter initialVelocity;

        /// <summary>
        /// Emission frequency, in number of particles per second.
        /// </summary>
        [TypeParameter]
        float emissionFrequency;

        /// <summary>
        /// Number of particles to create, or negative for no limit.
        /// </summary>
        [TypeParameter, RuntimeState]
        int numberToCreate;

        /// <summary>
        /// Time of next particle birth, in game time.
        /// </summary>
        TimeSpan nextBirth;

        #endregion SprayEmitter fields

        /// <summary>
        /// <c>true</c> if emitting has finished for good
        /// <c>false</c> otherwise.
        /// </summary>
        public override bool Finished { get { return numberToCreate == 0; } }

        /// <summary>
        /// Creates an uninitialised spray emitter.
        /// </summary>
        /// This constructor is for serialisation.
        public SprayEmitter()
        {
            radius = 15;
            sprayAngle = MathHelper.PiOver4;
            facingType = FacingType.Random;
            initialVelocity = new CurveLerp();
            emissionFrequency = 10;
            numberToCreate = -1;
            nextBirth = new TimeSpan(-1);
        }

        /// <summary>
        /// Returns created particles, adds created gobs to <c>DataEngine</c>.
        /// </summary>
        /// <returns>Created particles, or <c>null</c> if no particles were created.</returns>
        public override ICollection<Particle> Emit()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            TimeSpan now = AssaultWing.Instance.GameTime.TotalGameTime;
            List<Particle> particles = null;

            // Initialise 'nextBirth'.
            if (nextBirth.Ticks < 0)
                nextBirth = AssaultWing.Instance.GameTime.TotalGameTime;

            // Count how many to create.
            int createCount = Math.Max(0, (int)(1 + emissionFrequency * (now - nextBirth).TotalSeconds));
            if (numberToCreate >= 0)
            {
                createCount = Math.Min(createCount, numberToCreate);
                numberToCreate -= createCount;
            }
            nextBirth += TimeSpan.FromSeconds(createCount / emissionFrequency);

            if (createCount > 0 && textureNames.Length > 0)
                particles = new List<Particle>();

            // Create the particles at even spaces between oldPos and pos.
            Vector2 startPos = peng.OldPos;
            Vector2 endPos = peng.Pos;
            for (int i = 0; i < createCount; ++i)
            {
                int emitType = RandomHelper.GetRandomInt(textureNames.Length + gobTypeNames.Length);
                if (emitType < textureNames.Length)
                {
                    // Emit a particle.
                    float weight = (i + 1) / (float)createCount;
                    Vector2 iPos = Vector2.Lerp(startPos, endPos, weight);
                    Particle particle = new Particle();
                    particle.random = RandomHelper.GetRandomInt();
                    particle.pengInput = peng.Input;
                    particle.alpha = 1;
                    particle.birthTime = now;

                    // Randomise position with an even distribution over the circle sector
                    // defined by 'radius', the origin and 'sprayAngle'.
                    switch (peng.ParticleCoordinates)
                    {
                        case Peng.CoordinateSystem.Peng:
                            {
                                float directionAngle = RandomHelper.GetRandomFloat(-sprayAngle, sprayAngle);
                                Vector2 directionUnit = new Vector2(
                                     (float)Math.Cos(directionAngle),
                                     (float)Math.Sin(directionAngle));
                                float distance = radius * (float)Math.Sqrt(RandomHelper.globalRandomGenerator.NextDouble());
                                particle.pos = distance * directionUnit;
                                particle.move = initialVelocity.GetValue(0, particle.pengInput, particle.random) * directionUnit;
                                switch (facingType)
                                {
                                    case FacingType.Directed: particle.rotation = 0; break;
                                    case FacingType.Radial: particle.rotation = directionAngle; break;
                                    case FacingType.Random: particle.rotation = RandomHelper.GetRandomFloat(0, MathHelper.TwoPi); break;
                                }
                            }
                            break;
                        case Peng.CoordinateSystem.Game:
                            {
                                float directionAngle = peng.Rotation + RandomHelper.GetRandomFloat(-sprayAngle, sprayAngle);
                                Vector2 directionUnit = new Vector2(
                                     (float)Math.Cos(directionAngle),
                                     (float)Math.Sin(directionAngle));
                                float distance = radius * (float)Math.Sqrt(RandomHelper.globalRandomGenerator.NextDouble());
                                particle.pos = iPos + distance * directionUnit;
                                particle.move = initialVelocity.GetValue(0, particle.pengInput, particle.random) * directionUnit;
                                switch (facingType)
                                {
                                    case FacingType.Directed: particle.rotation = peng.Rotation; break;
                                    case FacingType.Radial: particle.rotation = directionAngle; break;
                                    case FacingType.Random: particle.rotation = RandomHelper.GetRandomFloat(0, MathHelper.TwoPi); break;
                                }
                            }
                            break;
                    }
                    particle.scale = 1;
                    particle.textureName = textureNames[emitType];

                    particles.Add(particle);
                }
                else
                {
                    // Emit a gob.
                    Gob gob = Gob.CreateGob(gobTypeNames[i - textureNames.Length]);
                    // TODO: initialise gob
                    data.AddGob(gob);
                }
            }

            return particles;
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
                if (emissionFrequency <= 0 || emissionFrequency > 100000)
                {
                    Log.Write("Correcting insane emission frequency " + emissionFrequency);
                    emissionFrequency = MathHelper.Clamp(emissionFrequency, 1, 100000);
                }
            }
        }

        #endregion
    }
}
