using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game.GobUtils;
using AW2.Game.Gobs;
using AW2.Helpers.Serialization;
using AW2.Helpers;

namespace AW2.Game.Weapons
{
    public class Shield : ShipDevice
    {
        [TypeParameter]
        private TimeSpan _protectionTime;
        [TypeParameter]
        private float _receivedDamageMultiplier;
        [TypeParameter]
        private float _chargeUsageMultiplier;
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _particleEngineNames;

        /// <summary>
        /// Time from which on the shield is inactive, in game time.
        /// </summary>
        private TimeSpan _inactivationTime;

        public Ship ShipOwner { get { return Owner as Ship; } }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Shield()
        {
            _protectionTime = TimeSpan.FromSeconds(1);
            _receivedDamageMultiplier = 0.1f;
            _chargeUsageMultiplier = 1;
            _particleEngineNames = new[] { (CanonicalString)"dummypeng" };
        }

        public Shield(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            base.Activate();
            ShipOwner.ReceivingDamage += ReceivingDamageHandler;
        }

        protected override void ShootImpl()
        {
            _inactivationTime = Owner.Game.GameTime.TotalGameTime + _protectionTime;
        }

        protected override void CreateVisuals()
        {
            GobHelper.CreatePengs(_particleEngineNames, Owner);
        }

        private float ReceivingDamageHandler(float damageAmount, DamageInfo cause)
        {
            var requiredCharge = _chargeUsageMultiplier * damageAmount;
            if (_inactivationTime <= Owner.Game.GameTime.TotalGameTime || Charge < requiredCharge) return damageAmount;
            Charge -= requiredCharge;
            return _receivedDamageMultiplier * damageAmount;
        }
    }
}
