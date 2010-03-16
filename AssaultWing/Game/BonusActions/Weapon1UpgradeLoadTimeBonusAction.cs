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
        [TypeParameter]
        float multiplier;

        [TypeParameter]
        float maxMultiplier;

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Weapon1UpgradeLoadTimeBonusAction()
            : base()
        {
            name = (CanonicalString)"dummyaction";
            multiplier = 0.5f;
            maxMultiplier = 0.1f;
        }

        public Weapon1UpgradeLoadTimeBonusAction(CanonicalString typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// This method upgrades the weapon
        /// </summary>
        private void UpgradeWeapon()
        {
            ShipDevice.OwnerHandleType deviceType = ShipDevice.OwnerHandleType.PrimaryWeapon;
            float currentMultiplier = player.Ship.GetDeviceLoadMultiplier(deviceType);
            float newMultiplier = currentMultiplier * multiplier;
            if (newMultiplier <= maxMultiplier)
                newMultiplier = maxMultiplier;
            player.Ship.SetDeviceLoadMultiplier(deviceType, newMultiplier);
 
        }
        /// <summary>
        /// Action method. Contains logic for enabling the action
        /// </summary>
        public override void DoAction(float duration)
        {
            base.DoAction(duration);
            UpgradeWeapon();
            SetActionMessage();
        }

        /// <summary>
        /// Enables the ActionMessage (used in BonusOverlay)
        /// </summary>
        private void SetActionMessage()
        {
            var data = AssaultWing.Instance.DataEngine;
            /*this if is waste of CPU if action is activated then, ship usually exists*/
            Weapon weapon1 = player.Ship != null ? player.Ship.Weapon1
               : (Weapon)data.GetTypeTemplate(player.Weapon1Name);
            bonusText = player.Weapon1Name+"\n"+bonusText;
            bonusIcon = AssaultWing.Instance.Content.Load<Texture2D>(bonusIconName);
        }

        /// <summary>
        /// Returs the default weapon for player after the Action Expires
        /// </summary>
        public override void RemoveAction()
        {
            player.Ship.SetDeviceLoadMultiplier(ShipDevice.OwnerHandleType.PrimaryWeapon, 1);
        }
    }
}
