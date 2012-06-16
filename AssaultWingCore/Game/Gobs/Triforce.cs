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
        [TypeParameter]
        private CanonicalString[] _surroundEffects;
        [TypeParameter]
        private float _surroundDamage;

        [TypeParameter]
        private float _range;
        [TypeParameter]
        private float _angle;
        [TypeParameter]
        private int _sliceCount;
        [TypeParameter]
        private float _damagePerHit;
        [TypeParameter]
        private TimeSpan _firstHitDelay;
        [TypeParameter]
        private TimeSpan _hitInterval;
        [TypeParameter]
        private TimeSpan _lifetime;
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
        /// <summary>
        /// Access via <see cref="RelativeSliceSlides"/>.
        /// </summary>
        private Vector2[] _relativeSliceSides;
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
        private bool IsHittable(CollisionArea area) { return area.Type.IsPhysical() && area.Owner.IsDamageable && area.Owner != Host; }
        /// <summary>
        /// Relative to the triforce's orientation and length.
        /// </summary>
        private Vector2[] RelativeSliceSides
        {
            get
            {
                if (_relativeSliceSides == null) _relativeSliceSides = new Vector2[_sliceCount + 1];
                return _relativeSliceSides;
            }
        }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Triforce()
        {
            _surroundEffects = new[] { (CanonicalString)"dummypeng" };
            _collisionAreas = new[] { new CollisionArea("Hit", new Circle(Vector2.Zero, 100), null, CollisionAreaType.Damage, CollisionMaterialType.Regular) };
            _surroundDamage = 500;
            _range = 500;
            _angle = MathHelper.PiOver4;
            _sliceCount = 15;
            _damagePerHit = 200;
            _firstHitDelay = TimeSpan.FromSeconds(0.1);
            _hitInterval = TimeSpan.FromSeconds(0.3);
            _lifetime = TimeSpan.FromSeconds(1.1);
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
            _vertexData = CreateVertexData(RelativeSliceSides);
        }

        public override void Activate()
        {
            base.Activate();
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
            for (int ray = 0; ray < _sliceCount + 1; ray++)
            {
                var relativeRayUnit = AWMathHelper.GetUnitVector2(_angle * ray / _sliceCount - _angle / 2);
                var worldRay = (_range * relativeRayUnit).Rotate(Rotation);
                var rayLength = Arena.GetDistanceToClosest(Pos, Pos + worldRay, area => area.Owner is Gobs.Wall);
                var relativeRayLength = rayLength.HasValue ? rayLength.Value / _range : 1f;
                RelativeSliceSides[ray] = relativeRayLength * relativeRayUnit;
            }
            _vertexData = CreateVertexData(RelativeSliceSides);
        }

        private void PerformHits()
        {
            if (!_surroundHitDone) HitInNamedAreas("Surround", _surroundDamage);
            _surroundHitDone = true;
            if (!IsFadingOut && _nextHitTimer.IsElapsed)
            {
                UpdateConeCollisionAreas(RelativeSliceSides);
                HitInNamedAreas("Cone", _damagePerHit);
                PunchWalls(RelativeSliceSides);
            }
        }

        private void HitInNamedAreas(string areaName, float damage)
        {
            var victims = new HashSet<Gob>();
            foreach (var hitArea in CollisionAreas.Where(a => a.Name == areaName))
            {
                Arena.QueryOverlappers(hitArea,
                    area => { if (IsHittable(area)) victims.Add(area.Owner); return true; },
                    area => area.Owner.IsDamageable);
            }
            foreach (var victim in victims) Hit(victim, damage);
        }

        private void Hit(Gob gob, float damage)
        {
            gob.InflictDamage(damage, new DamageInfo(this));
            GobHelper.CreatePengs(_hitEffects, gob);
            Game.Stats.SendHit(this, gob);
        }

        private void PunchWalls(Vector2[] relativeSliceSides)
        {
            foreach (var relativeSliceSide in relativeSliceSides)
            {
                var punchCenter = Pos + (_range * relativeSliceSide).Rotate(Rotation);
                if (Arena.MakeHole(punchCenter, _wallPunchRadius) == 0) continue;
                GobHelper.CreateGobs(_wallPunchEffects, Arena, punchCenter);
                if (Game.NetworkMode == Core.NetworkMode.Server)
                {
                    _wallPunchPosesForClient.Add(punchCenter);
                    ForcedNetworkUpdate = true;
                }
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
            var newCollisionAreas = new CollisionArea[_collisionAreas.Length + _sliceCount];
            Array.Copy(_collisionAreas, newCollisionAreas, _collisionAreas.Length);
            for (int i = 0; i < _sliceCount; i++)
                newCollisionAreas[_collisionAreas.Length + i] = new CollisionArea("Cone",
                    new Triangle(Vector2.Zero, Vector2.UnitX, Vector2.UnitY), this,
                    CollisionAreaType.Damage, CollisionMaterialType.Regular);
            _collisionAreas = newCollisionAreas;
            foreach (var area in _collisionAreas) area.Disable();
        }

        private void UpdateConeCollisionAreas(Vector2[] relativeSliceSides)
        {
            var vertices = GetSlices(relativeSliceSides).GetEnumerator();
            Func<Vector2> nextVertex = () =>
            {
                vertices.MoveNext();
                return vertices.Current * _range.ToFarseer();
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
        private VertexPositionTexture[] CreateVertexData(Vector2[] relativeSliceSides)
        {
            return GetSlices(relativeSliceSides)
                .Select(v => new VertexPositionTexture(
                    _range * new Vector3(v, 0),
                    (v + Vector2.One) / 2))
                .ToArray();
        }

        /// <summary>
        /// Returns the slices of the triforce as triangles.
        /// The triangle coordinates are relative to the triforce height.
        /// The triangles are returned vertex by vertex as a triangle list.
        /// </summary>
        private IEnumerable<Vector2> GetSlices(Vector2[] relativeSliceSides)
        {
            for (int i = 0; i < relativeSliceSides.Length - 1; i++)
            {
                yield return Vector2.Zero;
                yield return relativeSliceSides[i + 1];
                yield return relativeSliceSides[i];
            }
        }
    }
}
