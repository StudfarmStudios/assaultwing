using System.Linq;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Cloaks the shooter so that other players can't see him so easily.
    /// </summary>
    public class Cloak : ShipDevice
    {
        /// <summary>
        /// Between 0 (totally visible) and 1 (totally invisible).
        /// </summary>
        [TypeParameter]
        private float _cloakStrength;

        private bool _active;

        private new FiringOperatorContinuous FiringOperator {
            get { return (FiringOperatorContinuous)base.FiringOperator; }
            set { base.FiringOperator = value; }
        }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Cloak()
        {
            _cloakStrength = 0.9f;
        }

        public Cloak(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            FiringOperator = new FiringOperatorContinuous(this);
            PlayerOwner.WeaponFired += WeaponFiredHandler;
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
                FiringOperator.UseChargeForOneFrame();
                if (Charge == 0) DeactivateCloak();
            }
        }

        private void ActivateCloak()
        {
            _active = true;
            Owner.Alpha = 1 - _cloakStrength; // TODO: Alter ship alpha based on ship velocity
        }

        private void DeactivateCloak()
        {
            _active = false;
            Owner.Alpha = 1;
        }

        private void WeaponFiredHandler()
        {
            if (_active) DeactivateCloak();
        }
    }
}
