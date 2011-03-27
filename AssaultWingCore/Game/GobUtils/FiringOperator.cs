using System;
using AW2.Core;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Manages load time, charge usage, and timing of multiple shots of a <see cref="ShipDevice"/>.
    /// </summary>
    public class FiringOperator
    {
        private TimeSpan _nextShot;
        private int _shotsLeft;
        private bool _previousCanFire;
        private TimeSpan _loadedTime;
        private ShipDevice _device;

        public bool IsItTimeToShoot { get { return _nextShot <= _device.Arena.TotalTime && _shotsLeft > 0; } }
        public float VisualChargeUsage { get { return _device.FireCharge; } }
        public TimeSpan LoadedTime { get { return _loadedTime; } }
        public bool Loaded { get { return _loadedTime <= _device.Arena.TotalTime; } }
        public bool Charged { get { return _device.FireCharge <= _device.Charge; } }

        public FiringOperator(ShipDevice device)
        {
            _device = device;
            _previousCanFire = true;
        }

        public void StartFiring()
        {
            if (_nextShot < _device.Arena.TotalTime) _nextShot = _device.Arena.TotalTime; // Load time doesn't pile up
            _device.Charge -= _device.FireCharge;
            _loadedTime = TimeSpan.MaxValue; // Make the weapon unloaded for eternity until someone calls DoneFiring()
            _shotsLeft = _device.ShotCount;
        }

        public void Update()
        {
            if (_device.Owner.Game.NetworkMode != NetworkMode.Client &&
                _device.OwnerHandle != ShipDevice.OwnerHandleType.PrimaryWeapon)
            {
                if (Loaded && Charged && !_previousCanFire)
                    _device.PlayerOwner.Messages.Add(new PlayerMessage(_device.TypeName + " ready to use", PlayerMessage.PLAYER_STATUS_COLOR));
                _previousCanFire = Loaded && Charged;
            }
        }

        public void ShotFired()
        {
            _nextShot += TimeSpan.FromSeconds(_device.ShotSpacing);
            --_shotsLeft;
            if (_shotsLeft == 0) DoneFiring();
        }

        public void UseChargeForOneFrame()
        {
            _device.Charge -= _device.FireChargePerSecond * (float)_device.Owner.Game.GameTime.ElapsedGameTime.TotalSeconds;
        }

        private void DoneFiring()
        {
            _loadedTime = _device.Arena.TotalTime + TimeSpan.FromSeconds(_device.LoadTime * _device.LoadTimeMultiplier);
        }
    }
}
