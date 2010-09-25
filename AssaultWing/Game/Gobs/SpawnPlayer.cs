using System;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// An area where players' ships can be created.
    /// </summary>
    public class SpawnPlayer : Gob
    {
        /// <summary>
        /// Area in which spawning takes place.
        /// </summary>
        [RuntimeState]
        private IGeomPrimitive _spawnArea;

        /// <summary>
        /// Bounding volume of the 3D visuals of the gob, in world coordinates.
        /// </summary>
        public override BoundingSphere DrawBounds { get { return new BoundingSphere(); } }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public SpawnPlayer()
        {
            _spawnArea = new Everything();
        }

        public SpawnPlayer(CanonicalString typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Returns an estimate of imminent threat of spawning a player's ship
        /// to this spawn area.
        /// </summary>
        /// Threat is not measured in any specific units. Returned values are mutually comparable;
        /// the larger the return value, the more dangerous the spawn area is.
        public float GetThreat(Player player)
        {
            const float SAFE_DISTANCE = 1000;
            var threats =
                from gob in Arena.Gobs
                where gob.Owner != null && gob.Owner != player
                let distance = Geometry.Distance(new AW2.Helpers.Geometric.Point(gob.Pos), _spawnArea)
                select Math.Max(0, SAFE_DISTANCE - distance);
            return threats.Sum();
        }

        /// <summary>
        /// Positions a player's ship in the spawn as if it has just been born.
        /// </summary>
        public void Spawn(Ship ship)
        {
            ship.ResetPos(Arena.GetFreePosition(ship, _spawnArea), ship.Move, ship.Rotation);
        }
    }
}
