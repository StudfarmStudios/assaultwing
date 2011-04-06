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
        /// <summary>
        /// Values are between 0 (totally visible) and 1 (totally invisible).
        /// Argument is ship velocity in m/s (i.e. px/s).
        /// </summary>
        [TypeParameter]
        private Curve _cloakStrengthForVelocity;

        [TypeParameter]
        private string _runningSoundName;

        private bool _active;
        private bool _weaponFiredHandlerAdded;
        private SoundInstance _runningSound;

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Cloak()
        {
            _cloakStrengthForVelocity = new Curve();
            _cloakStrengthForVelocity.Keys.Add(new CurveKey(0, 0.99f));
            _cloakStrengthForVelocity.Keys.Add(new CurveKey(100, 0.95f));
            _cloakStrengthForVelocity.Keys.Add(new CurveKey(400, 0.75f));
            _cloakStrengthForVelocity.ComputeTangents(CurveTangent.Linear);
            _cloakStrengthForVelocity.PreLoop = CurveLoopType.Constant;
            _cloakStrengthForVelocity.PostLoop = CurveLoopType.Constant;
            _runningSoundName = "dummysound";
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

        protected override void CreateVisualsImpl()
        {
        }

        public override void Update()
        {
            base.Update();
            if (_active)
            {
                Owner.Alpha = 1 - _cloakStrengthForVelocity.Evaluate(Owner.Move.Length());
                FiringOperator.UseChargeForOneFrame();
                if (Charge == 0) DeactivateCloak();
            }
        }

        private void ActivateCloak()
        {
            if (!_weaponFiredHandlerAdded) PlayerOwner.WeaponFired += WeaponFiredHandler;
            _weaponFiredHandlerAdded = true;
            _active = true;
            Owner.IsHidden = true;
            if (Owner.Game.NetworkMode != Core.NetworkMode.Client)
                PlayerOwner.Messages.Add(new PlayerMessage("Aktv8td", PlayerMessage.DEFAULT_COLOR));
            FiringOperator.NextFireSkipsLoadAndCharge = true;
            _runningSound.EnsureIsPlaying();
        }

        private void DeactivateCloak()
        {
            _active = false;
            Owner.IsHidden = false;
            Owner.Alpha = 1;
            FiringOperator.NextFireSkipsLoadAndCharge = false;
            _runningSound.Stop();
        }

        private void WeaponFiredHandler()
        {
            if (!_active) return;
            DeactivateCloak();
            Owner.Game.SoundEngine.PlaySound("angry", Owner);
        }
    }
}
