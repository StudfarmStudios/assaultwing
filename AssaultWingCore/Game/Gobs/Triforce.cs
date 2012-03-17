using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Helpers.Geometric;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A triangular area that inflicts damage.
    /// </summary>
    public class Triforce : Gob
    {
        [TypeParameter]
        private float _triHeight;
        [TypeParameter]
        private float _triWidth;
        [TypeParameter]
        private float _damagePerSecond;
        [TypeParameter]
        private TimeSpan _lifetime;

        /// <summary>
        /// Name of the triforce area texture. The name indexes the texture database in GraphicsEngine.
        /// </summary>
        [TypeParameter]
        private CanonicalString _textureName;

        private CollisionArea _damageArea;
        private Texture2D _texture;
        private VertexPositionTexture[] _vertexData;
        private TimeSpan _deathTime;

        public override Matrix WorldMatrix
        {
            get
            {
                if (Host == null) return base.WorldMatrix;
                return AWMathHelper.CreateWorldMatrix(1, Host.DrawRotation + Host.DrawRotationOffset, Host.Pos + Host.DrawPosOffset);
            }
        }
        public Gob Host { get; set; }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Triforce()
        {
            _triHeight = 500;
            _triWidth = 200;
            _damagePerSecond = 200;
            _textureName = (CanonicalString)"dummytexture";
        }

        public Triforce(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void LoadContent()
        {
            base.LoadContent();
            _texture = Game.Content.Load<Texture2D>(_textureName);
            _vertexData = new[]
            {
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(_triHeight, _triWidth / 2, 0), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(_triHeight, -_triWidth / 2, 0), new Vector2(1, 0)),
            };
        }

        public override void Activate()
        {
            base.Activate();
            _deathTime = Arena.TotalTime + _lifetime;
            _damageArea = new CollisionArea("damage",
                new Triangle(Vector2.Zero, new Vector2(_triHeight, _triWidth / 2), new Vector2(_triHeight, -_triWidth / 2)),
                owner: this, type: CollisionAreaType.Receptor, collidesAgainst: CollisionAreaType.PhysicalDamageable,
                cannotOverlap: CollisionAreaType.None, collisionMaterial: CollisionMaterialType.Regular);
        }

        public override void Update()
        {
            if (Arena.TotalTime >= _deathTime) Die();
            base.Update();
            var damage = _damagePerSecond * (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
            foreach (var gob in Arena.GetOverlappingGobs(_damageArea, CollisionAreaType.PhysicalDamageable))
                if (gob != Host) gob.InflictDamage(damage, new GobUtils.DamageInfo(this));
        }

        public override void Draw3D(Matrix view, Matrix projection, Player viewer)
        {
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            gfx.BlendState = AW2.Graphics.GraphicsEngineImpl.AdditiveBlendPremultipliedAlpha;
            var effect = Game.GraphicsEngine.GameContent.TriforceEffect;
            effect.Projection = projection;
            effect.World = WorldMatrix;
            effect.View = view;
            effect.Alpha = Alpha;
            effect.Texture = _texture;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gfx.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, _vertexData, 0, _vertexData.Length - 2);
            }
        }
    }
}
