using System;

namespace AW2.Game.GobUtils
{
    public class FiringOperatorContinuous : FiringOperator
    {
        private bool _fireThisFrame;

        public override bool Loaded { get { return true; } }
        public override bool CanFire
        {
            get
            {
                float neededCharge = Device.FireChargePerSecond * (float)Device.Owner.Game.GameTime.ElapsedGameTime.TotalSeconds;
                return Loaded && neededCharge <= Device.Charge;
            }
        }
        public override bool IsItTimeToShoot { get { return _fireThisFrame && base.IsItTimeToShoot; } }
        public override float VisualChargeUsage { get { return 0; } }
        public override TimeSpan LoadedTime { get { return TimeSpan.Zero; } }

        public FiringOperatorContinuous(ShipDevice device)
            : base(device)
        {
        }

        public override bool IsFirePressed(AW2.UI.ControlState triggerState)
        {
            return triggerState.Force > 0;
        }

        public override bool TryFire()
        {
            if (!CanFire) return true;
            if (!base.TryFire()) return false;
            _fireThisFrame = true;
            Device.Charge -= Device.FireChargePerSecond * (float)Device.Owner.Game.GameTime.ElapsedGameTime.TotalSeconds;
            return true;
        }

        public override void Update()
        {
            base.Update();
            _fireThisFrame = false;
        }
    }
}
