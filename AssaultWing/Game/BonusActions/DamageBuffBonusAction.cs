using System;
using AW2.Game.GobUtils;
using AW2.Helpers;

namespace AW2.Game.BonusActions
{
    [GameActionType(2)]
    [LimitedSerialization]
    public class DamageBuffBonusAction : GameAction
    {
        [TypeParameter]
        private string _buffName;

        [TypeParameter]
        private float _damagePerSecond;

        /// <summary>
        /// This constructor is only for serialization.
        /// </summary>
        public DamageBuffBonusAction()
        {
            _buffName = "dummy damage buff";
            _damagePerSecond = 500;
        }

        public override void DoAction()
        {
            SetActionMessage();
            base.DoAction();
        }

        public override void Update()
        {
            float damage = AssaultWing.Instance.PhysicsEngine.ApplyChange(_damagePerSecond, AssaultWing.Instance.GameTime.ElapsedGameTime);
            Player.Ship.InflictDamage(damage, new DeathCause(Player.Ship, DeathCauseType.Damage));
        }

        private void SetActionMessage()
        {
            BonusText = _buffName;
            BonusIconName = "b_icon_general";
        }
    }
}
