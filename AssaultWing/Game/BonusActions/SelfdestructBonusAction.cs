using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Game.Gobs;

namespace AW2.Game.BonusActions
{
    class SelfDestructBonusAction : GameAction
    {
        [TypeParameter]
        CanonicalString weaponUpgrade;

        /// <summary>
        /// This constructor is only for serialization.
        /// </summary>
        public SelfDestructBonusAction()
        {
            weaponUpgrade = (CanonicalString)"dummyweapon";
        }

        private void UpgradeWeapon(CanonicalString g_weaponUpgrade)
        {
            player.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, g_weaponUpgrade);
            player.PostprocessEffectNames.EnsureContains((CanonicalString)"bomber_rage");
        }

        /// <summary>
        /// Action method. Contains logic for enabling the action
        /// </summary>
        public override void DoAction(float duration)
        {
            base.DoAction(duration);                
            UpgradeWeapon(weaponUpgrade);
            SetActionMessage();
        }

        private void SetActionMessage()
        {
            bonusIcon = AssaultWing.Instance.Content.Load<Texture2D>(bonusIconName);
        }

        public override void RemoveAction()
        {
            player.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, player.Weapon2Name);
            player.PostprocessEffectNames.Remove((CanonicalString)"bomber_rage");
        }
    }
}
