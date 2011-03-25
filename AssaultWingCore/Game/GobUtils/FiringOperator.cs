using System;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Controls firing of a <see cref="ShipDevice"/>. Successful firing requires
    /// that the device is loaded and it has enough charge.
    /// </summary>
    public abstract class FiringOperator
    {
        protected TimeSpan _nextShot;

        public abstract bool Loaded { get; }
        public abstract bool CanFire { get; }
        public virtual bool IsItTimeToShoot { get { return _nextShot <= Device.Arena.TotalTime; } }

        /// <summary>
        /// Amount of charge usage for visualisations.
        /// </summary>
        public abstract float VisualChargeUsage { get; }

        /// <summary>
        /// Time from which on the weapon is loaded, in game time.
        /// </summary>
        public abstract TimeSpan LoadedTime { get; }

        /// <summary>
        /// The <see cref="ShipDevice"/> this instance is attached to.
        /// </summary>
        protected ShipDevice Device { get; private set; }

        protected FiringOperator(ShipDevice device)
        {
            Device = device;
        }

        public abstract bool IsFirePressed(AW2.UI.ControlState triggerState);

        /// <summary>
        /// Returns true on successful firing.
        /// Returns false if firing failed.
        /// </summary>
        public virtual bool TryFire()
        {
            // Load time doesn't pile up
            if (_nextShot < Device.Arena.TotalTime)
                _nextShot = Device.Arena.TotalTime;
            return true;
        }

        public virtual void Update() { }

        public virtual void ShotFired()
        {
            _nextShot += TimeSpan.FromSeconds(Device.ShotSpacing);
        }

        public void UseChargeForOneFrame()
        {
            Device.Charge -= Device.FireChargePerSecond * (float)Device.Owner.Game.GameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}
