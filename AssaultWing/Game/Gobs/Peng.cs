using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public class Peng : Gob, IConsistencyCheckable
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
        }

        #region Peng fields

        [TypeParameter]
        ParticleEmitter emitter;
        [TypeParameter, ShallowCopy]
        ParticleUpdater updater;

        /// <summary>
        /// The coordinate system in which to interpret our particles' <c>pos</c> field.
        /// </summary>
        [TypeParameter]
        CoordinateSystem coordinateSystem;

        /// <summary>
        /// Marks peng to have dependency to Player
        /// </summary>
        [TypeParameter]
        bool playerRelated;

        /// <summary>
        /// External input argument of the peng, between 0 and 1.
        /// </summary>
        /// This value can be set by anyone and it may affect the behaviour
        /// of the peng's emitter and updater.
        [RuntimeState]
        float input;

        /// <summary>
        /// Currently active particles of this peng.
        /// </summary>
        [RuntimeState]
        List<Particle> particles;

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
        Gob _leader;

        #endregion Peng fields

        #region Peng properties

        public override bool IsRelevant { get { return false; } }

        public override IEnumerable<CanonicalString> TextureNames
        {
            get { return base.TextureNames.Union(emitter.TextureNames); }
        }

        public override Vector2 Pos
        {
            get
            {
                if (Leader == null) return base.Pos;
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
                if (Leader == null) return base.Rotation;
                if (LeaderBone == -1)
                    return Leader.Rotation + Leader.DrawRotationOffset;
                else
                    return Leader.GetBoneRotation(LeaderBone);
            }
        }

        /// <summary>
        /// External input argument of the peng, between 0 and 1.
        /// </summary>
        /// This value can be set by anyone and it may affect the behaviour
        /// of the peng's emitter and updater.
        public float Input { get { return input; } set { input = value; } }

        /// <summary>
        /// If <c>true</c>, the peng won't emit new particles.
        /// </summary>
        public bool Paused { get { return emitter.Paused; } set { emitter.Paused = value; } }

        /// <summary>
        /// If true, the peng stays alive even when all particles have been emitted and they have died.
        /// </summary>
        public bool IsKeptAlive { get; set; }

        /// <summary>
        /// The coordinate system in which to interpret the <c>pos</c> field of
        /// the particles of this peng.
        /// </summary>
        public CoordinateSystem ParticleCoordinates { get { return coordinateSystem; } set { coordinateSystem = value; } }

        /// <summary>
        /// Marks peng to have dependency to Player
        /// </summary>
        public bool PlayerRelated { get { return playerRelated; } set { playerRelated = value; } }

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
        public override Matrix WorldMatrix
        {
            get
            {
                return AWMathHelper.CreateWorldMatrix(1, Rotation + DrawRotationOffset, Pos + DrawPosOffset);
            }
        }

        /// <summary>
        /// Bounding volume of the 3D visuals of the gob, in world coordinates.
        /// </summary>
        public override BoundingSphere DrawBounds { get { return new BoundingSphere(); } }

        #endregion Peng properties

        /// <summary>
        /// Creates an uninitialised peng.
        /// </summary>
        /// This constructor is only for serialisation.
        public Peng()
        {
            emitter = new SprayEmitter();
            updater = new PhysicalUpdater();
            coordinateSystem = CoordinateSystem.Game;
            particles = new List<Particle>();

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
            input = 0;
            Leader = null;
            LeaderBone = -1;
            _oldDrawPos = new Vector2(Single.NaN);
            _prevDrawPos = new Vector2(Single.NaN);
            _oldPosTimestamp = TimeSpan.Zero;
            particles = new List<Particle>();
        }

        /// <summary>
        /// Restarts the peng's choreography. Does not erase existing particles.
        /// </summary>
        public void Restart()
        {
            emitter.Reset();
        }

        #region Methods related to gobs' functionality in the game world

        public override void Activate()
        {
            base.Activate();
            Movable = false; // Peng stays put or moves with its leader
        }

        public override void LoadContent()
        {
            base.LoadContent();
            emitter.LoadContent();
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            emitter.UnloadContent();
        }

        public override void Update()
        {
            base.Update();
            UpdateOldDrawPos();

            // Create particles.
            var newParticles = emitter.Emit();
            if (newParticles != null)
                particles.AddRange(newParticles);

            // Update and kill particles.
            for (int i = 0; i < particles.Count; )
                if (updater.Update(particles[i]))
                    particles.RemoveAt(i);
                else
                    ++i;

            // Die by our leader.
            if (Leader != null && (Leader.Dead || Leader.IsDisposed))
            {
                Die();
                Leader = null;
            }

            // Die if we're finished.
            if (!IsKeptAlive && particles.Count == 0 && emitter.Finished)
                Die();
        }

        public override void Draw2D(Matrix gameToScreen, SpriteBatch spriteBatch, float scale)
        {
            var gfxViewport = Game.GraphicsDeviceService.GraphicsDevice.Viewport;
            var viewportSize = new Vector3(gfxViewport.Width, gfxViewport.Height, gfxViewport.MaxDepth - gfxViewport.MinDepth);
            var pengToGame = WorldMatrix;
            var pengColor = PlayerRelated ? Owner.PlayerColor : Color.White;
            foreach (var particle in particles)
            {
                Vector2 posCenter; // particle center position in game world coordinates
                float drawRotation;
                switch (coordinateSystem)
                {
                    case CoordinateSystem.Peng:
                        posCenter = Vector2.Transform(particle.Pos, pengToGame);
                        drawRotation = particle.Rotation + Rotation + DrawRotationOffset;
                        break;
                    case CoordinateSystem.Game:
                        posCenter = particle.Pos;
                        drawRotation = particle.Rotation;
                        break;
                    default: throw new ApplicationException("Unknown CoordinateSystem: " + coordinateSystem);
                }
                var screenCenter = Vector2.Transform(posCenter, gameToScreen);
                drawRotation = -drawRotation; // negated, because screen Y coordinates are reversed

                // Sprite depth will be our given depth layer slightly adjusted by
                // particle's position in its lifespan.
                float layerDepth = MathHelper.Clamp(DepthLayer2D * 0.99f + 0.0098f * particle.LayerDepth, 0, 1);

                var texture = emitter.Textures[particle.TextureIndex];
                var color = Color.Multiply(pengColor, particle.Alpha);
                spriteBatch.Draw(texture, screenCenter, null, color, drawRotation,
                    new Vector2(texture.Width, texture.Height) / 2, particle.Scale * scale,
                    SpriteEffects.None, layerDepth);
            }
        }

        #endregion Methods related to gobs' functionality in the game world

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

        #region IConsistencyCheckable and Clonable Members

        /// <summary>
        /// Called on a cloned object after the cloning.
        /// </summary>
        public override void Cloned()
        {
            base.Cloned();
            emitter.Peng = this;
        }

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public override void MakeConsistent(Type limitationAttribute)
        {
            base.MakeConsistent(limitationAttribute);
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                // Make sure there's no null references.
                if (emitter == null)
                    throw new Exception("Serialization error: Peng emitter not defined in " + TypeName);
                if (updater == null)
                    throw new Exception("Serialization error: Peng updater not defined in " + TypeName);
            }
            if (limitationAttribute == typeof(RuntimeStateAttribute))
            {
                // Make sure there's no null references.
                if (particles == null)
                    particles = new List<Particle>();
            }
        }

        #endregion IConsistencyCheckable Members
    }
}