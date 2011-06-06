using System;
using System.Collections.Generic;
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
        private static readonly TimeSpan DOCK_EFFECT_STOP_DELAY = TimeSpan.FromSeconds(0);
        public static readonly TimeSpan UNDAMAGED_TIME_REQUIRED = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan PACIFIST_TIME_REQUIRED = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Speed of repairing damageable gobs, measured in repaired damage/second.
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

        [TypeParameter]
        private CanonicalString _dockIdleEffectName;
        [TypeParameter]
        private CanonicalString _dockActiveEffectName;

        private TimeSpan _lastDockSoundTime, _lastDockEffectTime;
        private SoundInstance _chargingSound, _dockSound;
        private List<Peng> _dockIdleEffects;
        private List<Peng> _dockActiveEffects;
        private bool _effectStateActive; // true = active; false = idle

        #endregion Dock fields

        private bool IsSoundIdle
        {
            get { return _lastDockSoundTime < Arena.TotalTime - DOCK_SOUND_STOP_DELAY; }
        }

        private bool IsEffectIdle
        {
            get { return _lastDockEffectTime < Arena.TotalTime - DOCK_EFFECT_STOP_DELAY; }
        }

        /// This constructor is only for serialisation.
        public Dock()
        {
            _repairSpeed = -10;
            _weapon1ChargeSpeed = 100;
            _weapon2ChargeSpeed = 100;
            _dockIdleEffectName = (CanonicalString)"dummypeng";
            _dockActiveEffectName = (CanonicalString)"dummypeng";
        }

        public Dock(CanonicalString typeName)
            : base(typeName)
        {
            Movable = false;
            Gravitating = false;
        }

        public override void Activate()
        {
            base.Activate();
            CreateDockEffects();
            _chargingSound = Game.SoundEngine.CreateSound("HomeBaseLoop", this);
            _dockSound = Game.SoundEngine.CreateSound("HomeBaseLoopLow", this);
        }

        public override void Update()
        {
            base.Update();
            if (IsSoundIdle) _chargingSound.Stop();
            if (IsEffectIdle) EnsureEffectIdle();
            _dockSound.EnsureIsPlaying();
        }

        public override Arena.CollisionSideEffectType Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck, Arena.CollisionSideEffectType sideEffectTypes)
        {
            var result = Arena.CollisionSideEffectType.None;
            // We assume we have only one Receptor collision area which handles docking.
            // Then 'theirArea.Owner' must be damageable.
            if (myArea.Name == "Dock")
            {
                var ship = theirArea.Owner as Ship;
                if (ship != null)
                {
                    var canRepair = CanRepair(ship);
                    if ((sideEffectTypes & Arena.CollisionSideEffectType.Reversible) != 0)
                    {
                        EnsureEffectActive();
                        if (canRepair) RepairShip(ship);
                        result |= Arena.CollisionSideEffectType.Reversible;
                    }
                    if ((sideEffectTypes & Arena.CollisionSideEffectType.Irreversible) != 0)
                    {
                        if (!canRepair && ship.Owner != null)
                        {
                            ship.Owner.NotifyRepairPending();
                            result |= Arena.CollisionSideEffectType.Irreversible;
                        }
                    }
                }
            }
            return result;
        }

        public override void Dispose()
        {
            if (_chargingSound != null)
            {
                _chargingSound.Dispose();
                _chargingSound = null;
            }
            if (_dockSound != null)
            {
                _dockSound.Dispose();
                _dockSound = null;
            }
            base.Dispose();
        }

        private void CreateDockEffects()
        {
            _dockActiveEffects = new List<Peng>();
            _dockIdleEffects = new List<Peng>();
            var boneIndices = GetNamedPositions("DockEffect");
            Action<CanonicalString, int, List<Peng>> createEffect = (name, boneIndex, storage) =>
                Gob.CreateGob<Peng>(Game, name, gob =>
                {
                    gob.Leader = this;
                    gob.LeaderBone = boneIndex;
                    Arena.Gobs.Add(gob);
                    storage.Add(gob);
                });
            foreach (var boneIndex in boneIndices)
            {
                createEffect(_dockActiveEffectName, boneIndex.Item2, _dockActiveEffects);
                createEffect(_dockIdleEffectName, boneIndex.Item2, _dockIdleEffects);
            }
            foreach (var activeEffect in _dockActiveEffects)
                activeEffect.Emitter.Pause();
        }

        private void EnsureDockSoundPlaying()
        {
            _lastDockSoundTime = Arena.TotalTime;
            _chargingSound.EnsureIsPlaying();
        }

        private void EnsureEffectActive()
        {
            _lastDockEffectTime = Arena.TotalTime;
            if (!_effectStateActive)
            {
                foreach (var peng in _dockIdleEffects) peng.Emitter.Pause();
                foreach (var peng in _dockActiveEffects) peng.Emitter.Resume();
            }
            _effectStateActive = true;
        }

        private void EnsureEffectIdle()
        {
            if (_effectStateActive)
            {
                foreach (var peng in _dockIdleEffects) peng.Emitter.Resume();
                foreach (var peng in _dockActiveEffects) peng.Emitter.Pause();
            }
            _effectStateActive = false;
        }

        private bool CanRepair(Ship ship)
        {
            return Game.DataEngine.ArenaTotalTime - ship.LastDamageTakenTime > UNDAMAGED_TIME_REQUIRED
                && Game.DataEngine.ArenaTotalTime - ship.LastWeaponFiredTime > PACIFIST_TIME_REQUIRED;
        }

        private bool IsFullyRepaired(Ship ship)
        {
            return ship.DamageLevel == 0
                && ship.ExtraDevice.Charge == ship.ExtraDevice.ChargeMax
                && ship.Weapon2.Charge == ship.Weapon2.ChargeMax;
        }

        private void RepairShip(Ship ship)
        {
            if (!IsFullyRepaired(ship)) EnsureDockSoundPlaying();
            var elapsedTime = Game.GameTime.ElapsedGameTime;
            ship.RepairDamage(Game.PhysicsEngine.ApplyChange(_repairSpeed, elapsedTime));
            ship.ExtraDevice.Charge += Game.PhysicsEngine.ApplyChange(_weapon1ChargeSpeed, elapsedTime);
            ship.Weapon2.Charge += Game.PhysicsEngine.ApplyChange(_weapon2ChargeSpeed, elapsedTime);
        }
    }
}
