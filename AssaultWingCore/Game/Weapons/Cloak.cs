using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Sound;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Cloaks the shooter so that other players can't see him so easily.
    /// </summary>
    public class Cloak : ShipDevice
    {
        [TypeParameter]
        private TimeSpan _fadeOutDuration;
        [TypeParameter]
        private TimeSpan _fadeInDuration;

        /// <summary>
        /// Values are between 0 (totally visible) and 1 (totally invisible).
        /// Argument is ship velocity in m/s (i.e. px/s).
        /// </summary>
        [TypeParameter]
        private Curve _cloakStrengthForVelocity;

        [TypeParameter]
        private string _runningSoundName;
        [TypeParameter]
        private string _breakOutSoundName;

        private bool _active;
        private bool _weaponFiredHandlerAdded;
        private bool _applyAlpha;
        private TimeSpan _fadeStartTime;
        private TimeSpan _deactivationSafetyTimeout;
        private SoundInstance _runningSound;

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Cloak()
        {
            _fadeOutDuration = TimeSpan.FromSeconds(1.5);
            _fadeInDuration = TimeSpan.FromSeconds(0.5);
            _cloakStrengthForVelocity = new Curve();
            _cloakStrengthForVelocity.Keys.Add(new CurveKey(0, 0.99f));
            _cloakStrengthForVelocity.Keys.Add(new CurveKey(100, 0.95f));
            _cloakStrengthForVelocity.Keys.Add(new CurveKey(400, 0.75f));
            _cloakStrengthForVelocity.ComputeTangents(CurveTangent.Linear);
            _cloakStrengthForVelocity.PreLoop = CurveLoopType.Constant;
            _cloakStrengthForVelocity.PostLoop = CurveLoopType.Constant;
            _runningSoundName = "dummysound";
            _breakOutSoundName = "angry";
        }

        public Cloak(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            base.Activate();
            _runningSound = Owner.Game.SoundEngine.CreateSound(_runningSoundName, Owner);
        }

        public override void Dispose()
        {
            PlayerOwner.WeaponFired -= WeaponFiredHandler;
            if (_runningSound != null)
            {
                _runningSound.Dispose();
                _runningSound = null;
            }
            base.Dispose();
        }

        protected override bool PermissionToFire()
        {
            return !_active || Owner.Arena.TotalTime > _deactivationSafetyTimeout;
        }

        protected override void ShootImpl()
        {
            if (_active)
            {
                DeactivateCloak();
                FiringOperator.ThisFireSkipsLoadReset = true;
            }
            else
                ActivateCloak();
        }

        public override void Update()
        {
            base.Update();
            if (_active)
            {
                var fadeAlphaMultiplier = MathHelper.Clamp((Owner.Arena.TotalTime - _fadeStartTime).Divide(_fadeOutDuration), 0, 1);
                Owner.Alpha = 1 - fadeAlphaMultiplier * _cloakStrengthForVelocity.Evaluate(Owner.Move.Length());
                FiringOperator.UseChargeForOneFrame();
                if (Charge == 0) DeactivateCloak();
            }
            else if (_applyAlpha)
            {
                var fadeAlphaMultiplier = 1 - MathHelper.Clamp((Owner.Arena.TotalTime - _fadeStartTime).Divide(_fadeInDuration), 0, 1);
                Owner.Alpha = 1 - fadeAlphaMultiplier * _cloakStrengthForVelocity.Evaluate(Owner.Move.Length());
                if (fadeAlphaMultiplier == 0) _applyAlpha = false;
            }
        }

        private void ActivateCloak()
        {
            if (!_weaponFiredHandlerAdded) PlayerOwner.WeaponFired += WeaponFiredHandler;
            _weaponFiredHandlerAdded = true;
            _active = true;
            Owner.IsHiding = true;
            if (Owner.Game.NetworkMode != Core.NetworkMode.Client)
                PlayerOwner.Messages.Add(new PlayerMessage("Aktv8td", PlayerMessage.DEFAULT_COLOR));
            FiringOperator.NextFireSkipsLoadAndCharge = true;
            _runningSound.EnsureIsPlaying();
            _fadeStartTime = Owner.Arena.TotalTime;
            _deactivationSafetyTimeout = Owner.Arena.TotalTime + _fadeOutDuration;
            _applyAlpha = true;
            Owner.Game.Stats.Send(new { Cloaked = PlayerOwner.LoginToken, Role = OwnerHandle, Type = TypeName.Value });
        }

        private void DeactivateCloak()
        {
            _active = false;
            Owner.IsHiding = false;
            FiringOperator.NextFireSkipsLoadAndCharge = false;
            _runningSound.Stop();
            _fadeStartTime = Owner.Arena.TotalTime;
            Owner.Game.Stats.Send(new { Uncloaked = PlayerOwner.LoginToken });
        }

        private void WeaponFiredHandler()
        {
            if (!_active) return;
            DeactivateCloak();
            Owner.Game.SoundEngine.PlaySound(_breakOutSoundName, Owner);
        }
    }
}
