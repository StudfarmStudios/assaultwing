using System;
using System.Collections.Generic;
using System.Linq;
using FarseerPhysics.Dynamics;
using AW2.Game.Collisions;
using AW2.Game.GobUtils;
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
        private static readonly TimeSpan DOCK_EFFECT_STOP_DELAY = TimeSpan.FromSeconds(0.1);
        public static readonly TimeSpan UNDAMAGED_TIME_REQUIRED = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan PACIFIST_TIME_REQUIRED = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan REPAIR_PENDING_NOTIFY_MIN = TimeSpan.FromSeconds(1);

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

        /// <summary>
        /// Factor of slowdown that is applied to ships that are in need of repair.
        /// Slowdown makes it easier to land on a dock and stay there.
        /// </summary>
        [TypeParameter]
        private float _repairingMoveSlowdownFactor;

        [TypeParameter]
        private CanonicalString _dockIdleEffectName;
        [TypeParameter]
        private CanonicalString _dockActiveEffectName;

        private SoundInstance _chargingSound, _dockSound;
        private List<Peng> _dockIdleEffects;
        private List<Peng> _dockActiveEffects;
        private bool _effectStateActive; // true = active; false = idle
        private Dictionary<Gob, TimeSpan> _lastRepairTimes; // in game time
        private List<LazyProxy<int, Gob>> _repairingGobsOnClient; // used only on game clients
        private CollisionArea _dockArea;

        #endregion Dock fields

        private IEnumerable<Gob> RepairingGobs
        {
            get
            {
                return Game.NetworkMode == Core.NetworkMode.Client
                    ? _repairingGobsOnClient.Select(proxy => proxy.GetValue()).Where(gob => gob != null)
                    : PhysicsHelper.GetContacting(_dockArea).Select(area => area.Owner);
            }
        }

        /// This constructor is only for serialisation.
        public Dock()
        {
            MoveType = MoveType.Static;
            _repairSpeed = -10;
            _weapon1ChargeSpeed = 100;
            _weapon2ChargeSpeed = 100;
            _repairingMoveSlowdownFactor = 0.85f;
            _dockIdleEffectName = (CanonicalString)"dummypeng";
            _dockActiveEffectName = (CanonicalString)"dummypeng";
        }

        public Dock(CanonicalString typeName)
            : base(typeName)
        {
            Gravitating = false;
            _lastRepairTimes = new Dictionary<Gob, TimeSpan>();
            _repairingGobsOnClient = new List<LazyProxy<int, Gob>>();
        }

        public override void Activate()
        {
            base.Activate();
            CreateDockEffects();
            _chargingSound = Game.SoundEngine.CreateSound("homeBaseLoop", this);
            _dockSound = Game.SoundEngine.CreateSound("homeBaseLoopLow", this);
            _dockArea = CollisionAreas.First(area => area.Name == "Dock");
        }

        public override void Update()
        {
            base.Update();
            if (RepairingGobs.Any(gob => _lastRepairTimes.ContainsKey(gob)))
            {
                _chargingSound.EnsureIsPlaying();
                EnsureEffectActive();
            }
            else
            {
                _chargingSound.Stop();
                EnsureEffectIdle();
            }
            _dockSound.EnsureIsPlaying();
            CheckEndedRepairs();
            foreach (var overlapper in RepairingGobs)
                if (overlapper is Ship) RepairShip((Ship)overlapper);
        }

        public override void Dispose()
        {
            if (_chargingSound != null) _chargingSound.Dispose();
            _chargingSound = null;
            if (_dockSound != null) _dockSound.Dispose();
            _dockSound = null;
            base.Dispose();
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                // Dock doesn't move, so Pos, Move and Rotation are needed only at creation.
                var reducedMode = mode.HasFlag(SerializationModeFlags.ConstantDataFromServer)
                    ? SerializationModeFlags.AllFromServer
                    : SerializationModeFlags.None;
                base.Serialize(writer, reducedMode);
                if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
                {
                    writer.Write((byte)_lastRepairTimes.Count);
                    foreach (var item in _lastRepairTimes)
                        writer.Write((short)item.Key.ID);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            var reducedMode = mode.HasFlag(SerializationModeFlags.ConstantDataFromServer)
                ? SerializationModeFlags.AllFromServer
                : SerializationModeFlags.None;
            base.Deserialize(reader, reducedMode, framesAgo);
            if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
            {
                var repairingCount = reader.ReadByte();
                _repairingGobsOnClient.Clear();
                for (int i = 0; i < repairingCount; i++)
                {
                    var proxy = new LazyProxy<int, Gob>(FindGob);
                    proxy.SetData(reader.ReadInt16());
                    _repairingGobsOnClient.Add(proxy);
                }
            }
        }

        private void CheckEndedRepairs()
        {
            if (!_lastRepairTimes.Any()) return; // avoid a couple of needless memory allocations
            var noMoreRepairing =
                (from item in _lastRepairTimes
                 where (Game.GameTime.TotalGameTime - item.Value).Frames() > 1
                 select item.Key).ToArray();
            foreach (var gob in noMoreRepairing)
            {
                _lastRepairTimes.Remove(gob);
                ForcedNetworkUpdate = true;
            }
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

        private void EnsureEffectActive()
        {
            if (_effectStateActive) return;
            foreach (var peng in _dockIdleEffects) peng.Emitter.Pause();
            foreach (var peng in _dockActiveEffects) peng.Emitter.Resume();
            _effectStateActive = true;
        }

        private void EnsureEffectIdle()
        {
            if (!_effectStateActive) return;
            foreach (var peng in _dockIdleEffects) peng.Emitter.Resume();
            foreach (var peng in _dockActiveEffects) peng.Emitter.Pause();
            _effectStateActive = false;
        }

        private bool CanRepair(Ship ship)
        {
            return TimeUntilRepairStarts(ship) == TimeSpan.Zero;
        }

        private bool ShouldNotifyPlayerAboutRepairPending(Ship ship)
        {
            return ship.Owner != null && Game.NetworkMode != Core.NetworkMode.Client
                && TimeUntilRepairStarts(ship) >= REPAIR_PENDING_NOTIFY_MIN;
        }

        private TimeSpan TimeUntilRepairStarts(Ship ship)
        {
            var now = Game.DataEngine.ArenaTotalTime;
            return AWMathHelper.Max(TimeSpan.Zero, AWMathHelper.Max(
                ship.LastDamageTakenTime + UNDAMAGED_TIME_REQUIRED - now,
                ship.LastWeaponFiredTime + PACIFIST_TIME_REQUIRED - now));
        }

        private bool IsFullyRepaired(Ship ship)
        {
            return ship.DamageLevel == 0
                && ship.ExtraDevice.Charge == ship.ExtraDevice.ChargeMax
                && ship.Weapon2.Charge == ship.Weapon2.ChargeMax;
        }

        private void RepairShip(Ship ship)
        {
            if (ShouldNotifyPlayerAboutRepairPending(ship)) ship.Owner.NotifyRepairPending();
            if (IsFullyRepaired(ship)) return;
            ship.Move *= _repairingMoveSlowdownFactor;
            if (!CanRepair(ship)) return;
            if (ship.Owner != null && !_lastRepairTimes.ContainsKey(ship))
            {
                ForcedNetworkUpdate = true;
            }
            _lastRepairTimes[ship] = Game.GameTime.TotalGameTime;
            var elapsedSeconds = (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
            ship.RepairDamage(_repairSpeed * elapsedSeconds);
            ship.ExtraDevice.Charge += _weapon1ChargeSpeed * elapsedSeconds;
            ship.Weapon2.Charge += _weapon2ChargeSpeed * elapsedSeconds;
        }
    }
}
