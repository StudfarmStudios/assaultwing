using System;
using Microsoft.Xna.Framework;
using AW2.Game.Collisions;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// An explosion; inflicts damage, makes a big flash, throws some stuff around.
    /// </summary>
    public class Explosion : Gob
    {
        /// <summary>
        /// Amount of damage to inflict as a function of distance from the explosion.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private Curve _inflictDamage;

        /// <summary>
        /// Radial medium flow from the center of the explosion. Pushes gobs away.
        /// </summary>
        [TypeParameter]
        private RadialFlow _radialFlow;

        /// <summary>
        /// The radius of the hole to make on impact to walls.
        /// </summary>
        [TypeParameter]
        private float _impactHoleRadius;

        /// <summary>
        /// Names of the particle engines to create.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _particleEngineNames;

        /// <summary>
        /// Name of the sound effect to play on creation.
        /// </summary>
        [TypeParameter]
        private string _sound;

        private TimeSpan? _damageTime;

        public override bool Cold { get { return false; } }
        public override BoundingSphere DrawBounds { get { return new BoundingSphere(); } }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Explosion()
        {
            _inflictDamage = new Curve();
            _inflictDamage.PreLoop = CurveLoopType.Constant;
            _inflictDamage.PostLoop = CurveLoopType.Constant;
            _inflictDamage.Keys.Add(new CurveKey(0, 200, 0, 0, CurveContinuity.Smooth));
            _inflictDamage.Keys.Add(new CurveKey(300, 0, -3, -3, CurveContinuity.Smooth));
            _radialFlow = new RadialFlow();
            _impactHoleRadius = 100;
            _particleEngineNames = new[] { (CanonicalString)"dummypeng" };
            _sound = "Explosion";
        }

        public Explosion(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            Game.SoundEngine.PlaySound(_sound, this);
            GobHelper.CreateGobs(_particleEngineNames, Arena, Pos, gob => gob.Owner = Owner);
            _radialFlow.Activate(this);
            Arena.MakeHole(Pos, _impactHoleRadius);
            base.Activate();
        }

        public override void Update()
        {
            base.Update();
            if (!_radialFlow.IsActive) Die();
            _radialFlow.Update();
        }

        public override void CollideReversible(CollisionArea myArea, CollisionArea theirArea)
        {
            if (_damageTime.HasValue && _damageTime.Value != Game.GameTime.TotalGameTime) return;
            // Note: RadialFlow attaches its own CollisionArea to Explosion. Do nothing with it.
            if (myArea.Type == CollisionAreaType.Damage)
            {
                Game.Stats.SendHit(this, theirArea.Owner);
                _damageTime = Game.GameTime.TotalGameTime;
                var damage = _inflictDamage.Evaluate(PhysicsHelper.Distance(theirArea, Pos));
                theirArea.Owner.InflictDamage(damage, new DamageInfo(this));
                myArea.Destroy();
            }
        }
    }
}
