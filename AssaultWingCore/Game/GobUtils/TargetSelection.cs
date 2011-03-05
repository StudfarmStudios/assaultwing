using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Selects targets for weapons and semi-intelligent bullets of weapons.
    /// </summary>
    public class TargetSelection
    {
        /// <summary>
        /// Chooses a target, preferring those that are
        /// 1. enemies or at least not friends, and
        /// 2. straight ahead, and
        /// 3. close
        /// </summary>
        public static Gob ChooseTarget(IEnumerable<Gob> candidates, Gob source, float direction, float maxRange)
        {
            var targets =
                from gob in candidates
                where !gob.Disabled && gob != source
                let ownerWeight = gob.Owner == source.Owner ? 5f : gob.Owner == null ? 1f : 0.5f
                let relativePos = (gob.Pos - source.Pos).Rotate(-direction)
                let distanceSquared = relativePos.LengthSquared()
                where distanceSquared <= maxRange * maxRange && relativePos.X >= 0
                orderby ownerWeight * (relativePos.X + 5 * Math.Abs(relativePos.Y)) ascending
                select gob;
            return targets.FirstOrDefault();
        }
    }
}
