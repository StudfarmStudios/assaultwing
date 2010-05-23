using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Geometric;


namespace AW2.Game.Gobs
{
    class FloatingBullet : Gob
    {
        /// <summary>
        /// Amount of damage to inflict on impact with a damageable gob.
        /// </summary>
        [TypeParameter]
        protected float impactDamage;

        /// <summary>
        /// The radius of the hole to make on impact to walls.
        /// </summary>
        [TypeParameter]
        float impactHoleRadius;

        /// <summary>
        /// A list of alternative model names for a bullet.
        /// </summary>
        /// The actual model name for a bullet is chosen from these by random.
        [TypeParameter, ShallowCopy]
        CanonicalString[] bulletModelNames;

        /// <summary>
        /// Lifetime of the bounce bullet, in game time seconds.
        /// Death is inevitable when lifetime has passed.
        /// </summary>
        [TypeParameter]
        float lifetime;

        /// <summary>
        /// If true, the bullet rotates by <see cref="rotationSpeed"/>.
        /// </summary>
        [TypeParameter]
        bool isRotating;

        /// <summary>
        /// Rotation speed in radians per second. Has an effect only when <see cref="isRotating"/> is true.
        /// </summary>
        [TypeParameter]
        float rotationSpeed;

        /// <summary>
        /// Time of certain death of the bullet, in game time.
        /// </summary>
        protected TimeSpan DeathTime { get; set; }

        /// <summary>
        /// MovementCurve for animating mine
        /// </summary>
        private MovementCurve _movementCurve;

        /// <summary>
        /// Targetpoint for animation
        /// </summary>
        private Vector2 _targetPos;

        /// <summary>
        /// Floating bullet original position
        /// </summary>
        private Vector2 _originalPos;

        /// <summary>
        /// Flag if floating bullet has stopped once
        /// </summary>
        private bool _bulletStopped = false;

        /// <summary>
        /// Circle representing radius of randomization
        /// </summary>
        private Circle _targetCircle;

        /// <summary>
        /// Names of all 3D models that this gob type will ever use.
        /// </summary>

        public override IEnumerable<CanonicalString> ModelNames
        {
            get { return base.ModelNames.Union(bulletModelNames); }
        }

        /// This constructor is only for serialisation.
        public FloatingBullet()
            : base()
        {
            impactDamage = 10;
            impactHoleRadius = 10;
            bulletModelNames = new CanonicalString[] { (CanonicalString)"dummymodel" };
            lifetime = 60;
            isRotating = false;
            rotationSpeed = 5;
            DeathTime = new TimeSpan(0, 1, 2);
        }

        public FloatingBullet(CanonicalString typeName)
            : base(typeName)
        {
            _gravitating = false;
        }

        public override void Activate()
        {
            DeathTime = Arena.TotalTime + TimeSpan.FromSeconds(lifetime);
            if (bulletModelNames.Length > 0)
            {
                int modelNameI = RandomHelper.GetRandomInt(bulletModelNames.Length);
                base.ModelName = bulletModelNames[modelNameI];
            }
            base.Activate();
        }

        public override void Update()
        {
            if (Arena.TotalTime >= DeathTime)
                Die(new DeathCause());

            base.Update();

            // Rotate floating bullet
            if (isRotating)
            {
                Rotation += rotationSpeed * (float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            }

            // Slow down the floating bullet if it hasn't stopped before
            if (!_bulletStopped)
            {
                Move *= 0.957f;
            }

            // When mine has nearly stopped start animating (this condition will succeed only once per floating bullet)
            if (!_bulletStopped && Move.Length() < 1)
            {
                _bulletStopped = true;
                _targetPos = Pos;
                _originalPos = Pos;
                _movementCurve = new MovementCurve(Pos);
                _targetCircle = new Circle(_originalPos, 15);
            }

            // Set movement vector to zero always when floating bullet has stopped
            if (_bulletStopped)
            {
                Move = Vector2.Zero;
            }

            // If floating bullet has stopped and current target position is same than current position randomize next target
            if (_bulletStopped && _targetPos == Pos)
            {
                _targetPos = Geometry.GetRandomLocation(_targetCircle);
                float animationLength = RandomHelper.GetRandomFloat(1.9f, 2.6f);
                _movementCurve.SetTarget(_targetPos, Arena.TotalTime, animationLength, MovementCurve.Curvature.SlowFastSlow);
            }

            // If floating bullet is stopped and current target positions is not the same than current position update the floating bullet position
            if (_bulletStopped && _targetPos != Pos)
            {
                Pos = _movementCurve.Evaluate(Arena.TotalTime);
            }
        }

        /// <summary>
        /// Performs collision operations for the case when one of this gob's collision areas
        /// is overlapping one of another gob's collision areas.
        /// </summary>
        /// <param name="myArea">The collision area of this gob.</param>
        /// <param name="theirArea">The collision area of the other gob.</param>
        /// <param name="stuck">If <b>true</b> then the gob is stuck, i.e.
        /// <b>theirArea.Type</b> matches <b>myArea.CannotOverlap</b> and it's not possible
        /// to backtrack out of the overlap. It is then up to this gob and the other gob 
        /// to resolve the overlap.</param>
        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
                theirArea.Owner.InflictDamage(impactDamage, new DeathCause(theirArea.Owner, DeathCauseType.Damage, this));
            Arena.MakeHole(Pos, impactHoleRadius);
            Die(new DeathCause());
        }
    }
}
