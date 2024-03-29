using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Players;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A message that floats in the arena.
    /// </summary>
    public class ArenaMessage : Gob
    {
        private Texture2D _icon;

        /// <summary>
        /// Visual scaling factor of the message as a function of seconds of game time since message birth.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private Curve _scaleCurve;

        /// <summary>
        /// Opaqueness level, between 0 (transparent) and 1 (opaque), of the message
        /// as a function of seconds of game time since message birth.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private Curve _alphaCurve;

        /// <summary>
        /// Message to display
        /// </summary>
        public string Message { set; get; }

        /// <summary>
        /// Icon to display
        /// </summary>
        public string IconName { set; get; }

        /// <summary>
        /// Tint color for the message
        /// </summary>
        public Color DrawColor { set; get; }

        public override void GetDraw3DBounds(out Vector2 min, out Vector2 max) { min = max = new Vector2(float.NaN); }
        private Texture2D IconBackground { get { return Game.GraphicsEngine.GameContent.ArenaMessageBackgroundTexture; } }
        private SpriteFont Font { get { return Game.GraphicsEngine.GameContent.ConsoleFont; } }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public ArenaMessage()
        {
            _scaleCurve = new Curve();
            _scaleCurve.Keys.Add(new CurveKey(0, 0.15f));
            _scaleCurve.Keys.Add(new CurveKey(0.6f, 0.15f));
            _scaleCurve.Keys.Add(new CurveKey(0.7f, 1));
            _scaleCurve.Keys.Add(new CurveKey(0.8f, 1));
            _scaleCurve.ComputeTangents(CurveTangent.Smooth);
            _scaleCurve.PostLoop = CurveLoopType.Constant;
            _alphaCurve = new Curve();
            _alphaCurve.Keys.Add(new CurveKey(0, 0));
            _alphaCurve.Keys.Add(new CurveKey(0.6f, 0));
            _alphaCurve.Keys.Add(new CurveKey(1.6f, 1));
            _alphaCurve.Keys.Add(new CurveKey(3.6f, 1));
            _alphaCurve.Keys.Add(new CurveKey(4.1f, 0));
            _alphaCurve.ComputeTangents(CurveTangent.Linear);
            _alphaCurve.PostLoop = CurveLoopType.Constant;
        }

        public ArenaMessage(CanonicalString typeName)
            : base(typeName)
        {
            DrawColor = Color.White;
        }

        public override void Draw2D(Matrix gameToScreen, SpriteBatch spriteBatch, float scale, Player viewer)
        {
            var backgroundPos = Vector2.Transform(Pos + DrawPosOffset, gameToScreen);
            var finalScale = _scaleCurve.Evaluate(AgeInGameSeconds) * scale;
            var origin = new Vector2(IconBackground.Width, IconBackground.Height) / 2;
            var iconPos = backgroundPos + new Vector2(-origin.X + 6, -_icon.Height / 2 - 1) * finalScale;
            var textPos = backgroundPos + new Vector2(_icon.Width + 1, 0) / 2 * finalScale;
            var textOrigin = Font.MeasureString(Message) / 2;
            var drawColor = Color.Multiply(DrawColor, _alphaCurve.Evaluate(AgeInGameSeconds));
            if (AgeInGameSeconds > _scaleCurve.Keys.Last().Position)
            {
                textPos = Vector2.Round(textPos);
                textOrigin = Vector2.Round(textOrigin);
            }
            var spriteDepth = 0.1f + (0.1f * ID) / Gob.MAX_ID; // Between 0.1 and 0.2
            spriteBatch.Draw(IconBackground, backgroundPos, null, drawColor,
                0, origin, finalScale, SpriteEffects.None, spriteDepth + 1E-7f);
            spriteBatch.Draw(_icon, iconPos, null, drawColor,
                0, Vector2.Zero, finalScale, SpriteEffects.None, spriteDepth);
            spriteBatch.DrawString(Font, Message, textPos, drawColor, 0f, textOrigin, finalScale, SpriteEffects.None, 0f);
        }

        public override void Update()
        {
            base.Update();
            if (_alphaCurve.Keys.Last().Position < AgeInGameSeconds)
                Die();
        }

        public override void LoadContent()
        {
            base.LoadContent();
            _icon = Game.Content.Load<Texture2D>(IconName);
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.Serialize(writer, mode);
                if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
                {
                    writer.Write((CanonicalString)IconName);
                    writer.Write((Color)DrawColor);
                    writer.Write((string)Message);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
                IconName = reader.ReadCanonicalString();
                DrawColor = reader.ReadColor();
                Message = reader.ReadString();
            }
        }
    }
}
