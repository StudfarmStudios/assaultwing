using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game.Arenas;
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

        /// <summary>
        /// Layers of the arena.
        /// </summary>
        [TypeParameter]
        private List<ArenaLayer> _layers;

        [TypeParameter]
        private ArenaInfo _info;

        /// <summary>
        /// Tunes to play in the background while playing this arena.
        /// </summary>
        [TypeParameter]
        private List<BackgroundMusic> _backgroundMusic;

        [TypeParameter]
        private string _binFilename;

        #endregion General fields

        #region Lighting related fields

        [TypeParameter]
        private Vector3 light0DiffuseColor;

        [TypeParameter]
        private Vector3 light0Direction;

        [TypeParameter]
        private bool light0Enabled;

        [TypeParameter]
        private Vector3 light0SpecularColor;

        [TypeParameter]
        private Vector3 light1DiffuseColor;

        [TypeParameter]
        private Vector3 light1Direction;

        [TypeParameter]
        private bool light1Enabled;

        [TypeParameter]
        private Vector3 light1SpecularColor;

        [TypeParameter]
        private Vector3 light2DiffuseColor;

        [TypeParameter]
        private Vector3 light2Direction;

        [TypeParameter]
        private bool light2Enabled;

        [TypeParameter]
        private Vector3 light2SpecularColor;

        [TypeParameter]
        private Vector3 fogColor;

        [TypeParameter]
        private bool fogEnabled;

        [TypeParameter]
        private float fogEnd;

        [TypeParameter]
        private float fogStart;

        #endregion Lighting related fields

        #region Collision related fields

        /// <summary>
        /// Registered collision areas by type. The array element for index <c>i</c>
        /// holds the collision areas for <c>(CollisionAreaType)(2 &lt;&lt; i)</c>.
        /// </summary>
        private SpatialGrid<CollisionArea>[] _collisionAreas;

        /// <summary>
        /// Marks which collision area types may collide, i.e.
        /// have <see cref="CollisionArea.CollidesAgainst"/> set to something
        /// else than <see cref="CollisionAreaType.None"/>.
        /// </summary>
        private bool[] _collisionAreaMayCollide;

        /// <summary>
        /// Distance outside the arena boundaries that we still allow some gobs to stay alive.
        /// </summary>
        private const float ARENA_OUTER_BOUNDARY_THICKNESS = 1000;

        /// <summary>
        /// Accuracy of finding the point of collision. Measured in game time.
        /// </summary>
        /// Exactly, when a collision occurs, the moving gob's point of collision
        /// will be no more than <see cref="COLLISION_ACCURACY"/>'s time of movement
        /// away from the actual point of collision.
        private readonly TimeSpan COLLISION_ACCURACY = new TimeSpan((long)(TimeSpan.TicksPerSecond * 0.01));

        /// <summary>
        /// Accuracy to which movement of a gob in a frame is done. 
        /// Measured in game time.
        /// </summary>
        /// This constant is to work around rounding errors.
        private readonly TimeSpan MOVEMENT_ACCURACY = new TimeSpan((long)(TimeSpan.TicksPerSecond * 0.00001666));

        /// <summary>
        /// The maximum number of times to try to move a gob in one frame.
        /// Each movement attempt ends either in success or a collision.
        /// </summary>
        /// Higher number means more accurate and responsive collisions
        /// but requires more CPU power in complex situations.
        /// Low numbers may result in "lazy" collisions.
        private const int MOVE_TRY_MAXIMUM = 4;

        /// <summary>
        /// Maximum length to move a gob at one time, in meters.
        /// </summary>
        /// Small values give better collision precision but require more computation.
        private const float MOVE_LENGTH_MAXIMUM = 10;

        /// <summary>
        /// The maximum number of attempts to find a free position for a gob.
        /// </summary>
        private const int FREE_POS_MAX_ATTEMPTS = 50;

        /// <summary>
        /// Radius in meters to check when determining if a position is free for a gob.
        /// </summary>
        private const float FREE_POS_CHECK_RADIUS_MIN = 20;

        /// <summary>
        /// Multiplier for collision damage.
        /// </summary>
        private const float COLLISION_DAMAGE_DOWNGRADE = 0.0006f;

        /// <summary>
        /// Minimum change of gob speed in a collision to cause damage and a sound effect.
        /// </summary>
        private const float MINIMUM_COLLISION_DELTA = 20;

        /// <summary>
        /// Excess area to cover by the spatial index of wall triangles,
        /// in addition to arena boundaries.
        /// </summary>
        private const float WALL_TRIANGLE_ARENA_EXCESS = 1000;

        /// <summary>
        /// Cell sizes of each type of collision area, or 
        /// a negative value if the type is not in use.
        /// </summary>
        /// Indexed by bit indices of <see cref="CollisionAreaType"/>.
        private static readonly float[] COLLISION_AREA_CELL_SIZE;

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

        public ArenaInfo Info { get { return _info; } set { _info = value; } }

        /// <summary>
        /// The width and height of the arena.
        /// </summary>
        public Vector2 Dimensions { get { return Info.Dimensions; } }

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

        public bool IsFogOverrideDisabled { get; set; }

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

        /// <summary>
        /// The bgmusics the arena contains when it is activated.
        /// </summary>
        public List<BackgroundMusic> BackgroundMusic { get { return _backgroundMusic; } }

        public bool IsActive { get { return this == Game.DataEngine.Arena; } }

        public event Action<Gob> GobAdded;
        public event Action<Gob> GobRemoved;

        #endregion // Arena properties

        static Arena()
        {
            COLLISION_AREA_CELL_SIZE = new float[CollisionArea.COLLISION_AREA_TYPE_COUNT];
            for (int i = 0; i < CollisionArea.COLLISION_AREA_TYPE_COUNT; ++i)
                COLLISION_AREA_CELL_SIZE[i] = -1;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.Receptor)] = float.MaxValue;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.Force)] = float.MaxValue;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.WallBounds)] = 500;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalShip)] = 100;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalShot)] = 100;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalWall)] = 20;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalWater)] = 100;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalGas)] = 100;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalOtherUndamageableUnmovable)] = 100;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalOtherDamageableUnmovable)] = 100;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalOtherUndamageableMovable)] = 100;
            COLLISION_AREA_CELL_SIZE[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalOtherDamageableMovable)] = 100;
        }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Arena()
        {
            Info = new ArenaInfo { Name = "dummyarena", Dimensions = new Vector2(4000, 4000) };
            _layers = new List<ArenaLayer>();
            _layers.Add(new ArenaLayer());
            Gobs = new GobCollection(_layers);
            Bin = new ArenaBin();
            _backgroundMusic = new List<BackgroundMusic>();
            light0DiffuseColor = Vector3.Zero;
            light0Direction = -Vector3.UnitZ;
            light0Enabled = true;
            light0SpecularColor = Vector3.Zero;
            light1DiffuseColor = Vector3.Zero;
            light1Direction = -Vector3.UnitZ;
            light1Enabled = false;
            light1SpecularColor = Vector3.Zero;
            light2DiffuseColor = Vector3.Zero;
            light2Direction = -Vector3.UnitZ;
            light2Enabled = false;
            light2SpecularColor = Vector3.Zero;
            fogColor = Vector3.Zero;
            fogEnabled = false;
            fogEnd = 1.0f;
            fogStart = 0.0f;
        }

        #region Public methods

        /// <summary>
        /// Loads an arena from file, or throws an exception on failure.
        /// </summary>
        public static Arena FromFile(AssaultWingCore game, string filename)
        {
            var arena = (Arena)TypeLoader.LoadTemplate(filename, typeof(Arena), typeof(TypeParameterAttribute));
            arena.Game = game;
            return arena;
        }

        /// <summary>
        /// Loads graphical content required by the arena.
        /// </summary>
        public void LoadContent()
        {
            foreach (var gob in Gobs) gob.LoadContent();
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
            UnloadContent();
            foreach (var gob in Gobs) gob.Dispose();
            Gobs.Clear();
            for (int i = 0; i < _collisionAreas.Length; ++i)
                _collisionAreas[i] = null;
        }

        /// <summary>
        /// Resets the arena for a new play session.
        /// </summary>
        public void Reset()
        {
            TotalTime = TimeSpan.Zero;
            FrameNumber = 0;
            InitializeCollisionAreas();
            InitializeGobs();
        }

        /// <summary>
        /// Moves the given gob and performs physical collisions in order to
        /// maintain overlap consistency as specified in <b>CollisionArea.CannotOverlap</b>
        /// of the moving gob's physical collision area.
        /// </summary>
        public void Move(Gob gob, int frameCount, bool allowSideEffects)
        {
            Move(gob, Game.TargetElapsedTime.Multiply(frameCount), allowSideEffects);
        }

        /// <summary>
        /// As <see cref="Move(Gob, int, bool)"/> but time delta is specified in game time.
        /// </summary>
        private void Move(Gob gob, TimeSpan moveTime, bool allowSideEffects)
        {
            if (!gob.Movable) return;
            if (gob.Disabled) return;
            CollisionArea gobPhysical = gob.PhysicalArea;
            if (gobPhysical != null) Unregister(gobPhysical);

            if (allowSideEffects)
            {
                // If the gob is stuck, let it resolve the situation.
                if (gobPhysical != null)
                    ForEachOverlapper(gobPhysical, gobPhysical.CannotOverlap, delegate(CollisionArea area2)
                    {
                        gob.Collide(gobPhysical, area2, true);
                        area2.Owner.Collide(area2, gobPhysical, true);
                        // No need for a physical collision -- the gobs are stuck.
                        return gob.Dead;
                    });
                if (gob.Dead) return;
            }

            int attempts = 0;
            while (moveTime > MOVEMENT_ACCURACY && attempts < MOVE_TRY_MAXIMUM)
            {
                var oldMove = gob.Move;
                var gobFrameMove = gob.Move * (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
                int moveChunkCount = (int)Math.Ceiling(gobFrameMove.Length() / MOVE_LENGTH_MAXIMUM);
                if (moveChunkCount == 0) moveChunkCount = 1;
                var chunkMoveTime = moveTime.Divide(moveChunkCount);
                for (int chunk = 0; chunk < moveChunkCount; ++chunk)
                {
                    var currentChunkMoveTime = chunkMoveTime;
                    TryMove(gob, ref currentChunkMoveTime, allowSideEffects);
                    moveTime -= chunkMoveTime - currentChunkMoveTime;
                    if (currentChunkMoveTime > TimeSpan.Zero) break; // stop iterating chunks if the gob collided
                }
                ++attempts;

                // If we just have to wait for another gob to move out of the way,
                // there's nothing more we can do.
                if (gob.Move == oldMove)
                    break;
            }
            if (gobPhysical != null) Register(gobPhysical);
            ArenaBoundaryActions(gob, allowSideEffects);
        }

        /// <summary>
        /// Performs nonphysical collisions. Must be called every frame
        /// after all gob movement is done.
        /// </summary>
        public void PerformNonphysicalCollisions()
        {
            for (int bitIndex = 0; bitIndex < _collisionAreas.Length; ++bitIndex)
            {
                var container = _collisionAreas[bitIndex];
                if (container != null && _collisionAreaMayCollide[bitIndex])
                    container.ForEachElement(area =>
                    {
                        ForEachOverlapper(area, area.CollidesAgainst, area2 =>
                        {
                            area.Owner.Collide(area, area2, false);
                            return false;
                        });
                        return false;
                    });
            }
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

        /// <summary>
        /// Registers a gob for collisions.
        /// </summary>
        public void Register(Gob gob) // TODO !!! This method should be private
        {
            foreach (CollisionArea area in gob.CollisionAreas)
            {
                Register(area);
                int bitIndex = AWMathHelper.LogTwo((int)area.Type);
                if (area.CollidesAgainst != CollisionAreaType.None)
                    _collisionAreaMayCollide[bitIndex] = true;
            }
        }

        /// <summary>
        /// Removes a previously registered gob from having collisions.
        /// </summary>
        public void Unregister(Gob gob) // TODO !!! This method should be private
        {
            foreach (CollisionArea area in gob.CollisionAreas)
                Unregister(area);
        }

        /// <summary>
        /// Removes a previously registered collision area from having collisions.
        /// </summary>
        public void Unregister(CollisionArea area)
        {
            SpatialGridElement<CollisionArea> element = (SpatialGridElement<CollisionArea>)area.CollisionData;
            if (element == null) return;
            _collisionAreas[AWMathHelper.LogTwo((int)area.Type)].Remove(element);
            area.CollisionData = null;
        }

        /// <summary>
        /// Tries to return a position in an area of the game world 
        /// where a gob is overlap consistent (e.g. not inside a wall).
        /// </summary>
        /// <param name="gob">The gob to position.</param>
        /// <param name="area">The area where to look for a position.</param>
        /// <returns>A position in the area where the gob is overlap consistent.</returns>
        public Vector2 GetFreePosition(Gob gob, IGeomPrimitive area)
        {
            Vector2 result;
            GetFreePosition(gob, area, out result);
            return result;
        }

        /// <summary>
        /// Tries to return a legal position in an area of the game world 
        /// where a gob is overlap consistent (e.g. not inside a wall).
        /// </summary>
        /// <param name="gob">The gob to position.</param>
        /// <param name="area">The area where to look for a position.</param>
        /// <param name="result">Best try for a position in the area where the gob is overlap consistent.</param>
        /// <returns>true if <paramref name="result"/> is legal and overlap consistent,
        /// false if the search failed.</returns>
        public bool GetFreePosition(Gob gob, IGeomPrimitive area, out Vector2 result)
        {
            // Iterate in the area for a while, looking for a free position.
            // Ultimately give up and return something that may be bad.
            for (int attempt = 1; attempt < FREE_POS_MAX_ATTEMPTS; ++attempt)
            {
                Vector2 tryPos = Geometry.GetRandomLocation(area);
                if (IsFreePosition(gob, tryPos))
                {
                    result = tryPos;
                    return true;
                }
            }
            result = Geometry.GetRandomLocation(area);
            return false;
        }

        /// <summary>
        /// Is a gob overlap consistent (e.g. not inside a wall) at a position. 
        /// </summary>
        /// <param name="gob">The gob.</param>
        /// <param name="position">The position.</param>
        /// <returns><b>true</b> iff the gob is overlap consistent at the position.</returns>
        public bool IsFreePosition(Gob gob, Vector2 position)
        {
            if (gob.PhysicalArea == null) return true;

            // Make sure Gob.WorldMatrix doesn't contain NaN's.
            var oldPos = gob.Pos;
            var oldRotation = gob.Rotation;
            gob.Pos = Vector2.Zero;
            gob.Rotation = 0;

            var boundingDimensions = gob.PhysicalArea.Area.BoundingBox.Dimensions;
            float checkRadiusMeters = MathHelper.Max(FREE_POS_CHECK_RADIUS_MIN,
                2 * MathHelper.Max(boundingDimensions.X, boundingDimensions.Y));
            float checkRadiusGobCoords = checkRadiusMeters / gob.Scale; // in gob coordinates
            var wallCheckArea = new CollisionArea("", new Circle(Vector2.Zero, checkRadiusGobCoords), gob,
                gob.PhysicalArea.Type, gob.PhysicalArea.CollidesAgainst, gob.PhysicalArea.CannotOverlap, CollisionMaterialType.Regular);
            gob.Pos = position;
            bool result = ArenaBoundaryLegal(gob) && !ForEachOverlapper(wallCheckArea, wallCheckArea.CannotOverlap, null);

            // Restore old values
            gob.Pos = oldPos;
            gob.Rotation = oldRotation;

            return result;
        }

        /// <summary>
        /// Removes a round area from all walls of the arena, i.e. makes a hole.
        /// </summary>
        /// <param name="holePos">Center of the hole, in world coordinates.</param>
        /// <param name="holeRadius">Radius of the hole, in meters.</param>
        public void MakeHole(Vector2 holePos, float holeRadius)
        {
            if (holeRadius <= 0) return;
            var boundingBox = new Rectangle(holePos.X - holeRadius, holePos.Y - holeRadius,
                holePos.X + holeRadius, holePos.Y + holeRadius);
            int wallBoundsIndex = AWMathHelper.LogTwo((int)CollisionAreaType.WallBounds);
            _collisionAreas[wallBoundsIndex].ForEachElement(boundingBox, area =>
            {
                ((Gobs.Wall)area.Owner).MakeHole(holePos, holeRadius);
                return false;
            });
        }

        /// <summary>
        /// Sets lighting for the effect.
        /// </summary>
        /// <param name="effect">The effect to modify.</param>
        public void PrepareEffect(BasicEffect effect)
        {
            effect.DirectionalLight0.DiffuseColor = light0DiffuseColor;
            effect.DirectionalLight0.Direction = light0Direction;
            effect.DirectionalLight0.Enabled = light0Enabled;
            effect.DirectionalLight0.SpecularColor = light0SpecularColor;
            effect.DirectionalLight1.DiffuseColor = light1DiffuseColor;
            effect.DirectionalLight1.Direction = light1Direction;
            effect.DirectionalLight1.Enabled = light1Enabled;
            effect.DirectionalLight1.SpecularColor = light1SpecularColor;
            effect.DirectionalLight2.DiffuseColor = light2DiffuseColor;
            effect.DirectionalLight2.Direction = light2Direction;
            effect.DirectionalLight2.Enabled = light2Enabled;
            effect.DirectionalLight2.SpecularColor = light2SpecularColor;
            effect.FogColor = fogColor;
            effect.FogEnabled = IsFogOverrideDisabled ? false : fogEnabled;
            effect.FogEnd = fogEnd;
            effect.FogStart = fogStart;
            effect.LightingEnabled = true;
        }

        #endregion Public methods

        #region Collision and moving methods

        /// <summary>
        /// Registers a collision area for collisions.
        /// </summary>
        private void Register(CollisionArea area)
        {
            if (area.CollisionData != null) throw new InvalidOperationException("Collision area is already registered");
            int bitIndex = AWMathHelper.LogTwo((int)area.Type);
            area.CollisionData = _collisionAreas[bitIndex].Add(area, area.Area.BoundingBox);
        }

        /// <summary>
        /// Registers a gob's physical collision areas for collisions.
        /// </summary>
        /// <seealso cref="Register(Gob)"/>
        private void RegisterPhysical(Gob gob)
        {
            foreach (var area in gob.CollisionAreas)
                if ((area.Type & CollisionAreaType.Physical) != 0)
                    Register(area);
        }

        /// <summary>
        /// Removes a previously registered gob's physical collision areas
        /// from the register.
        /// </summary>
        /// <seealso cref="Unregister(Gob)"/>
        private void UnregisterPhysical(Gob gob)
        {
            foreach (var area in gob.CollisionAreas)
                if ((area.Type & CollisionAreaType.Physical) != 0)
                    Unregister(area);
        }

        /// <summary>
        /// Tries to move a gob, stopping it at the first physical collision 
        /// and performing the physical collision.
        /// </summary>
        /// <param name="gob">The gob to move.</param>
        /// <param name="moveTime">Remaining duration of the move</param>
        /// <param name="allowSideEffects">Should effects other than changing the gob's
        /// position and movement be allowed.</param>
        private void TryMove(Gob gob, ref TimeSpan moveTime, bool allowSideEffects)
        {
            Vector2 oldPos = gob.Pos;
            Vector2 oldMove = gob.Move;
            TimeSpan moveGood = TimeSpan.Zero; // last known safe position
            TimeSpan moveBad = moveTime; // last known unsafe position, if 'badFound'
            bool badFound = false;
            CollisionArea gobPhysical = gob.PhysicalArea;
            bool badDueToOverlappers = false;

            // Find out last non-collision position and first colliding position, 
            // up to required accuracy.
            TimeSpan moveTry = moveTime;
            TimeSpan lastMoveTry = TimeSpan.Zero;
            float oldMoveLength = oldMove.Length();
            bool firstIteration = true;
            while (firstIteration || moveBad - moveGood > COLLISION_ACCURACY)
            {
                firstIteration = false;
                gob.Pos = LerpGobPos(oldPos, oldMove, moveTry);
                bool overlapperFound = gobPhysical == null ? false
                    : ForEachOverlapper(gobPhysical, gobPhysical.CannotOverlap, null);
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

            if (badFound)
            {
                gob.Pos = LerpGobPos(oldPos, oldMove, moveBad);
                PerformPhysicalCollisions(gob, allowSideEffects, gobPhysical, badDueToOverlappers);
            }

            // Return to last non-colliding position.
            gob.Pos = LerpGobPos(oldPos, oldMove, moveGood);
            moveTime -= moveGood;
        }

        private static Vector2 LerpGobPos(Vector2 startPos, Vector2 move, TimeSpan moveTime)
        {
            return startPos + move * (float)moveTime.TotalSeconds;
        }

        private void PerformPhysicalCollisions(Gob gob, bool allowSideEffects, CollisionArea gobPhysical, bool badDueToOverlappers)
        {
            if (badDueToOverlappers)
                ForEachOverlapper(gobPhysical, gobPhysical.CannotOverlap, delegate(CollisionArea area2)
                {
                    if (allowSideEffects)
                    {
                        gob.Collide(gobPhysical, area2, false);
                        area2.Owner.Collide(area2, gobPhysical, false);
                    }
                    PerformCollision(gobPhysical, area2, allowSideEffects);
                    return gob.Dead;
                });
            ArenaBoundaryActions(gob, allowSideEffects);
        }

        /// <summary>
        /// Performs an action on each collision area of certain types that overlap a collision area,
        /// or just finds out if there are any overlappers.
        /// </summary>
        /// <param name="area">The collision area to check overlap against.</param>
        /// <param name="types">The types of collision areas to consider.</param>
        /// <param name="action">The action to perform on each overlapper, or if <b>null</b>
        /// then the method will return on first found overlapper.
        /// If the action returns <b>true</b>, the iteration will break.</param>
        /// <returns><b>true</b> if an overlapper was found, <b>false</b> otherwise.</returns>
        private bool ForEachOverlapper(CollisionArea area, CollisionAreaType types,
            Predicate<CollisionArea> action)
        {
            var areaArea = area.Area;
            var boundingBox = areaArea.BoundingBox;
            bool overlapperFound = false;
            Gob areaOwner = area.Owner;
            bool areaOwnerCold = areaOwner.Cold;
            Player areaOwnerOwner = areaOwner.Owner;
            bool breakOut = false;
            for (int typeBit = 0; typeBit < _collisionAreas.Length; ++typeBit)
            {
                if (((1 << typeBit) & (int)types) == 0) continue;
                _collisionAreas[typeBit].ForEachElement(boundingBox, delegate(CollisionArea area2)
                {
                    if (!Geometry.Intersect(areaArea, area2.Area)) return false;
                    Gob area2Owner = area2.Owner;
                    if (areaOwner == area2Owner) return false;
                    if (area2Owner.Disabled) return false;
                    if ((areaOwnerCold || area2Owner.Cold) &&
                        areaOwnerOwner != null &&
                        areaOwnerOwner == area2Owner.Owner)
                        return false;

                    // All checks passed.
                    overlapperFound = true;
                    if (action == null || action(area2))
                        breakOut = true;
                    return breakOut;
                });
                if (breakOut) break;
            }
            return overlapperFound;
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
        public void PerformCollision(CollisionArea area1, CollisionArea area2, bool allowSideEffects)
        {
#if DEBUG_PROFILE
            ++AssaultWing.Instance.CollisionCount;
#endif
            // At least one area must be from a movable gob, lest there be no collision.
            bool area1Movable = (area1.Type & CollisionAreaType.PhysicalMovable) != 0;
            bool area2Movable = (area2.Type & CollisionAreaType.PhysicalMovable) != 0;
            if (!area1Movable) PerformCollisionMovableUnmovable(area2, area1, allowSideEffects);
            else if (!area2Movable) PerformCollisionMovableUnmovable(area1, area2, allowSideEffects);
            else PerformCollisionMovableMovable(area1, area2, allowSideEffects);
        }

        /// <summary>
        /// Performs a physical collision between a movable gob and an unmovable gob.
        /// </summary>
        /// <param name="movableArea">The overlapping collision area of the movable gob.</param>
        /// <param name="unmovableArea">The overlapping collision area of the unmovable gob.</param>
        /// <param name="allowSideEffects">Should effects other than changing the movable gob's
        /// position and movement be allowed.</param>
        private void PerformCollisionMovableUnmovable(CollisionArea movableArea, CollisionArea unmovableArea, bool allowSideEffects)
        {
            Gob movableGob = movableArea.Owner;
            Gob unmovableGob = unmovableArea.Owner;

            // Turn the coordinate axes so that both gobs are on the X-axis, unmovable gob on the left;
            // 'xUnit' and 'yUnit' will be the unit vectors of this system represented in game world coordinates;
            // 'move1' will be the movement vector of gob1 in this system (and gob2 stays put);
            // 'move1after' will be the resulting movement vector of gob1 in this system.
            Vector2 xUnit = Geometry.GetNormal(unmovableArea.Area, movableArea.Area);
            Vector2 yUnit = new Vector2(-xUnit.Y, xUnit.X);
            Vector2 move1 = new Vector2(Vector2.Dot(movableGob.Move, xUnit),
                                        Vector2.Dot(movableGob.Move, yUnit));

            // Only perform physical collision if the gobs are actually closing in on each other.
            if (move1.X >= 0)
            {
                // To work around rounding errors when the movable gob is sliding
                // almost parallel to the unmovable gob's area surface, 
                // adjust the movable gob to go a bit more towards the unmovable.
                if (move1.X < 0.08f)
                    move1.X -= 0.08f;
            }
            if (move1.X < 0)
            {
                // Elasticity factor is between 0 and 1. It is used for linear interpolation
                // between the perfectly elastic collision and the perfectly inelastic collision.
                // Friction factor is from 0 and up. In real life, friction force
                // is the product of the friction factor and the head-on component
                // of the force that pushes the colliding movable gob away. Here, we 
                // imitate that force by the change of the head-on component of
                // gob movement vector.
                float elasticity = Math.Min(1, movableArea.Elasticity * unmovableArea.Elasticity);
                float friction = movableArea.Friction * unmovableArea.Friction;
                float damageMultiplier = movableArea.Damage * unmovableArea.Damage;
                Vector2 move1afterElastic = new Vector2(-move1.X, move1.Y);
                // move1afterInelastic = Vector2.Zero
                Vector2 move1after;
                move1after.X = MathHelper.Lerp(0, move1afterElastic.X, elasticity);
                move1after.Y = AWMathHelper.InterpolateTowards(move1afterElastic.Y, 0, friction * (move1after.X - move1.X));
                movableGob.Move = xUnit * move1after.X + yUnit * move1after.Y;
                Vector2 move1Delta = move1 - move1after;

                if (allowSideEffects)
                {
                    // Inflict damage to damageable gobs.
                    if ((movableArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
                    {
                        if (move1Delta.Length() > MINIMUM_COLLISION_DELTA)
                            movableGob.InflictDamage(CollisionDamage(movableGob, move1Delta, damageMultiplier),
                                new DeathCause(movableGob, DeathCauseType.Collision, unmovableGob));
                    }
                    /* TODO: What if the unmovable gob wants to be damaged, too?
                    if ((unmovableArea.Type2 & CollisionAreaType.PhysicalDamageable) != 0)
                    {
                        Vector2 move2Delta = Vector2.Zero;
                        if (move1Delta.Length() > MINIMUM_COLLISION_DELTA)
                            unmovableGob.InflictDamage(CollisionDamage(unmovableGob, move2Delta, damageMultiplier));
                    }
                    */
                    PlayWallCollisionSound(movableGob, move1Delta);
                }
            }
        }

        /// <summary>
        /// Performs a physical collision between two movable gobs.
        /// </summary>
        /// <param name="movableArea1">The overlapping collision area of one movable gob.</param>
        /// <param name="movableArea2">The overlapping collision area of the other movable gob.</param>
        /// <param name="allowSideEffects">Should effects other than changing the first movable gob's
        /// position and movement be allowed.</param>
        private void PerformCollisionMovableMovable(CollisionArea movableArea1, CollisionArea movableArea2, bool allowSideEffects)
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

                if (allowSideEffects)
                {
                    /*We want to deal damage in physics engine because calculations only happen once per collision!*/
                    // Inflict damage to damageable gobs.
                    if ((movableArea1.Type & CollisionAreaType.PhysicalDamageable) != 0)
                    {
                        if (move1Delta.Length() > MINIMUM_COLLISION_DELTA)
                            gob1.InflictDamage(CollisionDamage(gob1, move1Delta, damageMultiplier),
                                new DeathCause(gob1, DeathCauseType.Collision, gob2));
                    }
                    if ((movableArea2.Type & CollisionAreaType.PhysicalDamageable) != 0)
                    {
                        if (move2after.Length() > MINIMUM_COLLISION_DELTA)
                            gob2.InflictDamage(CollisionDamage(gob2, move2after, damageMultiplier),
                                new DeathCause(gob2, DeathCauseType.Collision, gob1));
                    }
                    PlayGobCollisionSound(gob1, gob2, move1Delta, move2after);
                }
            }
        }

        private void PlayWallCollisionSound(Gob gob, Vector2 moveDelta)
        {
            // Be silent on mild collisions.
            if (moveDelta.Length() < MINIMUM_COLLISION_DELTA) return;

            if (!(gob is Gobs.Ship)) return; // happens a lot, we need some peaceful sound here!!!
            Game.SoundEngine.PlaySound("Collision");
        }

        private void PlayGobCollisionSound(Gob gob1, Gob gob2, Vector2 move1Delta, Vector2 move2Delta)
        {
            // Be silent on mild collisions.
            if (move1Delta.Length() < MINIMUM_COLLISION_DELTA && move2Delta.Length() < MINIMUM_COLLISION_DELTA) return;

            if (!(gob1 is Gobs.Ship) && !(gob2 is Gobs.Ship)) return; // happens a lot, we need some peaceful sound here!!!
            Game.SoundEngine.PlaySound("Shipcollision");
        }

        private float CollisionDamage(Gob gob, Vector2 moveDelta, float damageMultiplier)
        {
            return moveDelta.Length() / 2 * gob.Mass * COLLISION_DAMAGE_DOWNGRADE * damageMultiplier;
        }

        #endregion Collision and moving methods

        #region Arena boundary methods

        /// <summary>
        /// Returns the status of the gob's location with respect to arena boundary.
        /// </summary>
        private OutOfArenaBounds IsOutOfArenaBounds(Gob gob)
        {
            var result = OutOfArenaBounds.None;

            // The arena boundary.
            if (gob.Pos.X < 0) result |= OutOfArenaBounds.Left;
            if (gob.Pos.Y < 0) result |= OutOfArenaBounds.Bottom;
            if (gob.Pos.X > Dimensions.X) result |= OutOfArenaBounds.Right;
            if (gob.Pos.Y > Dimensions.Y) result |= OutOfArenaBounds.Top;

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
            if (gob is Gobs.Ship)
            {
                OutOfArenaBounds outOfBounds = IsOutOfArenaBounds(gob);
                return outOfBounds == OutOfArenaBounds.None;
            }
            return true;
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

            // Ships we restrict to the arena boundary.
            if (gob is Gobs.Ship)
            {
                if ((outOfBounds & OutOfArenaBounds.Left) != 0)
                    gob.Move = new Vector2(MathHelper.Max(0, gob.Move.X), gob.Move.Y);
                if ((outOfBounds & OutOfArenaBounds.Bottom) != 0)
                    gob.Move = new Vector2(gob.Move.X, MathHelper.Max(0, gob.Move.Y));
                if ((outOfBounds & OutOfArenaBounds.Right) != 0)
                    gob.Move = new Vector2(MathHelper.Min(0, gob.Move.X), gob.Move.Y);
                if ((outOfBounds & OutOfArenaBounds.Top) != 0)
                    gob.Move = new Vector2(gob.Move.X, MathHelper.Min(0, gob.Move.Y));
                return;
            }

            // Other projectiles disintegrate if they are outside 
            // the outer arena boundary.
            if ((outOfBounds & OutOfArenaBounds.OuterBoundary) != 0 && allowSideEffects)
                Gobs.Remove(gob);
        }

        #endregion Arena boundary methods

        #region IConsistencyCheckable Members

        public void MakeConsistent(Type limitationAttribute)
        {
            Gobs = new GobCollection(Layers);
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                light0DiffuseColor = Vector3.Clamp(light0DiffuseColor, Vector3.Zero, Vector3.One);
                light0Direction.Normalize();
                light0SpecularColor = Vector3.Clamp(light0SpecularColor, Vector3.Zero, Vector3.One);
                light1DiffuseColor = Vector3.Clamp(light1DiffuseColor, Vector3.Zero, Vector3.One);
                light1Direction.Normalize();
                light1SpecularColor = Vector3.Clamp(light1SpecularColor, Vector3.Zero, Vector3.One);
                light2DiffuseColor = Vector3.Clamp(light2DiffuseColor, Vector3.Zero, Vector3.One);
                light2Direction.Normalize();
                light2SpecularColor = Vector3.Clamp(light2SpecularColor, Vector3.Zero, Vector3.One);
                fogColor = Vector3.Clamp(fogColor, Vector3.Zero, Vector3.One);
                fogEnd = MathHelper.Max(fogEnd, 0);
                fogStart = MathHelper.Max(fogStart, 0);
                SetGameAndArenaToGobs();
            }
        }

        #endregion

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
            FindGameplayLayer(); // makes sure there is a gameplay backlayer
            var oldLayers = _layers;
            _layers = new List<ArenaLayer>();
            foreach (var layer in oldLayers)
            {
                _layers.Add(layer.EmptyCopy());
                foreach (var gob in layer.Gobs)
                    gob.Layer = layer;
            }
            Gobs = new GobCollection(_layers);
            FindGameplayLayer();
            foreach (var gob in new GobCollection(oldLayers))
                Gob.CreateGob(Game, gob, gobb =>
                {
                    gobb.Layer = _layers[oldLayers.IndexOf(gob.Layer)];
                    Gobs.Add(gobb);
                });
        }

        /// <summary>
        /// Initialises <see cref="_collisionAreas"/> for playing the arena.
        /// </summary>
        private void InitializeCollisionAreas()
        {
            _collisionAreas = new SpatialGrid<CollisionArea>[CollisionArea.COLLISION_AREA_TYPE_COUNT];
            _collisionAreaMayCollide = new bool[CollisionArea.COLLISION_AREA_TYPE_COUNT];
            Vector2 areaExcess = new Vector2(WALL_TRIANGLE_ARENA_EXCESS);
            Vector2 arrayDimensions = Dimensions + 2 * areaExcess;
            for (int i = 0; i < _collisionAreas.Length; ++i)
                if (COLLISION_AREA_CELL_SIZE[i] >= 0)
                    _collisionAreas[i] = new SpatialGrid<CollisionArea>(COLLISION_AREA_CELL_SIZE[i],
                        -areaExcess, arrayDimensions - areaExcess);
            _collisionAreaMayCollide.Initialize();
        }

        /// <summary>
        /// Initialises <see cref="GameplayLayer"/> and <see cref="GameplayBackLayer"/>
        /// for the current <see cref="Layers"/>.
        /// </summary>
        private void FindGameplayLayer()
        {
            int gameplayLayerIndex = Layers.FindIndex(layer => layer.IsGameplayLayer);
            if (gameplayLayerIndex == -1)
                throw new ArgumentException("Arena " + Info.Name + " doesn't have a gameplay layer");
            Gobs.GameplayLayer = Layers[gameplayLayerIndex];

            // Make sure the gameplay backlayer is located right before the gameplay layer.
            // Use a suitable layer if one is defined in the arena, otherwise create a new one.
            if (gameplayLayerIndex == 0 || Layers[gameplayLayerIndex - 1].Z != 0)
                Layers.Insert(gameplayLayerIndex, Gobs.GameplayBackLayer = new ArenaLayer(false, 0, ""));
            else
                Gobs.GameplayBackLayer = Layers[gameplayLayerIndex - 1];
        }

        private void AddShipTrackerToViewports(AW2.Game.Gobs.Ship ship)
        {
            var trackerItem = new GobTrackerItem(ship, null, GobTrackerItem.PLAYER_TEXTURE, true, true, false, true, ship.Owner.PlayerColor);

            foreach (var plr in Game.DataEngine.Players)
            {
                if (!plr.IsRemote && plr.ID != ship.Owner.ID)
                {
                    plr.AddGobTrackerItem(trackerItem);
                }
            }
        }

        private void AddDockTrackerToViewports(AW2.Game.Gobs.Dock dock)
        {
            var trackerItem = new GobTrackerItem(dock, null, GobTrackerItem.DOCK_TEXTURE, true, true, false, true, Color.White);

            foreach (var plr in Game.DataEngine.Players)
            {
                if (!plr.IsRemote)
                {
                    plr.AddGobTrackerItem(trackerItem);
                }
            }
        }

        private void AddBonusTrackerToViewports(AW2.Game.Gobs.Bonus.Bonus bonus)
        {
            var trackerItem = new GobTrackerItem(bonus, null, GobTrackerItem.BONUS_TEXTURE, true, true, false, true, Color.White);

            foreach (var plr in Game.DataEngine.Players)
            {
                if (!plr.IsRemote)
                {
                    plr.AddGobTrackerItem(trackerItem);
                }
            }
        }

        #region Callbacks

        private void GobAddedHandler(Gob gob)
        {
            if (IsActive) Game.GobsCounter.Increment();
            Prepare(gob);
            if (gob is AW2.Game.Gobs.Ship)
                AddShipTrackerToViewports((AW2.Game.Gobs.Ship)gob);
            if (gob is AW2.Game.Gobs.Dock)
                AddDockTrackerToViewports((AW2.Game.Gobs.Dock)gob);
            if (gob is AW2.Game.Gobs.Bonus.Bonus)
                AddBonusTrackerToViewports((AW2.Game.Gobs.Bonus.Bonus)gob);
            if (GobAdded != null) GobAdded(gob);
        }

        private bool GobRemovingHandler(Gob gob)
        {
            // Game client removes relevant gobs only when the server says so.
            return Game.NetworkMode != NetworkMode.Client || !gob.IsRelevant;
        }

        private void GobRemovedHandler(Gob gob)
        {
            if (IsActive)
                Game.GobsCounter.Decrement();
            if (gob.Layer == Gobs.GameplayLayer)
                Unregister(gob);
            gob.Dispose();
            if (GobRemoved != null) GobRemoved(gob);
        }

        #endregion
    }
}
