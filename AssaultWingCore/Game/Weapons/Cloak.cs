using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using System.Collections.Generic;

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

        private bool _active;
        private bool _weaponFiredHandlerAdded;

        private IEnumerable<Gobs.Peng> OwnersPengs { get { return Owner.Arena.Gobs.OfType<Gobs.Peng>().Where(p => p.Leader == Owner); } }

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
        }

        public Cloak(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Dispose()
        {
            PlayerOwner.WeaponFired -= WeaponFiredHandler;
            base.Dispose();
        }

        protected override void ShootImpl()
        {
            if (_active) DeactivateCloak();
            else ActivateCloak();
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
            Owner.Owner.ShowName = false;
            foreach (var peng in OwnersPengs) peng.Emitter.Pause();
            if (Owner.Game.NetworkMode != Core.NetworkMode.Client)
                PlayerOwner.Messages.Add(new PlayerMessage("Activ8td", PlayerMessage.DEFAULT_COLOR));
        }

        private void DeactivateCloak()
        {
            _active = false;
            Owner.Owner.ShowName = true;
            foreach (var peng in OwnersPengs) peng.Emitter.Resume();
            Owner.Alpha = 1;
        }

        private void WeaponFiredHandler()
        {
            if (_active) DeactivateCloak();
        }
    }
}
