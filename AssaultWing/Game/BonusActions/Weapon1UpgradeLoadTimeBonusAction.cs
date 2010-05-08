using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Gobs;
using AW2.Helpers;

namespace AW2.Game.BonusActions
{
    public class Weapon1UpgradeLoadTimeBonusAction : GameAction
    {
        [TypeParameter]
        private float multiplier;

        [TypeParameter]
        private float maxMultiplier;

        /// <summary>
        /// This constructor is only for serialization.
        /// </summary>
        public Weapon1UpgradeLoadTimeBonusAction()
        {
            multiplier = 0.8f;
            maxMultiplier = 0.5f;
        }

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
            BonusText = player.Weapon1Name+"\n"+BonusText;
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
