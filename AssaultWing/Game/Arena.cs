using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Events;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Net.Messages;
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
    [System.Diagnostics.DebuggerDisplay("{Name} Dimensions:{Dimensions} Layers:{Layers.Count} Gobs:{Gobs.Count}")]
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

        /// <summary>
        /// Arena File name is needed for arena loading.
        /// </summary>
        string fileName;

        GobCollection gobs;

        /// <summary>
        /// Layers of the arena.
        /// </summary>
        [TypeParameter]
        List<ArenaLayer> layers;

        /// <summary>
        /// Human-readable name of the arena.
        /// </summary>
        [TypeParameter]
        string name;

        /// <summary>
        /// Dimensions of the arena, i.e. maximum coordinates for gobs.
        /// </summary>
        /// Minimum coordinates are always (0,0).
        [TypeParameter]
        Vector2 dimensions;

        /// <summary>
        /// Tunes to play in the background while playing this arena.
        /// </summary>
        [TypeParameter]
        List<BackgroundMusic> backgroundMusic;

        #endregion General fields

        #region Lighting related fields

        [TypeParameter]
        Vector3 light0DiffuseColor;

        [TypeParameter]
        Vector3 light0Direction;

        [TypeParameter]
        bool light0Enabled;

        [TypeParameter]
        Vector3 light0SpecularColor;

        [TypeParameter]
        Vector3 light1DiffuseColor;

        [TypeParameter]
        Vector3 light1Direction;

        [TypeParameter]
        bool light1Enabled;

        [TypeParameter]
        Vector3 light1SpecularColor;

        [TypeParameter]
        Vector3 light2DiffuseColor;

        [TypeParameter]
        Vector3 light2Direction;

        [TypeParameter]
        bool light2Enabled;

        [TypeParameter]
        Vector3 light2SpecularColor;

        [TypeParameter]
        Vector3 fogColor;

        [TypeParameter]
        bool fogEnabled;

        [TypeParameter]
        float fogEnd;

        [TypeParameter]
        float fogStart;

        #endregion Lighting related fields

        #region Collision related fields

        /// <summary>
        /// Registered collision areas by type. The array element for index <c>i</c>
        /// holds the collision areas for <c>(CollisionAreaType)(2 &lt;&lt; i)</c>.
        /// </summary>
        SpatialGrid<CollisionArea>[] collisionAreas;

        /// <summary>
        /// Marks which collision area types may collide, i.e.
        /// have <see cref="CollisionArea.CollidesAgainst"/> set to something
        /// else than <see cref="CollisionAreaType.None"/>.
        /// </summary>
        bool[] collisionAreaMayCollide;

        /// <summary>
        /// Distance outside the arena boundaries that we still allow some gobs to stay alive.
        /// </summary>
        const float arenaOuterBoundaryThickness = 1000;

        /// <summary>
        /// Accuracy of finding the point of collision. Measured in time,
        /// with one frame as the unit.
        /// </summary>
        /// Exactly, when a collision occurs, the moving gob's point of collision
        /// will be no more than <b>collisionAccuracy</b> of a frame's time
        /// away from the actual point of collision.
        /// <b>1</b> means no iteration;
        /// <b>0</b> means iteration to infinite precision.
        const float collisionAccuracy = 0.6f;

        /// <summary>
        /// Accuracy to which movement of a gob in a frame is done. 
        /// Measured in time, with one frame as the unit.
        /// </summary>
        /// This constant is to work around rounding errors.
        const float movementAccuracy = 0.001f;

        /// <summary>
        /// The maximum number of times to try to move a gob in one frame.
        /// Each movement attempt ends either in success or a collision.
        /// </summary>
        /// Higher number means more accurate and responsive collisions
        /// but requires more CPU power in complex situations.
        /// Low numbers may result in "lazy" collisions.
        const int moveTryMaximum = 2;

        /// <summary>
        /// The maximum number of attempts to find a free position for a gob.
        /// </summary>
        const int freePosMaxAttempts = 50;

        /// <summary>
        /// Multiplier for collision damage.
        /// </summary>
        const float collisionDamageDownGrade = 0.0006f;

        /// <summary>
        /// Minimum change of gob speed in a collision to cause damage and a sound effect.
        /// </summary>
        const float minimumCollisionDelta = 20;

        /// <summary>
        /// Excess area to cover by the spatial index of wall triangles,
        /// in addition to arena boundaries.
        /// </summary>
        const float wallTriangleArenaExcess = 1000;

        /// <summary>
        /// Cell sizes of each type of collision area, or 
        /// a negative value if the type is not in use.
        /// </summary>
        /// Indexed by bit indices of <see cref="CollisionAreaType"/>.
        static readonly float[] collisionAreaCellSize;

        #endregion Collision related fields

        #region Arena properties

        /// <summary>
        /// The name of the arena.
        /// </summary>
        public string Name { get { return name; } set { name = value; } }

        /// <summary>
        /// The file name of the arena.
        /// </summary>
        public string FileName { get { return fileName; } set { fileName = value; } }

        /// <summary>
        /// The width and height of the arena.
        /// </summary>
        /// The allowed range of gob X-coordinates is from 0 to arena width.
        /// The allowed range of gob Y-coordinates is from 0 to arena height.
        public Vector2 Dimensions { get { return dimensions; } set { dimensions = value; } }

        /// <summary>
        /// Layers of the arena.
        /// </summary>
        public List<ArenaLayer> Layers { get { return layers; } }

        /// <summary>
        /// Gobs in the arena. Reflects the data in <see cref="Layers"/>.
        /// </summary>
        public GobCollection Gobs
        {
            get { return gobs; }
            private set
            {
                gobs = value;
                Gobs.Added += gob =>
                {
                    if (IsActive)
                        Prepare(gob);

                    // Game server notifies game clients of the new gob.
                    if (AssaultWing.Instance.NetworkMode == NetworkMode.Server && gob.IsRelevant)
                    {
                        var message = new GobCreationMessage();
                        message.CreateToNextArena = !IsActive;
                        message.GobTypeName = gob.TypeName;
                        message.LayerIndex = Layers.IndexOf(gob.Layer);
                        message.Write(gob, AW2.Net.SerializationModeFlags.All);
                        AssaultWing.Instance.NetworkEngine.GameClientConnections.Send(message);
                    }
                };
                Gobs.Removing += item =>
                {
                    // Game client removes relevant gobs only when the server says so.
                    return AssaultWing.Instance.NetworkMode != NetworkMode.Client || !item.IsRelevant;
                };
                Gobs.Removed += gob =>
                {
                    // Game server notifies game clients of the removal of relevant gobs.
                    if (AssaultWing.Instance.NetworkMode == NetworkMode.Server && gob.IsRelevant)
                    {
                        var message = new GobDeletionMessage();
                        message.GobId = gob.Id;
                        AssaultWing.Instance.NetworkEngine.GameClientConnections.Send(message);
                    }

                    if (gob.Layer == Gobs.GameplayLayer)
                        Unregister(gob);
                    gob.Dispose();
                };
            }
        }

        /// <summary>
        /// The bgmusics the arena contains when it is activated.
        /// </summary>
        public List<BackgroundMusic> BackgroundMusic { get { return backgroundMusic; } }

        private bool IsActive { get { return this == AssaultWing.Instance.DataEngine.Arena; } }

        #endregion // Arena properties

        static Arena()
        {
            collisionAreaCellSize = new float[CollisionArea.COLLISION_AREA_TYPE_COUNT];
            for (int i = 0; i < CollisionArea.COLLISION_AREA_TYPE_COUNT; ++i)
                collisionAreaCellSize[i] = -1;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.Receptor)] = float.MaxValue;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.Force)] = float.MaxValue;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.WallBounds)] = 500;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalShip)] = 100;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalShot)] = 100;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalWall)] = 20;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalWater)] = 100;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalGas)] = 100;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalOtherUndamageableUnmovable)] = 100;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalOtherDamageableUnmovable)] = 100;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalOtherUndamageableMovable)] = 100;
            collisionAreaCellSize[AWMathHelper.LogTwo((int)CollisionAreaType.PhysicalOtherDamageableMovable)] = 100;
        }

        /// <summary>
        /// Creates an uninitialised arena.
        /// </summary>
        /// This constructor is only for serialisation.
        public Arena()
        {
            name = "dummyarena";
            dimensions = new Vector2(4000, 4000);
            layers = new List<ArenaLayer>();
            layers.Add(new ArenaLayer());
            Gobs = new GobCollection(layers);
            backgroundMusic = new List<BackgroundMusic>();
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
            for (int i = 0; i < collisionAreas.Length; ++i)
                collisionAreas[i] = null;
            UnloadContent();
            Gobs.Clear();
        }

        /// <summary>
        /// Resets the arena for a new play session.
        /// </summary>
        public void Reset()
        {
            collisionAreas = new SpatialGrid<CollisionArea>[CollisionArea.COLLISION_AREA_TYPE_COUNT];
            collisionAreaMayCollide = new bool[CollisionArea.COLLISION_AREA_TYPE_COUNT];
            Vector2 areaExcess = new Vector2(wallTriangleArenaExcess);
            Vector2 arrayDimensions = Dimensions + 2 * areaExcess;
            for (int i = 0; i < collisionAreas.Length; ++i)
                if (collisionAreaCellSize[i] >= 0)
                    collisionAreas[i] = new SpatialGrid<CollisionArea>(collisionAreaCellSize[i],
                        -areaExcess, arrayDimensions - areaExcess);
            collisionAreaMayCollide.Initialize();
        }

        /// <summary>
        /// Moves the given gob and performs physical collisions in order to
        /// maintain overlap consistency as specified in <b>CollisionArea.CannotOverlap</b>
        /// of the moving gob's physical collision area.
        /// </summary>
        /// <param name="gob">The gob to move.</param>
        public void Move(Gob gob)
        {
            if (!gob.Movable) return;
            if (gob.Disabled) return;
            CollisionArea gobPhysical = gob.PhysicalArea;
            UnregisterPhysical(gob);

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

            float moveLeft = 1; // interpolation coefficient; between 0 and 1
            int moveTries = 0;
            while (moveLeft > movementAccuracy && moveTries < moveTryMaximum)
            {
                Vector2 oldMove = gob.Move;
                moveLeft = TryMove(gob, moveLeft);
                ++moveTries;

                // If we just have to wait for another gob to move out of the way,
                // there's nothing more we can do.
                if (gob.Move == oldMove)
                    break;
            }
            RegisterPhysical(gob);
            ArenaBoundaryActions(gob);
        }

        /// <summary>
        /// Performs nonphysical collisions. Must be called every frame
        /// after all gob movement is done.
        /// </summary>
        public void PerformNonphysicalCollisions()
        {
            for (int bitIndex = 0; bitIndex < collisionAreas.Length; ++bitIndex)
            {
                var container = collisionAreas[bitIndex];
                if (container != null && collisionAreaMayCollide[bitIndex])
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
        public void Prepare(Gob gob) // TODO !!! This method should be private
        {
            gob.Arena = this;
            gob.Activate();
            if (gob.Layer == Gobs.GameplayLayer)
                Register(gob);
            else
            {
                // Gobs outside the gameplay layer cannot collide.
                // To achieve this, we take away all the gob's collision areas.
                gob.ClearCollisionAreas();
            }
        }

        /// <summary>
        /// Registers a gob for collisions.
        /// </summary>
        public void Register(Gob gob) // TODO !!! This method should be private
        {
            foreach (CollisionArea area in gob.CollisionAreas)
            {
                int bitIndex = AWMathHelper.LogTwo((int)area.Type);
                area.CollisionData = collisionAreas[bitIndex].Add(area, area.Area.BoundingBox);
                if (area.CollidesAgainst != CollisionAreaType.None)
                    collisionAreaMayCollide[bitIndex] = true;
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
            collisionAreas[AWMathHelper.LogTwo((int)area.Type)].Remove(element);
            area.CollisionData = null;
        }

        /// <summary>
        /// Returns a position in an area of the game world 
        /// where a gob is overlap consistent (e.g. not inside a wall).
        /// </summary>
        /// <param name="gob">The gob to position.</param>
        /// <param name="area">The area where to look for a position.</param>
        /// <returns>A position in the area where the gob is overlap consistent.</returns>
        public Vector2 GetFreePosition(Gob gob, IGeomPrimitive area)
        {
            // Iterate in the area for a while, looking for a free position.
            // Ultimately give up and return something that may be bad.
            for (int attempt = 1; attempt < freePosMaxAttempts; ++attempt)
            {
                Vector2 tryPos = Geometry.GetRandomLocation(area);
                if (IsFreePosition(gob, tryPos))
                    return tryPos;
            }
            return Geometry.GetRandomLocation(area);
        }

        /// <summary>
        /// Is a gob overlap consistent (e.g. not inside a wall) at a position. 
        /// </summary>
        /// <param name="gob">The gob.</param>
        /// <param name="position">The position.</param>
        /// <returns><b>true</b> iff the gob is overlap consistent at the position.</returns>
        public bool IsFreePosition(Gob gob, Vector2 position)
        {
            Vector2 oldPos = gob.Pos;
            gob.Pos = position;
            CollisionArea gobPhysical = gob.PhysicalArea;
            bool result = ArenaBoundaryLegal(gob) && !ForEachOverlapper(gobPhysical, gobPhysical.CannotOverlap, null);
            gob.Pos = oldPos;
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
            collisionAreas[wallBoundsIndex].ForEachElement(boundingBox, area =>
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
            //effect.TextureEnabled = true;
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
            effect.FogEnabled = fogEnabled;
            effect.FogEnd = fogEnd;
            effect.FogStart = fogStart;
            effect.LightingEnabled = true;
        }

        #endregion Public methods

        #region Collision and moving methods

        /// <summary>
        /// Registers a gob's physical collision area for collisions.
        /// </summary>
        /// <see cref="Register(Gob)"/>
        private void RegisterPhysical(Gob gob)
        {
            CollisionArea area = gob.PhysicalArea;
            if (area == null) return;
            area.CollisionData = collisionAreas[AWMathHelper.LogTwo((int)area.Type)].Add(
                area, area.Area.BoundingBox);
        }

        /// <summary>
        /// Removes a previously registered gob's physical collision area
        /// from the register.
        /// </summary>
        /// <see cref="Unregister(Gob)"/>
        private void UnregisterPhysical(Gob gob)
        {
            CollisionArea area = gob.PhysicalArea;
            if (area == null) return;
            SpatialGridElement<CollisionArea> element = (SpatialGridElement<CollisionArea>)area.CollisionData;
            collisionAreas[AWMathHelper.LogTwo((int)area.Type)].Remove(element);
            area.CollisionData = null;
        }

        /// <summary>
        /// Tries to move a gob, stopping it at the first physical collision 
        /// and performing the physical collision.
        /// </summary>
        /// <param name="gob">The gob to move.</param>
        /// <param name="moveLeft">How much of the gob's movement is left this frame.
        /// <b>0</b> means the gob has completed moving;
        /// <b>1</b> means the gob has not moved yet.</param>
        /// <returns>How much of the gob's movement is still left,
        /// between <b>0</b> and <b>1</b>.</returns>
        private float TryMove(Gob gob, float moveLeft)
        {
            Vector2 oldPos = gob.Pos;
            Vector2 goalPos = gob.Pos + gob.Move * (float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            float moveGood = 0; // last known safe position
            float moveBad = moveLeft; // last known unsafe position, if 'badFound'
            bool badFound = false;
            CollisionArea gobPhysical = gob.PhysicalArea;
            bool badDueToOverlappers = false;

            // Find out last non-collision position and first colliding position, 
            // up to required accuracy.
            float moveTry = moveLeft;
            while (moveBad - moveGood > collisionAccuracy)
            {
                gob.Pos = Vector2.Lerp(oldPos, goalPos, moveTry);
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
                moveTry = MathHelper.Lerp(moveGood, moveBad, 0.5f);
            }

            // Perform physical collisions.
            if (badFound)
            {
                gob.Pos = Vector2.Lerp(oldPos, goalPos, moveBad);
                if (badDueToOverlappers)
                    ForEachOverlapper(gobPhysical, gobPhysical.CannotOverlap, delegate(CollisionArea area2)
                    {
                        gob.Collide(gobPhysical, area2, false);
                        area2.Owner.Collide(area2, gobPhysical, false);
                        PerformCollision(gobPhysical, area2);
                        return gob.Dead;
                    });
                ArenaBoundaryActions(gob);
            }

            // Return to last non-colliding position.
            gob.Pos = Vector2.Lerp(oldPos, goalPos, moveGood);
            return moveLeft - moveGood;
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
            for (int typeBit = 0; typeBit < collisionAreas.Length; ++typeBit)
            {
                if (((1 << typeBit) & (int)types) == 0) continue;
                collisionAreas[typeBit].ForEachElement(boundingBox, delegate(CollisionArea area2)
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
        public void PerformCollision(CollisionArea area1, CollisionArea area2)
        {
#if DEBUG_PROFILE
            ++AssaultWing.Instance.collisionCount;
#endif
            // At least one area must be from a movable gob, lest there be no collision.
            bool area1Movable = (area1.Type & CollisionAreaType.PhysicalMovable) != 0;
            bool area2Movable = (area2.Type & CollisionAreaType.PhysicalMovable) != 0;
            if (!area1Movable) PerformCollisionMovableUnmovable(area2, area1);
            else if (!area2Movable) PerformCollisionMovableUnmovable(area1, area2);
            else PerformCollisionMovableMovable(area1, area2);
        }

        /// <summary>
        /// Performs a physical collision between a movable gob and an unmovable gob.
        /// </summary>
        /// <param name="movableArea">The overlapping collision area of the movable gob.</param>
        /// <param name="unmovableArea">The overlapping collision area of the unmovable gob.</param>
        private void PerformCollisionMovableUnmovable(CollisionArea movableArea, CollisionArea unmovableArea)
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
                float elasticity = Math.Min(1, movableGob.Elasticity * unmovableGob.Elasticity);
                float friction = movableGob.Friction * unmovableGob.Friction;
                Vector2 move1afterElastic = new Vector2(-move1.X, move1.Y);
                // move1afterInelastic = Vector2.Zero
                Vector2 move1after;
                move1after.X = MathHelper.Lerp(0, move1afterElastic.X, elasticity);
                move1after.Y = AWMathHelper.InterpolateTowards(move1afterElastic.Y, 0, friction * (move1after.X - move1.X));
                movableGob.Move = xUnit * move1after.X + yUnit * move1after.Y;
                Vector2 move1Delta = move1 - move1after;

                // Inflict damage to damageable gobs.
                if ((movableArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
                {
                    if (move1Delta.Length() > minimumCollisionDelta)
                        movableGob.InflictDamage(CollisionDamage(movableGob, move1Delta),
                            new DeathCause(movableGob, DeathCauseType.Collision, unmovableGob));
                }
                /* TODO: What if the unmovable gob wants to be damaged, too?
                if ((unmovableArea.Type2 & CollisionAreaType.PhysicalDamageable) != 0)
                {
                    Vector2 move2Delta = Vector2.Zero;
                    if (move1Delta.Length() > minimumCollisionDelta)
                        unmovableGob.InflictDamage(CollisionDamage(unmovableGob, move2Delta));
                }
                */

                // Play a sound.
                if (move1Delta.Length() > minimumCollisionDelta)
                {
                    EventEngine eventEngine = (EventEngine)AssaultWing.Instance.Services.GetService(typeof(EventEngine));
                    SoundEffectEvent soundEvent = new SoundEffectEvent();
                    soundEvent.setAction(AW2.Sound.SoundOptions.Action.Collision);
                    eventEngine.SendEvent(soundEvent);
                }
            }
        }

        /// <summary>
        /// Performs a physical collision between two movable gobs.
        /// </summary>
        /// <param name="movableArea1">The overlapping collision area of one movable gob.</param>
        /// <param name="movableArea2">The overlapping collision area of the other movable gob.</param>
        private void PerformCollisionMovableMovable(CollisionArea movableArea1, CollisionArea movableArea2)
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
                float elasticity = Math.Min(1, gob1.Elasticity * gob2.Elasticity);
                float friction = gob1.Friction * gob2.Friction;
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
                gob2.Move = xUnit * move2after.X + yUnit * move2after.Y + gob2.Move;
                Vector2 move1Delta = move1 - move1after;
                // move2Delta = move2after because collision is calculated from the 2nd gob's point of view (2nd gob is still)

                /*We want to deal damage in physics engine because calculations only happen once per collision!*/
                // Inflict damage to damageable gobs.
                if ((movableArea1.Type & CollisionAreaType.PhysicalDamageable) != 0)
                {
                    if (move1Delta.Length() > minimumCollisionDelta)
                        gob1.InflictDamage(CollisionDamage(gob1, move1Delta),
                            new DeathCause(gob1, DeathCauseType.Collision, gob2));
                    if (move2after.Length() > minimumCollisionDelta)
                        gob2.InflictDamage(CollisionDamage(gob2, move2after),
                            new DeathCause(gob2, DeathCauseType.Collision, gob1));
                }

                // Play a sound only if actual collision happened!.
                if (move1Delta.Length() > minimumCollisionDelta || move2after.Length() > minimumCollisionDelta)
                {
                    EventEngine eventEngine = (EventEngine)AssaultWing.Instance.Services.GetService(typeof(EventEngine));
                    SoundEffectEvent soundEvent = new SoundEffectEvent();
                    soundEvent.setAction(AW2.Sound.SoundOptions.Action.Shipcollision);
                    eventEngine.SendEvent(soundEvent);
                }

            }
        }

        private float CollisionDamage(Gob gob, Vector2 moveDelta)
        {
            return moveDelta.Length() / 2 * gob.Mass * collisionDamageDownGrade;
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
            if (gob.Pos.X < -arenaOuterBoundaryThickness) result |= OutOfArenaBounds.OuterLeft;
            if (gob.Pos.Y < -arenaOuterBoundaryThickness) result |= OutOfArenaBounds.OuterBottom;
            if (gob.Pos.X > Dimensions.X + arenaOuterBoundaryThickness) result |= OutOfArenaBounds.OuterRight;
            if (gob.Pos.Y > Dimensions.Y + arenaOuterBoundaryThickness) result |= OutOfArenaBounds.OuterTop;

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
        private void ArenaBoundaryActions(Gob gob)
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
            if ((outOfBounds & OutOfArenaBounds.OuterBoundary) != 0)
                Gobs.Remove(gob);
            return;
        }

        #endregion Arena boundary methods

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
                dimensions = Vector2.Max(dimensions, new Vector2(500));
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

                var oldLayers = layers;
                layers = new List<ArenaLayer>();
                foreach (var layer in oldLayers)
                {
                    layers.Add(layer.EmptyCopy());
                    foreach (var gob in layer.Gobs)
                        gob.Layer = layer;
                }
                Gobs = new GobCollection(layers);
                foreach (var gob in new GobCollection(oldLayers))
                    Gob.CreateGob(gob, gobb =>
                    {
                        gobb.Layer = layers[oldLayers.IndexOf(gob.Layer)];
                        Gobs.Add(gobb);
                    });

                // Find the gameplay layer.
                int gameplayLayerIndex = Layers.FindIndex(layer => layer.IsGameplayLayer);
                if (gameplayLayerIndex == -1)
                    throw new ArgumentException("Arena " + Name + " doesn't have a gameplay layer");
                Gobs.GameplayLayer = Layers[gameplayLayerIndex];

                // Make sure the gameplay backlayer is located right before the gameplay layer.
                // Use a suitable layer if one is defined in the arena, otherwise create a new one.
                if (gameplayLayerIndex == 0 || Layers[gameplayLayerIndex - 1].Z != 0)
                    Layers.Insert(gameplayLayerIndex, Gobs.GameplayBackLayer = new ArenaLayer(false, 0, ""));
                else
                    Gobs.GameplayBackLayer = Layers[gameplayLayerIndex - 1];
            }
        }

        #endregion
    }
}
