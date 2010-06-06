using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Net;

namespace AW2.Game.Gobs
{
    public class GobProxy : INetworkSerializable
    {
        int _id;
        Arena _arena;
        Gob _gob;

        public Gob Gob
        {
            get
            {
                if (_id > 0)
                {
                    int id = _id;
                    _gob = _arena.Gobs.FirstOrDefault(gob => gob.Id == id);
                    if (_gob == null) Log.Write("ERROR: GobProxy cannot find gob by the ID " + id);
                    _id = 0;
                }
                return _gob;
            }
        }

        public GobProxy() { }
        public GobProxy(Gob gob) { _gob = gob; }

        public void SetId(int id) { _id = id; }
        public void SetArena(Arena arena) { _arena = arena; }

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                int gobID = Gob == null ? 0 : Gob.Id;
                writer.Write((int)gobID);
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, TimeSpan messageAge)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                SetId(reader.ReadInt32());
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
            }
        }
    }

    /// <summary>
    /// A lightning that is shot from a gob into another gob who will get hurt.
    /// </summary>
    [LimitedSerialization]
    public class Lightning : Gob
    {
        class Segment
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

        List<Segment> _segments;
        Texture2D _texture;
        VertexDeclaration _vertexDeclaration;
        static BasicEffect g_effect;
        VertexPositionTexture[] _vertexData;

        bool _damageDealt;

        /// <summary>
        /// Amount of damage to inflict on impact with a damageable gob.
        /// </summary>
        [TypeParameter]
        float impactDamage;

        /// <summary>
        /// Wildness of the lightning. 0 gives a straight line,
        /// 1 gives a very chaotic mess. 0.4 is a decent value.
        /// </summary>
        [TypeParameter]
        float wildness;

        /// <summary>
        /// Maximum length of each lightning segment in meters.
        /// </summary>
        [TypeParameter]
        float fineness;

        /// <summary>
        /// Thickness multiplier of the lightning. 1 makes the lightning
        /// as wide as its texture.
        /// </summary>
        [TypeParameter]
        float thickness;

        /// <summary>
        /// Name of the texture of the lightning. The name indexes the texture database in GraphicsEngine.
        /// </summary>
        [TypeParameter]
        CanonicalString textureName;

        /// <summary>
        /// Amount of lighting alpha as a function of time from creation,
        /// in seconds of game time.
        [TypeParameter]
        Curve alphaCurve;

        public GobProxy Shooter { get; set; }
        public int ShooterBoneIndex { get; set; }
        public GobProxy Target { get; set; }

        public override IEnumerable<CanonicalString> TextureNames
        {
            get { return base.TextureNames.Concat(new CanonicalString[] { textureName }); }
        }

        /// This constructor is only for serialisation.
        public Lightning()
            : base()
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

        /// <param name="typeName">The type of the lightning.</param>
        public Lightning(CanonicalString typeName)
            : base(typeName)
        {
            Shooter = new GobProxy();
            Target = new GobProxy();
        }

        #region Methods related to gobs' functionality in the game world

        public override void LoadContent()
        {
            base.LoadContent();
            _texture = AssaultWing.Instance.Content.Load<Texture2D>(textureName);
            var gfx = AssaultWing.Instance.GraphicsDevice;
            _vertexDeclaration = new VertexDeclaration(gfx, VertexPositionTexture.VertexElements);
            g_effect = g_effect ?? new BasicEffect(gfx, null);
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
            Shooter.SetArena(Arena);
            Target.SetArena(Arena);
            CreateMesh();
            _damageDealt = false;
        }

        public override void Update()
        {
            base.Update();
            CreateMesh();
            if (!_damageDealt)
            {
                if (Target.Gob != null)
                    Target.Gob.InflictDamage(impactDamage, new DeathCause(Target.Gob, DeathCauseType.Damage, this));
                _damageDealt = true;
            }
            float seconds = birthTime.SecondsAgoGameTime();
            Alpha = alphaCurve.Evaluate(seconds);
            if (seconds >= alphaCurve.Keys.Last().Position)
                Die(new DeathCause());
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.VertexDeclaration = _vertexDeclaration;
            gfx.RenderState.AlphaBlendEnable = true;
            gfx.RenderState.SourceBlend = Blend.SourceAlpha;
            gfx.RenderState.DestinationBlend = Blend.One;
            g_effect.Projection = projection;
            g_effect.View = view;
            g_effect.Alpha = Alpha;
            g_effect.Begin();
            foreach (EffectPass pass in g_effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, _vertexData, 0, _vertexData.Length - 2);
                pass.End();
            }
            g_effect.End();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        public override void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)Shooter.Gob.Id);
                writer.Write((int)ShooterBoneIndex);
                Target.Serialize(writer, mode);
            }
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
            }
        }

        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode, TimeSpan messageAge)
        {
            base.Deserialize(reader, mode, messageAge);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                Shooter.SetId(reader.ReadInt32());
                ShooterBoneIndex = reader.ReadInt32();
                Target.Deserialize(reader, mode, messageAge);
            }
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
            }
        }

        #endregion Methods related to serialisation

        #region Private methods

        private void CreateMesh()
        {
            if (Shooter.Gob == null)
            {
                // This will happen if 'Shooter' cannot be found by its gob ID
                _vertexData = new VertexPositionTexture[0];
                return;
            }
            var start = Shooter.Gob.GetNamedPosition(ShooterBoneIndex);
            if (Target.Gob != null)
            {
                _segments = new List<Segment> { new Segment(start, Target.Gob.Pos) };
            }
            else
            {
                var middle1 = Shooter.Gob.Pos + RandomHelper.GetRandomCirclePoint(100, Shooter.Gob.Rotation - MathHelper.PiOver4, Shooter.Gob.Rotation);
                var middle2 = Shooter.Gob.Pos + RandomHelper.GetRandomCirclePoint(100, Shooter.Gob.Rotation, Shooter.Gob.Rotation + MathHelper.PiOver4);
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
