using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;
using AW2.Game.Gobs;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game.BonusActions
{
    class Weapon2UpgradeBonusAction : GameAction
    {
        public Weapon2UpgradeBonusAction(CanonicalString typeName)
            : base(typeName)
        {

        }
        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Weapon2UpgradeBonusAction()
            : base()
        {
            name = (CanonicalString)"dummyaction";
        }

        /// <summary>
        /// This method upgrades the weapon
        /// </summary>
        private void UpgradeWeapon(CanonicalString weaponUpgrade)
        {
            player.Ship.Devices.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, weaponUpgrade);
        }
        /// <summary>
        /// Action method. Contains logic for enabling the action
        /// </summary>
        public override void DoAction(float duration)
        {
            base.DoAction(duration);

            var weapon2 = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(player.Ship.Devices.Weapon2Name);
            if (weapon2.UpgradeNames != null && weapon2.UpgradeNames.Length > 0)
            {
                CanonicalString weaponUpgrade = weapon2.UpgradeNames[0];
                UpgradeWeapon(weaponUpgrade);
            }
            SetActionMessage();
        }


        /// <summary>
        /// Enables the ActionMessage (used in BonusOverlay)
        /// </summary>
        private void SetActionMessage()
        {
            var data = AssaultWing.Instance.DataEngine;
            /*this if is waste of CPU if action is activated then, ship usually exists*/
            Weapon weapon2 = player.Ship != null ? player.Ship.Devices.Weapon2
               : (Weapon)data.GetTypeTemplate(player.Weapon2RealName);

            bonusText = player.Ship.Devices.Weapon2Name;
            bonusIconName = weapon2.IconName;
            bonusIcon = AssaultWing.Instance.Content.Load<Texture2D>(bonusIconName);
        }

        /// <summary>
        /// Returs the default weapon for player after the Action Expires
        /// </summary>
        public override void RemoveAction()
        {
            player.Ship.Devices.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, player.Weapon2RealName);
        }
    }
}
