using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Collisions;
using AW2.Game.Pengs;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// Particle engine, i.e. a peng.
    /// </summary>
    /// Peng creates particles and gobs. Particles are sprites that are fully
    /// managed by the peng. Created gobs are independent of the peng and are
    /// managed by general game logic. Peng has its own coordinate system
    /// in which its particles reside. Peng's coordinate system's origin is
    /// at <c>Pos</c> in game coordinates and it is turned <c>Rotation</c> 
    /// radians from game coordinate's orientation.
    [LimitedSerialization]
    public class Peng : Gob
    {
        /// <summary>
        /// Type of coordinate system to use with particles.
        /// </summary>
        public enum CoordinateSystem
        {
            /// <summary>
            /// Peng's own coordinate system.
            /// </summary>
            Peng,

            /// <summary>
            /// Game world coordinate system.
            /// </summary>
            Game,

            /// <summary>
            /// Particles stay at the peng's position.
            /// Particle position, movement, acceleration and rotation are copied from the peng.
            /// </summary>
            FixedToPeng,
        }

        #region Peng fields

        [TypeParameter]
        private SprayEmitter _emitter;
        [TypeParameter, ShallowCopy]
        private PhysicalUpdater _updater;

        /// <summary>
        /// The coordinate system in which to interpret our particles' <c>pos</c> field.
        /// </summary>
        [TypeParameter]
        private CoordinateSystem _coordinateSystem;

        /// <summary>
        /// Does the peng relate to a <see cref="Player"/>.
        /// </summary>
        [TypeParameter]
        private bool _playerRelated;

        /// <summary>
        /// If true, then <see cref="Leader"/> doesn't affect the peng's alpha value.
        /// </summary>
        [TypeParameter]
        private bool _disregardHidingLeader;

        [TypeParameter]
        private bool _dieImmediatelyWithLeader;

        /// <summary>
        /// External input argument of the peng, between 0 and 1.
        /// </summary>
        /// This value can be set by anyone and it may affect the behaviour
        /// of the peng's emitter and updater.
        [RuntimeState]
        private float _input;

        /// <summary>
        /// Currently active particles of this peng.
        /// </summary>
        [RuntimeState]
        private List<Particle> _particles;

        /// <summary>
        /// Drawing position (Pos + DrawPosOffset) of the peng in the previous frame, or NaN if unspecified.
        /// </summary>
        private Vector2 _oldDrawPos;

        /// <summary>
        /// Last known drawing position (Pos + DrawPosOffset) after a frame update, or NaN if unspecified.
        /// </summary>
        private Vector2 _prevDrawPos;

        /// <summary>
        /// Time of last update to field <see cref="_oldDrawPos"/>
        /// </summary>
        private TimeSpan _oldPosTimestamp;

        [ExcludeFromDeepCopy]
        private Gob _leader;

        private Vector2[] _particlePosesTemp; // for Draw2D()

        #endregion Peng fields

        #region Peng properties

        public override bool IsRelevant { get { return false; } }
        public Player VisibilityLimitedTo { get; set; }

        public override Vector2 Pos
        {
            get
            {
                if (Leader == null) return base.Pos + DrawPosOffset;
                if (LeaderBone == -1)
                    return Leader.Pos + Leader.DrawPosOffset;
                else
                    return Leader.GetNamedPosition(LeaderBone);
            }
        }

        /// <summary>
        /// Drawing position (Pos + DrawPosOffset) of the peng before movement in the current frame.
        /// </summary>
        public Vector2 OldDrawPos
        {
            get
            {
                if (float.IsNaN(_oldDrawPos.X)) return Pos + DrawPosOffset;
                return _oldDrawPos;
            }
        }

        public override Vector2 Move
        {
            get
            {
                if (Leader == null) return base.Move;
                return Leader.Move;
            }
        }

        public override float Rotation
        {
            get
            {
                if (Leader == null) return base.Rotation + DrawRotationOffset;
                if (LeaderBone == -1)
                    return Leader.Rotation + Leader.DrawRotationOffset;
                else
                    return Leader.GetBoneRotation(LeaderBone) + Leader.DrawRotationOffset;
            }
        }

        /// <summary>
        /// External input argument of the peng, between 0 and 1.
        /// </summary>
        /// This value can be set by anyone and it may affect the behaviour
        /// of the peng's emitter and updater.
        public float Input { get { return _input; } set { _input = value; } }

        /// <summary>
        /// The coordinate system in which to interpret the <c>pos</c> field of
        /// the particles of this peng.
        /// </summary>
        public CoordinateSystem ParticleCoordinates { get { return _coordinateSystem; } set { _coordinateSystem = value; } }

        /// <summary>
        /// <c>null</c> or the gob that determines the origin of the peng's coordinate system.
        /// </summary>
        /// Idiom: follow the leader.
        /// <seealso cref="LeaderBone"/>
        public Gob Leader { get { return _leader; } set { _leader = value; } }

        /// <summary>
        /// The index of the bone on the leader gob that is the origin of the peng's coordinate system,
        /// or -1 if the leader's center is the origin.
        /// </summary>
        /// If <c>Leader == null</c> then this field has no effect.
        /// <seealso cref="Leader"/>
        public int LeaderBone { get; set; }

        /// <summary>
        /// The world matrix of the peng, i.e. the transformation from
        /// peng coordinates to game coordinates.
        /// </summary>
        public override Matrix WorldMatrix { get { return AWMathHelper.CreateWorldMatrix(1, Rotation + DrawRotationOffset, Pos); } }

        /// <summary>
        /// Bounding volume of the 3D visuals of the gob, in world coordinates.
        /// </summary>
        public override BoundingSphere DrawBounds { get { return new BoundingSphere(); } }

        public SprayEmitter Emitter { get { return _emitter; } }
        public bool IsMovable { get; set; }

        #endregion Peng properties

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Peng()
        {
            _emitter = new SprayEmitter();
            _updater = new PhysicalUpdater();
            _coordinateSystem = CoordinateSystem.Game;
            _playerRelated = false;
            _disregardHidingLeader = false;
            _dieImmediatelyWithLeader = false;
            _particles = new List<Particle>();

            // Set better defaults than class Gob does.
            DrawMode2D = new DrawMode2D(DrawModeType2D.Transparent);
            Movable = false;

            // Remove default collision areas set by class Gob so that we don't need to explicitly state
            // in each peng's XML definition that there are no collision areas.
            _collisionAreas = new CollisionArea[0];
        }

        public Peng(CanonicalString typeName)
            : base(typeName)
        {
            _input = 0;
            Leader = null;
            LeaderBone = -1;
            _oldDrawPos = new Vector2(Single.NaN);
            _prevDrawPos = new Vector2(Single.NaN);
            _oldPosTimestamp = TimeSpan.Zero;
            _particles = new List<Particle>();
        }

        /// <summary>
        /// Restarts the peng's choreography. Does not erase existing particles.
        /// </summary>
        public void Restart()
        {
            _emitter.Reset();
        }

        public override void Cloned()
        {
            base.Cloned();
            _emitter.Peng = this;
        }

        #region Methods related to gobs' functionality in the game world

        public override void Activate()
        {
            base.Activate();
            _updater.Activate();
            Movable = IsMovable; // By default, Peng stays put or moves with its leader
        }

        public override void Update()
        {
            base.Update();
            UpdateOldDrawPos();
            CreateParticles();
            UpdateAndKillParticles();
            CheckLeaderDeath();
            CheckDeath();
        }

        private Color Color
        {
            get
            {
                if (!_playerRelated) return Color.White;
                var playerOwner = Owner as Player;
                return playerOwner != null ? playerOwner.Color : Color.White;
            }
        }

        private float BaseAlpha { get { return !_disregardHidingLeader && Leader != null && Leader.IsHiding ? Leader.Alpha : 1; } }

        public override void Draw2D(Matrix gameToScreen, SpriteBatch spriteBatch, float scale, Player viewer)
        {
            if (VisibilityLimitedTo != null && VisibilityLimitedTo != viewer) return;
            Func<Particle, Vector2> getParticleCenterInGameWorld;
            Func<Particle, float> getDrawRotation;
            GetPosAndRotationAccessors(out getParticleCenterInGameWorld, out getDrawRotation);
            if (_particlePosesTemp == null || _particlePosesTemp.Length < _particles.Count)
                _particlePosesTemp = new Vector2[_particles.Count * 2]; // allocate extra reserve for future
            for (int i = 0; i < _particles.Count; i++)
                _particlePosesTemp[i] = getParticleCenterInGameWorld(_particles[i]);
            Vector2.Transform(_particlePosesTemp, 0, ref gameToScreen, _particlePosesTemp, 0, _particles.Count);
            var pengColor = Color.Multiply(Color, BaseAlpha);
            for (int index = 0; index < _particles.Count; index++)
            {
                var particle = _particles[index];
                var screenCenter = _particlePosesTemp[index];
                var drawRotation = -getDrawRotation(particle); // negated, because screen Y coordinates are reversed

                // Sprite depth will be our given depth layer slightly adjusted by
                // particle's position in its lifespan.
                float layerDepth = MathHelper.Clamp(DepthLayer2D * 0.99f + 0.0098f * particle.LayerDepth, 0, 1);

                var texture = particle.Texture;
                var color = Color.Multiply(pengColor, particle.Alpha);
                spriteBatch.Draw(texture, screenCenter, null, color, drawRotation,
                    new Vector2(texture.Width, texture.Height) / 2, particle.Scale * scale,
                    SpriteEffects.None, layerDepth);
            }
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Private methods

        private void CreateParticles()
        {
            var newParticles = _emitter.Emit();
            if (newParticles != null)
                _particles.AddRange(newParticles);
        }

        private void UpdateAndKillParticles()
        {
            int write = 0;
            for (int read = 0; read < _particles.Count; read++)
                if (!_updater.Update(_particles[read]))
                {
                    // Keep particles[read]
                    if (write != read) _particles[write] = _particles[read];
                    write++;
                }
            _particles.RemoveRange(write, _particles.Count - write);
        }

        private void CheckLeaderDeath()
        {
            if (Leader == null) return;
            if (!Leader.Dead && !Leader.IsDisposed) return;
            _emitter.Finish();
            if (_dieImmediatelyWithLeader || _updater.AreParticlesImmortal)
                Die();
            else
                DetachFromLeader();
        }

        private void CheckDeath()
        {
            if (_particles.Count == 0 && _emitter.Finished) Die();
        }

        /// <summary>
        /// Maintains a valid state of field <c>oldPos</c>.
        /// </summary>
        /// Call this method after frame update has finished.
        /// Calling this method more than once after one frame update
        /// has no further effects.
        private void UpdateOldDrawPos()
        {
            if (_oldPosTimestamp >= Arena.TotalTime) return;
            if (float.IsNaN(_prevDrawPos.X))
                _oldDrawPos = Pos + DrawPosOffset;
            else
                _oldDrawPos = _prevDrawPos;
            _prevDrawPos = Pos + DrawPosOffset;
            _oldPosTimestamp = Arena.TotalTime;
        }

        private void GetPosAndRotationAccessors(out Func<Particle, Vector2> getParticleCenterInGameWorld, out Func<Particle, float> getDrawRotation)
        {
            switch (_coordinateSystem)
            {
                case CoordinateSystem.Peng:
                    {
                        var pengToGame = WorldMatrix;
                        var pengRotation = Rotation;
                        getParticleCenterInGameWorld = particle => Vector2.Transform(particle.Pos, pengToGame);
                        getDrawRotation = particle => particle.Rotation + pengRotation;
                        break;
                    }
                case CoordinateSystem.Game:
                    getParticleCenterInGameWorld = particle => particle.Pos;
                    getDrawRotation = particle => particle.Rotation;
                    break;
                case CoordinateSystem.FixedToPeng:
                    {
                        var pengPos = Pos;
                        var pengRotation = Rotation;
                        getParticleCenterInGameWorld = particle => pengPos;
                        getDrawRotation = particle => pengRotation;
                        break;
                    }
                default: throw new ApplicationException("Unknown CoordinateSystem: " + _coordinateSystem);
            }
        }

        private void DetachFromLeader()
        {
            if (Leader == null) return;
            var posWithLeader = Pos;
            var moveWithLeader = Move;
            var rotationWithLeader = Rotation;
            Leader = null;
            Pos = posWithLeader;
            Move = moveWithLeader;
            Rotation = rotationWithLeader;
        }

        #endregion Private methods
    }
}
