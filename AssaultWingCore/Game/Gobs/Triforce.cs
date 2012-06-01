using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using AW2.Game.Collisions;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;
using AWPoint = AW2.Helpers.Geometric.Point;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A triangular area that inflicts damage. The area is clipped to walls.
    /// The area is split into slices which are added to <see cref="Gob.CollisionAreas"/>.
    /// The slice areas stay disabled for performance reasons.
    /// </summary>
    public class Triforce : Gob
    {
        private const int SLICE_COUNT = 15;

        [TypeParameter]
        private CanonicalString[] _surroundEffects;
        [TypeParameter]
        private float _surroundDamage;

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

        private Texture2D _texture;
        private VertexPositionTexture[] _vertexData;
        private float[] _relativeLengths;
        private TimeSpan _deathTime;
        private AWTimer _nextHitTimer;
        private LazyProxy<int, Gob> _hostProxy;
        private List<Vector2> _wallPunchPosesForClient;
        private bool _surroundHitDone;

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
            _surroundEffects = new[] { (CanonicalString)"dummypeng" };
            _collisionAreas = new[] { new CollisionArea("Hit", new Circle(Vector2.Zero, 100), null, CollisionAreaType.Damage, CollisionMaterialType.Regular) };
            _surroundDamage = 500;
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
            _vertexData = CreateVertexData(relativeLengths);
        }

        public override void Activate()
        {
            base.Activate();
            _relativeLengths = new float[SLICE_COUNT + 1];
            _deathTime = Arena.TotalTime + _lifetime + FadeTime;
            _nextHitTimer = new AWTimer(() => Arena.TotalTime, _hitInterval);
            _nextHitTimer.SetCurrentInterval(_firstHitDelay);
            _wallPunchPosesForClient = new List<Vector2>();
            GobHelper.CreatePengs(_surroundEffects, this);
            InitializeCollisionAreas();
        }

        public override void Update()
        {
            UpdateLocation();
            UpdateGeometry();
            PerformHits();
            if (Arena.TotalTime >= _deathTime) Die();
            if (Host != null && Host.Dead)
            {
                _deathTime = Arena.TotalTime + FadeTime;
                Host = null;
            }
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
                _hostProxy = new LazyProxy<int, Gob>(Arena.FindGob);
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
            for (int ray = 0; ray < SLICE_COUNT + 1; ray++)
            {
                var fullRay = new Vector2(_triHeightForDamage, _triWidth * ((float)ray / SLICE_COUNT - 0.5f)).Rotate(Rotation);
                var distance = Arena.GetDistanceToClosest(Pos, Pos + fullRay,
                    area => area.Owner.MoveType != GobUtils.MoveType.Dynamic && area.Type.IsPhysical());
                _relativeLengths[ray] = (distance.HasValue ? distance.Value : fullRay.Length()) / _triHeightForDamage;
            }
            _vertexData = CreateVertexData(_relativeLengths);
        }

        private void PerformHits()
        {
            if (!_surroundHitDone) HitInNamedAreas("Surround", _surroundDamage);
            _surroundHitDone = true;
            if (!IsFadingOut && _nextHitTimer.IsElapsed)
            {
                UpdateConeCollisionAreas(_relativeLengths);
                HitInNamedAreas("Cone", _damagePerHit);
                PunchWalls();
            }
        }

        private void HitInNamedAreas(string areaName, float damage)
        {
            foreach (var hitArea in CollisionAreas.Where(a => a.Name == areaName))
            {
                Arena.QueryOverlappers(hitArea,
                    area => { Hit(area.Owner, damage); return true; },
                    area => area.Owner.IsDamageable);
            }
        }

        private void Hit(Gob gob, float damage)
        {
            if (!gob.IsDamageable || gob == Host) return;
            gob.InflictDamage(damage, new DamageInfo(this));
            GobHelper.CreatePengs(_hitEffects, gob);
            Game.Stats.SendHit(this, gob);
        }

        private void PunchWalls()
        {
            return; // TODO !!! Punch along damage polygon border.
            var startPos = Pos;
            var unitFront = Vector2.UnitX.Rotate(Rotation);
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

        private void InitializeCollisionAreas()
        {
            var newCollisionAreas = new CollisionArea[_collisionAreas.Length + SLICE_COUNT];
            Array.Copy(_collisionAreas, newCollisionAreas, _collisionAreas.Length);
            for (int i = 0; i < SLICE_COUNT; i++)
                newCollisionAreas[_collisionAreas.Length + i] = new CollisionArea("Cone",
                    new Triangle(Vector2.Zero, Vector2.UnitX, Vector2.UnitY), this,
                    CollisionAreaType.Damage, CollisionMaterialType.Regular);
            _collisionAreas = newCollisionAreas;
            foreach (var area in _collisionAreas) area.Disable();
        }

        private void UpdateConeCollisionAreas(float[] relativeLengths)
        {
            var vertices = GetSlices(relativeLengths).GetEnumerator();
            Func<Vector2> nextVertex = () =>
            {
                vertices.MoveNext();
                return vertices.Current * _triHeightForDamage * AWMathHelper.FARSEER_SCALE;
            };
            var coneAreas = CollisionAreas.Where(a => a.Name == "Cone");
            var sliceVerticeses = coneAreas.Select(a => ((PolygonShape)a.Fixture.Shape).Vertices);
            foreach (var sliceVertices in sliceVerticeses)
            {
                sliceVertices[0] = nextVertex();
                sliceVertices[1] = nextVertex();
                sliceVertices[2] = nextVertex();
            }
            Debug.Assert(!vertices.MoveNext());
        }

        /// <summary>
        /// Returns the vertex data for drawing the Triforce as a triangle list.
        /// </summary>
        private VertexPositionTexture[] CreateVertexData(float[] relativeLengths)
        {
            return GetSlices(relativeLengths)
                .Select(v => new VertexPositionTexture(
                    _triHeightForDamage * new Vector3(v, 0),
                    (v + Vector2.One) / 2))
                .ToArray();
        }

        /// <summary>
        /// Returns the slices of the triforce as triangles.
        /// The triangle coordinates are relative to the triforce height.
        /// The triangles are returned vertex by vertex as a triangle list.
        /// </summary>
        private IEnumerable<Vector2> GetSlices(float[] relativeLengths)
        {
            for (int i = 0; i < relativeLengths.Length - 1; i++)
            {
                yield return Vector2.Zero;
                yield return relativeLengths[i + 1] / _triHeightForDamage *
                    new Vector2(_triHeightForDamage, _triWidth * ((float)(i + 1) / (relativeLengths.Length - 1) - 0.5f));
                yield return relativeLengths[i] / _triHeightForDamage *
                    new Vector2(_triHeightForDamage, _triWidth * ((float)i / (relativeLengths.Length - 1) - 0.5f));
            }
        }
    }
}
