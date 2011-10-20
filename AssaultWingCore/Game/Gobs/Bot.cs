using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// Semi-intellectual armed flying gob.
    /// </summary>
    public class Bot : Gob
    {
        private const float ROTATION_SPEED = MathHelper.TwoPi / 10; // radians/second
        private const float TARGET_FIND_RANGE = 500;
        private static readonly TimeSpan TARGET_UPDATE_INTERVAL = TimeSpan.FromSeconds(1.5);

        [TypeParameter]
        private CanonicalString _weaponName;

        private Weapon _weapon;
        private LazyProxy<int, Gob> _targetProxy;
        private TimeSpan _nextTargetUpdate;

        public new BotPlayer Owner { get { return (BotPlayer)base.Owner; } set { base.Owner = value; } }
        private Gob Target { get { return _targetProxy != null ? _targetProxy.GetValue() : null; } set { _targetProxy = value; } }

        /// <summary>
        /// Only for deserialization.
        /// </summary>
        public Bot()
        {
            _weaponName = (CanonicalString)"dummyweapontype";
        }

        public Bot(CanonicalString typeName)
           : base(typeName)
        {
            Gravitating = false;
        }

        public override void Activate()
        {
            base.Activate();
            _weapon = Weapon.Create(_weaponName);
            _weapon.AttachTo(this, ShipDevice.OwnerHandleType.PrimaryWeapon);
            Game.DataEngine.Devices.Add(_weapon);
        }

        public override void Update()
        {
            base.Update();
            UpdateTarget();
            Aim();
            Shoot();
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                base.Serialize(writer, mode);
                if (mode.HasFlag(SerializationModeFlags.VaryingData))
                {
                    int targetID = Target != null ? Target.ID : Gob.INVALID_ID;
                    writer.Write((short)targetID);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if (Owner != null) Owner.SeizeBot(this);
            if (mode.HasFlag(SerializationModeFlags.VaryingData))
            {
                int targetID = reader.ReadInt16();
                _targetProxy = new LazyProxy<int, Gob>(FindGob);
                _targetProxy.SetData(targetID);
            }
        }

        private void UpdateTarget()
        {
            if (Game.NetworkMode == Core.NetworkMode.Client) return;
            if (Arena.TotalTime < _nextTargetUpdate) return;
            _nextTargetUpdate = Arena.TotalTime + TARGET_UPDATE_INTERVAL;
            var newTarget = TargetSelection.ChooseTarget(Game.DataEngine.Minions, this, Rotation, TARGET_FIND_RANGE, TargetSelection.SectorType.FullCircle);
            if (newTarget == null || newTarget.Owner == Owner)
                Target = null;
            else
            {
                Target = newTarget;
                if (Game.NetworkMode == Core.NetworkMode.Server) ForcedNetworkUpdate = true;
            }
        }

        private void Aim()
        {
            if (Target == null) return;
            var rotationStep = Game.PhysicsEngine.ApplyChange(ROTATION_SPEED, Game.GameTime.ElapsedGameTime);
            Rotation = AWMathHelper.InterpolateTowardsAngle(Rotation, (Target.Pos - Pos).Angle(), rotationStep);
        }

        private void Shoot()
        {
            if (Game.NetworkMode == Core.NetworkMode.Client) return;
            if (Target == null) return;
            _weapon.TryFire(new UI.ControlState(1, true));
        }
    }
}
