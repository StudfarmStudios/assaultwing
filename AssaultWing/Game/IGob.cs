using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game
{
    /// <summary>
    /// Superinterface for interfaces that represent different gob categories.
    /// </summary>
    /// This interface only acts as a marker of interfaces for gobs.
    /// Each subinterface of this interface represents a "category" of gob types.
    /// Categories can be used in fetching certain kinds of gobs from DataEngine.
    public interface IGob
    {
    }

    #region ICollidable and subinterfaces, i.e. collision types

    /// <summary>
    /// An area with which a gob can overlap with other gobs' areas,
    /// resulting in a collision.
    /// </summary>
    [LimitedSerialization]
    public struct CollisionArea
    {
        [TypeParameter]
        string name;

        /// <summary>
        /// Area in gob coordinates, not world coordinates.
        /// </summary>
        [TypeParameter]
        IGeomPrimitive area;

        /// <summary>
        /// Area in world coordinates, transformed by <b>oldWorldMatrix</b>.
        /// </summary>
        IGeomPrimitive transformedArea;
        Matrix oldWorldMatrix;

        ICollidable owner;
        
        object collisionData;

        /// <summary>
        /// Collision area name; either "General" for general collision
        /// checking (including physical collisions), or something else
        /// for a receptor area that can react to other gobs' general
        /// areas.
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// Is this collision area a receptor.
        /// </summary>
        public bool IsReceptor { get { return name != "General"; } }

        /// <summary>
        /// The geometric area for overlap testing, in game world coordinates,
        /// translated according to the hosting gob's location.
        /// </summary>
        public IGeomPrimitive Area
        {
            get
            {
                if (!((Gob)owner).WorldMatrix.Equals(oldWorldMatrix))
                {
                    oldWorldMatrix = ((Gob)owner).WorldMatrix;
                    transformedArea = area.Transform(oldWorldMatrix);
                }
                return transformedArea;
            }
        }

        /// <summary>
        /// The gob whose collision area this is.
        /// </summary>
        public ICollidable Owner { get { return owner; } set { owner = value; } }

        /// <summary>
        /// Data storage for PhysicsEngine.
        /// </summary>
        public object CollisionData { get { return collisionData; } set { collisionData = value; } }

        /// <summary>
        /// Creates a new collision area. The names should be unique within the gob.
        /// </summary>
        /// <param name="name">Collision area name; "General" for a general area,
        /// or something else for a receptor area. Don't give the same name to
        /// two collision areas of the same gob.</param>
        /// <param name="area">The geometric area.</param>
        /// <param name="owner">The gob whose collision area this is.</param>
        public CollisionArea(string name, IGeomPrimitive area, ICollidable owner)
        {
            this.name = name;
            this.area = area;
            this.transformedArea = null;
            this.oldWorldMatrix = 0 * Matrix.Identity;
            this.owner = owner;
            this.collisionData = null;
        }

        #region Equality methods

        /// <summary>
        /// Returns true iff the two collision areas are equal.
        /// </summary>
        /// <param name="a">One collision area.</param>
        /// <param name="b">Another collision area.</param>
        /// <returns>True iff the two collision areas are equal.</returns>
        public static bool operator ==(CollisionArea a, CollisionArea b)
        {
            return a.owner == b.owner && a.name == b.name;
        }

        /// <summary>
        /// Returns true iff the two collision areas are not equal.
        /// </summary>
        /// <param name="a">One collision area.</param>
        /// <param name="b">Another collision area.</param>
        /// <returns>True iff the two collision areas are not equal.</returns>
        public static bool operator !=(CollisionArea a, CollisionArea b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Determines whether two Object instances are equal. 
        /// </summary>
        public override bool Equals(object o)
        {
            if (o == null) return false;
            if (!(o is CollisionArea)) return false;
            return (CollisionArea)o == this;
        }

        /// <summary>
        /// Returns a hash code for the current Object.
        /// </summary>
        /// <returns>A hash code for the current Object.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion Equality methods
    }

    /// <summary>
    /// Interface for gobs that can collide.
    /// </summary>
    /// A collidable gob has one or more collision primitives (e.g. circles)
    /// that are checked against another gob's collision primitives for overlap.
    /// If the collision primitives of two gobs overlap, a collision occurs.
    /// If of the primitives is called General, it is used for physical collisions
    /// if the two gobs implement suitable subinterfaces of ICollidable
    /// (e.g. ISolid does physical collisions with IProjectile but not with IGas).
    public interface ICollidable : IGob
    {
        /// <summary>
        /// Returns the collision primitives of the gob.
        /// </summary>
        /// The primitives are located in the game world according to the gob's location.
        /// <returns>The collision primitives of the gob.</returns>
        CollisionArea[] GetPrimitives(); // TODO: Rename to GetAreas perhaps

        /// <summary>
        /// The index of the physical collision area of the collidable gob
        /// in <b>GetPrimitives()</b> or <b>-1</b> if it has none.
        /// </summary>
        int PhysicalArea { get; }

        /// <summary>
        /// Returns the distance from the edge of the general collision area
        /// of the collidable gob to a point in the game world.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>The shortest distance between the point and the gob's
        /// physical collision area.</returns>
        float DistanceTo(Vector2 point);

        /// <summary>
        /// Returns true iff the gob's position at the beginning of the frame was not colliding.
        /// </summary>
        /// <returns>True iff the gob's position at the beginning of the frame was not colliding</returns>
        bool HadSafePosition { get; set; }

        /// <summary>
        /// Performs collision operations with a gob whose general collision area
        /// has collided with one of our receptor areas.
        /// </summary>
        /// <param name="gob">The gob we collided with.</param>
        /// <param name="receptorName">The name of our colliding receptor area.</param>
        void Collide(ICollidable gob, string receptorName);
    }

    /// <summary>
    /// Interface for solid gobs.
    /// </summary>
    /// A solid gob collides with other solid gobs, gas gobs and wave gobs.
    public interface ISolid : ICollidable
    {
        /// <summary>
        /// The location of the center of mass of the gob.
        /// </summary>
        Vector2 Pos { get; set; }

        /// <summary>
        /// The movement vector of the gob.
        /// </summary>
        Vector2 Move { get; set; }

        /// <summary>
        /// The mass of the gob, in kilograms.
        /// </summary>
        float Mass { get;}
    }

    /// <summary>
    /// Interface for projectiles.
    /// </summary>
    /// Projectiles don't collide with each other. When they collide with something
    /// else, they disintegrate and inflict damage.
    public interface IProjectile : ISolid
    {
        /// <summary>
        /// The area the projectile destroys from thick gobs on impact.
        /// </summary>
        /// The area is translated according to the gob's location.
        Helpers.Polygon ImpactArea { get; }
    }

    /// <summary>
    /// Interface for thick gobs.
    /// </summary>
    /// A thick gob is a solid gob that is too thick for gas to pass through it.
    /// A thick gob is also "too thick" to move around, thus it's unaffected by
    /// collisions with other solid gobs.
    public interface IThick : ISolid
    {
        /// <summary>
        /// Returns the unit normal vector from the thick gob
        /// pointing towards the given location.
        /// </summary>
        /// <param name="pos">The location for the normal to point to.</param>
        /// <returns>The unit normal pointing to the given location.</returns>
        Vector2 GetNormal(Vector2 pos);

        /// <summary>
        /// Removes an area from the thick gob. 
        /// </summary>
        /// <param name="area">The area to remove. The polygon must be convex.</param>
        void MakeHole(Helpers.Polygon area);
    }

    /// <summary>
    /// Interface for gas gobs.
    /// </summary>
    /// A gas gob collides with solid gobs and passes through all but thick gobs.
    public interface IGas : ICollidable
    {
    }

    /// <summary>
    /// Interface for wave gobs.
    /// </summary>
    /// A wave gob collides with solid gobs and passes through all gobs.
    public interface IWave : ICollidable
    {
    }

    /// <summary>
    /// Interface for gobs providing a gravitational force.
    /// </summary>
    // TODO: IGravity might be useless as ICollidable, but useful in some other gob classification, yet to be seen.
    public interface IGravity : ICollidable
    {
        /// <summary>
        /// Returns the acceleration that the gravitational force applies to a mass
        /// located at the given position.
        /// </summary>
        /// <param name="pos">The position of the mass that is pulled by the force.</param>
        /// <returns>The acceleration created by the force.</returns>
        Vector2 GetGravity(Vector2 pos);
    }

    #endregion ICollidable and subinterfaces

    /// <summary>
    /// An entity that can sustain damage up to some limit and get repaired.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// The amount of damage, between 0 and <b>MaxDamageLevel</b>.
        /// 0 means the entity is in perfect condition;
        /// <b>MaxDamageLevel</b> means the entity is totally destroyed.
        /// </summary>
        float DamageLevel { get; }

        /// <summary>
        /// The maximum amount of damage the entity can sustain.
        /// </summary>
        float MaxDamageLevel { get; }

        /// <summary>
        /// Inflicts damage on the entity.
        /// </summary>
        /// <param name="damageAmount">If positive, amount of damage;
        /// if negative, amount of repair.</param>
        void InflictDamage(float damageAmount);
    }
}
