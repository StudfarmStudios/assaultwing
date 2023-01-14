using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using Microsoft.Xna.Framework;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Selects gob targets based on weighted criteria. The lowest weight is chosen.
    /// </summary>
    public class TargetSelector
    {
        /// <summary>
        /// Only targets up to this far away are considered.
        /// </summary>
        public float MaxRange { get; set; }

        /// <summary>
        /// Only targets up to this angle (in radians) from source direction are considered.
        /// <see cref="MathHelper.Pi"/> allows all targets regardless of angle
        /// </summary>
        public float MaxAngle { get; set; }

        /// <summary>
        /// Weight of a friendly target. Set to float.MaxValue to totally avoid choosing friendly targets.
        /// </summary>
        public float FriendlyWeight { get; set; }

        /// <summary>
        /// Weight of a neutral target (not friendly and not hostile).
        /// </summary>
        public float NeutralWeight { get; set; }

        /// <summary>
        /// Weight of an hostile target.
        /// </summary>
        public float HostileWeight { get; set; }

        /// <summary>
        /// Weight of one radian of angle of target from source direction.
        /// If zero then all allowed targets regardless of their angle are considered equal.
        /// </summary>
        public float AngleWeight { get; set; }

        public TargetSelector(float maxRange)
        {
            MaxRange = maxRange;
            MaxAngle = MathHelper.PiOver2;
            FriendlyWeight = 5;
            NeutralWeight = 1;
            HostileWeight = 0.5f;
            AngleWeight = 2.5f;
        }

        /// <summary>
        /// Chooses a target, preferring those that are
        /// 1. enemies or at least not friends, and
        /// 2. straight ahead, and
        /// 3. close
        /// </summary>
        /// <param name="filter">Gobs that map to false are ignored as potential targets.</param>
        public Gob ChooseTarget(IEnumerable<Gob> candidates, Gob source, float direction, Func<Gob, bool> filter = null)
        {
            var targets =
                from gob in candidates
                where !gob.Disabled && gob != source && !gob.IsHidden
                let distanceSquared = Vector2.DistanceSquared(gob.Pos, source.Pos)
                where distanceSquared <= MaxRange * MaxRange
                let ownerWeight = gob.IsFriend(source) ? FriendlyWeight : gob.Owner == null ? NeutralWeight : HostileWeight
                let relativePos = (gob.Pos - source.Pos).Rotate(-direction)
                let targetAngle = Math.Abs(relativePos.Angle())
                where targetAngle <= MaxAngle
                let totalWeight = ownerWeight * (1 + (float)Math.Sqrt(distanceSquared) * (1 + AngleWeight * targetAngle))
                where totalWeight < float.MaxValue
                orderby totalWeight ascending
                select gob;
            return filter != null ? targets.FirstOrDefault(filter) : targets.FirstOrDefault();
        }
    }
}
