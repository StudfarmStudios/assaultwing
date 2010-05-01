using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Gobs;
using AW2.Helpers;

namespace AW2.Game.BonusActions
{
    class RepairSystemBonusAction : GameAction
    {
        private static readonly TimeSpan UPDATE_CYCLE = TimeSpan.FromSeconds(1);
        private TimeSpan _lastActivatedTime;

        public override void DoAction(float duration)
        {
            base.DoAction(duration);
            GiveHealth();
            SetActionMessage();
        }

        private void SetActionMessage()
        {
            bonusIcon = AssaultWing.Instance.Content.Load<Texture2D>(bonusIconName);
        }

        public override void Update()
        {
            if (AssaultWing.Instance.GameTime.TotalArenaTime >= _lastActivatedTime + UPDATE_CYCLE)
                GiveHealth();
        }

        private void GiveHealth()
        {
            _lastActivatedTime = AssaultWing.Instance.GameTime.TotalArenaTime;
            float healthBonus = player.Ship.MaxDamageLevel * -0.05f;
            player.Ship.InflictDamage(healthBonus, new DeathCause());
        }
    }
}
