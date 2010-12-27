using System;
using AW2.Core;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Makes a device fire separate bursts.
    /// </summary>
    public class FiringOperatorSingle : FiringOperator
    {
        private int _shotsLeft;
        private bool _previousCanFire;
        private TimeSpan _loadedTime;

        public override bool Loaded { get { return _loadedTime <= Device.Arena.TotalTime; } }
        public override bool CanFire { get { return Loaded && Device.FireCharge <= Device.Charge; } }
        public override bool IsItTimeToShoot { get { return base.IsItTimeToShoot && _shotsLeft > 0; } }
        public override float VisualChargeUsage { get { return Device.FireCharge; } }
        public override TimeSpan LoadedTime { get { return _loadedTime; } }

        public FiringOperatorSingle(ShipDevice device)
            : base(device)
        {
            _previousCanFire = true;
        }

        public override bool IsFirePressed(AW2.UI.ControlState triggerState)
        {
            return triggerState.Pulse;
        }

        public override bool TryFire()
        {
            if (!CanFire) return false;
            if (!base.TryFire()) return false;
            Device.Charge -= Device.FireCharge;
            // Make the weapon unloaded for eternity until someone calls DoneFiring()
            _loadedTime = TimeSpan.MaxValue;
            _shotsLeft = Device.ShotCount;
            return true;
        }

        public override void Update()
        {
            if (Device.Owner.Game.NetworkMode != NetworkMode.Client &&
                Device.OwnerHandle != ShipDevice.OwnerHandleType.PrimaryWeapon)
            {
                if (CanFire && !_previousCanFire)
                    Device.PlayerOwner.SendMessage(new PlayerMessage(Device.TypeName + " ready to use", Player.PLAYER_STATUS_COLOR));
                _previousCanFire = CanFire;
            }
        }

        public override void ShotFired()
        {
            base.ShotFired();
            --_shotsLeft;
            if (_shotsLeft == 0) DoneFiring();
        }

        private void DoneFiring()
        {
            _loadedTime = Device.Arena.TotalTime + TimeSpan.FromSeconds(Device.LoadTime * Device.LoadTimeMultiplier);
        }
    }
}
