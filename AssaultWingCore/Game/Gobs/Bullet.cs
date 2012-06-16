using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Collisions;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A simple bullet.
    /// </summary>
    public class Bullet : Gob
    {
        private const float HEADING_MOVEMENT_MINIMUM_SQUARED = 1f * 1f;
        private const float HEADING_TURN_SPEED = 1.5f;

        /// <summary>
        /// Amount of damage to inflict on impact with a damageable gob.
        /// </summary>
        [TypeParameter]
        protected float _impactDamage;

        /// <summary>
        /// The radius of the hole to make on impact to walls.
        /// </summary>
        [TypeParameter]
        private float _impactHoleRadius;

        /// <summary>
        /// A list of alternative model names for a bullet.
        /// </summary>
        /// The actual model name for a bullet is chosen from these by random.
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _bulletModelNames;

        /// <summary>
        /// Lifetime of the bounce bullet, in game time seconds.
        /// Death is inevitable when lifetime has passed.
        /// </summary>
        [TypeParameter]
        private float _lifetime;

        /// <summary>
        /// If true, the bullet rotates by physics. Initial rotation speed comes from <see cref="rotationSpeed"/>.
        /// If false, the bullet heads towards where it's going.
        /// </summary>
        [TypeParameter]
        private bool _isRotating;

        /// <summary>
        /// Rotation speed in radians per second. Has an effect only when <see cref="isRotating"/> is true.
        /// </summary>
        [TypeParameter]
        private float _rotationSpeed;

        [TypeParameter]
        private Thruster _thruster;

        /// <summary>
        /// Time of certain death of the bullet, in game time.
        /// </summary>
        private TimeSpan DeathTime { get; set; }

        /// <summary>
        /// Names of all 3D models that this gob type will ever use.
        /// </summary>
        public override IEnumerable<CanonicalString> ModelNames
        {
            get { return base.ModelNames.Union(_bulletModelNames); }
        }

        /// This constructor is only for serialisation.
        public Bullet()
        {
            _impactDamage = 10;
            _impactHoleRadius = 10;
            _bulletModelNames = new CanonicalString[] { (CanonicalString)"dummymodel" };
            _lifetime = 60;
            _isRotating = false;
            _rotationSpeed = 5;
            _thruster = new Thruster();
        }

        public Bullet(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            DeathTime = Arena.TotalTime + TimeSpan.FromSeconds(_lifetime);
            if (_isRotating) RotationSpeed = _rotationSpeed;
            if (_bulletModelNames.Length > 0)
            {
                int modelNameI = RandomHelper.GetRandomInt(_bulletModelNames.Length);
                base.ModelName = _bulletModelNames[modelNameI];
            }
            base.Activate();
            _thruster.Activate(this);
        }

        public override void Update()
        {
            if (Arena.TotalTime >= DeathTime) Die();
            base.Update();
            if (!_isRotating && Move.LengthSquared() >= HEADING_MOVEMENT_MINIMUM_SQUARED)
                RotationSpeed = AWMathHelper.GetAngleSpeedTowards(Rotation, Move.Angle(), HEADING_TURN_SPEED, Game.TargetElapsedTime);
            _thruster.Thrust(1);
            _thruster.Update();
        }

        public override void Dispose()
        {
            _thruster.Dispose();
            base.Dispose();
        }

        public override bool CollideIrreversible(CollisionArea myArea, CollisionArea theirArea)
        {
            if (!theirArea.Type.IsPhysical()) return false;
            if (theirArea.Owner.IsDamageable)
            {
                Game.Stats.SendHit(this, theirArea.Owner);
                theirArea.Owner.InflictDamage(_impactDamage, new DamageInfo(this));
            }
            Arena.MakeHole(Pos, _impactHoleRadius);
            Die();
            return true;
        }
    }
}
