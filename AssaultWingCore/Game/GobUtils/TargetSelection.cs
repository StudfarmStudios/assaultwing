using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using Microsoft.Xna.Framework;

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
        public static Gob ChooseTarget(IEnumerable<Gob> candidates, Gob source, float direction, float maxRange,
            float maxAngle = MathHelper.PiOver2, float friendlyWeight = 5, float angleWeight = 5)
        {
            // TODO !!! Turn into an object method; most parameters turn into properties.
            var targets =
                from gob in candidates
                where !gob.Disabled && gob != source && !gob.IsHidden
                let ownerWeight = gob.Owner == source.Owner ? friendlyWeight : gob.Owner == null ? 1f : 0.5f
                let relativePos = (gob.Pos - source.Pos).Rotate(-direction)
                let distanceSquared = relativePos.LengthSquared()
                where distanceSquared <= maxRange * maxRange
                let targetAngle = Math.Abs(relativePos.Angle())
                where targetAngle <= maxAngle
                let totalWeight = ownerWeight * (1 + angleWeight * targetAngle)
                where totalWeight < float.MaxValue
                orderby totalWeight ascending
                select gob;
            return targets.FirstOrDefault();
        }
    }
}
