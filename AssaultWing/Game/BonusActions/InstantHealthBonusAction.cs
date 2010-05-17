﻿using System;
using AW2.Game.GobUtils;

namespace AW2.Game.BonusActions
{
    [GameActionType(1)]
    public class InstantHealthBonusAction : GameAction
    {
        public override void DoAction()
        {
            GiveHealth();
            SetActionMessage();
            base.DoAction();
        }

        private void GiveHealth()
        {
            float healthBonus = Player.Ship.MaxDamageLevel * 0.20f;
            if (healthBonus <= Player.Ship.DamageLevel)
                Player.Ship.DamageLevel -= healthBonus;
            else
                Player.Ship.DamageLevel = 0.0f;
        }

        private void SetActionMessage()
        {
            BonusText = "instant repair";
            BonusIconName = "b_icon_general";
        }
    }
}
