using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Events;
using AW2.Helpers;
using Edu.Psu.Cse.R_Tree_Framework.Indexes;
using Edu.Psu.Cse.R_Tree_Framework.Framework;

namespace AW2.Game
{
    /// <summary>
    /// A basic physics engine implementation.
    /// </summary>
    /// The physics engine performs collisions between gobs as follows.
    /// 
    /// Basic concepts:
    /// - <b>Overlap</b> of two gobs means that their collision primitives intersect.
    /// - <b>Collision</b> is the event of reacting to overlap of two gobs.
    /// - <b>Physical collision</b> is collision performed by physics engine based on
    /// which subinterfaces of IGob the gobs implement.
    /// - <b>Custom collisions</b> are collisions performed by Gob subclasses.
    /// 
    /// For a collision to occur between two gobs, at least the following conditions must hold:
    /// - the gobs must implement ICollidable or some ICollidable subinterfaces
    /// - the subinterfaces must allow collisions to happen (decided by physics engine)
    /// - the gobs must overlap
    /// 
    /// If these conditions are met, custom collisions are performed for the two gobs.
    /// In addition, if the following condition is met, also physical collision is
    /// performed for the two gobs:
    /// - the subinterfaces must disallow overlap (decided by physics engine)
    class PhysicsEngineImpl : PhysicsEngine
    {
        #region Type definitions

        /// <summary>
        /// Data about a collidable gob.
        /// </summary>
        [System.Diagnostics.DebuggerDisplay("ID:{recordID} box:(min:({minimumBoundingBox.minX},{minimumBoundingBox.minY}) max:({minimumBoundingBox.maxX},{minimumBoundingBox.maxY})) collisionArea:{collisionArea.name}")]
        private class CollisionData : Record
        {
            CollisionArea collisionArea;

            /// <summary>
            /// The collision area whose record this is.
            /// </summary>
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public CollisionArea CollisionArea { get { return collisionArea; } }

            /// <summary>
            /// Creates a collision data record for a collision area.
            /// </summary>
            /// <param name="recordID">A unique ID.</param>
            /// <param name="collisionArea">The collision area.</param>
            public CollisionData(int recordID, CollisionArea collisionArea)
                : base(recordID, new MinimumBoundingBox(
                    collisionArea.Area.BoundingBox.Min.X,
                    collisionArea.Area.BoundingBox.Min.Y, 
                    collisionArea.Area.BoundingBox.Max.X, 
                    collisionArea.Area.BoundingBox.Max.Y))
            {
                this.collisionArea = collisionArea;
            }
        }

        /// <summary>
        /// A triangle in the 3D model of a piece of wall in an arena.
        /// </summary>
        private struct WallTriangle : ICollisionArea
        {
            /// <summary>
            /// The wall instance where the triangle is from.
            /// </summary>
            public IHoleable wall;

            /// <summary>
            /// Index to <b>indexData</b> of the wall instance's 3D model
            /// where the triangle starts.
            /// </summary>
            public int triangleIndex;

            /// <summary>
            /// Creates a new wall triangle.
            /// </summary>
            /// <param name="wall">The wall the triangle belongs to.</param>
            /// <param name="triangleIndex">The starting index of the triangle in 
            /// the wall's 3D model's index data.</param>
            public WallTriangle(IHoleable wall, int triangleIndex)
            {
                this.wall = wall;
                this.triangleIndex = triangleIndex;
            }

            #region ICollisionArea Members

            /// <summary>
            /// Collision area name; either "General" for general collision
            /// checking (including physical collisions), or something else
            /// for a receptor area that can react to other gobs' general
            /// areas.
            /// </summary>
            public string Name { get { return "General"; } }

            /// <summary>
            /// The type of the collision area.
            /// </summary>
            public CollisionAreaType Type { get { return CollisionAreaType.Physical; } }

            /// <summary>
            /// The geometric area for overlap testing, in game world coordinates,
            /// translated according to the hosting gob's location.
            /// </summary>
            public IGeomPrimitive Area
            {
                get
                {
                    VertexPositionNormalTexture[] vertexData = wall.VertexData;
                    short[] indexData = wall.IndexData;
                    Vector3 v1 = vertexData[indexData[triangleIndex + 0]].Position;
                    Vector3 v2 = vertexData[indexData[triangleIndex + 1]].Position;
                    Vector3 v3 = vertexData[indexData[triangleIndex + 2]].Position;
                    Polygon triangle = new Polygon(new Vector2[] {
                            new Vector2(v1.X, v1.Y),
                            new Vector2(v2.X, v2.Y),
                            new Vector2(v3.X, v3.Y),
                        });
                    return triangle;
                }
            }

            /// <summary>
            /// The gob whose collision area this is.
            /// </summary>
            public ICollidable Owner { get { return wall as ICollidable; } }

            #endregion
        }

        /// <summary>
        /// Ways of being outside the arena boundaries.
        /// </summary>
        /// The arena boundary is the rectangle spanned by points (0, 0) and
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

        /// <summary>
        /// Flags that control search of physical overlappers.
        /// </summary>
        [Flags]
        private enum OverlapperFlags
        {
            /// <summary>
            /// Check overlap against gob's physical collision area.
            /// </summary>
            CheckPhysical = 0x0001,

            /// <summary>
            /// Check overlap against gob's receptor collision areas.
            /// </summary>
            CheckReceptor = 0x0002,

            /// <summary>
            /// Consider gob's inconsistent position as not overlapping anyone.
            /// </summary>
            ConsiderConsistency = 0x0010,

            /// <summary>
            /// Consider gob's coldness as not overlapping its own.
            /// </summary>
            ConsiderColdness = 0x0020,
        }

        #endregion Type definitions

        #region Constants

        /// <summary>
        /// Distance outside the arena boundaries that we still allow
        /// some gobs stay alive.
        /// </summary>
        float arenaOuterBoundaryThickness = 1000;

        /// <summary>
        /// Accuracy of finding the point of collision. Measured in time,
        /// with one frame as the unit.
        /// </summary>
        /// Exactly, when a collision occurs, the moving gob's point of collision
        /// will be no more than <b>collisionAccuracy</b> of a frame's time
        /// away from the actual point of collision.
        /// <b>1</b> means no iteration;
        /// <b>0</b> means iteration to infinite precision.
        float collisionAccuracy = 0.6f;

        /// <summary>
        /// Accuracy to which movement of a gob in a frame is done. 
        /// Measured in time, with one frame as the unit.
        /// </summary>
        /// This constant is to work around rounding errors.
        float movementAccuracy = 0.001f;

        /// <summary>
        /// The maximum number of times to try to move a gob in one frame.
        /// Each movement attempt ends either in success or a collision.
        /// </summary>
        /// Higher number means more accurate and responsive collisions
        /// but requires more CPU power in complex situations.
        /// Low numbers may result in "lazy" collisions.
        int moveTryMaximum = 2;

        /// <summary>
        /// Radius at which to start looking for a free position in the game world,
        /// measured in meters.
        /// </summary>
        float freePosRadiusMin = 50;

        /// <summary>
        /// Radius at which to stop looking for a free position in the game world,
        /// measured in meters.
        /// </summary>
        float freePosRadiusMax = 500;

        /// <summary>
        /// How much to increase the free position search radius after each failed
        /// attempt, measured in meters.
        /// </summary>
        float freePosRadiusStep = 10;

        /// <summary>
        /// Loud collision sound variable
        /// attempt, measured in meters.
        /// </summary>
        double collisionDamageDownGrade = 0.0006;

        /// <summary>
        /// Loud collision sound variable
        /// attempt, measured in meters.
        /// </summary>
        float loudCollisionSound = 100f;

        /// <summary>
        /// Minimum move delta
        /// For collision damage, sound effects
        /// </summary>
        float minimumCollisionDelta = 20f;

        /// <summary>
        /// Maximum length of an edge of a triangle in a wall's 3D model.
        /// </summary>
        float wallTriangleMaxSize = 15;

        /// <summary>
        /// Excess area to cover by the spatial index of wall triangles,
        /// in addition to arena boundaries.
        /// </summary>
        float wallTriangleArenaExcess = 200;

        #endregion Constants

        #region Fields

        /// <summary>
        /// The number of seconds the current frame represents.
        /// </summary>
        GameTime gameTime;

        /// <summary>
        /// The spatial index of physical collision areas collidable gobs.
        /// </summary>
        /// Receptors are kept in their own list as they don't
        /// collide with each other.
        R_Tree collidables;

        /// <summary>
        /// The next index to use for a new record in <b>collidables</b>.
        /// </summary>
        int unusedCollidablesIndex;

        /// <summary>
        /// Collision areas that are receptors, as opposed to physical collision areas.
        /// </summary>
        /// Receptors are not in the spatial index of physical collision areas
        /// because they don't collide with each other, and thus no-one
        /// needs to query for collision with a receptor.
        LinkedList<CollisionData> receptors;

        /// <summary>
        /// Collision areas that provide a force, as opposed to physical collision areas
        /// and receptors.
        /// </summary>
        /// Forces apply to gobs based on their location, not by their collision areas.
        /// That is why they are stored in a separate container.
        LinkedList<CollisionData> forces;

        /// <summary>
        /// Triangles of walls.
        /// </summary>
        /// Walls have special physical collision areas; they consist of
        /// triangles and they never move. Wall triangles can be removed, though.
        SpatialGrid<WallTriangle> wallTriangles;

        #endregion Fields

        /// <summary>
        /// Creates a new physics engine.
        /// </summary>
        public PhysicsEngineImpl()
        {
            this.collidables = new R_Tree();
            this.receptors = new LinkedList<CollisionData>();
            this.forces = new LinkedList<CollisionData>();
            this.wallTriangles = null;
            this.unusedCollidablesIndex = 0;
            this.gameTime = new GameTime();
        }

        #region PhysicsEngine Members

        /// <summary>
        /// Game timing information for the current frame.
        /// </summary>
        /// This is meant to be set by LogicEngine at the beginning of each frame
        /// and can be used all over game logic.
        public GameTime TimeStep { get { return gameTime; } set { gameTime = value; } }

        /// <summary>
        /// Resets the physics engine for a new arena.
        /// </summary>
        public void Reset()
        {
            gameTime = new GameTime();
            collidables = new R_Tree();
            receptors.Clear();
            forces.Clear();
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Vector2 areaExcess = new Vector2(wallTriangleArenaExcess);
            wallTriangles = new SpatialGrid<WallTriangle>(wallTriangleMaxSize,
                -areaExcess, data.Arena.Dimensions + areaExcess);
        }

        /// <summary>
        /// Applies drag to a gob. Drag is a force that manipulates the gob's
        /// movement closer to that of the medium. Drag constant measures the
        /// amount of this manipulation, 0 meaning no drag and 1 meaning
        /// absolute drag where the gob cannot escape the flow of the medium.
        /// Practical values are very small, under 0.1.
        /// </summary>
        /// Drag is the force that resists movement in a medium.
        /// <param name="gob">The gob to apply drag to.</param>
        /// <param name="flow">Direction and speed of flow of medium
        /// at the gob's location.</param>
        /// <param name="drag">Drag constant for the medium and the gob.</param>
        public void ApplyDrag(Gob gob, Vector2 flow, float drag)
        {
            gob.Move = (1 - drag) * (gob.Move - flow) + flow;
        }

        /// <summary>
        /// Moves the given gob and takes care of collisions.
        /// </summary>
        /// <param name="gob">The gob to move.</param>
        public void Move(Gob gob)
        {
            if ((gob.PhysicsApplyMode & PhysicsApplyMode.Move) != 0)
            {
                if (!gob.Disabled)
                    MoveAndCollidePhysical(gob);
            }
            else
            {
                // The gob doesn't move, so its position never needs to be corrected.
                gob.HadSafePosition = true;
            }
        }

        /// <summary>
        /// Performs additional collision checks. Must be called every frame
        /// after all gob movement is done.
        /// </summary>
        public void MovesDone()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

            // Perform receptor collisions against physical collision areas.
            foreach (CollisionData collData in receptors)
            {
                CollisionArea collArea = collData.CollisionArea;
                List<KeyValuePair<ICollisionArea, List<ICollisionArea>>> potentials = new List<KeyValuePair<ICollisionArea,List<ICollisionArea>>>();
                potentials.Add(new KeyValuePair<ICollisionArea,List<ICollisionArea>>(collArea, GetPotentialPhysicalOverlappers(collArea)));
                OverlapperFlags flags = OverlapperFlags.ConsiderConsistency | OverlapperFlags.ConsiderColdness;
                List<ICollisionArea> compromisers = ReducePotentialPhysicalOverlappers(potentials, flags);
                foreach (ICollisionArea collArea2 in compromisers)
                {
                    collArea.Owner.Collide(collArea2.Owner, collArea.Name);
                    if (collArea.Owner is Gob &&
                        (((Gob)collArea.Owner).PhysicsApplyMode & PhysicsApplyMode.ReceptorCollidesPhysically) != 0)
                    {
                        PerformCollision(collArea.Owner, collArea2.Owner);
                    }
                }
            }

            // Perform force collisions against gob locations.
            foreach (CollisionData collData in forces)
            {
                CollisionArea collArea = collData.CollisionArea;
                data.ForEachGob<ISolid>(delegate(Gob gob2)
                {
                    if (!gob2.Disabled &&
                        Geometry.Intersect(collArea.Area, new Helpers.Point(gob2.Pos)))
                        collArea.Owner.Collide((ICollidable)gob2, collArea.Name);
                });
            }
        }

        /// <summary>
        /// Registers a gob for collisions.
        /// </summary>
        /// <param name="gob">The gob.</param>
        public void Register(Gob gob)
        {
            // Non-collidable gobs need not be registered.
            ICollidable gobCollidable = gob as ICollidable;
            if (gobCollidable == null) return;

            // Walls get their collisions by their 3D model triangles.
            IHoleable gobHoleable = gob as IHoleable;
            if (gobHoleable != null)
            {
                for (int i = 0; i + 2 < gobHoleable.IndexData.Length; i += 3)
                {
                    Vector2 min, max;
                    AWMathHelper.MinAndMax(
                        gobHoleable.VertexData[gobHoleable.IndexData[i + 0]].Position,
                        gobHoleable.VertexData[gobHoleable.IndexData[i + 1]].Position,
                        gobHoleable.VertexData[gobHoleable.IndexData[i + 2]].Position,
                        out min, out max);
                    object handle = wallTriangles.Add(new WallTriangle(gobHoleable, i), min, max);
                    gobHoleable.WallTriangleHandles[i / 3] = handle;
                }
                return;
            }

            // Other gobs provide their own collision areas.
            CollisionArea[] collisionAreas = gobCollidable.GetPrimitives();
            for (int i = 0; i < collisionAreas.Length; ++i)
            {
                if (collisionAreas[i].CollisionData != null) continue;
                CollisionData collData = new CollisionData(unusedCollidablesIndex++, collisionAreas[i]);
                collisionAreas[i].CollisionData = collData;
                switch (collisionAreas[i].Type)
                {
                    case CollisionAreaType.Physical:
                        collidables.Insert(collData);
                        break;
                    case CollisionAreaType.Receptor:
                        receptors.AddLast(collData);
                        break;
                    case CollisionAreaType.Force:
                        forces.AddLast(collData);
                        break;
                }
            }
        }

        /// <summary>
        /// Removes a previously registered gob from the register.
        /// </summary>
        /// <param name="gob">The gob.</param>
        public void Unregister(Gob gob)
        {
            ICollidable gobCollidable = gob as ICollidable;
            if (gobCollidable == null) return;
            CollisionArea[] collisionAreas = gobCollidable.GetPrimitives();
            for (int i = 0; i < collisionAreas.Length; ++i)
            {
                if (collisionAreas[i].CollisionData == null) continue;
                CollisionData collData = (CollisionData)collisionAreas[i].CollisionData;
                switch (collisionAreas[i].Type)
                {
                    case CollisionAreaType.Physical:
                        collidables.Delete(collData);
                        break;
                    case CollisionAreaType.Receptor:
                        receptors.Remove(collData);
                        break;
                    case CollisionAreaType.Force:
                        forces.Remove(collData);
                        break;
                }
                collisionAreas[i].CollisionData = null;
            }
        }

        /// <summary>
        /// Applies the given force to the given gob.
        /// </summary>
        /// Note that the larger the mass of the gob is, the more force is needed to give it
        /// a good push.
        /// <param name="gob">The gob to apply the force to.</param>
        /// <param name="force">The force to apply, measured in Newtons.</param>
        public void ApplyForce(Gob gob, Vector2 force)
        {
            gob.Move += force / gob.Mass * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        /// <summary>
        /// Applies the given force to a gob, preventing gob speed from
        /// growing beyond a limit.
        /// </summary>
        /// Note that the larger the mass of the gob is, the more force is needed to give it
        /// a good push. Although the gob's speed cannot grow beyond <b>maxSpeed</b>,
        /// it can still maintain its value even if it's larger than <b>maxSpeed</b>.
        /// <param name="gob">The gob to apply the force to.</param>
        /// <param name="force">The force to apply, measured in Newtons.</param>
        /// <param name="maxSpeed">The speed limit beyond which the gob's speed cannot grow.</param>
        public void ApplyLimitedForce(Gob gob, Vector2 force, float maxSpeed)
        {
            float oldSpeed = gob.Move.Length();
            gob.Move += force / gob.Mass * (float)gameTime.ElapsedGameTime.TotalSeconds;
            float speed = gob.Move.Length();
            float speedLimit = MathHelper.Max(maxSpeed, oldSpeed);
            if (speed > speedLimit)
                gob.Move *= speedLimit/speed;
        }

        /// <summary>
        /// Applies the given momentum to the given gob.
        /// </summary>
        /// Note that the larger the mass of the gob is, the more momentum is needed to give it
        /// a good push.
        /// <param name="gob">The gob to apply the momentum to.</param>
        /// <param name="momentum">The momentum to apply, measured in Newton seconds.</param>
        public void ApplyMomentum(Gob gob, Vector2 momentum)
        {
            gob.Move += momentum / gob.Mass;
        }

        /// <summary>
        /// Returns the scalar amount that represents how much the given scalar change speed
        /// affects during the current frame.
        /// </summary>
        /// <param name="changePerSecond">The speed of change per second.</param>
        /// <returns>The amount of change during the current frame.</returns>
        public float ApplyChange(float changePerSecond)
        {
            return changePerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        /// <summary>
        /// Returns a position, near a preferred position, in the game world 
        /// where a gob is overlap consistent (e.g. not inside a wall).
        /// </summary>
        /// <param name="gob">The gob to position.</param>
        /// <param name="preferred">Preferred position, or <b>null</b>.</param>
        /// <returns>A position for the gob where it is overlap consistent.</returns>
        public Vector2 GetFreePosition(Gob gob, Vector2? preferred)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

            // Randomise a location if none is preferred.
            if (preferred == null)
                preferred = RandomHelper.GetRandomVector2(Vector2.Zero, data.Arena.Dimensions);

            // Non-collidable gobs are fine anywhere.
            ICollidable gobCollidable = gob as ICollidable;
            if (gobCollidable == null)
                return preferred.Value;

            // Iterate around the preferred location, steadily loosening the requirements.
            // Ultimately we give up and return something that we know is bad.
            Vector2 oldPos = gob.Pos;
            Vector2 tryPos = preferred.Value;
            for (float radius = freePosRadiusMin; radius < freePosRadiusMax; radius += freePosRadiusStep)
            {
                tryPos = RandomHelper.GetRandomVector2(preferred.Value, radius);
                gob.Pos = tryPos;
                OverlapperFlags flags = OverlapperFlags.CheckPhysical | OverlapperFlags.CheckReceptor;
                if (ArenaBoundaryLegal(gob) && GetPhysicalOverlappers(gobCollidable, flags).Count == 0)
                    break;
            }
            gob.Pos = oldPos;
            return tryPos;
        }

        /// <summary>
        /// Is a gob overlap consistent (e.g. not inside a wall) at a position. 
        /// </summary>
        /// <param name="gob">The gob.</param>
        /// <param name="position">The position.</param>
        /// <returns><b>true</b> iff the gob is overlap consistent at the position.</returns>
        public bool IsFreePosition(Gob gob, Vector2 position)
        {
            // Non-collidable gobs are fine anywhere.
            ICollidable gobCollidable = gob as ICollidable;
            if (gobCollidable == null)
                return true;

            Vector2 oldPos = gob.Pos;
            OverlapperFlags flags = OverlapperFlags.CheckPhysical | OverlapperFlags.ConsiderColdness;
            bool result = ArenaBoundaryLegal(gob) && GetPhysicalOverlappers(gobCollidable, flags).Count == 0;
            gob.Pos = oldPos;
            return result;
        }

        /// <summary>
        /// Removes a triangle from a wall that has been registered for collisions.
        /// </summary>
        /// <param name="wallTriangleHandle">A handle to the wall triangle to remove.</param>
        public void RemoveWallTriangle(object wallTriangleHandle)
        {
            SpatialGridElement<WallTriangle> element = wallTriangleHandle as SpatialGridElement<WallTriangle>;
            if (element == null)
                throw new ArgumentException("Cannot remove a non-SpatialGridElement<WallTriangle> instance");
            wallTriangles.Remove(element);
        }

        #endregion // PhysicsEngine Members

        #region Arena boundary methods

        /// <summary>
        /// Returns the status of the gob's location with respect to arena boundary.
        /// </summary>
        /// <param name="gob">The gob.</param>
        /// <returns>How is the gob out of the arena boundary, or is it inside.</returns>
        private OutOfArenaBounds IsOutOfArenaBounds(Gob gob)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            OutOfArenaBounds result = OutOfArenaBounds.None;

            // The arena boundary.
            if (gob.Pos.X < 0) result |= OutOfArenaBounds.Left;
            if (gob.Pos.Y < 0) result |= OutOfArenaBounds.Bottom;
            if (gob.Pos.X > data.Arena.Dimensions.X) result |= OutOfArenaBounds.Right;
            if (gob.Pos.Y > data.Arena.Dimensions.Y) result |= OutOfArenaBounds.Top;

            // The outer arena boundary.
            if (gob.Pos.X < -arenaOuterBoundaryThickness) result |= OutOfArenaBounds.OuterLeft;
            if (gob.Pos.Y < -arenaOuterBoundaryThickness) result |= OutOfArenaBounds.OuterBottom;
            if (gob.Pos.X > data.Arena.Dimensions.X + arenaOuterBoundaryThickness) result |= OutOfArenaBounds.OuterRight;
            if (gob.Pos.Y > data.Arena.Dimensions.Y + arenaOuterBoundaryThickness) result |= OutOfArenaBounds.OuterTop;

            return result;
        }

        /// <summary>
        /// Returns true iff the gob is positioned legally in respect of 
        /// the arena boundaries. Think of this as physical overlap
        /// consistency but against arena boundaries instead of other gobs.
        /// </summary>
        /// <param name="gob">The gob to check.</param>
        /// <returns><b>true</b> iff the gob is positioned legally in respect of 
        /// the arena boundaries.</returns>
        private bool ArenaBoundaryLegal(Gob gob)
        {
            // Ships must stay inside arena boundaries.
            if (gob is Gobs.Ship)
            {
                OutOfArenaBounds outOfBounds = IsOutOfArenaBounds(gob);
                return outOfBounds == OutOfArenaBounds.None;
            }
            
            // Other gobs can be anywhere.
            return true;
        }

        /// <summary>
        /// Performs necessary actions on the gob in respect of its position
        /// and the arena boundaries. Think of this as physical collision
        /// but against arena boundaries instead of other gobs.
        /// </summary>
        /// <param name="gob">The gob.</param>
        private void ArenaBoundaryActions(Gob gob)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            OutOfArenaBounds outOfBounds = IsOutOfArenaBounds(gob);

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
                data.RemoveGob(gob);
            return;
        }

        #endregion Arena boundary methods

        #region Collision checking methods

        /// <summary>
        /// Moves a gob for one frame and performs physical collisions when necessary.
        /// </summary>
        /// <param name="gob">The gob to move and collide physically.</param>
        private void MoveAndCollidePhysical(Gob gob)
        {
            UnregisterPhysical(gob);

            // Find out original consistency compromisers.
            List<ICollisionArea> originalCompromisers = null;
            if (!gob.HadSafePosition && gob is ICollidable)
            {
                OverlapperFlags flags = OverlapperFlags.CheckPhysical;
                originalCompromisers = GetPhysicalOverlappers((ICollidable)gob, flags);
            }

            float moveLeft = 1; // interpolation coefficient; between 0 and 1
            int moveTries = 0;
            while (moveLeft > movementAccuracy && moveTries < moveTryMaximum)
            {
                Vector2 oldMove = gob.Move;
                moveLeft = TryMove(gob, moveLeft, originalCompromisers);
                ++moveTries;

                // If we just have to wait for another gob to move out of the way,
                // there's nothing more we can do.
                if (gob.Move == oldMove)
                    break;
            }
            RegisterPhysical(gob);

            ArenaBoundaryActions(gob);

            if (gob.HadSafePosition)
            {
                // Possibly demote to having an unsafe position.
                OverlapperFlags flags = OverlapperFlags.CheckPhysical |
                    OverlapperFlags.ConsiderConsistency;
                if (gob.Cold && !OverlapConsistent(gob, flags))
                    gob.HadSafePosition = false;
            }
            else
            {
                // Possibly promote to having a safe position.
                OverlapperFlags flags = OverlapperFlags.CheckPhysical;
                if (OverlapConsistent(gob, flags))
                    gob.HadSafePosition = true;
            }
        }

        /// <summary>
        /// Registers a gob's physical collision area for collisions.
        /// </summary>
        /// <see cref="Register(Gob)"/>
        /// <param name="gob">The gob.</param>
        private void RegisterPhysical(Gob gob)
        {
            ICollidable gobCollidable = gob as ICollidable;
            if (gobCollidable == null) return;
            CollisionArea[] collisionAreas = gobCollidable.GetPrimitives();
            for (int i = 0; i < collisionAreas.Length; ++i)
            {
                if (collisionAreas[i].CollisionData != null) continue;
                if (collisionAreas[i].Type != CollisionAreaType.Physical) continue;
                CollisionData collData = new CollisionData(unusedCollidablesIndex++, collisionAreas[i]);
                collisionAreas[i].CollisionData = collData;
                collidables.Insert(collData);
            }
        }

        /// <summary>
        /// Removes a previously registered gob's physical collision area
        /// from the register.
        /// </summary>
        /// <see cref="Unregister(Gob)"/>
        /// <param name="gob">The gob.</param>
        private void UnregisterPhysical(Gob gob)
        {
            ICollidable gobCollidable = gob as ICollidable;
            if (gobCollidable == null) return;
            CollisionArea[] collisionAreas = gobCollidable.GetPrimitives();
            for (int i = 0; i < collisionAreas.Length; ++i)
            {
                if (collisionAreas[i].CollisionData == null) continue;
                if (collisionAreas[i].Type != CollisionAreaType.Physical) continue;
                CollisionData collData = (CollisionData)collisionAreas[i].CollisionData;
                collidables.Delete(collData);
                collisionAreas[i].CollisionData = null;
            }
        }

        /// <summary>
        /// Tries to move a gob, stopping it at the first physical collision 
        /// and performing the physical collision.
        /// </summary>
        /// <param name="gob">The gob to move.</param>
        /// <param name="moveLeft">How much of the gob's movement is left this frame.
        /// <b>0</b> means the gob has completed moving;
        /// <b>1</b> means the gob has not moved yet.</param>
        /// <param name="originalCompromisers">The list of physical overlap consistency
        /// compromisers of the gob. Only used if the gob is not physical overlap consistent.</param>
        /// <returns>How much of the gob's movement is still left,
        /// between <b>0</b> and <b>1</b>.</returns>
        private float TryMove(Gob gob, float moveLeft, List<ICollisionArea> originalCompromisers)
        {
            Vector2 oldPos = gob.Pos;
            Vector2 goalPos = gob.Pos + gob.Move * (float)gameTime.ElapsedGameTime.TotalSeconds;
            float moveGood = 0; // last known safe position
            float moveBad = moveLeft; // last known unsafe position, if 'badFound'
            bool badFound = false;
            List<ICollisionArea> badCompromisers = null; // relevant compromisers at 'moveBad'

            // Find out last non-collision position and first colliding position, 
            // up to required accuracy.
            float moveTry = moveLeft;
            while (moveBad - moveGood > collisionAccuracy)
            {
                gob.Pos = Vector2.Lerp(oldPos, goalPos, moveTry);
                List<ICollisionArea> newCompromisers = GetNewCompromisers(gob, originalCompromisers);
                if (ArenaBoundaryLegal(gob) && newCompromisers.Count == 0)
                    moveGood = moveTry;
                else
                {
                    badCompromisers = newCompromisers;
                    moveBad = moveTry;
                    badFound = true;
                }
                moveTry = MathHelper.Lerp(moveGood, moveBad, 0.5f);
            }

            // Perform physical collisions.
            if (badFound)
            {
                gob.Pos = Vector2.Lerp(oldPos, goalPos, moveBad);
                if (badCompromisers != null && badCompromisers.Count > 0)
                    CollidePhysical(gob, badCompromisers);
                ArenaBoundaryActions(gob);
            }

            // Return to last non-colliding position.
            gob.Pos = Vector2.Lerp(oldPos, goalPos, moveGood);
            return moveLeft - moveGood;
        }

        /// <summary>
        /// Helper for TryMove.
        /// Returns, for a gob, those physical consistency compromisers that
        /// didn't compromise the gob before its movement.
        /// </summary>
        /// <param name="gob">The gob.</param>
        /// <param name="originalCompromisers">The list of physical overlap consistency
        /// compromisers of the gob before movement. Only used if the gob is 
        /// not physical overlap consistent.</param>
        /// <returns>The gob's new physical overlap consistency compromisers.</returns>
        private List<ICollisionArea> GetNewCompromisers(Gob gob, List<ICollisionArea> originalCompromisers)
        {
            ICollidable gobCollidable = gob as ICollidable;
            if (gobCollidable == null)
                return new List<ICollisionArea>(0);
            if (gobCollidable.HadSafePosition)
            {
                OverlapperFlags flags = OverlapperFlags.CheckPhysical |
                    OverlapperFlags.ConsiderColdness |
                    OverlapperFlags.ConsiderConsistency;
                return GetPhysicalOverlappers(gobCollidable, flags);
            }
            else
            {
                OverlapperFlags flags = OverlapperFlags.CheckPhysical;
                List<ICollisionArea> currentCompromisers = GetPhysicalOverlappers(gobCollidable, flags);
                if (originalCompromisers == null)
                    return currentCompromisers;
                return currentCompromisers.FindAll(delegate(ICollisionArea currentCompromiser)
                {
                    return !originalCompromisers.Contains(currentCompromiser);
                });
            }
        }

        /// <summary>
        /// Performs physical collisions for the gob.
        /// </summary>
        /// <param name="gob">The gob.</param>
        /// <param name="compromisers">The gob's physical consistency compromisers
        /// that participate in the physical collisions.</param>
        private void CollidePhysical(Gob gob, List<ICollisionArea> compromisers)
        {
            ICollidable gobCollidable1 = gob as ICollidable;
            if (gobCollidable1 == null) return;

            foreach (ICollisionArea collArea2 in compromisers)
            {
                gobCollidable1.Collide(collArea2.Owner, "General");
                collArea2.Owner.Collide(gobCollidable1, collArea2.Name);
                PerformCollision(gobCollidable1, collArea2.Owner);
            }
        }

        /// <summary>
        /// Returns a list of physical collision areas that potentially overlap a
        /// collision area.
        /// </summary>
        /// <param name="collArea">The collision area.</param>
        /// <returns>A list of physical collision areas that potentially overlap a
        /// collision area.</returns>
        private List<ICollisionArea> GetPotentialPhysicalOverlappers(ICollisionArea collArea)
        {
            BoundingBox box = collArea.Area.BoundingBox;
            WindowQuery query = new WindowQuery(box.Min.X, box.Min.Y, box.Max.X, box.Max.Y);
            List<Record> potentialRecords = collidables.Search(query);
            List<ICollisionArea> potentials = potentialRecords.ConvertAll<ICollisionArea>(delegate(Record record) { return ((CollisionData)record).CollisionArea; });
            Vector2 min = new Vector2(box.Min.X, box.Min.Y);
            Vector2 max = new Vector2(box.Max.X, box.Max.Y);
            wallTriangles.ForEachElement(min, max, delegate(WallTriangle wallTriangle) { potentials.Add(wallTriangle); });
            return potentials;
        }

        /// <summary>
        /// Reduces a list of potentially overlapping physical collision areas
        /// to those that actually overlap.
        /// </summary>
        /// <param name="potentials">Potential overlappers for various collision areas.
        /// 'potentials' lists potential overlappers with the following semantics:
        /// It is a list of pairs (area, list), where
        /// 'area' is some collision area, and
        /// 'list' contains potential overlapping collision areas of 'area'.
        /// </param>
        /// <param name="flags">Flags that control what is considered overlapping.</param>
        /// <returns>The list of actual overlappers.</returns>
        private List<ICollisionArea> ReducePotentialPhysicalOverlappers(
            List<KeyValuePair<ICollisionArea, List<ICollisionArea>>> potentials, OverlapperFlags flags)
        {
            List<ICollisionArea> compromisers = new List<ICollisionArea>();
            foreach (KeyValuePair<ICollisionArea, List<ICollisionArea>> pair in potentials)
                compromisers.AddRange(pair.Value.FindAll(delegate(ICollisionArea collArea2)
                {
                    ICollidable gobCollidable1 = pair.Key.Owner;
                    ICollidable gobCollidable2 = collArea2.Owner;

                    // A gob is allowed to overlap itself.
                    if (gobCollidable2 == gobCollidable1) return false;

                    // Certain Gob subclasses are allowed to overlap.
                    if (CanOverlap(gobCollidable1, gobCollidable2)) return false;

                    // Gobs with unsafe position are allowed to overlap anyone.
                    if ((flags & OverlapperFlags.ConsiderConsistency) != 0 &&
                        !gobCollidable2.HadSafePosition)
                        return false;

                    Gob gobGob2 = gobCollidable2 as Gob;

                    // Disabled gobs are allowed to overlap anyone.
                    if (gobGob2 != null && gobGob2.Disabled)
                        return false;

                    // Cold gobs are allowed to overlap their own.
                    if ((flags & OverlapperFlags.ConsiderColdness) != 0)
                    {
                        Gob gobGob1 = gobCollidable1 as Gob;
                        if (gobGob1 != null && gobGob2 != null &&
                            (gobGob1.Cold || gobGob2.Cold) &&
                            gobGob1.Owner == gobGob2.Owner &&
                            gobGob1.Owner != null)
                            return false;
                    }

                    return Geometry.Intersect(pair.Key.Area, collArea2.Area);
                }));
            return compromisers;
        }

        /// <summary>
        /// Returns the list of physical collision areas that overlap a gob.
        /// </summary>
        /// <param name="gob">The gob.</param>
        /// <param name="flags">Flags that control what is considered overlapping.</param>
        /// <returns>The list of physical collision areas that overlap the gob.</returns>
        private List<ICollisionArea> GetPhysicalOverlappers(ICollidable gob, OverlapperFlags flags)
        {
            int physicalAreaI = gob.PhysicalArea;
            if ((flags & OverlapperFlags.ConsiderConsistency) != 0 && !gob.HadSafePosition)
                return new List<ICollisionArea>(0);
            if ((flags & OverlapperFlags.CheckPhysical) != 0 && physicalAreaI == -1)
                return new List<ICollisionArea>(0);

            List<KeyValuePair<ICollisionArea, List<ICollisionArea>>> potentials = new List<KeyValuePair<ICollisionArea, List<ICollisionArea>>>();
            foreach (ICollisionArea collArea in gob.GetPrimitives())
                if (((flags & OverlapperFlags.CheckPhysical) != 0 && collArea.Type == CollisionAreaType.Physical) ||
                    ((flags & OverlapperFlags.CheckReceptor) != 0 && collArea.Type == CollisionAreaType.Receptor))
                    potentials.Add(new KeyValuePair<ICollisionArea, List<ICollisionArea>>(collArea,
                        GetPotentialPhysicalOverlappers(collArea)));
            return ReducePotentialPhysicalOverlappers(potentials, flags);
        }

        /// <summary>
        /// Returns true iff the given gob doesn't overlap gobs that it shouldn't overlap.
        /// </summary>
        /// <param name="gob">The gob.</param>
        /// <param name="flags">Flags that control what is considered overlapping.</param>
        /// <returns>True iff the gob is overlap consistent.</returns>
        private bool OverlapConsistent(Gob gob, OverlapperFlags flags)
        {
            ICollidable gobCollidable = gob as ICollidable;
            if (gobCollidable == null) return true;
            return OverlapConsistent(gobCollidable, flags);
        }

        /// <summary>
        /// Returns true iff the given gob doesn't overlap gobs that it shouldn't overlap.
        /// </summary>
        /// <param name="gobCollidable">The gob.</param>
        /// <param name="flags">Flags that control what is considered overlapping.</param>
        /// <returns>True iff the gob is overlap consistent.</returns>
        private bool OverlapConsistent(ICollidable gobCollidable, OverlapperFlags flags)
        {
            return GetPhysicalOverlappers(gobCollidable, flags).Count == 0;
        }

        /// <summary>
        /// Performs a physical collision between the two gobs.
        /// </summary>
        /// Physical collisions are a means to maintain overlap consistency.
        /// <param name="gobCollidable1">The gob whose movement led into the collision.</param>
        /// <param name="gobCollidable2">The other gob who stayed still.</param>
        public void PerformCollision(ICollidable gobCollidable1, ICollidable gobCollidable2)
        {
            /* UNDONE if (gobCollidable1 is IThick && gobCollidable2 is IProjectile)
                PerformCollisionProjectileThick((IProjectile)gobCollidable2, (IThick)gobCollidable1);
            else if (gobCollidable1 is IProjectile && gobCollidable2 is IThick)
                PerformCollisionProjectileThick((IProjectile)gobCollidable1, (IThick)gobCollidable2);
            else*/ if (gobCollidable1 is IThick && gobCollidable2 is ISolid)
                PerformCollisionSolidThick((ISolid)gobCollidable2, (IThick)gobCollidable1);
            else if (gobCollidable1 is ISolid && gobCollidable2 is IThick)
                PerformCollisionSolidThick((ISolid)gobCollidable1, (IThick)gobCollidable2);
            else if (gobCollidable1 is ISolid && gobCollidable2 is ISolid)
                PerformCollisionSolidSolid((ISolid)gobCollidable1, (ISolid)gobCollidable2);
        }

        /// <summary>
        /// Bounces a solid gob off a thick gob. The thick gob isn't affected.
        /// </summary>
        /// <param name="gobSolid1">The solid gob.</param>
        /// <param name="gobThick2">The thick gob.</param>
        private void PerformCollisionSolidThick(ISolid gobSolid1, IThick gobThick2)
        {
            // Play a sound.


            // We perform an elastic collision.
            float elasticity = 0.1f; // TODO: Add elasticity and friction to Wall.
            float friction = 1.0f;
            Vector2 xUnit = gobThick2.GetNormal(gobSolid1.Pos);
            Vector2 yUnit = new Vector2(-xUnit.Y, xUnit.X);
            Vector2 move1 = new Vector2(Vector2.Dot(gobSolid1.Move, xUnit),
                                        Vector2.Dot(gobSolid1.Move, yUnit));

            // Only perform physical collision if the gobs are actually closing in on each other.
            if (move1.X >= 0)
            {
                // To work around rounding errors when the solid gob is sliding
                // almost parallel to the thick gob's surface, fake the solid to
                // go a bit more towards the thick.
                move1.X -= 0.05f;
            }
            if (move1.X < 0)
            {
                Vector2 move1after = new Vector2(-move1.X * elasticity, move1.Y * friction);
                gobSolid1.Move = xUnit * move1after.X + yUnit * move1after.Y;
                if (gobSolid1 is Gobs.Ship)
                {
                    Vector2 move1Delta = move1 - move1after;
                    if (move1Delta.Length() > minimumCollisionDelta)
                    {
                        IDamageable damaGob1 = gobSolid1 as IDamageable;
                        damaGob1.InflictDamage(CollisionDamage(gobSolid1,move1Delta));
                    }
                }
                if (((Vector2)(move1 - move1after)).Length() > minimumCollisionDelta) // HACK: Figure out a better skip condition for collision sounds
                {
                    EventEngine eventEngine = (EventEngine)AssaultWing.Instance.Services.GetService(typeof(EventEngine));
                    SoundEffectEvent soundEvent = new SoundEffectEvent();
                    soundEvent.setAction(AW2.Sound.SoundOptions.Action.Collision);
                    eventEngine.SendEvent(soundEvent);
                }
            }
        }

        /// <summary>
        ///  Bounces two solid gobs off each other.
        /// </summary>
        /// <param name="gobSolid1">One solid gob.</param>
        /// <param name="gobSolid2">The other solid gob.</param>
        private void PerformCollisionSolidSolid(ISolid gobSolid1, ISolid gobSolid2)
        {
            // We perform a perfectly elastic collision.
            // First make gob2 as the point of reference,
            // then turn the coordinate axes so that both gobs are on the X-axis;
            // 'xUnit' and 'yUnit' will be the unit vectors of this system represented in game world coordinates;
            // 'move1' will be the movement vector of gob1 in this system (and gob2 stays put);
            // 'move1after' will be the resulting movement vector of gob1 in this system.
            Vector2 relMove = gobSolid1.Move - gobSolid2.Move;
            Vector2 xUnit = Vector2.Normalize(gobSolid2.Pos - gobSolid1.Pos);
            Vector2 yUnit = new Vector2(-xUnit.Y, xUnit.X);
            Vector2 move1 = new Vector2(Vector2.Dot(relMove, xUnit),
                                        Vector2.Dot(relMove, yUnit));

            // Only perform physical collision if the gobs are actually closing in on each other.
            if (move1.X > 0)
            {
                Vector2 move1after = new Vector2(move1.X * (gobSolid1.Mass - gobSolid2.Mass) / (gobSolid1.Mass + gobSolid2.Mass), move1.Y);
                Vector2 move2after = new Vector2(move1.X * 2 * gobSolid1.Mass / (gobSolid1.Mass + gobSolid2.Mass), 0);
                gobSolid1.Move = xUnit * move1after.X + yUnit * move1after.Y + gobSolid2.Move;
                gobSolid2.Move = xUnit * move2after.X + yUnit * move2after.Y + gobSolid2.Move;

                /*We want to deal damage in physics engine because calculations only happen once per collision!*/
                if (gobSolid2 is Gobs.Ship && gobSolid1 is Gobs.Ship)
                {
                    Vector2 move1Delta = move1 - move1after;
                    if (move1Delta.Length() > minimumCollisionDelta)
                    {
                        IDamageable damaGob1 = gobSolid1 as IDamageable;
                        damaGob1.InflictDamage(CollisionDamage(gobSolid1,move1Delta));
                        
                    }
                    if (move2after.Length() > minimumCollisionDelta)
                    {
                        //move2after = move2Delta because collision is calculated from the 2nd gob's point of view (2nd gob is still)
                        IDamageable damaGob2 = gobSolid2 as IDamageable;
                        damaGob2.InflictDamage(CollisionDamage(gobSolid2, move2after));
                    }

                }

                // Play a sound only if actual collision happened!.
                if (((Vector2)(move1 - move1after)).Length() > minimumCollisionDelta || move2after.Length()>minimumCollisionDelta) // HACK: Figure out a better skip condition for collision sounds
                {
                    EventEngine eventEngine = (EventEngine)AssaultWing.Instance.Services.GetService(typeof(EventEngine));
                    SoundEffectEvent soundEvent = new SoundEffectEvent();
                    soundEvent.setAction(AW2.Sound.SoundOptions.Action.Shipcollision);
                    eventEngine.SendEvent(soundEvent);
                }

            }


        }

        /// <summary>
        /// Returns true iff the two collidable gobs are allowed to overlap.
        /// </summary>
        /// Overlap consistency must be maintained for gobs for which this method returns false.
        /// <param name="gob1">One gob.</param>
        /// <param name="gob2">The other gob.</param>
        /// <returns>True iff the two collidable gobs can overlap.</returns>
        private bool CanOverlap(ICollidable gob1, ICollidable gob2)
        {
            if (gob1 is ISolid && gob2 is ISolid) return false;
            if (gob1 is ISolid && gob2 is IThick) return false;
            if (gob1 is IThick && gob2 is ISolid) return false;
            if (gob1 is IGas && gob2 is IThick) return false;
            if (gob1 is IThick && gob2 is IGas) return false;
            return true;
        }

        /// <summary>
        /// Returns true iff overlaps of the two collidable gobs are ever noticed.
        /// </summary>
        /// Collisions will never be performed for gobs for which this method returns false.
        /// <param name="gob1">One gob.</param>
        /// <param name="gob2">The other gob.</param>
        /// <returns>True iff overlaps of the two collidable gobs are ever noticed.</returns>
        private bool CanCollide(ICollidable gob1, ICollidable gob2)
        {
            if (gob1 is IProjectile && gob2 is IProjectile) return false;
            return true;
        }

        private float CollisionDamage(ISolid gobSolid, Vector2 moveDelta)
        {
            float damage =(float)((moveDelta.Length() / 2) * gobSolid.Mass * collisionDamageDownGrade);
            return damage;
        }

        #endregion // Collision checking methods

    }
}
