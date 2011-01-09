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

        public DamageInfo(Gob cause)
        {
            Cause = cause;
        }

        private DamageInfo() { }

        public BoundDamageInfo Bind(Gob target, TimeSpan time)
        {
            if (target == null) throw new ArgumentNullException("target");
            return new BoundDamageInfo(this, target, time);
        }
    }

    public class BoundDamageInfo : DamageInfo
    {
        public enum SourceTypeType { Unspecified, OwnPlayer, EnemyPlayer };

        public Gob Target { get; private set; }
        public TimeSpan Time { get; private set; }
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
        public Player PlayerCause { get { return Cause.Owner; } }

        public BoundDamageInfo(DamageInfo info, Gob target, TimeSpan time)
            : base(info.Cause)
        {
            if (target == null) throw new ArgumentNullException("target");
            Target = target;
            Time = time;
        }
    }
}
