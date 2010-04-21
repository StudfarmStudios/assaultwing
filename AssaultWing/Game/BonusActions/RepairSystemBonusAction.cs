using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Game.Gobs;

namespace AW2.Game.BonusActions
{
    class RepairSystemBonusAction : GameAction
    {
        static readonly TimeSpan updateCycle = TimeSpan.FromSeconds(1);
        TimeSpan lastActivatedTime;

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

        /// <summary>
        /// Actions that do something when active
        /// </summary>
        public override void Update()
        {
            if (AssaultWing.Instance.GameTime.TotalArenaTime >= lastActivatedTime + updateCycle)
            {
                GiveHealth();
            }
        }

        /// <summary>
        /// This method gives the health to the player
        /// </summary>
        private void GiveHealth()
        {
            lastActivatedTime = AssaultWing.Instance.GameTime.TotalArenaTime;
            float healthBonus = player.Ship.MaxDamageLevel * -0.05f;
            player.Ship.InflictDamage(healthBonus, new DeathCause());
            if (healthBonus <= player.Ship.DamageLevel)
                player.Ship.DamageLevel -= healthBonus;
            else
                player.Ship.DamageLevel = 0.0f;
        }
    }
}
