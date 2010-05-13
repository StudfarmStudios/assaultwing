using System;

namespace AW2.Game.BonusActions
{
    [GameActionType(2)]
    public class RepairSystemBonusAction : GameAction
    {
        private static readonly TimeSpan UPDATE_CYCLE = TimeSpan.FromSeconds(1);
        private TimeSpan _lastActivatedTime;

        public override void DoAction()
        {
            GiveHealth();
            SetActionMessage();
            base.DoAction();
        }

        public override void Update()
        {
            if (AssaultWing.Instance.DataEngine.ArenaTotalTime >= _lastActivatedTime + UPDATE_CYCLE)
                GiveHealth();
        }

        private void GiveHealth()
        {
            _lastActivatedTime = AssaultWing.Instance.DataEngine.ArenaTotalTime;
            float healthBonus = Player.Ship.MaxDamageLevel * -0.05f;
            Player.Ship.InflictDamage(healthBonus, new DeathCause());
        }

        private void SetActionMessage()
        {
            BonusText = "repair system";
            BonusIconName = "b_icon_general";
        }
    }
}
