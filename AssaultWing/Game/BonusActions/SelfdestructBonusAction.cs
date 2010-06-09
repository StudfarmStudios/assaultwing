using System;
using AW2.Game.GobUtils;
using AW2.Helpers;

namespace AW2.Game.BonusActions
{
    [GameActionType(3)]
    public class SelfDestructBonusAction : GameAction
    {
        public override void RemoveAction()
        {
            Player.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, Player.Weapon2Name);
            Player.PostprocessEffectNames.Remove((CanonicalString)"bomber_rage");
        }

        public override bool DoAction()
        {
            UpgradeWeapon();
            SetActionMessage();
            return base.DoAction();
        }

        private void UpgradeWeapon()
        {
            Player.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, (CanonicalString)"selfdestructship");
            Player.PostprocessEffectNames.EnsureContains((CanonicalString)"bomber_rage");
        }

        private void SetActionMessage()
        {
            BonusText = "suicide bomber";
            BonusIconName = "b_icon_suicide";
        }
    }
}
