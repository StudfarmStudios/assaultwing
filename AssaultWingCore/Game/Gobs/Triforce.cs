using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A triangular area that inflicts damage.
    /// </summary>
    public class Triforce : Gob
    {
        [TypeParameter]
        private float _triHeightForDamage;
        [TypeParameter]
        private float _triHeightForWallPunches;
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
        [TypeParameter]
        private int _wallPunchesPerHit;
        [TypeParameter]
        private float _wallPunchRadius;

        /// <summary>
        /// Name of the triforce area texture. The name indexes the texture database in GraphicsEngine.
        /// </summary>
        [TypeParameter]
        private CanonicalString _textureName;
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _hitEffects;
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _wallPunchEffects;

        private CollisionArea _damageArea;
        private Texture2D _texture;
        private VertexPositionTexture[] _vertexData;
        private TimeSpan _deathTime;
        private AWTimer _nextHitTimer;
        private LazyProxy<int, Gob> _hostProxy;
        private List<Vector2> _wallPunchPosesForClient;

        public override Matrix WorldMatrix
        {
            get
            {
                if (Host == null) return base.WorldMatrix;
                return AWMathHelper.CreateWorldMatrix(1, Host.DrawRotation + Host.DrawRotationOffset, Host.Pos + Host.DrawPosOffset);
            }
        }
        public Gob Host { get { return _hostProxy != null ? _hostProxy.GetValue() : null; } set { _hostProxy = value; } }
        private TimeSpan FadeTime { get { return _firstHitDelay; } }
        private bool IsFadingOut { get { return Arena.TotalTime + FadeTime >= _deathTime; } }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Triforce()
        {
            _triHeightForDamage = 500;
            _triHeightForWallPunches = 100;
            _triWidth = 200;
            _damagePerHit = 200;
            _firstHitDelay = TimeSpan.FromSeconds(0.1);
            _hitInterval = TimeSpan.FromSeconds(0.3);
            _lifetime = TimeSpan.FromSeconds(1.1);
            _wallPunchesPerHit = 10;
            _wallPunchRadius = 10;
            _textureName = (CanonicalString)"dummytexture";
            _hitEffects = new[] { (CanonicalString)"dummypeng" };
            _wallPunchEffects = new[] { (CanonicalString)"dummypeng" };
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
                new VertexPositionTexture(new Vector3(_triHeightForDamage, _triWidth / 2, 0), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(_triHeightForDamage, -_triWidth / 2, 0), new Vector2(1, 0)),
            };
        }

        public override void Activate()
        {
            base.Activate();
            _deathTime = Arena.TotalTime + _lifetime + FadeTime;
            _nextHitTimer = new AWTimer(() => Arena.TotalTime, _hitInterval);
            _nextHitTimer.SetCurrentInterval(_firstHitDelay);
            _damageArea = new CollisionArea("damage",
                new Triangle(Vector2.Zero, new Vector2(_triHeightForDamage, _triWidth / 2), new Vector2(_triHeightForDamage, -_triWidth / 2)),
                owner: this, type: CollisionAreaType.Receptor, collidesAgainst: CollisionAreaType.PhysicalDamageable,
                cannotOverlap: CollisionAreaType.None, collisionMaterial: CollisionMaterialType.Regular);
            _wallPunchPosesForClient = new List<Vector2>();
        }

        public override void Update()
        {
            if (Arena.TotalTime >= _deathTime) Die();
            if (IsFadingOut) return;
            if (Host != null && Host.Dead) _deathTime = Arena.TotalTime + FadeTime;
            HitPeriodically();
        }

        public override void Draw3D(Matrix view, Matrix projection, Player viewer)
        {
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            gfx.BlendState = AW2.Graphics.GraphicsEngineImpl.AdditiveBlendPremultipliedAlpha;
            var effect = Game.GraphicsEngine.GameContent.TriforceEffect;
            effect.Projection = projection;
            effect.World = WorldMatrix;
            effect.View = view;
            effect.Alpha = Alpha * GetAlphaByAge();
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
                    writer.Write((byte)_wallPunchPosesForClient.Count);
                    foreach (var wallPunchPos in _wallPunchPosesForClient)
                        writer.WriteHalf(wallPunchPos);
                    _wallPunchPosesForClient.Clear();
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
                int wallPunchCount = reader.ReadByte();
                for (int i = 0; i < wallPunchCount; i++)
                    GobHelper.CreateGobs(_wallPunchEffects, Arena, reader.ReadHalfVector2());
            }
        }

        private void HitPeriodically()
        {
            if (IsFadingOut || !_nextHitTimer.IsElapsed) return;
            HitGobs();
            PunchWalls();
        }

        private void HitGobs()
        {
            foreach (var victim in Arena.GetOverlappingGobs(_damageArea, CollisionAreaType.PhysicalDamageable))
                if (victim != Host)
                {
                    victim.InflictDamage(_damagePerHit, new GobUtils.DamageInfo(this));
                    GobHelper.CreatePengs(_hitEffects, victim);
                }
        }

        private void PunchWalls()
        {
            if (Host == null) return;
            var startPos = Host.Pos;
            var unitFront = Vector2.UnitX.Rotate(Host.Rotation);
            var unitLeft = unitFront.Rotate90();
            var punches = 0;
            var distance = 0f;
            while (punches < _wallPunchesPerHit && distance <= _triHeightForWallPunches)
            {
                var halfWidth = _triWidth / 2 / _triHeightForDamage * distance;
                var punchCenter = startPos + unitFront * distance + unitLeft * RandomHelper.GetRandomFloat(-halfWidth, halfWidth);
                if (Arena.MakeHole(punchCenter, _wallPunchRadius) > 0)
                {
                    punches++;
                    GobHelper.CreateGobs(_wallPunchEffects, Arena, punchCenter);
                    if (Game.NetworkMode == Core.NetworkMode.Server)
                    {
                        _wallPunchPosesForClient.Add(punchCenter);
                        ForcedNetworkUpdate = true;
                    }
                    distance += _wallPunchRadius;
                }
                else
                    distance += _wallPunchRadius * 2.5f;
            }
        }

        private float GetAlphaByAge()
        {
            var fadeSeconds = (float)_firstHitDelay.TotalSeconds;
            var lifeSeconds = (float)_lifetime.TotalSeconds;
            if (Age < FadeTime) return Age.Divide(FadeTime);
            if (IsFadingOut) return Math.Max(0, (_deathTime - Arena.TotalTime).Divide(FadeTime));
            return 1;
        }
    }
}
