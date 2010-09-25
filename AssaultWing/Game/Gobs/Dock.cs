using System;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Sound;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A docking platform.
    /// </summary>
    public class Dock : Gob
    {
        #region Dock fields

        private static readonly TimeSpan DOCK_SOUND_STOP_DELAY = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// Speed of repairing damageable gobs, measured in damage/second.
        /// Use a negative value for repairing, positive for damaging.
        /// </summary>
        [TypeParameter]
        private float _repairSpeed;

        /// <summary>
        /// Speed of charging primary weapons of ships, measured in charge/second.
        /// </summary>
        [TypeParameter]
        private float _weapon1ChargeSpeed;

        /// <summary>
        /// Speed of charging secondary weapons of ships, measured in charge/second.
        /// </summary>
        [TypeParameter]
        private float _weapon2ChargeSpeed;

        private TimeSpan _lastDockSoundTime;
        private SoundInstance _dockSound;

        #endregion Dock fields

        private bool MustBeSilent
        {
            get { return _lastDockSoundTime < Arena.TotalTime - DOCK_SOUND_STOP_DELAY; }
        }

        /// This constructor is only for serialisation.
        public Dock()
        {
            _repairSpeed = -10;
            _weapon1ChargeSpeed = 100;
            _weapon2ChargeSpeed = 100;
        }

        public Dock(CanonicalString typeName)
            : base(typeName)
        {
            movable = false;
        }

        public override void Activate()
        {
            base.Activate();
            _dockSound = Game.SoundEngine.CreateSound("HomeBaseLoop");
        }

        public override void Update()
        {
            base.Update();
            if (MustBeSilent) _dockSound.Stop();
        }

        public static readonly TimeSpan UNDAMAGED_TIME_REQUIRED = TimeSpan.FromSeconds(5);

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            // We assume we have only one Receptor collision area which handles docking.
            // Then 'theirArea.Owner' must be damageable.
            if (myArea.Name == "Dock")
            {
                Ship ship = theirArea.Owner as Ship;
                bool canDock = false;

                // If the ship is not null and the player hasn't taken damage for long enough time then set canDock to true (player can dock)
                if (ship != null)
                {
                    TimeSpan totalGameTime = Game.DataEngine.ArenaTotalTime;

                    if (ship.LastDamageTaken == TimeSpan.Zero || 
                        totalGameTime - ship.LastDamageTaken > UNDAMAGED_TIME_REQUIRED)
                    {
                        canDock = true;
                    }
                }

                if (ship != null && theirArea.Owner.Owner != null && canDock) EnsureDockSoundPlaying();                

                if (ship != null && canDock)
                {
                    ship.InflictDamage(Game.PhysicsEngine.ApplyChange(_repairSpeed, Game.GameTime.ElapsedGameTime), new DeathCause());
                    ship.ExtraDevice.Charge += Game.PhysicsEngine.ApplyChange(_weapon1ChargeSpeed, Game.GameTime.ElapsedGameTime);
                    ship.Weapon2.Charge += Game.PhysicsEngine.ApplyChange(_weapon2ChargeSpeed, Game.GameTime.ElapsedGameTime);
                }
            }
        }

        public override void Dispose()
        {
            _dockSound.Dispose();
            base.Dispose();
        }

        private void EnsureDockSoundPlaying()
        {
            _lastDockSoundTime = Arena.TotalTime;
            _dockSound.EnsureIsPlaying();
        }
    }
}
