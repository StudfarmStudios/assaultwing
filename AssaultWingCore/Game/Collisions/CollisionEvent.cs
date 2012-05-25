using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AW2.Helpers.Serialization;
using AW2.Core;

namespace AW2.Game.Collisions
{
    public class CollisionEvent
    {
        private const float COLLISION_DAMAGE_ACCELERATION_MIN = 8 * 60; // Note: Adjusted for target FPS of 60.
        private const float COLLISION_DAMAGE_PER_ACCELERATION = 0.045f;

        private CollisionArea _area1;
        private CollisionArea _area2;
        private int _gob1ID;
        private int _gob2ID;
        private int _area1ID;
        private int _area2ID;
        private float _impulse; // Not used on game clients.
        private CollisionSoundType? _collisionSound; // Used only on game clients.

        public bool SkipReversibleSideEffects { get; set; }
        public bool SkipIrreversibleSideEffects { get; set; }
        public bool IrreversibleSideEffectsPerformed { get; private set; }

        /// <summary>
        /// Creates an uninitialized collision event. Initialize by calling
        /// <see cref="SetCollisionAreas"/> and <see cref="SetImpulse"/>.
        /// </summary>
        public CollisionEvent()
        {
            _gob1ID = _gob2ID = Gob.INVALID_ID;
            _area1ID = _area2ID = -1;
        }

        public void SetCollisionAreas(CollisionArea area1, CollisionArea area2)
        {
            _area1 = area1;
            _area2 = area2;
            var gob1 = area1.Owner;
            var gob2 = area2.Owner;
            _gob1ID = gob1.ID;
            _gob2ID = gob2.ID;
            // Note: Walls are the only gobs to have over 4 collision areas; there can be hundreds of them.
            // To fit collision area IDs into as few bits as possible, walls will always collide with
            // their first collision area. This should not have a visible effect on game clients.
            _area1ID = gob1 is Gobs.Wall ? 0 : gob1.GetCollisionAreaID(area1);
            _area2ID = gob2 is Gobs.Wall ? 0 : gob2.GetCollisionAreaID(area2);
        }

        public void SetImpulse(float impulse)
        {
            _impulse = impulse;
        }

        /// <summary>
        /// To be called only on game clients. Other game instances determine collision sound
        /// by the collision impulse and colliding gobs.
        /// </summary>
        public void SetCollisionSound(CollisionSoundType collisionSound)
        {
            _collisionSound = collisionSound;
        }

        public void Handle()
        {
            if (_area1 == null || _area2 == null) return; // May happen on a game client.
            if (!SkipReversibleSideEffects) HandleReversibleSideEffects();
            if (!SkipIrreversibleSideEffects) HandleIrreversibleSideEffects();
        }

        public void Serialize(NetworkBinaryWriter writer)
        {
            writer.Write((short)_gob1ID);
            writer.Write((short)_gob2ID);
            if (_area1ID > 0x03 || _area2ID > 0x03)
                throw new ApplicationException("Too large collision area identifier: " + _area1ID + " or " + _area2ID);
            var mixedData = (byte)((byte)_area1ID & 0x03);
            mixedData |= (byte)(((byte)_area2ID & 0x03) << 2);
            Debug.Assert(Enum.GetValues(typeof(CollisionSoundType)).Length < 4, "More possible values for CollisionSoundType than serialization expects.");
            mixedData |= (byte)(((byte)GetCollisionSound() & 0x03) << 4);
            writer.Write((byte)mixedData);
        }

        public static CollisionEvent Deserialize(NetworkBinaryReader reader)
        {
            var gob1ID = reader.ReadInt16();
            var gob2ID = reader.ReadInt16();
            var mixedData = reader.ReadByte();
            var area1ID = mixedData & 0x03;
            var area2ID = (mixedData >> 2) & 0x03;
            var collisionSound = (CollisionSoundType)((mixedData >> 4) & 0x03);
            var collisionEvent = new CollisionEvent();
            var arena = AssaultWingCore.Instance.DataEngine.Arena;
            var gob1 = arena.FindGob(collisionEvent._gob1ID).Item2;
            var gob2 = arena.FindGob(collisionEvent._gob2ID).Item2;
            if (gob1 == null && gob2 == null) return collisionEvent;
            var area1 = gob1.GetCollisionArea(collisionEvent._area1ID);
            var area2 = gob2.GetCollisionArea(collisionEvent._area2ID);
            collisionEvent.SetCollisionAreas(area1, area2);
            collisionEvent.SetCollisionSound(collisionSound);
            return collisionEvent;
        }

        private void HandleReversibleSideEffects()
        {
            var gob1 = _area1.Owner;
            var gob2 = _area2.Owner;
            var targetFPS = gob1.Game.TargetFPS;
            var gob1Damage = _area2.Damage * COLLISION_DAMAGE_PER_ACCELERATION * Math.Max(0, GetAcceleration(gob1, _impulse) - COLLISION_DAMAGE_ACCELERATION_MIN);
            var gob2Damage = _area1.Damage * COLLISION_DAMAGE_PER_ACCELERATION * Math.Max(0, GetAcceleration(gob2, _impulse) - COLLISION_DAMAGE_ACCELERATION_MIN);
            if (gob1.IsDamageable) gob1.InflictDamage(gob1Damage, new GobUtils.DamageInfo(gob2));
            if (gob2.IsDamageable) gob2.InflictDamage(gob2Damage, new GobUtils.DamageInfo(gob1));
            gob1.CollideReversible(_area1, _area2);
            gob2.CollideReversible(_area2, _area1);
        }

        private void HandleIrreversibleSideEffects()
        {
            var irreversibleSideEffects = _area1.Owner.CollideIrreversible(_area1, _area2) | _area2.Owner.CollideIrreversible(_area2, _area1);
            var game = _area1.Owner.Game;
            var sound = GetCollisionSound();
            var soundPos = _area1.Owner.MoveType == GobUtils.MoveType.Dynamic ? _area1.Owner : _area2.Owner;
            if (sound != CollisionSoundType.None) game.SoundEngine.PlaySound(sound.ToString(), soundPos);
            IrreversibleSideEffectsPerformed = irreversibleSideEffects || sound != CollisionSoundType.None;
        }

        private CollisionSoundType GetCollisionSound()
        {
            if (_collisionSound.HasValue) return _collisionSound.Value;
            if (!_area1.Type.IsPhysical() || !_area2.Type.IsPhysical()) return CollisionSoundType.None;
            var gob1 = _area1.Owner;
            var gob2 = _area2.Owner;
            var gob1HitHard = GetAcceleration(gob1, _impulse) >= COLLISION_DAMAGE_ACCELERATION_MIN;
            var gob2HitHard = GetAcceleration(gob2, _impulse) >= COLLISION_DAMAGE_ACCELERATION_MIN;
            var gob1MakesSound = gob1HitHard && gob1 is Gobs.Ship;
            var gob2MakesSound = gob2HitHard && gob2 is Gobs.Ship;
            if (gob1MakesSound && gob2MakesSound) return CollisionSoundType.ShipCollision;
            if (gob1MakesSound || gob2MakesSound)
                return gob1.MoveType == GobUtils.MoveType.Dynamic && gob2.MoveType == GobUtils.MoveType.Dynamic
                    ? CollisionSoundType.ShipCollision :  CollisionSoundType.Collision;
            return CollisionSoundType.None;
        }

        private float GetAcceleration(Gob gob, float impulse)
        {
            var force = impulse * gob.Game.TargetFPS;
            return force / gob.Mass;
        }
    }
}
