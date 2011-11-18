using System;
using System.Collections.Generic;
using AW2.Core;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.BonusActions
{
    public class DamageBuffBonusAction : Gobs.BonusAction
    {
        [TypeParameter]
        private string _buffName; // not CanonicalString because this doesn't contain any "well known string" such as a content texture name

        [TypeParameter]
        private CanonicalString _bonusIconName;

        [TypeParameter]
        private float _damagePerSecond;

        [TypeParameter]
        private CanonicalString _pengTypeName;

        private List<Gobs.Peng> _myPengs;

        public override string BonusText { get { return _buffName; } }
        public override CanonicalString BonusIconName { get { return _bonusIconName; } }
        public Gob Cause { get; set; }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public DamageBuffBonusAction()
        {
            _buffName = "dummy damage buff";
            _bonusIconName = (CanonicalString)"dummytexture";
            _damagePerSecond = 500;
            _pengTypeName = (CanonicalString)"dummypeng";
        }

        public DamageBuffBonusAction(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            base.Activate();
            if (_pengTypeName != "" && _myPengs == null && Host != null)
                _myPengs = GobHelper.CreatePengs(new[] { _pengTypeName }, Host);
        }

        public override void Dispose()
        {
            if (_myPengs != null) foreach (var peng in _myPengs) peng.Die();
            base.Dispose();
        }

        public override void Update()
        {
            base.Update();
            float damage = Game.PhysicsEngine.ApplyChange(_damagePerSecond, Game.GameTime.ElapsedGameTime);
            if (Host != null)
            {
                if (damage > 0)
                    Host.InflictDamage(damage, new DamageInfo(Cause));
                else
                    Host.RepairDamage(-damage);
            }
        }
    }
}
