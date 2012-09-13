using System;
using System.Linq;
using AW2.Game.BonusActions;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    public class SelfDestruct : Weapon
    {
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _deathGobTypes;

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public SelfDestruct()
        {
            _deathGobTypes = new CanonicalString[0];
        }

        public SelfDestruct(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            Owner.IgnoreLastDamagerFor(TimeSpan.FromSeconds(0.1));
            GobHelper.CreateGobs(_deathGobTypes, Arena, Owner.Pos, gob => gob.Owner = PlayerOwner);
            var suicideBomberBonus = Owner.BonusActions.OfType<Weapon2UpgradeBonusAction>().FirstOrDefault();
            if (suicideBomberBonus != null) suicideBomberBonus.TimeOut();
        }
    }
}
