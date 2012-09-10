using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using FarseerPhysics.Collision;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using AW2.Game;
using AW2.Game.Collisions;
using AW2.Helpers.Geometric;

namespace AW2.Helpers
{
    /// <summary>
    /// Contains helper methods for Farseer physics.
    /// </summary>
    public static class PhysicsHelper
    {
        /// <summary>
        /// Ratio of Farseer Physics Engine coordinates to Assault Wing coordinates.
        /// E.g. Farseer_X = Assault_Wing_X * FARSEER_SCALE. Farseer provides the most
        /// accurate simulation when body sizes are between 0.1 and 10.
        /// </summary>
        private const float FARSEER_SCALE = 0.1f;

        public static float ToFarseer(this float a) { return a * FARSEER_SCALE; }
        public static float FromFarseer(this float a) { return a / FARSEER_SCALE; }
        public static Vector2 ToFarseer(this Vector2 a) { return a * FARSEER_SCALE; }
        public static Vector2 FromFarseer(this Vector2 a) { return a / FARSEER_SCALE; }

        /// <summary>
        /// Creates an axis aligned bounding box in Farseer coordinates,
        /// given the corner points in Assault Wing coordinates.
        /// </summary>
        public static AABB CreateAABB(Vector2 minPos, Vector2 maxPos)
        {
            return new AABB(FARSEER_SCALE * minPos, FARSEER_SCALE * maxPos);
        }

        /// <summary>
        /// Creates a Farseer vertex array in Farseer coordinates,
        /// given vertices in Assault Wing coordinates.
        /// </summary>
        public static Vertices CreateVertices(params Vector2[] poses)
        {
            var vertices = new Vertices(poses);
            var scale = new Vector2(FARSEER_SCALE);
            vertices.Scale(ref scale);
            return vertices;
        }

        public static World CreateWorld(Vector2 gravity, Vector2 worldMin, Vector2 worldMax)
        {
            return new World(gravity * FARSEER_SCALE, PhysicsHelper.CreateAABB(worldMin, worldMax));
        }

        public static void ApplyForce(Gob gob, Vector2 force)
        {
            gob.Body.ApplyForce(force * FARSEER_SCALE);
        }

        public static void ApplyImpulse(Gob gob, Vector2 impulse)
        {
            gob.Body.ApplyLinearImpulse(impulse * FARSEER_SCALE);
        }

        /// <summary>
        /// Applies the given force to a gob, preventing gob speed from growing beyond a limit.
        /// Although the gob's speed cannot grow beyond <paramref name="maxSpeed"/>,
        /// it can still maintain its value even if it's larger than <paramref name="maxSpeed"/>.
        /// </summary>
        public static void ApplyLimitedForce(Gob gob, Vector2 force, float maxSpeed)
        {
            var unlimitedMove = gob.Move + force.ForceToMoveDelta(gob);
            var limitedMove = unlimitedMove.Clamp(0, Math.Max(maxSpeed, gob.Move.Length()));
            SetMove(gob, limitedMove);
        }

        /// <summary>
        /// Applies drag to a gob. Drag is a force that manipulates the gob's movement towards
        /// the <paramref name="flow"/> of the medium. <paramref name="drag"/> measures the
        /// amount of this manipulation, 0 meaning no drag and 1 meaning absolute drag where
        /// the gob cannot escape the flow. Practical values are very small, under 0.1.
        /// </summary>
        public static void ApplyDrag(Gob gob, Vector2 flow, float drag)
        {
            SetMove(gob, (1 - drag) * (gob.Move - flow) + flow);
        }

        /// <summary>
        /// Returns true if <paramref name="area"/> is free of overlappers. A CollisionArea won't be
        /// an overlapper if <paramref name="filter"/> returns false for it.
        /// </summary>
        public static bool IsFreePosition(World world, IGeomPrimitive area, Func<CollisionArea, bool> filter = null)
        {
            var shape = area.GetShape();
            AABB shapeAabb;
            Transform shapeTransform = new Transform();
            shapeTransform.SetIdentity();
            shape.ComputeAABB(out shapeAabb, ref shapeTransform, 0);
            Transform fixtureTransform;
            var isFree = true;
            world.QueryAABB(otherFixture =>
            {
                otherFixture.Body.GetTransform(out fixtureTransform);
                if (!otherFixture.IsSensor &&
                    (filter == null || filter((CollisionArea)otherFixture.UserData)) &&
                    AABB.TestOverlap(otherFixture.Shape, 0, shape, 0, ref fixtureTransform, ref shapeTransform))
                    isFree = false;
                return isFree;
            }, ref shapeAabb);
            return isFree;
        }

        /// <summary>
        /// Returns distance to the closest collision area, if any, along a directed line segment.
        /// Collision areas for whom <paramref name="filter"/> returns false are ignored.
        /// </summary>
        public static float? GetDistanceToClosest(World world, Vector2 from, Vector2 to, Func<CollisionArea, bool> filter, CollisionAreaType[] areaTypes)
        {
            Category categories = 0;
            foreach (var areaType in areaTypes) categories |= areaType.Category();
            Vector2? closestPoint = null;
            world.RayCast((Fixture fixture, Vector2 point, Vector2 normal, float fraction) =>
            {
                var area = (CollisionArea)fixture.UserData;
                if (filter(area)) closestPoint = point / FARSEER_SCALE;
                return fraction; // Skip everything that is farther away.
            }, from * FARSEER_SCALE, to * FARSEER_SCALE, categories);
            return closestPoint.HasValue ? Vector2.Distance(from, closestPoint.Value) : (float?)null;
        }

        /// <summary>
        /// Invokes an action for all collision areas that overlap an area.
        /// </summary>
        /// <param name="action">If returns false, the query will exit.</param>
        /// <param name="preFilter">Filters potential overlappers. This delegate is supposed to be light.
        /// It is called before testing for precise overlapping which is a more costly operation. To return true
        /// if the candidate qualifies for more precise overlap testing.</param>
        public static void QueryOverlappers(World world, CollisionArea area, Func<CollisionArea, bool> action, Func<CollisionArea, bool> preFilter = null)
        {
            var shape = area.Fixture.Shape;
            AABB shapeAabb;
            Transform shapeTransform;
            area.Fixture.Body.GetTransform(out shapeTransform);
            shape.ComputeAABB(out shapeAabb, ref shapeTransform, 0);
            Transform fixtureTransform;
            world.QueryAABB(otherFixture =>
            {
                if (preFilter == null || preFilter((CollisionArea)otherFixture.UserData))
                {
                    otherFixture.Body.GetTransform(out fixtureTransform);
                    if (AABB.TestOverlap(otherFixture.Shape, 0, shape, 0, ref fixtureTransform, ref shapeTransform))
                        return action((CollisionArea)otherFixture.UserData);
                }
                return true;
            }, ref shapeAabb);
        }

        /// <summary>
        /// Invokes an action for all collision areas whose AABB overlaps a rectangular area.
        /// </summary>
        /// <param name="action">If returns false, the query will exit.</param>
        public static void QueryOverlappers(World world, Vector2 min, Vector2 max, Func<CollisionArea, bool> action)
        {
            var aabb = CreateAABB(min, max);
            world.QueryAABB(fixture => action((CollisionArea)fixture.UserData), ref aabb);
        }

        /// <summary>
        /// Returns all collision areas that are in contact with a collision area.
        /// </summary>
        public static IEnumerable<CollisionArea> GetContacting(CollisionArea area)
        {
            var contactEdge = area.Fixture.Body.ContactList;
            while (contactEdge != null)
            {
                if (contactEdge.Contact.FixtureA == area.Fixture) yield return (CollisionArea)contactEdge.Contact.FixtureB.UserData;
                if (contactEdge.Contact.FixtureB == area.Fixture) yield return (CollisionArea)contactEdge.Contact.FixtureA.UserData;
                contactEdge = contactEdge.Next;
            }
        }

        /// <summary>
        /// Returns the distance of a point from a collision area.
        /// </summary>
        public static float Distance(CollisionArea area, Vector2 pos)
        {
            // TODO: Shortest distance to area boundary.
            return Vector2.Distance(area.Owner.Pos, pos);
        }

        private static void SetMove(Gob gob, Vector2 move)
        {
            ApplyForce(gob, (move - gob.Move).MoveDeltaToForce(gob));
        }

        private static Vector2 ForceToMoveDelta(this Vector2 force, Gob gob)
        {
            Debug.Assert(gob.Mass > 0);
            return force * (float)gob.Game.GameTime.ElapsedGameTime.TotalSeconds / gob.Mass;
        }

        private static Vector2 MoveDeltaToForce(this Vector2 moveDelta, Gob gob)
        {
            return moveDelta / (float)gob.Game.GameTime.ElapsedGameTime.TotalSeconds * gob.Mass;
        }
    }
}
