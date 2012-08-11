using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;
using Rectangle = AW2.Helpers.Geometric.Rectangle;

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
            _spawnArea = new AW2.Helpers.Geometric.Rectangle(Vector2.Zero, new Vector2(1000, 1000));
        }

        public SpawnPlayer(CanonicalString typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Positions a ship using any of the spawn areas in the arena that contains the ship.
        /// </summary>
        public static void PositionNewShip(Ship ship, Arena arena)
        {
            Func<IGeomPrimitive, int, IEnumerable<Vector2>> getRandomPoses = (area, count) =>
                Enumerable.Range(0, count)
                .Select(x => arena.GetFreePosition(LARGE_GOB_PHYSICAL_RADIUS, area));
            var spawnPoses = arena.Gobs.OfType<SpawnPlayer>()
                .SelectMany(spawn => getRandomPoses(spawn._spawnArea, 5));
            var poses = spawnPoses.Any()
                ? spawnPoses
                : getRandomPoses(new Rectangle(Vector2.Zero, arena.Dimensions), 20);
            var posesWithThreats = poses
                .Select(pos => new { pos, threat = GetThreat(ship, pos) })
                .ToList()
                .OrderBy(x => x.threat)
                .ToList();
            var leastThreat = posesWithThreats[0].threat;
            var bestSpawns = posesWithThreats.TakeWhile(x => x.threat == leastThreat).ToList();
            var bestPos = bestSpawns[RandomHelper.GetRandomInt(bestSpawns.Count)].pos;
            ship.ResetPos(bestPos, Vector2.Zero, Gob.DEFAULT_ROTATION);
        }

        /// <summary>
        /// Returns an estimate of imminent threat to a gob at a position.
        /// Threat is not measured in any specific units. Returned values are mutually comparable;
        /// the larger the return value, the more dangerous the position is.
        /// </summary>
        private static float GetThreat(Gob gob, Vector2 pos)
        {
            const float SAFE_DISTANCE = 2000;
            var threats =
                from plr in gob.Game.DataEngine.Players
                let ship = plr.Ship
                where plr != gob.Owner && ship != null
                let distance = Vector2.Distance(pos, ship.Pos)
                select Math.Max(0, SAFE_DISTANCE - distance);
            return threats.Sum();
        }
    }
}
