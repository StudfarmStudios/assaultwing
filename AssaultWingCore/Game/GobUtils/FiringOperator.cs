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
        private TimeSpan _loadedTime;
        private ShipDevice _device;
        public int _shotsLeft;

        public bool IsItTimeToShoot { get { return _nextShot <= _device.Arena.TotalTime && _shotsLeft > 0; } }
        public float VisualChargeUsage { get { return _device.FireCharge; } }
        public TimeSpan LoadedTime { get { return _loadedTime; } }
        public bool Loaded { get { return NextFireSkipsLoadAndCharge || _loadedTime <= _device.Arena.TotalTime; } }
        public bool Charged { get { return NextFireSkipsLoadAndCharge || _device.FireCharge <= _device.Charge; } }
        public bool NextFireSkipsLoadAndCharge { get; set; }
        public bool ThisFireSkipsLoadReset { get; set; }
        public bool IsFiring { get; private set; }

        public FiringOperator(ShipDevice device)
        {
            _device = device;
        }

        public void StartFiring()
        {
            if (_nextShot < _device.Arena.TotalTime) _nextShot = _device.Arena.TotalTime; // Load time doesn't pile up
            if (!NextFireSkipsLoadAndCharge)
            {
                _device.Charge -= _device.FireCharge;
                _loadedTime = TimeSpan.MaxValue; // Make the weapon unloaded for eternity until someone calls DoneFiring()
            }
            _shotsLeft = _device.ShotCount;
            NextFireSkipsLoadAndCharge = false;
            IsFiring = true;
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

        public void DoneFiring()
        {
            if (!IsFiring) return;
            IsFiring = false;
            _shotsLeft = 0;
            if (!ThisFireSkipsLoadReset)
                _loadedTime = _device.Arena.TotalTime + TimeSpan.FromSeconds(_device.LoadTime * _device.LoadTimeMultiplier);
            ThisFireSkipsLoadReset = false;
        }
    }
}
