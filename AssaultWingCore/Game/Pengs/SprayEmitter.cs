using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// Particle emitter spilling stuff radially outwards from a circle sector 
    /// (or a full circle) in a radius from its center.
    /// The center is located at the origin of the peng's coordinate system.
    /// </summary>
    [LimitedSerialization]
    public class SprayEmitter : IConsistencyCheckable
    {
        /// <summary>
        /// Type of initial particle facing.
        /// </summary>
        private enum FacingType
        {
            /// <summary>
            /// Particles face the average emission direction.
            /// </summary>
            Directed,

            /// <summary>
            /// Particles face the direction where they start moving,
            /// that is, away from the emission center.
            /// </summary>
            Forward,

            /// <summary>
            /// Particles face in random directions.
            /// </summary>
            Random,
        }

        #region SprayEmitter fields

        /// <summary>
        /// Names of textures of particles to emit.
        /// </summary>
        [TypeParameter]
        private CanonicalString[] _textureNames;

        /// <summary>
        /// Names of types of gobs to emit.
        /// </summary>
        [TypeParameter]
        private CanonicalString[] _gobTypeNames;

        [ExcludeFromDeepCopy]
        private Peng _peng;

        /// <summary>
        /// Radius of emission circle.
        /// </summary>
        [TypeParameter]
        private float _radius;

        /// <summary>
        /// Half width of emission sector, in radians.
        /// </summary>
        /// Setting spray angle to pi (3.14159...) will spray particles
        /// in a full circle.
        [TypeParameter]
        private float _sprayAngle;

        /// <summary>
        /// Type of particle facing at emission.
        /// </summary>
        [TypeParameter]
        private FacingType _facingType;

        /// <summary>
        /// Initial magnitude of particle velocity, in meters per second.
        /// </summary>
        /// The 'age' argument of this peng parameter will always be set to zero.
        /// The direction of particle velocity will be away from the emission center.
        [TypeParameter, ShallowCopy]
        private PengParameter _initialVelocity;

        /// <summary>
        /// Emission frequency, in number of particles per second.
        /// </summary>
        [TypeParameter]
        private float _emissionFrequency;

        /// <summary>
        /// Number of particles to create, or negative for no limit.
        /// </summary>
        [TypeParameter]
        private int _numberToCreate;

        /// <summary>
        /// Time of next particle birth, in game time.
        /// </summary>
        private TimeSpan _nextBirth;

        private int _numberCreated;
        private int _pausedCount;
        private bool _forceFinish;

        #endregion SprayEmitter fields

        #region Properties

        /// <summary>
        /// Names of textures of particles to emit.
        /// </summary>
        public CanonicalString[] TextureNames { get { return _textureNames; } }

        /// <summary>
        /// Names of types of gobs to emit.
        /// </summary>
        public CanonicalString[] GobTypeNames { get { return _gobTypeNames; } }

        /// <summary>
        /// The peng this emitter belongs to.
        /// </summary>
        public Peng Peng { get { return _peng; } set { _peng = value; } }

        /// <summary>
        /// If <c>true</c>, no particles will be emitted.
        /// </summary>
        public bool Paused { get { return _pausedCount > 0; } }

        /// <summary>
        /// Number of particles to create, or negative for no limit.
        /// </summary>
        public int NumberToCreate { set { _numberToCreate = value; } }
        public bool IsEndless { get { return _numberToCreate < 0; } }
        public float EmissionFrequency { get { return _emissionFrequency; } }

        /// <summary>
        /// <c>true</c> if emitting has finished for good, <c>false</c> otherwise.
        /// </summary>
        public bool Finished { get { return _forceFinish || (_numberToCreate > 0 && _numberCreated >= _numberToCreate); } }

        #endregion Properties

        /// <summary>
        /// This constructor only is for serialisation.
        /// </summary>
        public SprayEmitter()
        {
            _textureNames = new[] { (CanonicalString)"dummytexture" };
            _gobTypeNames = new[] { (CanonicalString)"dummygob" };
            _radius = 15;
            _sprayAngle = MathHelper.PiOver4;
            _facingType = FacingType.Random;
            _initialVelocity = new SimpleCurve();
            _emissionFrequency = 10;
            _numberToCreate = -1;
            _nextBirth = new TimeSpan(-1);
        }

        public void Pause()
        {
            _pausedCount++;
        }

        public void Resume()
        {
            if (_pausedCount <= 0) throw new ApplicationException("Cannot resume when not paused");
            // Forget about creating particles whose creation was due 
            // while we were paused.
            if (_nextBirth < Peng.Arena.TotalTime)
                _nextBirth = Peng.Arena.TotalTime;
            _pausedCount--;
        }

        public void Reset()
        {
            _numberCreated = 0;
        }

        public void Finish()
        {
            _forceFinish = true;
        }

        /// <summary>
        /// Returns created particles, adds created gobs to <c>DataEngine</c>.
        /// Returns <c>null</c> if no particles were created.
        /// </summary>
        public IEnumerable<Particle> Emit(bool skipParticles)
        {
            if (Paused) return null;
            if (Finished) return null;
            List<Particle> particles = null;

            // Initialise 'nextBirth'.
            if (_nextBirth.Ticks < 0)
            {
                // Start half an emit step back to prevent rounding errors when one emission per frame is wanted.
                _nextBirth = Peng.Arena.TotalTime - TimeSpan.FromSeconds(0.5f / _emissionFrequency);
            }

            // Count how many to create.
            int createCount = Math.Max(0, (int)(1 + _emissionFrequency * (Peng.Arena.TotalTime - _nextBirth).TotalSeconds));
            if (_numberToCreate >= 0)
            {
                createCount = Math.Min(createCount, _numberToCreate);
                _numberCreated += createCount;
            }
            // Note: TimeSpan.FromSeconds rounds to the nearest millisecond which may be too inaccurate.
            _nextBirth += TimeSpan.FromMilliseconds(1000 * createCount / _emissionFrequency);

            if (!skipParticles && createCount > 0 && _textureNames.Length > 0)
                particles = new List<Particle>();

            // Create the particles. They are created 
            // with an even distribution over the circle sector
            // defined by 'radius', the origin and 'sprayAngle'.
            for (int i = 0; i < createCount; ++i)
            {
                // Find out type of emitted thing (which gob or particle) and create it.
                int emitType = RandomHelper.GetRandomInt(_textureNames.Length + _gobTypeNames.Length);
                if (skipParticles && emitType < _textureNames.Length) continue;

                // The emitted thing init routine must be an Action<Gob>
                // so that it can be passed to Gob.CreateGob. Particle init
                // is included in the same routine because of large similarities.
                Action<Gob> emittedThingInit = gob => GobCreation(gob, createCount, i, emitType, ref particles);
                if (emitType < _textureNames.Length)
                    emittedThingInit(null);
                else
                    Gob.CreateGob<Gob>(Peng.Game, _gobTypeNames[emitType - _textureNames.Length], emittedThingInit);
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
                // Make sure there's no null references.
                if (_initialVelocity == null)
                    throw new Exception("Serialization error: SprayEmitter initialVelocity not defined");

                if (_emissionFrequency <= 0 || _emissionFrequency > 100000)
                {
                    Log.Write("Correcting insane emission frequency " + _emissionFrequency);
                    _emissionFrequency = MathHelper.Clamp(_emissionFrequency, 1, 100000);
                }
            }
            _nextBirth = new TimeSpan(-1);
        }

        #endregion

        private void GobCreation(Gob gob, int createCount, int i, int emitType, ref List<Particle> particles)
        {
            // Find out emission parameters.
            // We have to loop because some choices of parameters may not be wanted.
            int maxAttempts = 20;
            bool attemptOk = false;
            for (int attempt = 0; !attemptOk && attempt < maxAttempts; ++attempt)
            {
                bool lastAttempt = attempt == maxAttempts - 1;
                attemptOk = true;
                int random = RandomHelper.GetRandomInt();
                float directionAngle, rotation;
                Vector2 directionUnit, pos, move;
                switch (Peng.ParticleCoordinates)
                {
                    case Peng.CoordinateSystem.Peng:
                        RandomHelper.GetRandomCirclePoint(_radius, -_sprayAngle, _sprayAngle,
                            out pos, out directionUnit, out directionAngle);
                        move = _initialVelocity.GetValue(0, random) * directionUnit;
                        switch (_facingType)
                        {
                            case FacingType.Directed: rotation = 0; break;
                            case FacingType.Forward: rotation = directionAngle; break;
                            case FacingType.Random: rotation = RandomHelper.GetRandomFloat(0, MathHelper.TwoPi); break;
                            default: throw new Exception("SprayEmitter: Unhandled particle facing type " + _facingType);
                        }
                        break;
                    case Peng.CoordinateSystem.Game:
                        {
                            float posWeight = (i + 1) / (float)createCount;
                            var startPos = Peng.OldDrawPos;
                            var endPos = Peng.Pos + Peng.DrawPosOffset;
                            var iPos = Vector2.Lerp(startPos, endPos, posWeight);
                            var drawRotation = Peng.Rotation + Peng.DrawRotationOffset;
                            RandomHelper.GetRandomCirclePoint(_radius, drawRotation - _sprayAngle, drawRotation + _sprayAngle,
                                out pos, out directionUnit, out directionAngle);
                            pos += iPos;
                            move = Peng.Move + _initialVelocity.GetValue(0, random) * directionUnit;

                            // HACK: 'move' will be added to 'pos' in PhysicalUpdater during this same frame
                            pos -= move * (float)Peng.Game.GameTime.ElapsedGameTime.TotalSeconds;

                            switch (_facingType)
                            {
                                case FacingType.Directed: rotation = Peng.Rotation; break;
                                case FacingType.Forward: rotation = directionAngle; break;
                                case FacingType.Random: rotation = RandomHelper.GetRandomFloat(0, MathHelper.TwoPi); break;
                                default: throw new Exception("SprayEmitter: Unhandled particle facing type " + _facingType);
                            }
                        }
                        break;
                    case Peng.CoordinateSystem.FixedToPeng:
                        pos = Vector2.Zero;
                        move = Vector2.Zero;
                        rotation = 0;
                        directionAngle = 0;
                        break;
                    default:
                        throw new ApplicationException("SprayEmitter: Unhandled peng coordinate system " + Peng.ParticleCoordinates);
                }

                // Set the thing's parameters.
                if (emitType < _textureNames.Length)
                {
                    var particle = new Particle
                    {
                        Alpha = 1,
                        Move = move,
                        PengInput = Peng.Input,
                        Pos = pos,
                        Random = random,
                        Direction = directionAngle,
                        DirectionVector = Vector2.UnitX.Rotate(directionAngle),
                        Rotation = rotation,
                        Scale = 1,
                        Texture = Peng.Game.Content.Load<Texture2D>(TextureNames[emitType]),
                    };
                    particles.Add(particle);
                }
                else
                {
                    // Bail out if the position is not free for the gob.
                    if (!lastAttempt && !Peng.Arena.IsFreePosition(new Circle(pos, Gob.SMALL_GOB_PHYSICAL_RADIUS)))
                    {
                        attemptOk = false;
                        continue;
                    }
                    gob.Owner = Peng.Owner;
                    gob.ResetPos(pos, move, rotation);
                    Peng.Arena.Gobs.Add(gob);
                }
            }
        }
    }
}
