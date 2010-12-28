using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A lightning that is shot from a gob into another gob who will get hurt.
    /// </summary>
    [LimitedSerialization]
    public class Lightning : Gob
    {
        private class Segment
        {
            public Vector2 StartPoint { get; private set; }
            public Vector2 EndPoint { get; private set; }
            public float Length { get { return Vector2.Distance(StartPoint, EndPoint); } }
            public Vector2 Vector { get { return EndPoint - StartPoint; } }
            public Segment(Vector2 start, Vector2 end)
            {
                StartPoint = start;
                EndPoint = end;
            }
        }

        private List<Segment> _segments;
        private Texture2D _texture;
        private static BasicEffect g_effect;
        private VertexPositionTexture[] _vertexData;

        private bool _damageDealt;

        /// <summary>
        /// Amount of damage to inflict on impact with a damageable gob.
        /// </summary>
        [TypeParameter]
        private float impactDamage;

        /// <summary>
        /// Wildness of the lightning. 0 gives a straight line,
        /// 1 gives a very chaotic mess. 0.4 is a decent value.
        /// </summary>
        [TypeParameter]
        private float wildness;

        /// <summary>
        /// Maximum length of each lightning segment in meters.
        /// </summary>
        [TypeParameter]
        private float fineness;

        /// <summary>
        /// Thickness multiplier of the lightning. 1 makes the lightning
        /// as wide as its texture.
        /// </summary>
        [TypeParameter]
        private float thickness;

        /// <summary>
        /// Name of the texture of the lightning. The name indexes the texture database in GraphicsEngine.
        /// </summary>
        [TypeParameter]
        private CanonicalString textureName;

        /// <summary>
        /// Amount of lighting alpha as a function of time from creation,
        /// in seconds of game time.
        [TypeParameter]
        private Curve alphaCurve;

        public LazyProxy<int, Gob> Shooter { get; set; }
        public int ShooterBoneIndex { get; set; }
        public LazyProxy<int, Gob> Target { get; set; }

        public override IEnumerable<CanonicalString> TextureNames
        {
            get { return base.TextureNames.Concat(new CanonicalString[] { textureName }); }
        }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Lightning()
        {
            impactDamage = 200;
            wildness = 0.4f;
            fineness = 20;
            thickness = 1;
            textureName = (CanonicalString)"dummytexture";
            alphaCurve = new Curve();
            alphaCurve.Keys.Add(new CurveKey(0, 1));
            alphaCurve.Keys.Add(new CurveKey(0.5f, 0));
            alphaCurve.ComputeTangents(CurveTangent.Linear);
            alphaCurve.PreLoop = CurveLoopType.Constant;
            alphaCurve.PostLoop = CurveLoopType.Constant;
        }

        public Lightning(CanonicalString typeName)
            : base(typeName)
        {
            Shooter = new LazyProxy<int, Gob>(FindGob);
            Target = new LazyProxy<int, Gob>(FindGob);
        }

        #region Methods related to gobs' functionality in the game world

        public override void LoadContent()
        {
            base.LoadContent();
            _texture = Game.Content.Load<Texture2D>(textureName);
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            g_effect = g_effect ?? new BasicEffect(gfx);
            g_effect.World = Matrix.Identity;
            g_effect.Texture = _texture;
            g_effect.TextureEnabled = true;
            g_effect.VertexColorEnabled = false;
            g_effect.LightingEnabled = false;
            g_effect.FogEnabled = false;
        }

        public override void Activate()
        {
            base.Activate();
            CreateMesh();
            _damageDealt = false;
        }

        public override void Update()
        {
            base.Update();
            CreateMesh();
            if (!_damageDealt)
            {
                var target = Target.GetValue();
                if (target != null) target.InflictDamage(impactDamage, new DeathCause(target, DeathCauseType.Damage, this));
                _damageDealt = true;
            }
            Alpha = alphaCurve.Evaluate(AgeInGameSeconds);
            if (AgeInGameSeconds >= alphaCurve.Keys.Last().Position)
                Die();
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            gfx.BlendState = AW2.Graphics.GraphicsEngineImpl.AdditiveBlendPremultipliedAlpha;
            g_effect.Projection = projection;
            g_effect.View = view;
            g_effect.Alpha = Alpha;
            foreach (var pass in g_effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gfx.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, _vertexData, 0, _vertexData.Length - 2);
            }
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)Shooter.GetValue().ID);
                writer.Write((int)ShooterBoneIndex);
                var target = Target.GetValue();
                int targetID = target == null ? 0 : target.ID;
                writer.Write((int)targetID);
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                Shooter.SetData(reader.ReadInt32());
                ShooterBoneIndex = reader.ReadInt32();
                int targetID = reader.ReadInt32();
                if (targetID != 0) Target.SetData(targetID);
            }
        }

        #endregion Methods related to serialisation

        #region Private methods

        private Tuple<bool, Gob> FindGob(int id)
        {
            var gob = Arena.Gobs.FirstOrDefault(g => g.ID == id);
            return Tuple.Create(gob != null, gob);
        }

        private void CreateMesh()
        {
            if (Shooter.GetValue() == null)
            {
                // This will happen if 'Shooter' cannot be found by its gob ID
                _vertexData = new VertexPositionTexture[] {
                    new VertexPositionTexture(Vector3.Zero, Vector2.Zero),
                    new VertexPositionTexture(Vector3.Zero, Vector2.Zero),
                    new VertexPositionTexture(Vector3.Zero, Vector2.Zero)
                };
                return;
            }
            var start = Shooter.GetValue().GetNamedPosition(ShooterBoneIndex);
            if (Target.GetValue() != null)
            {
                _segments = new List<Segment> { new Segment(start, Target.GetValue().Pos) };
            }
            else
            {
                Gob shooter = Shooter;
                var drawRotation = shooter.Rotation + shooter.DrawRotationOffset;
                var middle1 = shooter.Pos + RandomHelper.GetRandomCirclePoint(100, drawRotation - MathHelper.PiOver4, drawRotation);
                var middle2 = shooter.Pos + RandomHelper.GetRandomCirclePoint(100, drawRotation, drawRotation + MathHelper.PiOver4);
                _segments = new List<Segment>
                {
                    new Segment(start, middle1),
                    new Segment(middle1, middle2),
                    new Segment(middle2, start)
                };
            }
            FineSegments(wildness, fineness);
            CreateVertexData(thickness);
        }

        private void FineSegments(float wildness, float fineness)
        {
            for (int i = 0; i < 7 && Fine(fineness, wildness, ref _segments); ++i) ;
        }

        /// <summary>
        /// Divides segments longer than given limit. Returns <c>true</c> if divisions were made.
        /// </summary>
        private static bool Fine(float fineness, float wildness, ref List<Segment> segments)
        {
            bool divided = false;
            var newSegments = new List<Segment>();
            foreach (var segment in segments)
            {
                if (segment.Length > fineness)
                {
                    var randomFront = segment.Vector * RandomHelper.GetRandomFloat(-wildness / 2, wildness / 2);
                    var randomSide = segment.Vector.Rotate90() * RandomHelper.GetRandomFloat(-wildness, wildness);
                    var middle = Vector2.Lerp(segment.StartPoint, segment.EndPoint, 0.5f) + randomFront + randomSide;
                    newSegments.Add(new Segment(segment.StartPoint, middle));
                    newSegments.Add(new Segment(middle, segment.EndPoint));
                    divided = true;
                }
                else
                    newSegments.Add(segment);
            }
            segments = newSegments;
            return divided;
        }

        private void CreateVertexData(float thickness)
        {
            var vertices = new List<VertexPositionTexture>(_segments.Count * 2 + 2);
            float lastX = 0;
            Vector2 lastLeft, lastRight;
            ComputeExtrusionPoints(_segments.First().StartPoint, _segments.First().Vector, _segments.First().Vector,
                thickness, out lastLeft, out lastRight);
            vertices.Add(new VertexPositionTexture(new Vector3(lastRight, 0), new Vector2(lastX, 0)));
            vertices.Add(new VertexPositionTexture(new Vector3(lastLeft, 0), new Vector2(lastX, 1)));
            for (int i = 0; i < _segments.Count; ++i)
            {
                float x = lastX + _segments[i].Length / (_texture.Width * thickness);
                var inSegment = _segments[i];
                var outSegment = _segments[i < _segments.Count - 1 ? i + 1 : i];
                Vector2 left, right;
                ComputeExtrusionPoints(inSegment.EndPoint, inSegment.Vector, outSegment.Vector,
                    thickness, out left, out right);
                vertices.Add(new VertexPositionTexture(new Vector3(right, 0), new Vector2(x, 0)));
                vertices.Add(new VertexPositionTexture(new Vector3(left, 0), new Vector2(x, 1)));
                lastX = x;
                lastLeft = left;
                lastRight = right;
            }
            _vertexData = vertices.ToArray();
        }

        private void ComputeExtrusionPoints(Vector2 middle, Vector2 @in, Vector2 @out, float thickness, out Vector2 left, out Vector2 right)
        {
            var unit = Vector2.Normalize(@in + @out);
            var extrusion = unit.Rotate90() * thickness * _texture.Height / 2;
            left = middle + extrusion;
            right = middle - extrusion;
        }

        #endregion
    }
}
