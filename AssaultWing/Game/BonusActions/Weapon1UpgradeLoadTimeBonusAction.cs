using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;
using AW2.Game.Gobs;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game.BonusActions
{
    class Weapon1UpgradeLoadTimeBonusAction : GameAction
    {
        public Weapon1UpgradeLoadTimeBonusAction(CanonicalString typeName)
            : base(typeName)
        {

        }
        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Weapon1UpgradeLoadTimeBonusAction()
            : base()
        {
            name = (CanonicalString)"dummyaction";
        }

        /// <summary>
        /// This method upgrades the weapon
        /// </summary>
        private void UpgradeWeapon(CanonicalString weaponUpgrade)
        {
            player.Ship.Devices.SetDeviceType(ShipDevice.OwnerHandleType.PrimaryWeapon, weaponUpgrade);
        }
        /// <summary>
        /// Action method. Contains logic for enabling the action
        /// </summary>
        public override void DoAction(float duration)
        {
            base.DoAction(duration);
            /*
            var weapon1 = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(player.Ship.Devices.Weapon1Name);
            if (weapon1.UpgradeNames != null && weapon1.UpgradeNames.Length > 0)
            {
                Console.WriteLine(weapon1.UpgradeNames[0]);
                CanonicalString weaponUpgrade = weapon1.UpgradeNames[0];
                UpgradeWeapon(weaponUpgrade);
            }
             */
            SetActionMessage();
        }

        /// <summary>
        /// Enables the ActionMessage (used in BonusOverlay)
        /// </summary>
        private void SetActionMessage()
        {
            var data = AssaultWing.Instance.DataEngine;
            /*this if is waste of CPU if action is activated then, ship usually exists*/
            Weapon weapon1 = player.Ship != null ? player.Ship.Devices.Weapon1
               : (Weapon)data.GetTypeTemplate(player.Weapon1RealName);
            bonusText = player.Weapon1RealName+ "\nspeedloader";
            bonusIconName = "b_icon_rapid_fire_1";
            bonusIcon = AssaultWing.Instance.Content.Load<Texture2D>(bonusIconName);
        }

        /// <summary>
        /// Returs the default weapon for player after the Action Expires
        /// </summary>
        public override void RemoveAction()
        {
            player.Ship.Devices.SetDeviceType(ShipDevice.OwnerHandleType.PrimaryWeapon, player.Weapon1RealName);
        }
    }
}
