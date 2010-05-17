using System;
using AW2.Game.GobUtils;

namespace AW2.Game.BonusActions
{
    [GameActionType(4)]
    public class Weapon1UpgradeLoadTimeBonusAction : GameAction
    {
        private static float LOAD_TIME_MULTIPLIER = 0.8f;

        public override void DoAction()
        {
            UpgradeWeapon();
            SetActionMessage();
            base.DoAction();
        }

        public override void RemoveAction()
        {
            Player.Ship.SetDeviceLoadMultiplier(ShipDevice.OwnerHandleType.PrimaryWeapon, 1);
        }

        private void UpgradeWeapon()
        {
            var deviceType = ShipDevice.OwnerHandleType.PrimaryWeapon;
            float currentMultiplier = Player.Ship.GetDeviceLoadMultiplier(deviceType);
            float newMultiplier = currentMultiplier * LOAD_TIME_MULTIPLIER;
            Player.Ship.SetDeviceLoadMultiplier(deviceType, newMultiplier);

        }

        private void SetActionMessage()
        {
            BonusText = Player.Weapon1Name + "\nspeedloader";
            BonusIconName = "b_icon_rapid_fire_1";
        }
    }
}
