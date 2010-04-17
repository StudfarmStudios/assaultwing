using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;
using AW2.Game.Gobs;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game.BonusActions
{
    class InstantHealthBonusAction : GameAction
    {
        private void GiveHealth()
        {
            float healthBonus = player.Ship.MaxDamageLevel * 0.20f;
            if (healthBonus <= player.Ship.DamageLevel)
                player.Ship.DamageLevel -= healthBonus;
            else
                player.Ship.DamageLevel = 0.0f;
        }   

        /// <summary>
        /// Action method. Contains logic for enabling the action
        /// </summary>
        public override void DoAction(float duration)
        {
            base.DoAction(duration);
            GiveHealth();
            SetActionMessage();
        }

        /// <summary>
        /// Enables the ActionMessage (used in BonusOverlay)
        /// </summary>
        private void SetActionMessage()
        {
            bonusIcon = AssaultWing.Instance.Content.Load<Texture2D>(bonusIconName);
        }
    }
}
