using System;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Information about a damaging event of a <see cref="Gob"/>.
    /// </summary>
    public class DamageInfo
    {
        public static DamageInfo Unspecified { get; private set; }

        public Gob Cause { get; private set; }

        static DamageInfo()
        {
            Unspecified = new DamageInfo();
        }

        public DamageInfo(DamageInfo info)
            : this(info.Cause)
        {
        }

        public DamageInfo(Gob cause)
        {
            Cause = cause;
        }

        private DamageInfo() { }

        public BoundDamageInfo Bind(Gob target)
        {
            if (target == null) throw new ArgumentNullException("target");
            return new BoundDamageInfo(this, target);
        }
    }

    public class BoundDamageInfo : DamageInfo
    {
        public enum SourceTypeType
        {
            /// <summary>
            /// Damaged by Nature.
            /// </summary>
            Unspecified,

            /// <summary>
            /// Damaged by the player himself.
            /// </summary>
            Self,

            /// <summary>
            /// Damaged by an opponent.
            /// </summary>
            EnemyPlayer,

            /// <summary>
            /// Damaged by a player on the same team.
            /// </summary>
            OwnTeamPlayer,
        };

        public Gob Target { get; private set; }
        public SourceTypeType SourceType
        {
            get
            {
                if (Cause == null || Cause.Owner == null) return SourceTypeType.Unspecified;
                if (Cause.Owner == Target.Owner) return SourceTypeType.Self;
                if (Cause.IsFriend(Target)) return SourceTypeType.OwnTeamPlayer;
                return SourceTypeType.EnemyPlayer;
            }
        }

        public BoundDamageInfo(DamageInfo info, Gob target)
            : base(info)
        {
            if (target == null) throw new ArgumentNullException("target");
            Target = target;
        }
    }
}
