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

        public override void GetDraw3DBounds(out Vector2 min, out Vector2 max) { min = max = new Vector2(float.NaN); }

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
        /// Positions a minion (e.g. a ship) using any of the spawn areas in the arena.
        /// </summary>
        public static void PositionNewMinion(Gob minion, Arena arena)
        {
            Func<IGeomPrimitive, int, IEnumerable<Vector2>> getRandomPoses = (area, count) =>
                Enumerable.Range(0, count)
                .Select(x => arena.GetFreePosition(LARGE_GOB_PHYSICAL_RADIUS, area));
            var spawnPoses = arena.Gobs.All<SpawnPlayer>()
                .SelectMany(spawn => getRandomPoses(spawn._spawnArea, 5));
            var poses = spawnPoses.Any()
                ? spawnPoses
                : getRandomPoses(new Rectangle(Vector2.Zero, arena.Dimensions), 20);
            var posesWithThreats = poses
                .Select(pos => new { pos, mood = GetMood(minion, pos) })
                .ToList()
                .OrderByDescending(x => x.mood)
                .ToList();
            var bestMood = posesWithThreats[0].mood;
            var bestSpawns = posesWithThreats.TakeWhile(x => x.mood == bestMood).ToList();
            var bestPos = bestSpawns[RandomHelper.GetRandomInt(bestSpawns.Count)].pos;
            minion.ResetPos(bestPos, Vector2.Zero, Gob.DEFAULT_ROTATION);
        }

        /// <summary>
        /// Returns an estimate of the current mood of a position for a gob.
        /// Mood is not measured in any specific units. Returned values are mutually comparable;
        /// the larger the return value, the better the mood is.
        /// </summary>
        private static float GetMood(Gob gob, Vector2 pos)
        {
            const float MOOD_DISTANCE_MIN = 400;
            const float MOOD_DISTANCE_MAX = 2000;
            var constituents =
                from minion in gob.Game.DataEngine.Minions
                where minion != gob
                let distance = Vector2.Distance(pos, minion.Pos)
                let sign = gob.IsFriend(minion) ? 1 : -1
                let amplitude = MathHelper.Clamp(1 - (distance - MOOD_DISTANCE_MIN) / (MOOD_DISTANCE_MAX - MOOD_DISTANCE_MIN), 0, 1)
                select sign * amplitude;
            return constituents.Sum();
        }
    }
}
