using System;
using AW2.Game.GobUtils;

namespace AW2.Game.BonusActions
{
    [GameActionType(4)]
    public class Weapon1UpgradeLoadTimeBonusAction : GameAction
    {
        private static float LOAD_TIME_MULTIPLIER = 0.6f;

        public override bool DoAction()
        {
            if (Player.Ship != null) UpgradeWeapon();
            SetActionMessage();
            return base.DoAction();
        }

        public override void RemoveAction()
        {
            // Multiple load time bonuses are handled by the same action; reset them all here.
            Player.Ship.Weapon1.LoadTimeMultiplier = 1;
        }

        private void UpgradeWeapon()
        {
            Player.Ship.Weapon1.LoadTimeMultiplier *= LOAD_TIME_MULTIPLIER;
        }

        private void SetActionMessage()
        {
            BonusText = Player.Weapon1Name + "\nspeedloader";
            BonusIconName = "b_icon_rapid_fire_1";
        }
    }
}
