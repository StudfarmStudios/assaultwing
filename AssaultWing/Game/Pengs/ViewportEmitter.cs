using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using Rectangle = AW2.Helpers.Geometric.Rectangle;
using AW2.Graphics;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// Particle emitter maintaining a homogeneous fill of things in all 
    /// viewports up to a density.
    /// </summary>
    /// Things (particles and gobs) are created at random locations inside
    /// all viewports and also outside all viewports up to a distance. Creating
    /// things outside viewports is meant to compensate for things moving in
    /// one direction so that one edge of a viewport soon has less things
    /// than other parts of the viewport.
    /// 
    /// Things are also created when viewports move. The aim is to fill the
    /// area that became exposed in any viewport during the last frame. The 
    /// expected density of thusly created things is a parameter.
    /// 
    /// The <c>Pos</c> and <c>Move</c> properties of the peng is redundant.
    /// The <c>Rotation</c> property of the peng affects initial particle rotation.
    [LimitedSerialization]
    public class ViewportEmitter : ParticleEmitter, IConsistencyCheckable
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
            /// Particles face the direction where they start moving.
            /// </summary>
            Forward,

            /// <summary>
            /// Particles face in random directions.
            /// </summary>
            Random,
        }

        #region ViewportEmitter fields

        /// <summary>
        /// Maximum difference of initial particle rotation from peng's rotation, in radians.
        /// </summary>
        /// Setting spray angle to pi (3.14159...) will make particles face
        /// a totally random direction at emission.
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
        /// The direction of particle velocity will be in the particle's emission direction.
        [TypeParameter]
        PengParameter initialVelocity;

        /// <summary>
        /// Emission frequency, in number of particles per second.
        /// </summary>
        [TypeParameter]
        float emissionFrequency;

        /// <summary>
        /// Time of next particle birth, in game time.
        /// </summary>
        TimeSpan nextBirth;

        /// <summary>
        /// Areas covered by viewports in last update, in game coordinates.
        /// </summary>
        List<Rectangle> oldViewports;

        #endregion SprayEmitter fields

        /// <summary>
        /// <c>true</c> if emitting has finished for good
        /// <c>false</c> otherwise.
        /// </summary>
        public override bool Finished { get { return false; } }

        /// <summary>
        /// Creates an uninitialised viewport emitter.
        /// </summary>
        /// This constructor is for serialisation.
        public ViewportEmitter()
        {
            sprayAngle = MathHelper.Pi;
            facingType = FacingType.Forward;
            initialVelocity = new CurveLerp();
            emissionFrequency = 10;
            nextBirth = new TimeSpan(-1);
        }

        /// <summary>
        /// Returns created particles, adds created gobs to <c>DataEngine</c>.
        /// </summary>
        /// <returns>Created particles, or <c>null</c> if no particles were created.</returns>
        public override ICollection<Particle> Emit()
        {
            TimeSpan now = AssaultWing.Instance.GameTime.TotalArenaTime;
            float z = Peng.Layer.Z;
            List<Particle> particles = null;

            // Find out newly exposed areas in viewports.
            // We assume that the viewports are iterated in the same order
            // as last frame. Furthermore, we don't mind overlapping viewports.
            // This might lead to many or too few things being created when
            // there are two or more viewports.
            List<Rectangle> viewports = new List<Rectangle>();
            AssaultWing.Instance.DataEngine.ForEachViewport(delegate(AWViewport viewport)
            {
                viewports.Add(new Rectangle(viewport.WorldAreaMin(z), viewport.WorldAreaMax(z)));
            });
            List<Rectangle> exposedAreas = new List<Rectangle>();
            for (int i = 0; i < viewports.Count && i < oldViewports.Count; ++i)
            {
                Rectangle @new = viewports[i];
                Rectangle old = oldViewports[i];

                // New exposure on the left of the viewport
                if (@new.Min.X < old.Min.X)
                    exposedAreas.Add(new Rectangle(@new.Min.X, @new.Min.Y,
                        Math.Min(old.Min.X, @new.Max.X), @new.Max.Y));

                // New exposure on the right of the viewport
                if (@new.Max.X > old.Max.X)
                    exposedAreas.Add(new Rectangle(Math.Max(old.Max.X, @new.Min.X), @new.Min.Y,
                        @new.Max.X, @new.Max.Y));

                // New exposure at the bottom of the viewport, minus left and right
                if (@new.Min.Y < old.Min.Y && Math.Max(old.Min.X, @new.Min.X) < Math.Min(old.Max.X, @new.Max.X))
                    exposedAreas.Add(new Rectangle(Math.Max(old.Min.X, @new.Min.X), @new.Min.Y,
                        Math.Min(old.Max.X, @new.Max.X), old.Min.Y));

                // New exposure at the top of the viewport, minus left and right
                if (@new.Max.Y > old.Max.Y && Math.Max(old.Min.X, @new.Min.X) < Math.Min(old.Max.X, @new.Max.X))
                    exposedAreas.Add(new Rectangle(Math.Max(old.Min.X, @new.Min.X), @old.Max.Y,
                        Math.Min(old.Max.X, @new.Max.X), @new.Max.Y));
            }
            oldViewports = viewports;


            // Initialise 'nextBirth'.
            if (nextBirth.Ticks < 0)
                nextBirth = AssaultWing.Instance.GameTime.TotalArenaTime;

            // Count how many to create.
            int createCount = Math.Max(0, (int)(1 + emissionFrequency * (now - nextBirth).TotalSeconds));
            nextBirth += TimeSpan.FromSeconds(createCount / emissionFrequency);

            if (createCount > 0 && textureNames.Length > 0)
                particles = new List<Particle>();

            // Create the particles. They are created 
            // with an even distribution over the circle sector
            // defined by 'radius', the origin and 'sprayAngle'.
            for (int i = 0; i < createCount; ++i)
            {
                // Find out emission parameters.
                float pengInput = Peng.Input;
                int random = RandomHelper.GetRandomInt();
                float directionAngle, rotation;
                Vector2 directionUnit, pos, move;
                switch (Peng.ParticleCoordinates)
                {
                    case Peng.CoordinateSystem.Game:
                        {
                            directionAngle = Peng.Rotation + RandomHelper.GetRandomFloat(-sprayAngle, sprayAngle);
                            directionUnit = new Vector2((float)Math.Cos(directionAngle), (float)Math.Sin(directionAngle));
                            pos = Vector2.Zero; // TODO: åarticle pos
                            move = Peng.Move + initialVelocity.GetValue(0, pengInput, random) * directionUnit;
                            switch (facingType)
                            {
                                case FacingType.Directed: rotation = Peng.Rotation; break;
                                case FacingType.Forward: rotation = directionAngle; break;
                                case FacingType.Random: rotation = RandomHelper.GetRandomFloat(0, MathHelper.TwoPi); break;
                                default: throw new Exception("ViewportEmitter: Unhandled particle facing type " + facingType);
                            }
                        }
                        break;
                    default:
                        throw new Exception("ViewportEmitter: Unhandled peng coordinate system " + Peng.ParticleCoordinates);
                }

                // Find out type of emitted thing (gob or particle) and create it.
                int emitType = RandomHelper.GetRandomInt(textureNames.Length + gobTypeNames.Length);
                if (emitType < textureNames.Length)
                {
                    // Emit a particle.
                    Particle particle = new Particle();
                    particle.alpha = 1;
                    particle.birthTime = now;
                    particle.move = move;
                    particle.pengInput = pengInput;
                    particle.pos = pos;
                    particle.random = random;
                    particle.rotation = rotation;
                    particle.scale = 1;
                    particle.textureIndex = emitType;
                    particles.Add(particle);
                }
                else
                {
                    // Emit a gob.
                    Gob.CreateGob(gobTypeNames[emitType - textureNames.Length], gob =>
                    {
                        gob.Owner = Peng.Owner;
                        gob.Pos = pos;
                        gob.Move = move;
                        gob.Rotation = rotation;
                        Peng.Arena.Gobs.Add(gob);
                    });
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
                if (Peng.ParticleCoordinates == Peng.CoordinateSystem.Peng)
                {
                    Log.Write("ViewportEmitter: Correcting unsupported coordinate system " + Peng.ParticleCoordinates);
                    Peng.ParticleCoordinates = Peng.CoordinateSystem.Game;
                }
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
