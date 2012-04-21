using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;
using AWPoint = AW2.Helpers.Geometric.Point;

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

        private CollisionArea _fullDamageArea;
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
            var relativeLengths = new[] { _triHeightForDamage, _triHeightForDamage };
            _vertexData = CreateVertexData(relativeLengths, CreateDamagePolygonVertices(relativeLengths));
        }

        public override void Activate()
        {
            base.Activate();
            _deathTime = Arena.TotalTime + _lifetime + FadeTime;
            _nextHitTimer = new AWTimer(() => Arena.TotalTime, _hitInterval);
            _nextHitTimer.SetCurrentInterval(_firstHitDelay);
            var fullConeArea = new Triangle(Vector2.Zero, new Vector2(_triHeightForDamage, _triWidth / 2), new Vector2(_triHeightForDamage, -_triWidth / 2));
            var fullConeCircumsphere = BoundingSphere.CreateFromPoints(new[] { fullConeArea.P1, fullConeArea.P2, fullConeArea.P3 }.Select(p => new Vector3(p, 0)));
            _fullDamageArea = CreateCollisionArea(new Circle(new Vector2(fullConeCircumsphere.Center.X, fullConeCircumsphere.Center.Y), fullConeCircumsphere.Radius));
            _wallPunchPosesForClient = new List<Vector2>();
        }

        public override void Update()
        {
            if (Arena.TotalTime >= _deathTime) Die();
            if (IsFadingOut) return;
            if (Host != null && Host.Dead) _deathTime = Arena.TotalTime + FadeTime;
            UpdateLocation();
            UpdateGeometry();
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
                gfx.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, _vertexData, 0, _vertexData.Length / 3);
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

        private void UpdateLocation()
        {
            if (Host == null) return;
            Pos = Host.Pos + Host.DrawPosOffset;
            Rotation = Host.Rotation + Host.DrawRotationOffset;
        }

        private void UpdateGeometry()
        {
            throw new NotImplementedException("Rewrite using Farseer raycasting");
#if false // !!!
            var potentialObstacles = Arena.GetOverlappers(_fullDamageArea, CollisionAreaType.PhysicalWall).ToArray();
            var rayCount = 4;
            var rayStep = _wallPunchRadius;
            var relativeLengths = new float[rayCount];
            for (int ray = 0; ray < rayCount; ray++)
            {
                var fullRay = new Vector2(_triHeightForDamage, _triWidth * (0.5f - (float)ray / (rayCount - 1)));
                var rayUnit = (fullRay / _triHeightForDamage).Rotate(Rotation);
                var distance = 0f;
                while ((distance += rayStep) <= _triHeightForDamage)
                    if (potentialObstacles.Any(area => Geometry.Intersect(new AWPoint(Pos + rayUnit * distance), area.Area))) break;
                relativeLengths[ray] = distance / _triHeightForDamage;
            }
            var polygonVertices = CreateDamagePolygonVertices(relativeLengths);
            _vertexData = CreateVertexData(relativeLengths, polygonVertices);
            _damageArea = CreateCollisionArea(CreateDamageArea(polygonVertices));
#endif
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
            return; // TODO !!! Punch along damage polygon border.
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

        private CollisionArea CreateCollisionArea(IGeomPrimitive gobArea)
        {
            return new CollisionArea("damage", gobArea,
                owner: this, type: CollisionAreaType.Receptor, collidesAgainst: CollisionAreaType.PhysicalDamageable,
                cannotOverlap: CollisionAreaType.None, collisionMaterial: CollisionMaterialType.Regular);
        }

        /// <summary>
        /// Returns the vertex data for drawing the Triforce as a triangle list.
        /// </summary>
        /// <param name="relativeLengths">The relative lengths of equally spaced rays covering
        /// the cone area, between 0 (short) and 1 (long).</param>
        private VertexPositionTexture[] CreateVertexData(float[] relativeLengths, Vector2[] polygonVertices)
        {
            var vertexData = new VertexPositionTexture[(relativeLengths.Length - 1) * 3];
            for (int i = 0; i < relativeLengths.Length - 1; i++)
            {
                var texX1 = (float)i / (relativeLengths.Length - 1);
                var texX2 = (float)(i + 1) / (relativeLengths.Length - 1);
                vertexData[i * 3 + 0] = new VertexPositionTexture(
                    Vector3.Zero,
                    Vector2.Zero);
                vertexData[i * 3 + 1] = new VertexPositionTexture(
                    new Vector3(polygonVertices[1 + i], 0),
                    relativeLengths[i] * new Vector2(texX1, 1 - texX1));
                vertexData[i * 3 + 2] = new VertexPositionTexture(
                    new Vector3(polygonVertices[1 + i + 1], 0),
                    relativeLengths[i + 1] * new Vector2(texX2, 1 - texX2));
            };
            return vertexData;
        }

        /// <summary>
        /// Returns the area of damage in gob coordinates.
        /// </summary>
        private Polygon CreateDamageArea(Vector2[] polygonVertices)
        {
            return new Polygon(polygonVertices);
        }

        /// <summary>
        /// Returns the vertices of the polygon area of damage. The first vertex is the gob origin.
        /// The vertices wind clockwise.
        /// </summary>
        private Vector2[] CreateDamagePolygonVertices(float[] relativeLengths)
        {
            var vertices = new Vector2[relativeLengths.Length + 1];
            vertices[0] = Vector2.Zero;
            for (int i = 0; i < relativeLengths.Length; i++)
                vertices[1 + i] = relativeLengths[i] * new Vector2(_triHeightForDamage,
                    _triWidth * (0.5f - (float)i / (relativeLengths.Length - 1)));
            return vertices;
        }
    }
}
