using System;
using AW2.Game.GobUtils;
using AW2.Helpers;

namespace AW2.Game.BonusActions
{
    [GameActionType(3)]
    public class SelfDestructBonusAction : GameAction
    {
        private static readonly CanonicalString EFFECT_NAME = (CanonicalString)"bomber_rage";

        public override void RemoveAction()
        {
            Player.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, Player.Weapon2Name);
            Player.PostprocessEffectNames.Remove(EFFECT_NAME);
        }

        public override bool DoAction()
        {
            UpgradeWeapon();
            SetActionMessage();
            return base.DoAction();
        }

        private void UpgradeWeapon()
        {
            if (Player.Ship == null) return;
            Player.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, (CanonicalString)"selfdestructship");
            Player.PostprocessEffectNames.EnsureContains(EFFECT_NAME);
        }

        private void SetActionMessage()
        {
            BonusText = "suicide bomber";
            BonusIconName = "b_icon_suicide";
        }
    }
}
