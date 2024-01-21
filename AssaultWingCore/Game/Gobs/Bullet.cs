using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Collisions;
using AW2.Game.GobUtils;
using AW2.Game.Players;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A simple bullet.
    /// </summary>
    public class Bullet : Gob, IConsistencyCheckable
    {
        private const float HEADING_MOVEMENT_MINIMUM_SQUARED = 1f * 1f;
        private const float HEADING_TURN_SPEED = 3.0f;

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
        /// Name of the texture to draw behind the bullet, or null if there's no texture.
        /// </summary>
        [TypeParameter]
        private CanonicalString _backgroundTextureName;

        /// <summary>
        /// Scale of the background texture.
        /// </summary>
        [TypeParameter]
        private float _backgroundScale;

        /// <summary>
        /// Alpha of the background texture.
        /// </summary>
        [TypeParameter]
        private float _backgroundAlpha;

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

        private Texture2D _backgroundTexture;

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Bullet()
        {
            _impactDamage = 10;
            _impactHoleRadius = 10;
            _bulletModelNames = new[] { (CanonicalString)"dummymodel" };
            _backgroundTextureName = CanonicalString.Null;
            _backgroundScale = 1;
            _backgroundAlpha = 1;
            _isRotating = false;
            _rotationSpeed = 5;
            _thruster = new Thruster();
        }

        public Bullet(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void LoadContent()
        {
            base.LoadContent();
            if (!_backgroundTextureName.IsNull) _backgroundTexture = Game.Content.Load<Texture2D>(_backgroundTextureName);
        }

        public override void Activate()
        {
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
            base.Update();
            if (!_isRotating && Move.LengthSquared() >= HEADING_MOVEMENT_MINIMUM_SQUARED)
                RotationSpeed = AWMathHelper.GetAngleSpeedTowards(Rotation, Move.Angle(), HEADING_TURN_SPEED, AW2.Core.AssaultWingCore.TargetElapsedTime);
            _thruster.Thrust(1);
            _thruster.Update();
        }

        public override void Draw2D(Matrix gameToScreen, SpriteBatch spriteBatch, float scale, Player viewer)
        {
            if (_backgroundTexture == null) return;
            var screenCenter = Vector2.Transform(Pos + DrawPosOffset, gameToScreen);
            var drawRotation = -(Rotation + DrawRotationOffset); // negated, because screen Y coordinates are reversed
            var color = Color.Multiply(Color.White, Alpha * _backgroundAlpha);
            spriteBatch.Draw(_backgroundTexture, screenCenter, null, color, drawRotation,
                new Vector2(_backgroundTexture.Width, _backgroundTexture.Height) / 2, _backgroundScale,
                SpriteEffects.None, 0.5f);
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
                theirArea.Owner.InflictDamage(_impactDamage, new DamageInfo(this));
            }
            Arena.MakeHole(Pos, _impactHoleRadius);
            Die();
            return true;
        }

        public void MakeConsistent(Type limitationAttribute)
        {
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                if (_backgroundTextureName == "") _backgroundTextureName = CanonicalString.Null;
            }
        }
    }
}
