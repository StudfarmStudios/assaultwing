using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Helpers.Geometric;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.GobUtils;

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
        private float _damagePerHit;
        [TypeParameter]
        private TimeSpan _firstHitDelay;
        [TypeParameter]
        private TimeSpan _hitInterval;
        [TypeParameter]
        private TimeSpan _lifetime;

        /// <summary>
        /// Name of the triforce area texture. The name indexes the texture database in GraphicsEngine.
        /// </summary>
        [TypeParameter]
        private CanonicalString _textureName;
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _hitEffects;

        private CollisionArea _damageArea;
        private Texture2D _texture;
        private VertexPositionTexture[] _vertexData;
        private TimeSpan _deathTime;
        private TimeSpan _nextHitTime;
        private LazyProxy<int, Gob> _hostProxy;

        public override Matrix WorldMatrix
        {
            get
            {
                if (Host == null) return base.WorldMatrix;
                return AWMathHelper.CreateWorldMatrix(1, Host.DrawRotation + Host.DrawRotationOffset, Host.Pos + Host.DrawPosOffset);
            }
        }
        public Gob Host { get { return _hostProxy != null ? _hostProxy.GetValue() : null; } set { _hostProxy = value; } }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Triforce()
        {
            _triHeight = 500;
            _triWidth = 200;
            _damagePerHit = 200;
            _firstHitDelay = TimeSpan.FromSeconds(0.1);
            _hitInterval = TimeSpan.FromSeconds(0.3);
            _lifetime = TimeSpan.FromSeconds(1.1);
            _textureName = (CanonicalString)"dummytexture";
            _hitEffects = new[] { (CanonicalString)"dummypeng" };
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
            _nextHitTime = Arena.TotalTime + _firstHitDelay;
            _damageArea = new CollisionArea("damage",
                new Triangle(Vector2.Zero, new Vector2(_triHeight, _triWidth / 2), new Vector2(_triHeight, -_triWidth / 2)),
                owner: this, type: CollisionAreaType.Receptor, collidesAgainst: CollisionAreaType.PhysicalDamageable,
                cannotOverlap: CollisionAreaType.None, collisionMaterial: CollisionMaterialType.Regular);
        }

        public override void Update()
        {
            if (Arena.TotalTime >= _deathTime) Die();
            if (Host != null && Host.Dead) Die();
            HitGobs();
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

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                base.Serialize(writer, mode);
                if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
                {
                    var hostID = Host != null ? Host.ID : Gob.INVALID_ID;
                    writer.Write((short)hostID);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
            {
                int hostID = reader.ReadInt16();
                _hostProxy = new LazyProxy<int, Gob>(FindGob);
                _hostProxy.SetData(hostID);
            }
        }

        private void HitGobs()
        {
            if (Dead || _nextHitTime > Arena.TotalTime) return;
            _nextHitTime += _hitInterval;
            foreach (var victim in Arena.GetOverlappingGobs(_damageArea, CollisionAreaType.PhysicalDamageable))
                if (victim != Host)
                {
                    victim.InflictDamage(_damagePerHit, new GobUtils.DamageInfo(this));
                    GobHelper.CreatePengs(_hitEffects, victim);
                }
        }
    }
}
