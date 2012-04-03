using System;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.BonusActions;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    public class SelfDestruct : Weapon
    {
        [TypeParameter, ShallowCopy]
        CanonicalString[] deathGobTypes;

        /// This constructor is only for serialisation.
        public SelfDestruct()
        {
            deathGobTypes = new CanonicalString[0];
        }

        public SelfDestruct(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            GobHelper.CreateGobs(deathGobTypes, Arena, Owner.Pos, gob => gob.Owner = PlayerOwner);
            var suicideBomberBonus = Owner.BonusActions.FirstOrDefault(act => act is Weapon2UpgradeBonusAction);
            if (suicideBomberBonus != null) suicideBomberBonus.TimeOut();
        }
    }
}
