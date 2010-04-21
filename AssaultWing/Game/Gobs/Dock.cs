using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using Microsoft.Xna.Framework.Audio;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A docking platform.
    /// </summary>
    public class Dock : Gob
    {
        #region Dock fields

        static readonly TimeSpan DOCK_SOUND_STOP_DELAY = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// Speed of repairing damageable gobs, measured in damage/second.
        /// Use a negative value for repairing, positive for damaging.
        /// </summary>
        [TypeParameter]
        float repairSpeed;

        /// <summary>
        /// Speed of charging primary weapons of ships, measured in charge/second.
        /// </summary>
        [TypeParameter]
        float weapon1ChargeSpeed;

        /// <summary>
        /// Speed of charging secondary weapons of ships, measured in charge/second.
        /// </summary>
        [TypeParameter]
        float weapon2ChargeSpeed;

        TimeSpan _lastDockSoundTime;
        Cue _dockSoundCue;

        #endregion Dock fields

        private bool MustBeSilent
        {
            get { return _lastDockSoundTime < AssaultWing.Instance.GameTime.TotalArenaTime - DOCK_SOUND_STOP_DELAY; }
        }

        /// This constructor is only for serialisation.
        public Dock()
            : base()
        {
            this.repairSpeed = -10;
            this.weapon1ChargeSpeed = 100;
            this.weapon2ChargeSpeed = 100;
        }

        public Dock(CanonicalString typeName)
            : base(typeName)
        {
            movable = false;
        }

        public override void Update()
        {
            base.Update();
            if (MustBeSilent) EnsureDockSoundStopped();
        }

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            // We assume we have only one Receptor collision area which handles docking.
            // Then 'theirArea.Owner' must be damageable.
            if (myArea.Name == "Dock")
            {
                EnsureDockSoundPlaying();
                theirArea.Owner.InflictDamage(AssaultWing.Instance.PhysicsEngine.ApplyChange(repairSpeed, AssaultWing.Instance.GameTime.ElapsedGameTime), new DeathCause());
                Ship ship = theirArea.Owner as Ship;
                if (ship != null)
                {
                    ship.ExtraDevice.Charge += AssaultWing.Instance.PhysicsEngine.ApplyChange(weapon1ChargeSpeed, AssaultWing.Instance.GameTime.ElapsedGameTime);
                    ship.Weapon2.Charge += AssaultWing.Instance.PhysicsEngine.ApplyChange(weapon2ChargeSpeed, AssaultWing.Instance.GameTime.ElapsedGameTime);
                }
            }
        }

        public override void Dispose()
        {
            if (_dockSoundCue != null)
            {
                _dockSoundCue.Dispose();
                _dockSoundCue = null;
            }
            base.Dispose();
        }

        private void EnsureDockSoundPlaying()
        {
            _lastDockSoundTime = AssaultWing.Instance.GameTime.TotalArenaTime;
            if (_dockSoundCue != null && _dockSoundCue.IsPlaying) return;
            if (_dockSoundCue != null) _dockSoundCue.Dispose();
            _dockSoundCue = AssaultWing.Instance.SoundEngine.GetCue("Docking");
            _dockSoundCue.Play();
        }

        private void EnsureDockSoundStopped()
        {
            if (_dockSoundCue != null && _dockSoundCue.IsPlaying) _dockSoundCue.Stop(AudioStopOptions.AsAuthored);
        }
    }
}
