using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarseerPhysics.Collision;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using AW2.Core;
using AW2.Game.Arenas;
using AW2.Game.GobUtils;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;
using AW2.Sound;
using Point = AW2.Helpers.Geometric.Point;
using Rectangle = AW2.Helpers.Geometric.Rectangle;

namespace AW2.Game
{
    /// <summary>
    /// A game arena; a rectangular area where gobs exist and interact. Contains the physical world
    /// maintained by Farseer Physics Engine.
    /// </summary>
    /// <see cref="Arena"/> uses limited (de)serialisation for saving and loading arenas.
    /// Therefore only those fields that describe the arenas initial state -- not 
    /// fields that describe the arenas state during gameplay -- should be marked as 
    /// 'type parameters' by <see cref="AW2.Helpers.TypeParameterAttribute"/>
    [LimitedSerialization]
    [System.Diagnostics.DebuggerDisplay("{Info.Name} Dimensions:{Info.Dimensions} Layers:{Layers.Count} Gobs:{Gobs.Count}")]
    public class Arena : IConsistencyCheckable
    {
        #region Type definitions

        [Flags]
        public enum CollisionSoundTypes
        {
            None = 0x00,
            WallCollision = 0x01,
            ShipCollision = 0x02,
        };

        public class CollisionEvent
        {
            private const float COLLISION_SOUND_MINIMUM_IMPULSE = 2000;

            private CollisionArea _area1;
            private CollisionArea _area2;
            private int _gob1ID;
            private int _gob2ID;
            private int _area1ID;
            private int _area2ID;
            private float _impulse;

            public bool SkipReversibleSideEffects { get; set; }
            public bool SkipIrreversibleSideEffects { get; set; }
            public bool IrreversibleSideEffectsPerformed { get; private set; }

            /// <summary>
            /// Creates an uninitialized collision event. Initialize by calling
            /// <see cref="SetCollisionAreas"/> and <see cref="SetImpulse"/>.
            /// </summary>
            public CollisionEvent()
            {
                _gob1ID = _gob2ID = Gob.INVALID_ID;
                _area1ID = _area2ID = -1;
            }

            public void SetCollisionAreas(CollisionArea area1, CollisionArea area2)
            {
                _area1 = area1;
                _area2 = area2;
                var gob1 = area1.Owner;
                var gob2 = area2.Owner;
                _gob1ID = gob1.ID;
                _gob2ID = gob2.ID;
                // Note: Walls are the only gobs to have over 4 collision areas; there can be hundreds of them.
                // To fit collision area IDs into as few bits as possible, walls will always collide with
                // their first collision area. This should not have a visible effect on game clients.
                _area1ID = gob1 is Gobs.Wall ? 0 : gob1.GetCollisionAreaID(area1);
                _area2ID = gob2 is Gobs.Wall ? 0 : gob2.GetCollisionAreaID(area2);
            }

            public void SetImpulse(float impulse)
            {
                _impulse = impulse;
            }

            public void Handle()
            {
                if (_area1 == null || _area2 == null) return; // May happen on a game client.
                if (!SkipReversibleSideEffects) HandleReversibleSideEffects();
                if (!SkipIrreversibleSideEffects) HandleIrreversibleSideEffects();
            }

            public void Serialize(NetworkBinaryWriter writer)
            {
                writer.Write((short)_gob1ID);
                writer.Write((short)_gob2ID);
                if (_area1ID > 0x03 || _area2ID > 0x03)
                    throw new ApplicationException("Too large collision area identifier: " + _area1ID + " or " + _area2ID);
                var mixedData = (byte)((byte)_area1ID & 0x03);
                mixedData |= (byte)(((byte)_area2ID & 0x03) << 2);
                if (_impulse >= COLLISION_SOUND_MINIMUM_IMPULSE) mixedData |= 0x10;
                writer.Write((byte)mixedData);
            }

            public static CollisionEvent Deserialize(NetworkBinaryReader reader)
            {
                var gob1ID = reader.ReadInt16();
                var gob2ID = reader.ReadInt16();
                var mixedData = reader.ReadByte();
                var area1ID = mixedData & 0x03;
                var area2ID = (mixedData >> 2) & 0x03;
                var strongImpulse = (mixedData & 0x10) != 0;
                var impulse = strongImpulse ? COLLISION_SOUND_MINIMUM_IMPULSE : 0;
                var collisionEvent = new CollisionEvent();
                var arena = AssaultWingCore.Instance.DataEngine.Arena;
                var gob1 = arena.FindGob(collisionEvent._gob1ID).Item2;
                var gob2 = arena.FindGob(collisionEvent._gob2ID).Item2;
                if (gob1 == null && gob2 == null) return collisionEvent;
                var area1 = gob1.GetCollisionArea(collisionEvent._area1ID);
                var area2 = gob2.GetCollisionArea(collisionEvent._area2ID);
                collisionEvent.SetCollisionAreas(area1, area2);
                collisionEvent.SetImpulse(impulse);
                return collisionEvent;
            }

            private void HandleReversibleSideEffects()
            {
                _area1.Owner.CollideReversible(_area1, _area2);
                _area2.Owner.CollideReversible(_area2, _area1);
            }

            private void HandleIrreversibleSideEffects()
            {
                var irreversibleSideEffects = _area1.Owner.CollideIrreversible(_area1, _area2) | _area2.Owner.CollideIrreversible(_area2, _area1);
                var game = _area1.Owner.Game;
                var sounds = GetCollisionSounds();
                if (sounds.HasFlag(CollisionSoundTypes.WallCollision))
                    game.SoundEngine.PlaySound("Collision", _area1.Owner);
                if (sounds.HasFlag(CollisionSoundTypes.ShipCollision))
                    game.SoundEngine.PlaySound("Shipcollision", _area1.Owner);
                IrreversibleSideEffectsPerformed = irreversibleSideEffects || sounds != CollisionSoundTypes.None;
            }

            private CollisionSoundTypes GetCollisionSounds()
            {
                if (!_area1.IsPhysical || !_area2.IsPhysical) return CollisionSoundTypes.None;
                if (_impulse < COLLISION_SOUND_MINIMUM_IMPULSE) return CollisionSoundTypes.None;
                if (_area1.Owner is Gobs.Ship && _area2.Owner is Gobs.Ship) return CollisionSoundTypes.ShipCollision;
                if (_area1.Owner is Gobs.Ship || _area2.Owner is Gobs.Ship)
                {
                    if (_area1.Owner.Movable && _area2.Owner.Movable) return CollisionSoundTypes.ShipCollision;
                    return CollisionSoundTypes.WallCollision;
                }
                return CollisionSoundTypes.None;
            }
        }

        private struct CollisionEventKey
        {
            private int gob1ID;
            private int gob2ID;

            public CollisionEventKey(Gob gob1, Gob gob2)
            {
                gob1ID = Math.Min(gob1.ID, gob2.ID);
                gob2ID = Math.Max(gob1.ID, gob2.ID);
            }

            public override int GetHashCode()
            {
                return gob1ID ^ (gob2ID << 16);
            }

            public override bool Equals(object other)
            {
                if (!(other is CollisionEventKey)) return false;
                var otherKey = (CollisionEventKey)other;
                return gob1ID == otherKey.gob1ID && gob2ID == otherKey.gob2ID;
            }
        }

        #endregion Type definitions

        #region General fields

        private AssaultWingCore _game;
        private GobCollection _gobs;
        private World _world;

        /// <summary>
        /// Layers of the arena.
        /// </summary>
        [TypeParameter]
        private List<ArenaLayer> _layers;

        [TypeParameter]
        private ArenaInfo _info;

        [TypeParameter]
        private BackgroundMusic _backgroundMusic;

        [TypeParameter]
        private string _binFilename;

        [TypeParameter]
        private LightingSettings _lighting;

        [TypeParameter]
        private Vector2 _gravity;

        private List<SoundInstance> _ambientSounds = new List<SoundInstance>();

        #endregion General fields

        #region Collision related fields

        private const float GOB_DESTRUCTION_BOUNDARY = 1000;
        private const int FREE_POS_MAX_ATTEMPTS = 50;
        private const float MINIMUM_COLLISION_DELTA = 20;

        private Dictionary<CollisionEventKey, CollisionEvent> _collisionEvents = new Dictionary<CollisionEventKey, CollisionEvent>();

        #endregion Collision related fields

        #region Arena properties

        public AssaultWingCore Game
        {
            get { return _game; }
            private set
            {
                _game = value;
                SetGameAndArenaToGobs();
            }
        }
        public byte ID { get; set; }
        public ArenaInfo Info { get { return _info; } set { _info = value; } }
        public Vector2 Dimensions { get { return Info.Dimensions; } }
        public Rectangle BoundedAreaNormal { get { return new Rectangle(Vector2.Zero, Dimensions); } }
        public Rectangle BoundedAreaExtreme { get { return new Rectangle(-new Vector2(GOB_DESTRUCTION_BOUNDARY), Dimensions + new Vector2(GOB_DESTRUCTION_BOUNDARY)); } }

        /// <summary>
        /// Filename of the arena's binary data container.
        /// </summary>
        public string BinFilename { get { return _binFilename; } set { _binFilename = value; } }

        /// <summary>
        /// Binary data container.
        /// </summary>
        public ArenaBin Bin { get; private set; }

        /// <summary>
        /// Total time the arena has been running.
        /// </summary>
        public TimeSpan TotalTime { get; set; }

        /// <summary>
        /// Current frame number, or the number of frames elapsed since gameplay started in the arena.
        /// </summary>
        public int FrameNumber { get; set; }

        /// <summary>
        /// Layers of the arena.
        /// </summary>
        public List<ArenaLayer> Layers { get { return _layers; } }

        /// <summary>
        /// Is the arena meant to be played. Otherwise it is only for looking at.
        /// </summary>
        public bool IsForPlaying { get; set; }

        /// <summary>
        /// Gobs in the arena. Reflects the data in <see cref="Layers"/>.
        /// </summary>
        public GobCollection Gobs
        {
            get { return _gobs; }
            private set
            {
                _gobs = value;
                _gobs.Added += GobAddedHandler;
                _gobs.Removing += GobRemovingHandler;
                _gobs.Removed += GobRemovedHandler;
            }
        }
        public IEnumerable<Gob> GobsInRelevantLayers { get { return Gobs.GameplayLayer.Gobs.Union(Gobs.GameplayOverlayLayer.Gobs); } }

        public BackgroundMusic BackgroundMusic { get { return _backgroundMusic; } }

        public bool IsActive { get { return this == Game.DataEngine.Arena; } }

        public event Action<Gob> GobAdded;
        public event Action<Gob> GobRemoved;

        #endregion Arena properties

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Arena()
        {
            Info = new ArenaInfo { Name = (CanonicalString)"dummyarena", Dimensions = new Vector2(4000, 4000) };
            _layers = new List<ArenaLayer>();
            _layers.Add(new ArenaLayer());
            Gobs = new GobCollection(_layers);
            Bin = new ArenaBin();
            _gravity = new Vector2(0, -30);
            _lighting = new LightingSettings();
            _backgroundMusic = new BackgroundMusic();
        }

        #region Public methods

        /// <summary>
        /// Loads an arena from file, or throws an exception on failure.
        /// </summary>
        public static Arena FromFile(AssaultWingCore game, string filename)
        {
            var arena = (Arena)TypeLoader.LoadTemplate(filename, typeof(Arena), typeof(TypeParameterAttribute), false);
            arena.Game = game;
            return arena;
        }

        /// <summary>
        /// Unloads graphical content required by the arena.
        /// </summary>
        public void UnloadContent()
        {
            foreach (var gob in Gobs) gob.UnloadContent();
        }

        /// <summary>
        /// Releases resources allocated for the arena.
        /// </summary>
        public void Dispose()
        {
            UninitializeAmbientSounds();
            UnloadContent();
            foreach (var gob in Gobs) gob.Dispose();
            Gobs.Clear();
        }

        /// <summary>
        /// Initializes the arena for a new play session.
        /// </summary>
        public void Initialize()
        {
            TotalTime = TimeSpan.Zero;
            FrameNumber = 0;
            InitializeWorld();
            InitializeGobs();
            InitializeAmbientSounds();
        }

        private void UninitializeAmbientSounds()
        {
            foreach (var sound in _ambientSounds) sound.Stop();
            _ambientSounds.Clear();
        }

        private void InitializeAmbientSounds()
        {
            // Just in case
            UninitializeAmbientSounds();

            // Background
            _ambientSounds.Add(Game.SoundEngine.CreateSound("amazonasAmbience"));

            // HACK! (Add sound property on objects or sound source gob)

            var goldObjectId = new CanonicalString("amazon_chest_1");
            var shovelObjectId = new CanonicalString("amazon_shovel_1");
            foreach (var layer in _layers)
            {
                foreach (var gob in layer.Gobs)
                {
                    var names = gob.ModelNames.ToArray();

                    if (names.Contains(goldObjectId))
                    {
                        _ambientSounds.Add(Game.SoundEngine.CreateSound("amazonasCoins", gob));
                    }
                    else if (gob.ModelNames.Contains(shovelObjectId))
                    {
                        _ambientSounds.Add(Game.SoundEngine.CreateSound("amazonasLeaves", gob));
                    }

                    gob.Layer = layer;
                }
            }
            foreach (var sound in _ambientSounds) sound.Play();
        }

        /// <summary>
        /// Updates arena contents for the current time step.
        /// </summary>
        public void Update()
        {
            _world.Step((float)Game.GameTime.ElapsedGameTime.TotalSeconds);
            PerformCustomCollisions();
            if (Game.NetworkMode != NetworkMode.Client)
                foreach (var gob in Gobs.GameplayLayer.Gobs)
                    if (!Geometry.Intersect(BoundedAreaExtreme, new Point(gob.Pos)))
                        gob.Die();
        }

        private void PerformCustomCollisions()
        {
            foreach (var collisionEvent in _collisionEvents.Values)
            {
                var impulse = 100; // TODO !!! Find collision impulse for this event
                collisionEvent.SetImpulse(impulse);
                collisionEvent.Handle();
            }
        }

        public IEnumerable<CollisionEvent> GetCollisionEvents()
        {
            return _collisionEvents.Values;
        }

        public void ResetCollisionEvents()
        {
            _collisionEvents.Clear();
        }

        [Obsolete("Use Farseer Fixtures and AW Collision methods")]
        public IEnumerable<Gob> GetOverlappingGobs(CollisionArea area, CollisionAreaType types)
        {
            // !!! throw new NotImplementedException();
            yield break;
        }

        /// <summary>
        /// Tries to return a position in the arena where there is no physical obstacles in a radius.
        /// </summary>
        /// <param name="area">The area where to look for a position.</param>
        /// <returns>A position in the area where a gob might be overlap consistent.</returns>
        public Vector2 GetFreePosition(float radius, IGeomPrimitive area)
        {
            Vector2 result;
            GetFreePosition(radius, area, out result);
            return result;
        }

        /// <summary>
        /// Tries to return a position in the arena where there is no physical obstacles in a radius.
        /// </summary>
        /// <param name="radius">The radius of requested free space.</param>
        /// <param name="area">The area where to look for a position.</param>
        /// <param name="result">Best try for a position in the area where the gob is overlap consistent.</param>
        /// <returns>true if <paramref name="result"/> is legal and overlap consistent,
        /// false if the search failed.</returns>
        public bool GetFreePosition(float radius, IGeomPrimitive area, out Vector2 result)
        {
            // Iterate in the area for a while, looking for a free position.
            // Ultimately give up and return something that may be bad.
            for (int attempt = 1; attempt < FREE_POS_MAX_ATTEMPTS; ++attempt)
            {
                var tryPos = Geometry.GetRandomLocation(area);
                if (IsFreePosition(new Circle(tryPos, radius)))
                {
                    result = tryPos;
                    return true;
                }
            }
            result = Geometry.GetRandomLocation(area);
            return false;
        }

        /// <summary>
        /// Is an area free of physical gobs.
        /// </summary>
        public bool IsFreePosition(IGeomPrimitive area)
        {
            if (!BoundedAreaNormal.Contains(area.BoundingBox)) return false;
            var shape = area.GetShape();
            AABB shapeAabb;
            Transform shapeTransform = new Transform();
            shapeTransform.SetIdentity();
            shape.ComputeAABB(out shapeAabb, ref shapeTransform, 0);
            Transform fixtureTransform;
            var isFree = true;
            _world.QueryAABB(otherFixture =>
            {
                otherFixture.Body.GetTransform(out fixtureTransform);
                if (!otherFixture.IsSensor && AABB.TestOverlap(otherFixture.Shape, 0, shape, 0, ref fixtureTransform, ref shapeTransform))
                    isFree = false;
                return isFree;
            }, ref shapeAabb);
            return isFree;
        }

        /// <summary>
        /// Invokes an action for all fixtures that overlap an area.
        /// </summary>
        /// <param name="action">If returns false, the query will exit.</param>
        /// <param name="preFilter">Filters fixtures that may overlap the area. This delegate is supposed to be light.
        /// It is called before testing for precise overlapping which is a more costly operation. To return true
        /// if the fixture qualifies for more precise overlap testing.</param>
        private void QueryOverlappingFixtures(IGeomPrimitive area, Func<Fixture, bool> action, Func<Fixture, bool> preFilter = null)
        {
            var shape = area.GetShape();
            AABB shapeAabb;
            var shapeTransform = new Transform();
            shapeTransform.SetIdentity();
            shape.ComputeAABB(out shapeAabb, ref shapeTransform, 0);
            Transform fixtureTransform;
            _world.QueryAABB(otherFixture =>
            {
                if (preFilter == null || preFilter(otherFixture))
                {
                    otherFixture.Body.GetTransform(out fixtureTransform);
                    if (AABB.TestOverlap(otherFixture.Shape, 0, shape, 0, ref fixtureTransform, ref shapeTransform))
                        return action(otherFixture);
                }
                return true;
            }, ref shapeAabb);
        }

        /// <returns>(true, gob) if a gob was found by the ID, or (false, null) if a gob was not found.</returns>
        public Tuple<bool, Gob> FindGob(int id)
        {
            var gob = id == Gob.INVALID_ID
                ? null
                : Gobs.FirstOrDefault(g => g.ID == id); // TODO !!! Use Dictionary for faster retrieval.
            return Tuple.Create(gob != null, gob);
        }

        /// <summary>
        /// Returns the number of pixels removed.
        /// </summary>
        public int MakeHole(Vector2 holePos, float holeRadius)
        {
            if (holeRadius <= 0) return 0;
            var holeHalfDiagonal = new Vector2(holeRadius);
            var holeAabb = AWMathHelper.CreateAABB(holePos - holeHalfDiagonal, holePos + holeHalfDiagonal);
            var pixelsRemoved = 0;
            var handledWalls = new HashSet<Gobs.Wall>();
            _world.QueryAABB(fixture =>
            {
                var wall = fixture.Body.UserData as Gobs.Wall;
                if (wall != null && handledWalls.Add(wall))
                    wall.MakeHole(holePos, holeRadius);
                return true;
            }, ref holeAabb);
            return pixelsRemoved;
        }

        public void PrepareEffect(BasicEffect effect)
        {
            _lighting.PrepareEffect(effect);
            effect.LightingEnabled = true;
        }

        public void MakeConsistent(Type limitationAttribute)
        {
            Gobs = new GobCollection(Layers);
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                SetGameAndArenaToGobs();
            }
        }

        #endregion Public methods

        #region Private methods

        /// <summary>
        /// Registers a gob for collisions.
        /// </summary>
        private void Register(Gob gob)
        {
            var body = BodyFactory.CreateBody(_world, gob);
            body.IsStatic = !gob.Movable;
            body.IgnoreGravity = !gob.Gravitating || !gob.Movable;
            body.FixedRotation = gob.DampAngularVelocity; // Note: Recomputes body mass from fixtures.
            gob.Body = body;
            var gobScale = Matrix.CreateScale(gob.Scale); // TODO !!! Get rid of Gob.Scale
            foreach (var area in gob.CollisionAreas)
            {
                var isPhysicalArea = area.CannotOverlap != CollisionAreaType.None;
                if (isPhysicalArea && area.CollidesAgainst != CollisionAreaType.None)
                    throw new ApplicationException("A physical collision area cannot act as a receptor");
                var fixture = body.CreateFixture(area.AreaGob.Transform(gobScale).GetShape(), area);
                fixture.Friction = area.Friction;
                fixture.Restitution = area.Elasticity;
                fixture.IsSensor = !isPhysicalArea;
                fixture.CollisionCategories = (Category)area.Type;
                fixture.CollidesWith = isPhysicalArea ? (Category)area.CannotOverlap : (Category)area.CollidesAgainst;
                area.Fixture = fixture;
            }
            body.Mass = gob.Mass; // Override mass from fixtures.
        }

        /// <summary>
        /// Removes a previously registered gob from having collisions.
        /// </summary>
        private void Unregister(Gob gob)
        {
            if (gob.Body == null) return;
            _world.RemoveBody(gob.Body);
            gob.Body = null;
            foreach (var area in gob.CollisionAreas) area.Fixture = null;
        }

        /// <summary>
        /// Prepares a gob for a game session.
        /// </summary>
        private void Prepare(Gob gob)
        {
            gob.Arena = this;
            gob.Activate();
            if (IsForPlaying)
            {
                if (gob.Layer == Gobs.GameplayLayer)
                    Register(gob);
                else
                {
                    // Gobs outside the gameplay layer cannot collide.
                    // To achieve this, we take away all the gob's collision areas.
                    gob.ClearCollisionAreas();
                }
            }
        }

        private void SetGameAndArenaToGobs()
        {
            foreach (var gob in Gobs)
            {
                gob.Arena = this;
                gob.Game = Game;
            }
        }

        private void InitializeWorld()
        {
            var arenaBounds = AWMathHelper.CreateAABB(Vector2.Zero, Dimensions);
            _world = new World(AWMathHelper.FARSEER_SCALE * _gravity, arenaBounds);
            _world.ContactManager.PostSolve += PostSolveHandler;
            _world.ContactManager.BeginContact += BeginContactHandler;
        }

        /// <summary>
        /// Initialises the gobs that are initially contained in the arena for playing the arena.
        /// </summary>
        /// This is done by taking copies of all the gobs. In effect, this turns deserialised
        /// gobs into properly initialised gobs. Namely, deserialised gobs are created by 
        /// the parameterless constructor that doesn't properly initialise all fields.
        private void InitializeGobs()
        {
            InitializeSpecialLayers();
            var oldLayers = _layers;
            _layers = new List<ArenaLayer>();
            foreach (var layer in oldLayers)
            {
                _layers.Add(layer.EmptyCopy());
                foreach (var gob in layer.Gobs)
                    gob.Layer = layer;
            }
            Gobs = new GobCollection(_layers);
            InitializeSpecialLayers();
            foreach (var gob in new GobCollection(oldLayers))
                Gob.CreateGob(Game, gob, gobb =>
                {
                    gobb.Layer = _layers[oldLayers.IndexOf(gob.Layer)];
                    Gobs.Add(gobb);
                });
        }

        private void InitializeSpecialLayers()
        {
            int gameplayLayerIndex = Layers.FindIndex(layer => layer.IsGameplayLayer);
            if (gameplayLayerIndex == -1)
                throw new ArgumentException("Arena " + Info.Name + " doesn't have a gameplay layer");
            Gobs.GameplayLayer = Layers[gameplayLayerIndex];

            // Make sure the gameplay backlayer is located right after the gameplay layer.
            // Use a suitable layer if one is defined in the arena, otherwise create a new one.
            if (gameplayLayerIndex == Layers.Count - 1 || Layers[gameplayLayerIndex + 1].Z != 0)
                Layers.Insert(gameplayLayerIndex + 1, new ArenaLayer(false, 0, ""));
            Gobs.GameplayOverlayLayer = Layers[gameplayLayerIndex + 1];

            // Make sure the gameplay backlayer is located right before the gameplay layer.
            // Use a suitable layer if one is defined in the arena, otherwise create a new one.
            if (gameplayLayerIndex == 0 || Layers[gameplayLayerIndex - 1].Z != 0)
                Layers.Insert(gameplayLayerIndex, new ArenaLayer(false, 0, ""));
            Gobs.GameplayBackLayer = Layers[gameplayLayerIndex - 1];
        }

        private void AddGobTrackerToViewports(Gob gob, string textureName)
        {
            var trackerItem = new GobTrackerItem(gob, null, textureName);
            foreach (var plr in Game.DataEngine.Players)
                if (plr.IsLocal) plr.GobTrackerItems.Add(trackerItem);
        }

        #endregion Private methods

        #region Callbacks

        public void DebugDrawFixtures(Matrix view, Matrix projection)
        {
            var broadPhase = _world.ContactManager.BroadPhase;
            foreach (var body in _world.BodyList)
            {
                if (!body.Enabled) continue;
                foreach (var fixture in body.FixtureList)
                {
                    for (int t = 0; t < fixture.ProxyCount; ++t)
                    {
                        var proxy = fixture.Proxies[t];
                        AABB aabb;
                        broadPhase.GetFatAABB(proxy.ProxyId, out aabb);
                        Graphics3D.DebugDrawPolyline(view, projection, Matrix.Identity,
                            aabb.LowerBound / AWMathHelper.FARSEER_SCALE,
                            new Vector2(aabb.LowerBound.X, aabb.UpperBound.Y) / AWMathHelper.FARSEER_SCALE,
                            aabb.UpperBound / AWMathHelper.FARSEER_SCALE,
                            new Vector2(aabb.UpperBound.X, aabb.LowerBound.Y) / AWMathHelper.FARSEER_SCALE,
                            aabb.LowerBound / AWMathHelper.FARSEER_SCALE);
                    }
                }
            }
        }

        private void GobAddedHandler(Gob gob)
        {
            Prepare(gob);
            if (gob is AW2.Game.Gobs.Ship || gob is AW2.Game.Gobs.Bot) AddGobTrackerToViewports(gob, "gui_tracker_player");
            if (gob is AW2.Game.Gobs.Dock) AddGobTrackerToViewports(gob, "gui_tracker_dock");
            if (gob is AW2.Game.Gobs.Bonus) AddGobTrackerToViewports(gob, "gui_tracker_bonus");
            if (gob is AW2.Game.Gobs.Wall) Game.DataEngine.ArenaSilhouette.UpdateArenaRadarSilhouette = true;
            if (GobAdded != null) GobAdded(gob);
        }

        private bool GobRemovingHandler(Gob gob)
        {
            // Game client removes relevant gobs only when the server says so.
            return Game.NetworkMode != NetworkMode.Client || !gob.IsRelevant;
        }

        private void GobRemovedHandler(Gob gob)
        {
            if (gob.Layer == Gobs.GameplayLayer) Unregister(gob);
            gob.Dispose();
            if (GobRemoved != null) GobRemoved(gob);
        }

        private bool BeginContactHandler(Contact contact)
        {
            // Remember which gobs collided.
            var areaA = (CollisionArea)contact.FixtureA.UserData;
            var areaB = (CollisionArea)contact.FixtureB.UserData;
            var eventKey = new CollisionEventKey(areaA.Owner, areaB.Owner);
            CollisionEvent collisionEvent = null;
            if (!_collisionEvents.TryGetValue(eventKey, out collisionEvent))
            {
                collisionEvent = new CollisionEvent { SkipIrreversibleSideEffects = Game.NetworkMode == NetworkMode.Client };
                _collisionEvents.Add(eventKey, collisionEvent);
            }
            collisionEvent.SetCollisionAreas(areaA, areaB);
            // Always break sensor contacts so that we get a new collision event each frame.
            return areaA.IsPhysical && areaB.IsPhysical;
        }

        private void PostSolveHandler(Contact contact, ContactConstraint impulse)
        {
            // Remember the collision impulse.
            for (int i = 0; i < impulse.PointCount; i++)
            {
                var point = impulse.Points[i];
                // TODO !!! Store point until the end of the frame, then figure out collision sounds
                // based on normal impulse and write it to _collisionEvents before sending to client.
                //Log.Write("!!! {1} hit {2}, NormalImpulse={0}", point.NormalImpulse,
                //    ((CollisionArea)contact.FixtureA.UserData).Owner.TypeName,
                //    ((CollisionArea)contact.FixtureB.UserData).Owner.TypeName);
            }
        }

        #endregion Callbacks
    }
}
