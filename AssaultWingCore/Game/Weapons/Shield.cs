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
        private static readonly TimeSpan BLOCK_EFFECT_INTERVAL_MIN = TimeSpan.FromSeconds(0.2);
        [TypeParameter]
        private TimeSpan _protectionTime;
        [TypeParameter]
        private float _receivedDamageMultiplier;
        [TypeParameter]
        private float _chargeUsageMultiplier;
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _activationPengs;
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _blockFailPengs;
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _blockSuccessPengs;
        [TypeParameter]
        private string _blockSuccessSound;
        [TypeParameter]
        private string _blockFailSound;

        /// <summary>
        /// Time from which on the shield is inactive, in game time.
        /// </summary>
        private TimeSpan _inactivationTime;

        private TimeSpan _lastFailEffectTime;
        private TimeSpan _lastSuccessEffectTime;

        public Ship ShipOwner { get { return Owner as Ship; } }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Shield()
        {
            _protectionTime = TimeSpan.FromSeconds(1);
            _receivedDamageMultiplier = 0.1f;
            _chargeUsageMultiplier = 1;
            _activationPengs = new[] { (CanonicalString)"dummypeng" };
            _blockFailPengs = new[] { (CanonicalString)"dummypeng" };
            _blockSuccessPengs = new[] { (CanonicalString)"dummypeng" };
            _blockSuccessSound = "dummysound";
            _blockFailSound = "dummysound";
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
            GobHelper.CreatePengs(_activationPengs, Owner);
        }

        private float ReceivingDamageHandler(float damageAmount, DamageInfo cause)
        {
            var requiredCharge = _chargeUsageMultiplier * damageAmount;
            if (_inactivationTime <= Owner.Game.GameTime.TotalGameTime) return damageAmount;
            if (Charge < requiredCharge)
            {
                CreateBlockEffect(_blockFailSound, _blockFailPengs, ref _lastFailEffectTime);
                return damageAmount;
            }
            Charge -= requiredCharge;
            CreateBlockEffect(_blockSuccessSound, _blockSuccessPengs, ref _lastSuccessEffectTime);
            return _receivedDamageMultiplier * damageAmount;
        }

        private void CreateBlockEffect(string sound, CanonicalString[] pengs, ref TimeSpan lastEffectTime)
        {
            if (lastEffectTime + BLOCK_EFFECT_INTERVAL_MIN > Owner.Game.GameTime.TotalGameTime) return;
            lastEffectTime = Owner.Game.GameTime.TotalGameTime;
            GobHelper.CreatePengs(pengs, Owner);
            Owner.Game.SoundEngine.PlaySound(sound, Owner);
        }
    }
}
