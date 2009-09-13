using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// Message after bonus is consumed.
    /// </summary>
    public class BonusMessage : Gob
    {
        private readonly CanonicalString iconBackgroundName = (CanonicalString)"gui_bonuscollect_bg";
        private Texture2D icon;
        private Texture2D iconBackground;
        private SpriteFont messageFont;
        private static Curve scaleCurve;
        private static Curve alphaCurve;

        /// <summary>
        /// Message to display
        /// </summary>
        public string Message { set; get; }

        /// <summary>
        /// Icon to display
        /// </summary>
        public string IconName { set; get; }

        /// <summary>
        /// Names of all textures that this gob type will ever use.
        /// </summary>
        public override IEnumerable<CanonicalString> TextureNames
        {
            get { return base.TextureNames.Concat(new CanonicalString[] { iconBackgroundName }); }
        }

        /// <summary>
        /// Bounding volume of the 3D visuals of the gob, in world coordinates.
        /// </summary>
        public override BoundingSphere DrawBounds { get { return new BoundingSphere(); } }

        static BonusMessage()
        {
            scaleCurve = new Curve();
            scaleCurve.Keys.Add(new CurveKey(0, 0.15f));
            scaleCurve.Keys.Add(new CurveKey(0.3f, 1));
            scaleCurve.Keys.Add(new CurveKey(0.5f, 1));
            scaleCurve.ComputeTangents(CurveTangent.Smooth);
            scaleCurve.PostLoop = CurveLoopType.Constant;

            alphaCurve = new Curve();
            alphaCurve.Keys.Add(new CurveKey(0, 0));
            alphaCurve.Keys.Add(new CurveKey(0.2f, 1));
            alphaCurve.Keys.Add(new CurveKey(2.0f, 1));
            alphaCurve.Keys.Add(new CurveKey(2.5f, 0));
            alphaCurve.ComputeTangents(CurveTangent.Linear);
            alphaCurve.PostLoop = CurveLoopType.Constant;
        }

        /// <summary>
        /// Creates an uninitialised bonusmessage.
        /// </summary>
        /// This constructor is only for serialisation.
        public BonusMessage()
            : base()
        {
        }

        /// <summary>
        /// Creates a bonusmessage.
        /// </summary>
        /// <param name="typeName">The type of the bonusmessage.</param>
        public BonusMessage(CanonicalString typeName)
            : base(typeName)
        {
        }

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Draws the gob's 2D graphics.
        /// </summary>
        /// Assumes that the sprite batch has been Begun already and will be
        /// Ended later by someone else.
        /// <param name="gameToScreen">Transformation from game coordinates 
        /// to screen coordinates (pixels).</param>
        /// <param name="spriteBatch">The sprite batch to draw sprites with.</param>
        /// <param name="scale">Scale of graphics.</param>
        public override void Draw2D(Matrix gameToScreen, SpriteBatch spriteBatch, float scale)
        {
            Vector2 backgroundPos = Vector2.Transform(Pos, gameToScreen);

            float timePassed = birthTime.SecondsAgo();
            float finalScale = scaleCurve.Evaluate(timePassed) * scale;
            Vector2 origin = new Vector2(iconBackground.Width, iconBackground.Height) / 2;
            Vector2 iconPos = backgroundPos + new Vector2(-origin.X + 6, -icon.Height / 2) * finalScale;
            Vector2 textPos = backgroundPos + new Vector2(icon.Width + 6, 0) / 2 * finalScale;
            Vector2 textOrigin = messageFont.MeasureString(Message) / 2;
            Color drawColor = new Color(1, 1, 1, alphaCurve.Evaluate(timePassed));
            if (timePassed > scaleCurve.Keys.Last().Position)
            {
                textPos = textPos.Round();
                textOrigin = textOrigin.Round();
            }
            spriteBatch.Draw(iconBackground, backgroundPos, null, drawColor,
                0, origin, finalScale, SpriteEffects.None, 0.2f);
            spriteBatch.Draw(icon, iconPos, null, drawColor,
                0, Vector2.Zero, finalScale, SpriteEffects.None, 0.1f);
            spriteBatch.DrawString(messageFont, Message, textPos, drawColor, 0f, textOrigin, finalScale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Updates the gob according to physical laws.
        /// </summary>
        /// Overriden Update methods should explicitly call this method in order to have 
        /// physical laws apply to the gob and the gob's exhaust engines updated.
        public override void Update()
        {
            base.Update();
            float timePassed = birthTime.SecondsAgo();
            if (alphaCurve.Keys.Last().Position < timePassed)
                Die(new DeathCause());
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            base.LoadContent();
            icon = AssaultWing.Instance.Content.Load<Texture2D>(IconName);
            iconBackground = AssaultWing.Instance.Content.Load<Texture2D>(iconBackgroundName);
            messageFont = AssaultWing.Instance.Content.Load<SpriteFont>("ConsoleFont");
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public override void UnloadContent()
        {
            base.UnloadContent();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        /// <summary>
        /// Serialises the gob to a binary writer.
        /// </summary>
        public override void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)((CanonicalString)IconName).Canonical);
                writer.Write((string)Message, 48, false);
            }
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
            }
        }

        /// <summary>
        /// Deserialises the gob from a binary writer.
        /// </summary>
        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode)
        {
            base.Deserialize(reader, mode);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                int canonical = reader.ReadInt32();
                IconName = (CanonicalString)canonical;
                Message = reader.ReadString(48);
            }
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
            }
        }

        #endregion Methods related to serialisation
}
}
