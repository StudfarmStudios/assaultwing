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
    /// A game arena, i.e., a rectangular area where gobs exist and interact.
    /// </summary>
    /// <para>
    /// <see cref="Arena"/> uses limited (de)serialisation for saving and loading arenas.
    /// Therefore only those fields that describe the arenas initial state -- not 
    /// fields that describe the arenas state during gameplay -- should be marked as 
    /// 'type parameters' by <see cref="AW2.Helpers.TypeParameterAttribute"/>
    /// </para>
    /// <para>
    /// <see cref="Arena"/> handles collisions between gobs as follows.
    /// </para>
    /// <para>
    /// <list type="bullet">
    /// <listheader>Basic concepts</listheader>
    /// <item><b>Collision</b> is the event of reacting to overlap of two collision areas.</item>
    /// <item><b>Physical collision</b> is a lifelike collision performed by physics engine.
    ///   Together with backtracking the position of the currently moving gob they
    ///   undo overlapping of two collision areas that are marked to not being able to
    ///   overlap each other.</item>
    /// <item><b>Custom collisions</b> are any collisions that are not physical collisions.
    ///   Each Gob subclass can define its own custom collisions.</item>
    ///   </list>
    /// </para>
    /// 
    /// <para>
    /// Gobs' collisions are done in two steps.
    /// 
    /// Step 1, Moving All Gobs.
    /// When a gob moves, its physical area (max. one allowed for movable gobs!) is checked against areas of types 'cannotOverlap'. If there are such overlaps, this gob is backtracked out of the overlaps and a collision is performed. (Note: Other than physical areas don't have the chance to trigger backtracking, although it could easily be implemented if found useful.)
    /// 
    /// Step 2, Checking Remaining Collisions.
    /// This gob's areas (physical and other) are checked against areas of types 'collidesAgainst'. (Note: It is redundant to list any types from 'cannotOverlap' again in 'collidesAgainst' -- their overlaps have been backtracked away in the previous step.) If there are such overlaps, collisions are performed once for each overlap.
    /// </para>
    /// 
    /// <para>
    /// Collision Principle 1:
    /// For collisions that may use backtracking, each gob is responsible for checking for overlap that causes this kind of collision as the gob moves. If backtracking is used to resolve the overlap, the moving gob is always the one that backtracks. 
    /// Example: physical collisions such as a ship bouncing off a wall.
    /// CollisionAreaTypes: Physical{ Ship | Shot | Wall | *able }
    /// 
    /// Collision Principle 2:
    /// For collisions that can be described as actions that one gob performs on another (but not vice versa), the action performer is responsible for overlap checking. The check is done after all gobs have moved. It is not possible to do backtracking for this kind of collision. 
    /// Example: Forces that take place in an area such as gravity.
    /// CollisionAreaTypes: Receptor | Force | Physical{ Water | Gas }
    /// </para>
    /// 
    /// <para>
    /// Important: For the whole system of physical collisions to act consistently, the CollisionArea.cannotOverlap must be a symmetric relation over all gobtypes. That is, if gobtype A cannot overlap gobtype B, then it must also be that gobtype B cannot overlap gobtype A, with the only allowed exception that if B is not movable then it need not bother with this requirement.
    /// </para>
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
            public bool Stuck { get; private set; }
            public bool CollideBothWays { get; private set; }
            public CollisionSoundTypes Sound { get; private set; }

            public CollisionEvent(int gob1ID, int gob2ID, int area1ID, int area2ID, bool stuck, bool collideBothWays, CollisionSoundTypes sound)
                : this()
            {
                Gob1ID = gob1ID;
                Gob2ID = gob2ID;
                Area1ID = area1ID;
                Area2ID = area2ID;
                Stuck = stuck;
                CollideBothWays = collideBothWays;
                Sound = sound;
            }

            public CollisionEvent(CollisionArea area1, CollisionArea area2, bool stuck, bool collideBothWays, CollisionSoundTypes sound)
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
                Stuck = stuck;
                CollideBothWays = collideBothWays;
                Sound = sound;
            }
        }

        /// <summary>
        /// Ways of being outside arena boundaries.
        /// </summary>
        /// The arena boundary is a rectangle spanned by points (0, 0) and
        /// (arenaWidth, arenaHeight). The outer arena boundary is the arena
        /// boundary stretched a constant distance to all directions.
        [Flags]
        private enum OutOfArenaBounds
        {
            /// <summary>
            /// Over the top boundary.
            /// </summary>
            Top = 0x0001,

            /// <summary>
            /// Below the bottom boundary.
            /// </summary>
            Bottom = 0x0002,

            /// <summary>
            /// Left of the left boundary.
            /// </summary>
            Left = 0x0004,

            /// <summary>
            /// Right of the right boundary.
            /// </summary>
            Right = 0x0008,

            /// <summary>
            /// Over the outer top boundary.
            /// </summary>
            OuterTop = 0x0010,

            /// <summary>
            /// Below the outer bottom boundary.
            /// </summary>
            OuterBottom = 0x0020,

            /// <summary>
            /// Left of the outer left boundary.
            /// </summary>
            OuterLeft = 0x0040,

            /// <summary>
            /// Right of the outer right boundary.
            /// </summary>
            OuterRight = 0x0080,

            /// <summary>
            /// Inside the arena boundary.
            /// Absolute value, not to be OR'ed or AND'ed.
            /// </summary>
            None = 0,

            /// <summary>
            /// Out of the outer arena boundary.
            /// </summary>
            OuterBoundary = OuterTop | OuterBottom | OuterLeft | OuterRight,
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
        /// Distance outside the arena boundaries that we still allow some gobs to stay alive.
        /// </summary>
        public const float ARENA_OUTER_BOUNDARY_THICKNESS = 1000;

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

        /// <summary>
        /// The width and height of the arena.
        /// </summary>
        public Vector2 Dimensions { get { return Info.Dimensions; } }

        public float MinCoordinate { get { return -ARENA_OUTER_BOUNDARY_THICKNESS; } }
        public float MaxCoordinate { get { return MathHelper.Max(Dimensions.X, Dimensions.Y) + ARENA_OUTER_BOUNDARY_THICKNESS; } }
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

        #endregion // Arena properties

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

        public void Update()
        {
            _world.Step((float)Game.GameTime.ElapsedGameTime.TotalSeconds);
        }

        /// <summary>
        /// Moves the given gob and performs physical collisions in order to
        /// maintain overlap consistency as specified in <b>CollisionArea.CannotOverlap</b>
        /// of the moving gob's physical collision area.
        /// </summary>
        [Obsolete]
        public void Move(Gob gob, int frameCount, bool allowIrreversibleSideEffects)
        {
            Move(gob, Game.TargetElapsedTime.Multiply(frameCount), allowIrreversibleSideEffects);
        }

        /// <summary>
        /// As <see cref="Move(Gob, int, bool)"/> but time delta is specified in game time.
        /// </summary>
        [Obsolete]
        public void Move(Gob gob, TimeSpan moveTime, bool allowIrreversibleSideEffects)
        {
            if (!gob.Movable) return;
            if (gob.Disabled) return;
            var gobPhysical = gob.PhysicalArea;
            if (gobPhysical != null) Unregister(gobPhysical);

            // If the gob is stuck, let it resolve the situation.
            if (gobPhysical != null)
            {
                var stuckOverlappers = GetOverlappers(gobPhysical, gobPhysical.CannotOverlap);
                PerformCollisions(gob, gobPhysical, true, allowIrreversibleSideEffects, stuckOverlappers, CollisionSoundTypes.None);
                if (gob.Dead) return;
            }

            var colliders = new List<CollisionArea>();
            int attempts = 0;
            var moveTimeLeft = moveTime;
            var soundsToPlay = CollisionSoundTypes.None;
            while (moveTimeLeft > TimeSpan.FromSeconds(0.00001666) && attempts < 4)
            {
                var oldMove = gob.Move;
                var gobFrameMove = gob.Move * (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
                int moveChunkCount = (int)Math.Ceiling(gobFrameMove.Length() / 10);
                if (moveChunkCount == 0) moveChunkCount = 1;
                var chunkMoveTime = moveTimeLeft.Divide(moveChunkCount);
                for (int chunk = 0; chunk < moveChunkCount; ++chunk)
                {
                    var currentChunkMoveTime = chunkMoveTime;
                    var moveResult = TryMove(gob, ref currentChunkMoveTime, allowIrreversibleSideEffects);
                    colliders.AddRange(moveResult.Item1);
                    soundsToPlay |= moveResult.Item2;
                    moveTimeLeft -= chunkMoveTime - currentChunkMoveTime;
                    if (currentChunkMoveTime > TimeSpan.Zero) break; // stop iterating chunks if the gob collided
                }
                ++attempts;

                // If we just have to wait for another gob to move out of the way,
                // there's nothing more we can do.
                if (gob.Move == oldMove)
                    break;
            }
            // !!! PlayCollisionSounds(gob, soundsToPlay);
            if (gob.Gravitating) gob.Move += _gravity * (float)moveTime.TotalSeconds;
            PerformCollisions(gob, gobPhysical, false, allowIrreversibleSideEffects, colliders, soundsToPlay);
            if (gobPhysical != null) Register(gobPhysical);
            ArenaBoundaryActions(gob, allowIrreversibleSideEffects);
        }

        [Obsolete]
        private void PerformCollisions(Gob gob, CollisionArea gobPhysical, bool stuck, bool allowIrreversibleSideEffects, IEnumerable<CollisionArea> colliders, CollisionSoundTypes soundsToPlay)
        {
            foreach (var collider in colliders.Distinct())
            {
                gob.CollideReversible(gobPhysical, collider, stuck);
                collider.Owner.CollideReversible(collider, gobPhysical, stuck);
                var irreversibleSideEffects = soundsToPlay != CollisionSoundTypes.None;
                if (allowIrreversibleSideEffects)
                {
                    irreversibleSideEffects |= gob.CollideIrreversible(gobPhysical, collider, stuck);
                    irreversibleSideEffects |= collider.Owner.CollideIrreversible(collider, gobPhysical, stuck);
                }
                if (irreversibleSideEffects) _collisionEvents.Add(new CollisionEvent(gobPhysical, collider, stuck: stuck, collideBothWays: true, sound: soundsToPlay));
                if (gob.Dead) break;
            }
        }

        public List<CollisionEvent> GetCollisionEvents()
        {
            return _collisionEvents;
        }

        public void ResetCollisionEvents()
        {
            _collisionEvents.Clear();
        }

        /// <summary>
        /// Registers a collision area for collisions.
        /// </summary>
        [Obsolete]
        private void Register(CollisionArea area)
        {
            throw new ApplicationException();
        }

        /// <summary>
        /// Removes a previously registered collision area from having collisions.
        /// </summary>
        [Obsolete]
        public void Unregister(CollisionArea area)
        {
            area.Fixture.Dispose();
        }

        /// <summary>
        /// Registers a gob for collisions.
        /// </summary>
        private void Register(Gob gob)
        {
            var body = BodyFactory.CreateBody(_world, gob.Pos, gob);
            body.OnCollision += BodyCollisionHandler;
            body.IsStatic = !gob.Movable;
            body.IgnoreGravity = !gob.Gravitating || !gob.Movable;
            body.Mass = gob.Mass;
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
            var holeAabb = new AABB(holePos, holeRadius * 2, holeRadius * 2);
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

        #endregion Public methods

        #region Collision and moving methods

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
        /// Tries to move a gob, stopping it at the first physical collision 
        /// and performing the physical collision. Returns the collision areas
        /// of other gobs this gob collided into (there may be repetition).
        /// </summary>
        /// <param name="gob">The gob to move.</param>
        /// <param name="moveTime">Remaining duration of the move</param>
        /// <param name="allowSideEffects">Should effects other than changing the gob's
        /// position and movement be allowed.</param>
        [Obsolete]
        private Tuple<List<CollisionArea>, CollisionSoundTypes> TryMove(Gob gob, ref TimeSpan moveTime, bool allowSideEffects)
        {
            var colliders = new List<CollisionArea>();
            var oldPos = gob.Pos;
            var oldMove = gob.Move;
            var moveGood = TimeSpan.Zero; // last known safe position
            var moveBad = moveTime; // last known unsafe position, if 'badFound'
            var badFound = false;
            var gobPhysical = gob.PhysicalArea;
            var badDueToOverlappers = false;

            // Find out last non-collision position and first colliding position, 
            // up to required accuracy.
            var moveTry = moveTime;
            var lastMoveTry = TimeSpan.Zero;
            var oldMoveLength = oldMove.Length();
            var firstIteration = true;
            while (firstIteration || moveBad - moveGood > TimeSpan.FromSeconds(0.01))
            {
                firstIteration = false;
                gob.Pos = LerpGobPos(oldPos, oldMove, moveTry);
                bool overlapperFound = gobPhysical == null ? false
                    : GetOverlappers(gobPhysical, gobPhysical.CannotOverlap).Any();
                if (ArenaBoundaryLegal(gob) && !overlapperFound)
                {
                    moveGood = moveTry;
                }
                else
                {
                    moveBad = moveTry;
                    badFound = true;
                    badDueToOverlappers = overlapperFound;
                }
                lastMoveTry = moveTry;
                moveTry = TimeSpan.FromTicks((moveGood.Ticks + moveBad.Ticks) / 2);
            }

            var soundsToPlay = CollisionSoundTypes.None;
            if (badFound)
            {
                gob.Pos = LerpGobPos(oldPos, oldMove, moveBad);
                if (badDueToOverlappers)
                {
                    var physicalColliders = GetPhysicalColliders(gobPhysical);
                    colliders.AddRange(physicalColliders);
                    foreach (var collider in colliders) soundsToPlay |= PerformCollision(gobPhysical, collider, allowSideEffects);
                }
                ArenaBoundaryActions(gob, allowSideEffects);
            }

            // Return to last non-colliding position.
            gob.Pos = LerpGobPos(oldPos, oldMove, moveGood);
            moveTime -= moveGood;
            return Tuple.Create(colliders, soundsToPlay);
        }

        private static Vector2 LerpGobPos(Vector2 startPos, Vector2 move, TimeSpan moveTime)
        {
            return startPos + move * (float)moveTime.TotalSeconds;
        }

        [Obsolete]
        private IEnumerable<CollisionArea> GetPhysicalColliders(CollisionArea gobPhysical)
        {
            throw new ApplicationException();
        }

        /// <summary>
        /// Returns collision areas of certain types that overlap a collision area.
        /// </summary>
        [Obsolete]
        public IEnumerable<CollisionArea> GetOverlappers(CollisionArea area, CollisionAreaType types)
        {
            throw new ApplicationException();
        }

        /// <summary>
        /// Performs a physical collision between two gobs whose overlapping
        /// collision areas are given.
        /// </summary>
        /// Physical collisions are a means to maintain overlap consistency.
        /// <param name="area1">The overlapping collision area of one gob.</param>
        /// <param name="area2">The overlapping collision area of the other gob.</param>
        /// <param name="allowSideEffects">Should effects other than changing the gob's
        /// position and movement be allowed.</param>
        [Obsolete]
        private CollisionSoundTypes PerformCollision(CollisionArea area1, CollisionArea area2, bool allowSideEffects)
        {
            // At least one area must be from a movable gob, lest there be no collision.
            bool area1Movable = (area1.Type & CollisionAreaType.PhysicalMovable) != 0;
            bool area2Movable = (area2.Type & CollisionAreaType.PhysicalMovable) != 0;
            if (!area1Movable) return PerformCollisionMovableUnmovable(area2, area1, allowSideEffects);
            else if (!area2Movable) return PerformCollisionMovableUnmovable(area1, area2, allowSideEffects);
            else return PerformCollisionMovableMovable(area1, area2, allowSideEffects);
        }

        /// <summary>
        /// Performs a physical collision between a movable gob and an unmovable gob.
        /// </summary>
        /// <param name="movableArea">The overlapping collision area of the movable gob.</param>
        /// <param name="unmovableArea">The overlapping collision area of the unmovable gob.</param>
        /// <param name="allowSideEffects">Should effects other than changing the movable gob's
        /// position and movement be allowed.</param>
        [Obsolete]
        private CollisionSoundTypes PerformCollisionMovableUnmovable(CollisionArea movableArea, CollisionArea unmovableArea, bool allowSideEffects)
        {
            throw new ApplicationException();
#if false
            if (collision) {
                if (allowSideEffects && move1Delta.LengthSquared() >= MINIMUM_COLLISION_DELTA * MINIMUM_COLLISION_DELTA)
                {
                    // Inflict damage to damageable gobs.
                    if ((movableArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
                        unmovableGob.PhysicalCollisionInto(movableGob, move1Delta, damageMultiplier);
                    /* TODO: What if the unmovable gob wants to be damaged, too?
                    if ((unmovableArea.Type2 & CollisionAreaType.PhysicalDamageable) != 0)
                        movableGob.PhysicalCollisionInto(unmovableGob, Vector2.Zero, damageMultiplier);
                    */
                    if (movableGob is Gobs.Ship) return CollisionSoundTypes.WallCollision;
                }
            }
            return CollisionSoundTypes.None;
#endif
        }

        /// <summary>
        /// Performs a physical collision between two movable gobs.
        /// </summary>
        /// <param name="movableArea1">The overlapping collision area of one movable gob.</param>
        /// <param name="movableArea2">The overlapping collision area of the other movable gob.</param>
        /// <param name="allowSideEffects">Should effects other than changing the first movable gob's
        /// position and movement be allowed.</param>
        [Obsolete]
        private CollisionSoundTypes PerformCollisionMovableMovable(CollisionArea movableArea1, CollisionArea movableArea2, bool allowSideEffects)
        {
            Gob gob1 = movableArea1.Owner;
            Gob gob2 = movableArea2.Owner;

            // First make gob2 the point of reference,
            // then turn the coordinate axes so that both gobs are on the X-axis;
            // 'xUnit' and 'yUnit' will be the unit vectors of this system represented in game world coordinates;
            // 'move1' will be the movement vector of gob1 in this system (and gob2 stays put);
            // 'move1after' will be the resulting movement vector of gob1 in this system.
            Vector2 relMove = gob1.Move - gob2.Move;
            Vector2 xUnit = Vector2.Normalize(gob2.Pos - gob1.Pos);
            Vector2 yUnit = new Vector2(-xUnit.Y, xUnit.X);
            Vector2 move1 = new Vector2(Vector2.Dot(relMove, xUnit),
                                        Vector2.Dot(relMove, yUnit));

            // Only perform physical collision if the gobs are actually closing in on each other.
            if (move1.X > 0)
            {
                // Elasticity factor is between 0 and 1. It is used for linear interpolation
                // between the perfectly elastic collision and the perfectly inelastic collision.
                // Friction factor is from 0 and up. In real life, friction force
                // is the product of the friction factor and the head-on component
                // of the force that pushes the colliding gobs apart. Here, we 
                // imitate that force by the change of the head-on component of
                // gob movement vector.
                float elasticity = Math.Min(1, movableArea1.Elasticity * movableArea2.Elasticity);
                float friction = movableArea1.Friction * movableArea2.Friction;
                float damageMultiplier = movableArea1.Damage * movableArea2.Damage;
                Vector2 move1afterElastic = new Vector2(move1.X * (gob1.Mass - gob2.Mass) / (gob1.Mass + gob2.Mass), move1.Y);
                Vector2 move1afterInelastic = move1 * gob1.Mass / (gob1.Mass + gob2.Mass);
                Vector2 move1after;
                move1after.X = MathHelper.Lerp(move1afterInelastic.X, move1afterElastic.X, elasticity);
                move1after.Y = AWMathHelper.InterpolateTowards(move1afterElastic.Y, move1afterInelastic.Y, friction * (move1.X - move1after.X));
                Vector2 move2afterElastic = new Vector2(move1.X * 2 * gob1.Mass / (gob1.Mass + gob2.Mass), 0);
                // move2afterInelastic = move1afterInelastic because the gobs stick together
                Vector2 move2after;
                move2after.X = MathHelper.Lerp(move1afterInelastic.X, move2afterElastic.X, elasticity);
                move2after.Y = AWMathHelper.InterpolateTowards(move2afterElastic.Y, move1afterInelastic.Y, friction * move2after.X);
                gob1.Move = xUnit * move1after.X + yUnit * move1after.Y + gob2.Move;
                if (allowSideEffects) gob2.Move = xUnit * move2after.X + yUnit * move2after.Y + gob2.Move;
                Vector2 move1Delta = move1 - move1after;
                // move2Delta = move2after because collision is calculated from the 2nd gob's point of view (2nd gob is still)

                if (allowSideEffects &&
                    (move1Delta.LengthSquared() >= MOVABLE_MOVABLE_COLLISION_SOUND_TRESHOLD * MOVABLE_MOVABLE_COLLISION_SOUND_TRESHOLD ||
                    move2after.LengthSquared() >= MOVABLE_MOVABLE_COLLISION_SOUND_TRESHOLD * MOVABLE_MOVABLE_COLLISION_SOUND_TRESHOLD))
                {
                    if ((movableArea1.Type & CollisionAreaType.PhysicalDamageable) != 0)
                        gob2.PhysicalCollisionInto(gob1, move1Delta, damageMultiplier);
                    if ((movableArea2.Type & CollisionAreaType.PhysicalDamageable) != 0)
                        gob1.PhysicalCollisionInto(gob2, move2after, damageMultiplier);
                    if (gob1 is Gobs.Ship || gob2 is Gobs.Ship) return CollisionSoundTypes.ShipCollision;
                }
            }
            return CollisionSoundTypes.None;
        }

        #endregion Collision and moving methods

        #region Arena boundary methods

        /// <summary>
        /// Returns the status of the gob's location with respect to arena boundary.
        /// </summary>
        private OutOfArenaBounds IsOutOfArenaBounds(Gob gob)
        {
            var result = OutOfArenaBounds.None;
            var tolerance = Game.NetworkMode == NetworkMode.Server ? 1 : 0; // gob pos over the network is low-precision, so some tolerance is needed

            // The arena boundary.
            if (gob.Pos.X < 0 + tolerance) result |= OutOfArenaBounds.Left;
            if (gob.Pos.Y < 0 + tolerance) result |= OutOfArenaBounds.Bottom;
            if (gob.Pos.X > Dimensions.X - tolerance) result |= OutOfArenaBounds.Right;
            if (gob.Pos.Y > Dimensions.Y - tolerance) result |= OutOfArenaBounds.Top;

            // The outer arena boundary.
            if (gob.Pos.X < -ARENA_OUTER_BOUNDARY_THICKNESS) result |= OutOfArenaBounds.OuterLeft;
            if (gob.Pos.Y < -ARENA_OUTER_BOUNDARY_THICKNESS) result |= OutOfArenaBounds.OuterBottom;
            if (gob.Pos.X > Dimensions.X + ARENA_OUTER_BOUNDARY_THICKNESS) result |= OutOfArenaBounds.OuterRight;
            if (gob.Pos.Y > Dimensions.Y + ARENA_OUTER_BOUNDARY_THICKNESS) result |= OutOfArenaBounds.OuterTop;

            return result;
        }

        /// <summary>
        /// Returns true iff the gob is positioned legally in respect of 
        /// the arena boundaries. Think of this as physical overlap
        /// consistency but against arena boundaries instead of other gobs.
        /// </summary>
        private bool ArenaBoundaryLegal(Gob gob)
        {
            return !gob.IsKeptInArenaBounds || IsOutOfArenaBounds(gob) == OutOfArenaBounds.None;
        }

        /// <summary>
        /// Performs necessary actions on the gob in respect of its position
        /// and the arena boundaries. Think of this as physical collision
        /// but against arena boundaries instead of other gobs.
        /// </summary>
        /// <param name="allowSideEffects">Should effects other than changing the gob's
        /// position and movement be allowed.</param>
        private void ArenaBoundaryActions(Gob gob, bool allowSideEffects)
        {
            var outOfBounds = IsOutOfArenaBounds(gob);
            if (gob.IsKeptInArenaBounds)
            {
                if (outOfBounds.HasFlag(OutOfArenaBounds.Left))
                    gob.Move = new Vector2(MathHelper.Max(0, gob.Move.X), gob.Move.Y);
                if (outOfBounds.HasFlag(OutOfArenaBounds.Bottom))
                    gob.Move = new Vector2(gob.Move.X, MathHelper.Max(0, gob.Move.Y));
                if (outOfBounds.HasFlag(OutOfArenaBounds.Right))
                    gob.Move = new Vector2(MathHelper.Min(0, gob.Move.X), gob.Move.Y);
                if (outOfBounds.HasFlag(OutOfArenaBounds.Top))
                    gob.Move = new Vector2(gob.Move.X, MathHelper.Min(0, gob.Move.Y));
                return;
            }

            // Gobs that are not kept in arena bounds disintegrate if they are outside 
            // the outer arena boundary.
            if ((outOfBounds & OutOfArenaBounds.OuterBoundary) != 0 && allowSideEffects)
                Gobs.Remove(gob);
        }

        #endregion Arena boundary methods

        public void MakeConsistent(Type limitationAttribute)
        {
            Gobs = new GobCollection(Layers);
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                SetGameAndArenaToGobs();
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
            var outerBoundaryThickness = new Vector2(ARENA_OUTER_BOUNDARY_THICKNESS);
            var arenaBounds = new AABB(-outerBoundaryThickness, Dimensions + outerBoundaryThickness);
            _world = new World(_gravity, arenaBounds);
            var arenaBoundaryVertices = new Vertices(new[]
            {
                Vector2.Zero,
                new Vector2(0, Dimensions.Y),
                Dimensions,
                new Vector2(Dimensions.X, 0),
            });
            var arenaBoundary = BodyFactory.CreateLoopShape(_world, arenaBoundaryVertices);
            arenaBoundary.CollisionCategories = Category.All;
            arenaBoundary.CollidesWith = Category.All;
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

        #region Callbacks

        public void DebugDrawGob(Gob gob, Matrix view, Matrix projection)
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
                            aabb.LowerBound, new Vector2(aabb.LowerBound.X, aabb.UpperBound.Y),
                            aabb.UpperBound, new Vector2(aabb.UpperBound.X, aabb.LowerBound.Y), aabb.LowerBound);
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
            var irreversibleSideEffects = PerformCustomCollision(myArea, theirArea, false);
            if (Game.NetworkMode == NetworkMode.Server && irreversibleSideEffects)
                _collisionEvents.Add(new CollisionEvent(myArea, theirArea, stuck: false /*!!!*/, collideBothWays: false /*!!!*/, sound: CollisionSoundTypes.None /*!!!*/));
            return true;
        }

        #endregion
    }
}
