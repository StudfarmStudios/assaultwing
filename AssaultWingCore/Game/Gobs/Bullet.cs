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
    /// A simple bullet.
    /// </summary>
    public class Bullet : Gob
    {
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
        /// If true, the bullet rotates by <see cref="rotationSpeed"/>.
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
            DeathTime = new TimeSpan(0, 1, 2);
        }

        public Bullet(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            DeathTime = Arena.TotalTime + TimeSpan.FromSeconds(_lifetime);
            if (_bulletModelNames.Length > 0)
            {
                int modelNameI = RandomHelper.GetRandomInt(_bulletModelNames.Length);
                base.ModelName = _bulletModelNames[modelNameI];
            }
            base.Activate();
            _thruster.Activate(this, true);
        }

        public override void Update()
        {
            if (Arena.TotalTime >= DeathTime) Die();
            base.Update();
            if (_isRotating)
            {
                Rotation += _rotationSpeed * (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
            }
            else
            {
                // Fly nose first, but only if we're moving fast enough.
                if (Move.LengthSquared() > 1 * 1) {
                    float rotationGoal = (float)Math.Acos( Move.X / Move.Length() );
                    if (Move.Y < 0)
                        rotationGoal = MathHelper.TwoPi - rotationGoal;
                    Rotation = rotationGoal;
                }
            }
            _thruster.Update();
        }

        public override void Dispose()
        {
            _thruster.Dispose();
            base.Dispose();
        }

        public override Arena.CollisionSideEffectType Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck, Arena.CollisionSideEffectType sideEffectTypes)
        {
            var result = Arena.CollisionSideEffectType.None;
            if (sideEffectTypes.HasFlag(Arena.CollisionSideEffectType.Reversible))
            {
                if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
                {
                    theirArea.Owner.InflictDamage(_impactDamage, new DamageInfo(this));
                    result |= Arena.CollisionSideEffectType.Reversible;
                }
            }
            if (sideEffectTypes.HasFlag(Arena.CollisionSideEffectType.Irreversible))
            {
                Arena.MakeHole(Pos, _impactHoleRadius);
                Die();
                result |= Arena.CollisionSideEffectType.Irreversible;
            }
            return result;
        }
    }
}
