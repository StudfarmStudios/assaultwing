using System;
using AW2.Helpers;

namespace AW2.Game.BonusActions
{
    [GameActionType(5)]
    public class Weapon2UpgradeBonusAction : GameAction
    {
        public override void DoAction()
        {
            var weapon2 = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(Player.Ship.Weapon2Name);
            if (weapon2.UpgradeNames != null && weapon2.UpgradeNames.Length > 0)
            {
                var weaponUpgrade = weapon2.UpgradeNames[0];
                UpgradeWeapon(weaponUpgrade);
            }
            SetActionMessage();
            base.DoAction();
        }

        public override void RemoveAction()
        {
            Player.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, Player.Weapon2Name);
        }

        private void UpgradeWeapon(CanonicalString weaponUpgrade)
        {
            Player.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, weaponUpgrade);
        }

        private void SetActionMessage()
        {
            BonusText = Player.Ship.Weapon2Name;
            BonusIconName = Player.Ship.Weapon2.IconName;
        }
    }
}
