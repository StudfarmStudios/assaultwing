using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.GobUtils;
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

        private static readonly VertexPositionTexture ZERO_VERTEX = new VertexPositionTexture(Vector3.Zero, Vector2.Zero);
        private static readonly VertexPositionTexture[] EMPTY_MESH = new[] { ZERO_VERTEX, ZERO_VERTEX, ZERO_VERTEX };
        private Texture2D _texture;
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
            get { return base.TextureNames.Concat(new[] { textureName }); }
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
        }

        public override void Activate()
        {
            base.Activate();
            _vertexData = CreateMesh();
            _damageDealt = false;
        }

        public override void Update()
        {
            base.Update();
            _vertexData = CreateMesh();
            if (!_damageDealt)
            {
                var target = Target.GetValue();
                if (target != null) target.InflictDamage(impactDamage, new DamageInfo(this));
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
            var effect = Game.GraphicsEngine.GameContent.LightningEffect;
            effect.Projection = projection;
            effect.View = view;
            effect.Alpha = Alpha;
            effect.Texture = _texture;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gfx.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, _vertexData, 0, _vertexData.Length - 2);
            }
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
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

        private VertexPositionTexture[] CreateMesh()
        {
            Gob shooter = Shooter;
            Gob target = Target;
            if (shooter == null) return EMPTY_MESH;
            var segments = GetInitialSegments(shooter, ShooterBoneIndex, target);
            segments = FineSegments(segments, wildness, fineness);
            return CreateVertexData(segments, _texture.Width * thickness, _texture.Height * thickness);
        }

        private static List<Segment> GetInitialSegments(Gob shooter, int shooterBoneIndex, Gob target)
        {
            var start = shooter.GetNamedPosition(shooterBoneIndex);
            if (target != null) return new List<Segment> { new Segment(start, target.Pos) };
            var drawRotation = shooter.Rotation + shooter.DrawRotationOffset;
            var middle1 = shooter.Pos + RandomHelper.GetRandomCirclePoint(100, drawRotation - MathHelper.PiOver4, drawRotation);
            var middle2 = shooter.Pos + RandomHelper.GetRandomCirclePoint(100, drawRotation, drawRotation + MathHelper.PiOver4);
            return new List<Segment>
            {
                new Segment(start, middle1),
                new Segment(middle1, middle2),
                new Segment(middle2, start)
            };
        }

        private static List<Segment> FineSegments(List<Segment> segments, float wildness, float fineness)
        {
            var workSegments = segments;
            for (int i = 0; i < 7; ++i)
            {
                int previousCount = workSegments.Count;
                workSegments = Fine(workSegments, wildness, fineness);
                if (workSegments.Count == previousCount) break;
            }
            return workSegments;
        }

        private static List<Segment> Fine(List<Segment> segments, float wildness, float fineness)
        {
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
                }
                else
                    newSegments.Add(segment);
            }
            return newSegments;
        }

        private static VertexPositionTexture[] CreateVertexData(List<Segment> segments, float width, float height)
        {
            var vertices = new List<VertexPositionTexture>(segments.Count * 2 + 2);
            float lastX = 0;
            Vector2 lastLeft, lastRight;
            var firstSegment = segments.First();
            ComputeExtrusionPoints(firstSegment.StartPoint, firstSegment.Vector, firstSegment.Vector, height, out lastLeft, out lastRight);
            vertices.Add(new VertexPositionTexture(new Vector3(lastRight, 0), new Vector2(lastX, 0)));
            vertices.Add(new VertexPositionTexture(new Vector3(lastLeft, 0), new Vector2(lastX, 1)));
            for (int i = 0; i < segments.Count; ++i)
            {
                float x = lastX + segments[i].Length / width;
                var inSegment = segments[i];
                var outSegment = segments[i < segments.Count - 1 ? i + 1 : i];
                Vector2 left, right;
                ComputeExtrusionPoints(inSegment.EndPoint, inSegment.Vector, outSegment.Vector, height, out left, out right);
                vertices.Add(new VertexPositionTexture(new Vector3(right, 0), new Vector2(x, 0)));
                vertices.Add(new VertexPositionTexture(new Vector3(left, 0), new Vector2(x, 1)));
                lastX = x;
                lastLeft = left;
                lastRight = right;
            }
            return vertices.ToArray();
        }

        private static void ComputeExtrusionPoints(Vector2 middle, Vector2 @in, Vector2 @out, float height, out Vector2 left, out Vector2 right)
        {
            var unit = Vector2.Normalize(@in + @out);
            var extrusion = unit.Rotate90() * height / 2;
            left = middle + extrusion;
            right = middle - extrusion;
        }

        #endregion
    }
}
