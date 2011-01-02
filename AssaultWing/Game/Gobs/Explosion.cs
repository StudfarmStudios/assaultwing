using System;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// An explosion; inflicts damage, makes a big flash, throws some stuff around.
    /// </summary>
    public class Explosion : Gob
    {
        #region Explosion fields

        /// <summary>
        /// Amount of damage to inflict as a function of distance from the explosion.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private Curve _inflictDamage;

        /// <summary>
        /// Speed of gas flow away from the explosion's center, measured in
        /// meters per second as a function of the distance from the explosion's
        /// center. Gas flow affects the movement of gobs.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private Curve _flowSpeed;

        /// <summary>
        /// Time, in seconds of game time, of how long there is a gas flow away
        /// from the center of the explosion.
        /// </summary>
        [TypeParameter]
        private float _flowTime;

        /// <summary>
        /// Time of gas flow end, in game time.
        /// </summary>
        private TimeSpan _flowEndTime;

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

        private bool _firstCollisionChecked;

        #endregion Explosion fields

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
            _flowSpeed = new Curve();
            _flowSpeed.PreLoop = CurveLoopType.Constant;
            _flowSpeed.PostLoop = CurveLoopType.Constant;
            _flowSpeed.Keys.Add(new CurveKey(0, 6000, 0, 0, CurveContinuity.Smooth));
            _flowSpeed.Keys.Add(new CurveKey(300, 0, -1.5f, -1.5f, CurveContinuity.Smooth));
            _flowTime = 0.5f;
            _impactHoleRadius = 100;
            _particleEngineNames = new CanonicalString[] { (CanonicalString)"dummypeng" };
            _sound = "Explosion";
        }

        public Explosion(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            Game.SoundEngine.PlaySound(_sound.ToString());

            CreateParticleEngines();
            _flowEndTime = Arena.TotalTime + TimeSpan.FromSeconds(_flowTime);
            Arena.MakeHole(Pos, _impactHoleRadius);
            base.Activate();
        }

        public override void Update()
        {
            base.Update();
            if (!_firstCollisionChecked)
            {
                _firstCollisionChecked = true;
                RemoveCollisionAreas(area => area.Name != "Force");
            }
            if (Arena.TotalTime >= _flowEndTime)
                Die();
        }

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            // We assume we have only these collision areas, all receptors with specific names:
            // "Hit" is assumed to collide only against damageables;
            // "Force" is assumed to collide only against movables.
            if (myArea.Name == "Force")
            {
                Vector2 difference = theirArea.Owner.Pos - this.Pos;
                float differenceLength = difference.Length();
                Vector2 flow = difference / differenceLength *
                    _flowSpeed.Evaluate(differenceLength);
                Game.PhysicsEngine.ApplyDrag(theirArea.Owner, flow, 0.003f);
            }
            else if (myArea.Name == "Hit")
            {
                float distance = theirArea.Area.DistanceTo(this.Pos);
                float damage = _inflictDamage.Evaluate(distance);
                theirArea.Owner.InflictDamage(damage, new DeathCause(theirArea.Owner, this));
            }
        }

        private void CreateParticleEngines()
        {
            foreach (var pengName in _particleEngineNames)
            {
                Gob.CreateGob<Gob>(Game, pengName, gob =>
                {
                    gob.ResetPos(this.Pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                    Arena.Gobs.Add(gob);
                });
            }
        }
    }
}
