using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;
using AW2.Game.Gobs;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game.BonusActions
{
    class SelfDestructBonusAction : GameAction
    {
        [TypeParameter]
        CanonicalString weaponUpgrade;

        public SelfDestructBonusAction(CanonicalString typeName)
            : base(typeName)
        {

        }
        /// <summary>
        /// Only for serialization.
        /// </summary>
        public SelfDestructBonusAction()
            : base()
        {
            name = (CanonicalString)"dummyaction";
        }

        /// <summary>
        /// This method upgrades the weapon
        /// </summary>
        private void UpgradeWeapon(CanonicalString g_weaponUpgrade)
        {
            player.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, g_weaponUpgrade);
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


        /// <summary>
        /// Enables the ActionMessage (used in BonusOverlay)
        /// </summary>
        private void SetActionMessage()
        {
            bonusIcon = AssaultWing.Instance.Content.Load<Texture2D>(bonusIconName);
        }

        /// <summary>
        /// Returs the default weapon for player after the Action Expires
        /// </summary>
        public override void RemoveAction()
        {
            player.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, player.Weapon2Name);
        }
    }
}
