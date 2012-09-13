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
        public enum SourceTypeType { Unspecified, OwnPlayer, EnemyPlayer };

        public Gob Target { get; private set; }
        public SourceTypeType SourceType
        {
            get
            {
                if (Cause == null || Cause.Owner == null) return SourceTypeType.Unspecified;
                return Cause.Owner == Target.Owner
                    ? SourceTypeType.OwnPlayer
                    : SourceTypeType.EnemyPlayer;
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
