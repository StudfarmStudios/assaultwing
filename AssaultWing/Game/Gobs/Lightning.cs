using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A lightning that is shot from a gob into another gob who will get hurt.
    /// </summary>
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

        List<Segment> segments;
        Texture2D texture;
        VertexDeclaration vertexDeclaration;
        static BasicEffect effect;
        VertexPositionTexture[] vertexData;

        bool damageDealt;

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

        public Gob Shooter { get; set; }
        public int ShooterBoneIndex { get; set; }
        public Gob Target { get; set; }

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
        }

        #region Methods related to gobs' functionality in the game world

        public override void LoadContent()
        {
            base.LoadContent();
            texture = AssaultWing.Instance.Content.Load<Texture2D>(textureName);
            var gfx = AssaultWing.Instance.GraphicsDevice;
            vertexDeclaration = new VertexDeclaration(gfx, VertexPositionTexture.VertexElements);
            effect = effect ?? new BasicEffect(gfx, null);
            effect.World = Matrix.Identity;
            effect.Texture = texture;
            effect.TextureEnabled = true;
            effect.VertexColorEnabled = false;
            effect.LightingEnabled = false;
            effect.FogEnabled = false;
        }

        public override void Activate()
        {
            base.Activate();
            CreateMesh();
            damageDealt = false;
        }

        public override void Update()
        {
            base.Update();
            CreateMesh();
            if (!damageDealt)
            {
                Target.InflictDamage(impactDamage, new DeathCause(Target, DeathCauseType.Damage, this));
                damageDealt = true;
            }
            float seconds = birthTime.SecondsAgoGameTime();
            Alpha = alphaCurve.Evaluate(seconds);
            if (seconds >= alphaCurve.Keys.Last().Position)
                Die(new DeathCause());
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.VertexDeclaration = vertexDeclaration;
            gfx.RenderState.AlphaBlendEnable = true;
            gfx.RenderState.SourceBlend = Blend.SourceAlpha;
            gfx.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            effect.Projection = projection;
            effect.View = view;
            effect.Alpha = Alpha;
            effect.Begin();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, vertexData, 0, vertexData.Length - 2);
                pass.End();
            }
            effect.End();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Private methods

        void CreateMesh()
        {
            var start = Shooter.GetNamedPosition(ShooterBoneIndex);
            var end = Target.Pos;
            CreateSegments(start, end, wildness, fineness);
            CreateVertexData(thickness);
        }

        void CreateSegments(Vector2 start, Vector2 end, float wildness, float fineness)
        {
            segments = new List<Segment> { new Segment(start, end) }; // creates a list with just one element
            for (int i = 0; i < 7 && Fine(fineness, wildness); ++i) ;
        }

        /// <summary>
        /// Divides segments longer than given limit. Returns <c>true</c> if divisions were made.
        /// </summary>
        bool Fine(float fineness, float wildness)
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

        void CreateVertexData(float thickness)
        {
            var vertices = new List<VertexPositionTexture>(segments.Count * 2 + 2);
            float lastX = 0;
            Vector2 lastLeft, lastRight;
            ComputeExtrusionPoints(segments.First().StartPoint, segments.First().Vector, segments.First().Vector,
                thickness, out lastLeft, out lastRight);
            vertices.Add(new VertexPositionTexture(new Vector3(lastRight, 0), new Vector2(lastX, 0)));
            vertices.Add(new VertexPositionTexture(new Vector3(lastLeft, 0), new Vector2(lastX, 1)));
            for (int i = 0; i < segments.Count; ++i)
            {
                float x = lastX + segments[i].Length / (texture.Width * thickness);
                var inSegment = segments[i];
                var outSegment = segments[i < segments.Count - 1 ? i + 1 : i];
                Vector2 left, right;
                ComputeExtrusionPoints(inSegment.EndPoint, inSegment.Vector, outSegment.Vector,
                    thickness, out left, out right);
                vertices.Add(new VertexPositionTexture(new Vector3(right, 0), new Vector2(x, 0)));
                vertices.Add(new VertexPositionTexture(new Vector3(left, 0), new Vector2(x, 1)));
                lastX = x;
                lastLeft = left;
                lastRight = right;
            }
            vertexData = vertices.ToArray();
        }

        void ComputeExtrusionPoints(Vector2 middle, Vector2 @in, Vector2 @out, float thickness, out Vector2 left, out Vector2 right)
        {
            var unit = Vector2.Normalize(@in + @out);
            var extrusion = unit.Rotate90() * thickness * texture.Height / 2;
            left = middle + extrusion;
            right = middle - extrusion;
        }

        #endregion
    }
}
