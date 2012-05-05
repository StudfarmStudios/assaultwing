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

        public struct CollisionEvent
        {
            public int Gob1ID { get; private set; }
            public int Gob2ID { get; private set; }
            public int Area1ID { get; private set; }
            public int Area2ID { get; private set; }

            public CollisionEvent(int gob1ID, int gob2ID, int area1ID, int area2ID)
                : this()
            {
                Gob1ID = gob1ID;
                Gob2ID = gob2ID;
                Area1ID = area1ID;
                Area2ID = area2ID;
            }

            public CollisionEvent(CollisionArea area1, CollisionArea area2)
                : this()
            {
                var gob1 = area1.Owner;
                var gob2 = area2.Owner;
                Gob1ID = gob1.ID;
                Gob2ID = gob2.ID;
                // Note: Walls are the only gobs to have over 4 collision areas; there can be hundreds of them.
                // To fit collision area IDs into as few bits as possible, walls will always collide with
                // their first collision area. This should not have a visible effect on game clients.
                Area1ID = gob1 is Gobs.Wall ? 0 : gob1.GetCollisionAreaID(area1);
                Area2ID = gob2 is Gobs.Wall ? 0 : gob2.GetCollisionAreaID(area2);
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

        /// <summary>
        /// The maximum number of attempts to find a free position for a gob.
        /// </summary>
        private const int FREE_POS_MAX_ATTEMPTS = 50;

        /// <summary>
        /// Minimum change of gob speed in a collision to cause damage and a sound effect.
        /// </summary>
        private const float MINIMUM_COLLISION_DELTA = 20;
        private const float MOVABLE_MOVABLE_COLLISION_SOUND_TRESHOLD = 40;

        private List<CollisionEvent> _collisionEvents = new List<CollisionEvent>();

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
        public Rectangle BoundedArea { get { return new Rectangle(Vector2.Zero, Dimensions); } }

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
        }

        public List<CollisionEvent> GetCollisionEvents()
        {
            return _collisionEvents;
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

        /// <summary>
        /// Performs custom collision effects when a collision area collides into another one.
        /// </summary>
        /// <returns>True if irreversible side effects happened.</returns>
        public bool PerformCustomCollision(CollisionArea myArea, CollisionArea theirArea, bool forceIrreversibleSideEffectsOnClient)
        {
            var stuck = false; // !!!
            myArea.Owner.CollideReversible(myArea, theirArea, stuck);
            if (Game.NetworkMode == NetworkMode.Client && !forceIrreversibleSideEffectsOnClient) return false;
            var irreversibleSideEffects = myArea.Owner.CollideIrreversible(myArea, theirArea, stuck);
            var sounds = GetCollisionSounds(myArea, theirArea);
            if (sounds.HasFlag(CollisionSoundTypes.WallCollision))
                Game.SoundEngine.PlaySound("Collision", myArea.Owner);
            if (sounds.HasFlag(CollisionSoundTypes.ShipCollision))
                Game.SoundEngine.PlaySound("Shipcollision", myArea.Owner);
            return irreversibleSideEffects || sounds != CollisionSoundTypes.None;
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

        private CollisionSoundTypes GetCollisionSounds(CollisionArea myArea, CollisionArea theirArea)
        {
            if (!myArea.IsPhysical || !theirArea.IsPhysical) return CollisionSoundTypes.None;
            if (!(myArea.Owner is Gobs.Ship)) return CollisionSoundTypes.None;
            if (theirArea.Owner is Gobs.Ship)
                return myArea.Owner.ID < theirArea.Owner.ID ? CollisionSoundTypes.ShipCollision : CollisionSoundTypes.None; // Only one ship makes the sound
            else
                return theirArea.Owner.Movable ? CollisionSoundTypes.ShipCollision : CollisionSoundTypes.WallCollision;
        }

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
            body.OnCollision += BodyCollisionHandler; // Note: Handler is set on each fixture.
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

        private void InitializeWorld()
        {
            var arenaBounds = AWMathHelper.CreateAABB(Vector2.Zero, Dimensions);
            _world = new World(AWMathHelper.FARSEER_SCALE * _gravity, arenaBounds);
            var corners = new[]
            {
                Vector2.Zero,
                new Vector2(0, Dimensions.Y),
                Dimensions,
                new Vector2(Dimensions.X, 0),
            };
            var arenaBoundary = BodyFactory.CreateLoopShape(_world, AWMathHelper.CreateVertices(corners));
            arenaBoundary.CollisionCategories = Category.All;
            arenaBoundary.CollidesWith = Category.All;
            arenaBoundary.Restitution = 0;
            arenaBoundary.Friction = 0;
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

        private bool BodyCollisionHandler(Fixture myFixture, Fixture theirFixture, Contact contact)
        {
            var myArea = (CollisionArea)myFixture.UserData;
            var theirArea = (CollisionArea)theirFixture.UserData;
            if (myArea != null && theirArea != null)
            {
                var irreversibleSideEffects = PerformCustomCollision(myArea, theirArea, false);
                if (Game.NetworkMode == NetworkMode.Server && irreversibleSideEffects)
                    _collisionEvents.Add(new CollisionEvent(myArea, theirArea));
            }
            return true;
        }

        #endregion Callbacks
    }
}
