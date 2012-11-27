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
using AW2.Game.Collisions;
using AW2.Game.Gobs;
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
        [System.Diagnostics.DebuggerDisplay("Gob:{Gob} OldPos:{OldPos} OldMove:{OldMove}")]
        public class GobUpdateData
        {
            public Gob Gob { get; private set; }
            public Vector2 OldPos { get; private set; }
            public float OldRotation { get; private set; }
            public float OldRotationSpeed { get; private set; }
            public int FramesAgo { get; private set; }
            public GobUpdateData(Gob gob, int framesAgo)
            {
                Gob = gob;
                OldPos = gob.Pos;
                OldRotation = gob.Rotation;
                OldRotationSpeed = gob.RotationSpeed;
                FramesAgo = framesAgo;
            }
        }

        [System.Diagnostics.DebuggerDisplay("Gob1:{_gob1ID} Gob2:{_gob2ID}")]
        private struct CollisionEventKey
        {
            private int _gob1ID;
            private int _gob2ID;

            public CollisionEventKey(Gob gob1, Gob gob2)
            {
                _gob1ID = Math.Min(gob1.ID, gob2.ID);
                _gob2ID = Math.Max(gob1.ID, gob2.ID);
            }

            public override int GetHashCode()
            {
                return _gob1ID ^ (_gob2ID << 16);
            }

            public override bool Equals(object other)
            {
                if (!(other is CollisionEventKey)) return false;
                var otherKey = (CollisionEventKey)other;
                return _gob1ID == otherKey._gob1ID && _gob2ID == otherKey._gob2ID;
            }
        }

        private struct GobUpdateFinalizationData
        {
            public Gob Gob;
            public object TemporaryStaticData;
            public GobUpdateFinalizationData(Gob gob, object temporaryStaticData) { Gob = gob; TemporaryStaticData = temporaryStaticData; }
        }

        #region General fields

        private AssaultWingCore _game;
        private GobCollection _gobs;
        private World _world;
        private AWTimer _areaBoundaryCheckTimer;
        private List<GobUpdateFinalizationData> _gobUpdateFinalizationDatas = new List<GobUpdateFinalizationData>();

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

        #endregion General fields

        #region Collision related fields

        public const float GOB_DESTRUCTION_BOUNDARY = 1000;
        private const int FREE_POS_MAX_ATTEMPTS = 50;

        private Dictionary<CollisionEventKey, CollisionEvent> _collisionEvents = new Dictionary<CollisionEventKey, CollisionEvent>();
        private Dictionary<CollisionEventKey, float> _collisionImpulses = new Dictionary<CollisionEventKey, float>();

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
        /// Time when the playing started at the arena. Measured in real time.
        /// </summary>
        public TimeSpan StartTime { get; set; }

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
        /// Releases resources allocated for the arena.
        /// </summary>
        public void Dispose()
        {
            foreach (var gob in Gobs) gob.Dispose();
            Gobs.Clear();
        }

        /// <summary>
        /// Initializes the arena for a new play session.
        /// This method usually takes a long time to run. It's therefore a good
        /// idea to make it run in a background thread.
        /// </summary>
        public void Initialize()
        {
            TotalTime = TimeSpan.Zero;
            StartTime = Game.GameTime.TotalRealTime;
            FrameNumber = 0;
            InitializeWorld();
            InitializeGobs();
        }

        /// <summary>
        /// Updates arena contents for the current time step.
        /// </summary>
        public void Update()
        {
            ResetCollisionEvents();
            _world.Step((float)Game.TargetElapsedTime.TotalSeconds);
            PerformCustomCollisions();
            if (_areaBoundaryCheckTimer.IsElapsed && Game.NetworkMode != NetworkMode.Client)
            {
                var boundedAreaExtreme = BoundedAreaExtreme;
                foreach (var gob in Gobs.GameplayLayer.Gobs)
                    if (!Geometry.Intersect(gob.Pos, boundedAreaExtreme))
                        gob.Die();
            }
        }

        public void FinalizeGobUpdatesOnClient(Dictionary<int, GobUpdateData> gobsToUpdate, int frameCount)
        {
            Gobs.FinishAddsAndRemoves();
            foreach (var gob in GobsInRelevantLayers)
            {
                if (gob.MoveType == AW2.Game.GobUtils.MoveType.Static) continue;
                if (gobsToUpdate.ContainsKey(gob.ID)) continue;
                _gobUpdateFinalizationDatas.Add(new GobUpdateFinalizationData(gob, gob.Body.SetTemporaryStatic()));
            }
            // Rotation of local ships is already up to date.
            var localShips = gobsToUpdate.Values.Where(data => data.Gob is Gobs.Ship && data.Gob.Owner != null && data.Gob.Owner.IsLocal).ToArray();
            foreach (var data in localShips) data.Gob.RotationSpeed = 0;
            for (int i = 0; i < frameCount; i++) Update();
            foreach (var x in _gobUpdateFinalizationDatas) x.Gob.Body.RestoreTemporaryStatic(x.TemporaryStaticData);
            _gobUpdateFinalizationDatas.Clear();
            foreach (var data in localShips) data.Gob.RotationSpeed = data.OldRotationSpeed;
            foreach (var data in gobsToUpdate.Values) data.Gob.SmoothJitterOnClient(data);
        }

        public IEnumerable<CollisionEvent> GetCollisionEvents()
        {
            return _collisionEvents.Values;
        }

        /// <summary>
        /// All potential attack targets. Useful as input to <see cref="AW2.Game.GobUtils.TargetSelector.ChooseTarget"/>.
        /// </summary>
        public IEnumerable<Gob> PotentialTargets
        {
            get
            {
                return Gobs.All(typeof(Ship))
                    .Union(Gobs.All(typeof(SnakeShip)))
                    .Union(Gobs.All(typeof(Bot)))
                    .Union(Gobs.All(typeof(FloatingBullet)));
            }
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
        /// Is an area free of physical gobs. If <paramref name="filter"/> returns false for
        /// a CollisionArea, it is not considered as an obstacle.
        /// </summary>
        public bool IsFreePosition(IGeomPrimitive area, Func<CollisionArea, bool> filter = null)
        {
            if (!BoundedAreaNormal.Contains(area.BoundingBox)) return false;
            return PhysicsHelper.IsFreePosition(_world, area, filter);
        }

        /// <summary>
        /// Returns distance to the closest collision area, if any, along a directed line segment.
        /// Collision areas for whom <paramref name="filter"/> returns false are ignored.
        /// </summary>
        public float? GetDistanceToClosest(Vector2 from, Vector2 to, Func<CollisionArea, bool> filter, CollisionAreaType[] areaTypes)
        {
            return PhysicsHelper.GetDistanceToClosest(_world, from, to, filter, areaTypes);
        }

        /// <summary>
        /// Invokes an action for all collision areas that overlap an area.
        /// </summary>
        /// <param name="action">If returns false, the query will exit.</param>
        /// <param name="preFilter">Filters potential overlappers. This delegate is supposed to be light.
        /// It is called before testing for precise overlapping which is a more costly operation. To return true
        /// if the candidate qualifies for more precise overlap testing.</param>
        public void QueryOverlappers(CollisionArea area, Func<CollisionArea, bool> action, Func<CollisionArea, bool> preFilter = null)
        {
            PhysicsHelper.QueryOverlappers(_world, area, action, preFilter);
        }

        /// <summary>
        /// Returns the number of pixels removed.
        /// </summary>
        public int MakeHole(Vector2 holePos, float holeRadius)
        {
            if (holeRadius <= 0) return 0;
            var holeHalfDiagonal = new Vector2(holeRadius);
            var walls = new HashSet<Gobs.Wall>();
            PhysicsHelper.QueryOverlappers(_world, holePos - holeHalfDiagonal, holePos + holeHalfDiagonal, area =>
            {
                walls.Add(area.Owner as Gobs.Wall);
                return true;
            });
            return walls.Sum(wall => wall == null ? (int?)null : wall.MakeHole(holePos, holeRadius)).Value;
        }

        public void PrepareEffect(BasicEffect effect)
        {
            _lighting.PrepareEffect(effect);
            effect.LightingEnabled = true;
        }

        public void MakeConsistent(Type limitationAttribute)
        {
            Gobs = new GobCollection(Layers);
            Gobs.FinishAddsAndRemoves();
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                SetGameAndArenaToGobs();
            }
        }

        public void DebugDrawFixtures(Matrix view, Matrix projection)
        {
            var context = new Graphics3D.DebugDrawContext(view, projection);
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
                        Graphics3D.DebugDrawPolyline(context,
                            aabb.LowerBound.FromFarseer(),
                            new Vector2(aabb.LowerBound.X, aabb.UpperBound.Y).FromFarseer(),
                            aabb.UpperBound.FromFarseer(),
                            new Vector2(aabb.UpperBound.X, aabb.LowerBound.Y).FromFarseer(),
                            aabb.LowerBound.FromFarseer());
                    }
                }
            }
        }

        public void DebugDrawBroadPhase(Matrix view, Matrix projection)
        {
            var context = new Graphics3D.DebugDrawContext(view, projection);
            Func<int, Color> getColor = count =>
                count <= 0 ? new Color(0f, 0f, 1f) :
                count <= 5 ? new Color(0f, 1f, 0f) :
                count <= 25 ? new Color(1f, 0.5f, 0f) :
                count <= 125 ? new Color(1f, 1f, 0f) :
                new Color(1f, 1f, 1f);
            Func<AABB, Vector2[]> getVertices = aabb =>
            {
                var awCenter = aabb.Center.FromFarseer();
                var extents = aabb.Extents.FromFarseer();
                var reducedExtents = extents - new Vector2((float)Math.Log(extents.X + 1, 1.5), (float)Math.Log(extents.Y + 1, 1.5));
                return new[]
                {
                    awCenter - reducedExtents,
                    awCenter + reducedExtents.MirrorX(),
                    awCenter + reducedExtents,
                    awCenter + reducedExtents.MirrorY(),
                    awCenter - reducedExtents,
                };
            };
            var spansAndElementCounts = new List<Tuple<AABB, int>>();
            _world.ContactManager.BroadPhase.GetSpans(ref spansAndElementCounts);
            foreach (var aabbAndCount in spansAndElementCounts)
            {
                context.Color = getColor(aabbAndCount.Item2);
                Graphics3D.DebugDrawPolyline(context, getVertices(aabbAndCount.Item1));
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
            body.BodyType =
                gob.MoveType == MoveType.Static ? BodyType.Static :
                gob.MoveType == MoveType.Kinematic ? BodyType.Kinematic :
                BodyType.Dynamic;
            body.IgnoreGravity = !gob.Gravitating || gob.MoveType != MoveType.Dynamic;
            var originalGobMass = gob.Mass; // gob.Mass will come from body.Mass after the body is assigned.
            gob.Body = body;
            foreach (var area in gob.CollisionAreas) area.Initialize(gob.Scale); // TODO !!! Get rid of Gob.Scale
            // Compute physical shape density from gob mass. This way bodies have inertia and
            // are able to gain angular velocity in collisions.
            // TODO !!! Specify density directly in CollisionArea.
            var physicalAreas = gob.CollisionAreas.Where(area => area.Type.IsPhysical()).ToArray();
            if (physicalAreas.Length > 0)
            {
                foreach (var physicalArea in physicalAreas)
                    physicalArea.Fixture.Shape.Density = originalGobMass / physicalAreas.Length / physicalArea.Fixture.Shape.MassData.Area;
                foreach (var nonphysicalArea in gob.CollisionAreas.Where(area => !area.Type.IsPhysical()))
                    nonphysicalArea.Fixture.Shape.Density = 0;
                body.ResetMassData(); // Compute masses of shapes. Their sum will equal gob.Mass. Also inertia etc. will be computed.
            }
            body.FixedRotation = gob.DampAngularVelocity; // Note: Recomputes body mass from fixtures.
        }

        /// <summary>
        /// Removes a previously registered gob from having collisions.
        /// </summary>
        private void Unregister(Gob gob)
        {
            if (gob.Body == null) return;
            _world.RemoveBody(gob.Body);
        }

        /// <summary>
        /// Prepares a gob for a game session.
        /// </summary>
        private void Prepare(Gob gob)
        {
            gob.Arena = this;
            if (IsForPlaying)
            {
                if (gob.Layer == Gobs.GameplayLayer || gob.Layer == Gobs.GameplayOverlayLayer)
                    Register(gob);
                else
                {
                    // Gobs outside the gameplay layer cannot collide.
                    // To achieve this, we take away all the gob's collision areas.
                    gob.ClearCollisionAreas();
                }
            }
            gob.Activate();
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
            _world = PhysicsHelper.CreateWorld(_gravity, BoundedAreaExtreme.Min, BoundedAreaExtreme.Max);
            _world.ContactManager.PostSolve += PostSolveHandler;
            _world.ContactManager.BeginContact += BeginContactHandler;
            _areaBoundaryCheckTimer = new AWTimer(() => TotalTime, TimeSpan.FromSeconds(10));
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
                layer.Gobs.FinishAddsAndRemoves();
                _layers.Add(layer.EmptyCopy());
                foreach (var gob in layer.Gobs)
                    gob.Layer = layer;
            }
            for (int layerIndex = 0; layerIndex < _layers.Count; layerIndex++) _layers[layerIndex].Index = layerIndex;
            Gobs = new GobCollection(_layers);
            InitializeSpecialLayers();
            foreach (var gob in new GobCollection(oldLayers))
                Gob.CreateGob(Game, gob, gobb =>
                {
                    gobb.Layer = _layers[oldLayers.IndexOf(gob.Layer)];
                    Gobs.Add(gobb);
                });
            Gobs.FinishAddsAndRemoves();
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

        private void PerformCustomCollisions()
        {
            foreach (var collisionEvent in _collisionEvents)
            {
                var impulse = 0f;
                _collisionImpulses.TryGetValue(collisionEvent.Key, out impulse); // Is not found for sensor fixtures.
                collisionEvent.Value.SetImpulse(impulse);
                collisionEvent.Value.Handle();
            }
        }

        private void ResetCollisionEvents()
        {
            _collisionEvents.Clear();
            _collisionImpulses.Clear();
        }

        private void AddGobTrackerToViewports(Gob gob, string textureName)
        {
            var trackerItem = new GobTrackerItem(gob, null, textureName);
            foreach (var plr in Game.DataEngine.Players)
                if (plr.IsLocal) plr.GobTrackerItems.Add(trackerItem);
        }

        #endregion Private methods

        #region Callbacks

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
            if (gob.Layer == Gobs.GameplayLayer || gob.Layer == Gobs.GameplayOverlayLayer) Unregister(gob);
            gob.Dispose();
            if (GobRemoved != null) GobRemoved(gob);
        }

        private bool BeginContactHandler(Contact contact)
        {
            var areaA = (CollisionArea)contact.FixtureA.UserData;
            var areaB = (CollisionArea)contact.FixtureB.UserData;
            if (areaA.Owner.Owner == areaB.Owner.Owner && (areaA.Owner.Cold || areaB.Owner.Cold)) return false;
            // Remember which gobs collided.
            var eventKey = new CollisionEventKey(areaA.Owner, areaB.Owner);
            CollisionEvent collisionEvent = null;
            if (!_collisionEvents.TryGetValue(eventKey, out collisionEvent))
            {
                collisionEvent = new CollisionEvent { SkipIrreversibleSideEffects = Game.NetworkMode == NetworkMode.Client };
                _collisionEvents.Add(eventKey, collisionEvent);
            }
            collisionEvent.SetCollisionAreas(areaA, areaB);
            return true;
        }

        private void PostSolveHandler(Contact contact, ContactConstraint impulse)
        {
            var gob1 = ((CollisionArea)contact.FixtureA.UserData).Owner;
            var gob2 = ((CollisionArea)contact.FixtureB.UserData).Owner;
            var eventKey = new CollisionEventKey(gob1, gob2);
            var previousMaxImpulse = 0f;
            _collisionImpulses.TryGetValue(eventKey, out previousMaxImpulse);
            _collisionImpulses[eventKey] = Math.Max(previousMaxImpulse, impulse.Points.Take(impulse.PointCount).Max(p => p.NormalImpulse));
        }

        #endregion Callbacks
    }
}
