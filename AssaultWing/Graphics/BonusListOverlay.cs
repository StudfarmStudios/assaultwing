using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component displaying a player's current bonuses.
    /// </summary>
    class BonusListOverlay : OverlayComponent
    {
        Player player;
        Texture2D bonusBackgroundTexture;
        Texture2D bonusIconWeapon1LoadTimeTexture;
        Texture2D bonusIconWeapon2LoadTimeTexture;
        Texture2D bonusDurationTexture;
        SpriteFont bonusFont;
        
        /// <summary>
        /// Times, in game time, at which the player's bonus boxes started
        /// sliding in to the player's viewport overlay or out of it.
        /// </summary>
        PlayerBonusItems<TimeSpan> bonusEntryTimeins;

        /// <summary>
        /// Start position relative X and Y adjustments for sliding bonus boxes, 
        /// usually between 0 and 1. The adjustment is the relative coordinate
        /// at which the box was when it started its current movement.
        /// Normally this is 0 for entering boxes and 1 for exiting boxes.
        /// </summary>
        PlayerBonusItems<Vector2> bonusEntryPosAdjustments;

        /// <summary>
        /// Which directions the player's bonus boxes are moving in.
        /// <b>true</b> means entry movement;
        /// <b>false</b> means exit movement.
        /// </summary>
        PlayerBonusItems<bool> bonusEntryDirections;

        /// <summary>
        /// The X-movement curve of a bonux box that enters a player's
        /// viewport overlay.
        /// </summary>
        /// The curve defines the relative X-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// not visible; 1 means the box is fully visible.
        Curve bonusBoxEntry;

        /// <summary>
        /// The X-movement curve of a bonux box that leaves a player's
        /// viewport overlay.
        /// </summary>
        /// The curve defines the relative X-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// not visible; 1 means the box is fully visible.
        Curve bonusBoxExit;

        /// <summary>
        /// The Y-movement curve of a bonux box that is giving space for another
        /// bonus box that is entering a player's viewport overlay.
        /// </summary>
        /// The curve defines the relative Y-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// still blocking the other box; 1 means the box has moved totally aside.
        Curve bonusBoxAvoid;

        /// <summary>
        /// The Y-movement curve of a bonux box that is closing in space from another
        /// bonus box that is leaving a player's viewport overlay.
        /// </summary>
        /// The curve defines the relative Y-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// still blocking the other box; 1 means the box has moved totally aside.
        Curve bonusBoxClose;

        /// <summary>
        /// The dimensions of the component in pixels.
        /// </summary>
        /// The return value field <c>Point.X</c> is the width of the component,
        /// and the field <c>Point.Y</c> is the height of the component,
        public override Point Dimensions
        {
            get
            {
                // Our dimensions are changing and most often they involve fractions.
                // Therefore it's easiest to keep the viewport as it is.
                GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
                return new Point(gfx.Viewport.Width, gfx.Viewport.Height);
            }
        }

        /// <summary>
        /// Creates a bonus list overlay.
        /// </summary>
        /// <param name="player">The player whose bonuses to display.</param>
        public BonusListOverlay(Player player)
            : base(HorizontalAlignment.Right, VerticalAlignment.Center)
        {
            this.player = player;
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            bonusBackgroundTexture = data.GetTexture(TextureName.BonusBackground);
            bonusIconWeapon1LoadTimeTexture = data.GetTexture(TextureName.BonusIconWeapon1LoadTime);
            bonusIconWeapon2LoadTimeTexture = data.GetTexture(TextureName.BonusIconWeapon2LoadTime);
            bonusDurationTexture = data.GetTexture(TextureName.BonusDuration);
            bonusFont = data.GetFont(FontName.Overlay);

            bonusEntryTimeins = new PlayerBonusItems<TimeSpan>();
            bonusEntryPosAdjustments = new PlayerBonusItems<Vector2>();
            bonusEntryDirections = new PlayerBonusItems<bool>();

            bonusBoxEntry = new Curve();
            bonusBoxEntry.Keys.Add(new CurveKey(0, 0));
            bonusBoxEntry.Keys.Add(new CurveKey(0.3f, 1));
            bonusBoxEntry.Keys.Add(new CurveKey(0.9f, 1));
            bonusBoxEntry.Keys.Add(new CurveKey(2.0f, 1));
            bonusBoxEntry.ComputeTangents(CurveTangent.Smooth);
            bonusBoxEntry.PostLoop = CurveLoopType.Constant;
            bonusBoxExit = new Curve();
            bonusBoxExit.Keys.Add(new CurveKey(0, 1));
            bonusBoxExit.Keys.Add(new CurveKey(0.4f, 0.2f));
            bonusBoxExit.Keys.Add(new CurveKey(1.0f, 0));
            bonusBoxExit.ComputeTangents(CurveTangent.Smooth);
            bonusBoxExit.PostLoop = CurveLoopType.Constant;
            bonusBoxAvoid = new Curve();
            bonusBoxAvoid.Keys.Add(new CurveKey(0, 0));
            bonusBoxAvoid.Keys.Add(new CurveKey(0.2f, 0.8f));
            bonusBoxAvoid.Keys.Add(new CurveKey(0.5f, 1));
            bonusBoxAvoid.Keys.Add(new CurveKey(1.0f, 1));
            bonusBoxAvoid.ComputeTangents(CurveTangent.Smooth);
            bonusBoxAvoid.PostLoop = CurveLoopType.Constant;
            bonusBoxClose = new Curve();
            bonusBoxClose.Keys.Add(new CurveKey(0, 1));
            bonusBoxClose.Keys.Add(new CurveKey(0.4f, 0.8f));
            bonusBoxClose.Keys.Add(new CurveKey(1.0f, 0));
            bonusBoxClose.ComputeTangents(CurveTangent.Smooth);
            bonusBoxClose.PostLoop = CurveLoopType.Constant;
        }

        /// <summary>
        /// Draws the overlay graphics component using the guarantee that the
        /// graphics device's viewport is set to the exact area needed by the component.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use. <c>Begin</c> is assumed
        /// to have been called and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            // Lists bottom left coordinates of areas reserved for displayed
            // bonus boxes relative to this overlay component's top right corner,
            // with the exception that the first coordinates are (0,0).
            // Bonus boxes are then drawn vertically centered in these areas.
            // The last Y coordinate will state the height of the whole reserved bonus box area.
            List<Vector2> bonusPos = new List<Vector2>();

            // Lists the types of bonuses whose coordinates are in 'bonusPos'.
            List<PlayerBonus> bonusBonus = new List<PlayerBonus>();

            bonusPos.Add(Vector2.Zero);
            bonusBonus.Add(PlayerBonus.None);

            // Find out which boxes to draw and where.
            foreach (PlayerBonus bonus in Enum.GetValues(typeof(PlayerBonus)))
            {
                if (bonus == PlayerBonus.None) continue;

                // Calculate bonus box position.
                float slideTime = (float)(AssaultWing.Instance.GameTime.TotalGameTime.TotalSeconds
                    - bonusEntryTimeins[bonus].TotalSeconds);
                Vector2 adjustment = bonusEntryPosAdjustments[bonus];
                Vector2 curvePos, shift, scale;
                if (bonusEntryDirections[bonus])
                {
                    curvePos = new Vector2(bonusBoxEntry.Evaluate(slideTime), bonusBoxAvoid.Evaluate(slideTime));
                    shift = new Vector2(adjustment.X - bonusBoxEntry.Evaluate(0),
                        adjustment.Y - bonusBoxAvoid.Evaluate(0));
                    scale = new Vector2((adjustment.X - bonusBoxEntry.Keys[bonusBoxEntry.Keys.Count - 1].Value)
                        / (bonusBoxEntry.Evaluate(0) - bonusBoxEntry.Keys[bonusBoxEntry.Keys.Count - 1].Value),
                        (adjustment.Y - bonusBoxAvoid.Keys[bonusBoxAvoid.Keys.Count - 1].Value)
                        / (bonusBoxAvoid.Evaluate(0) - bonusBoxAvoid.Keys[bonusBoxAvoid.Keys.Count - 1].Value));
                }
                else
                {
                    curvePos = new Vector2(bonusBoxExit.Evaluate(slideTime), bonusBoxClose.Evaluate(slideTime));
                    shift = new Vector2(adjustment.Y - bonusBoxExit.Evaluate(0),
                        adjustment.Y - bonusBoxClose.Evaluate(0));
                    scale = new Vector2((adjustment.X - bonusBoxExit.Keys[bonusBoxExit.Keys.Count - 1].Value)
                        / (bonusBoxExit.Evaluate(0) - bonusBoxExit.Keys[bonusBoxExit.Keys.Count - 1].Value),
                        (adjustment.Y - bonusBoxClose.Keys[bonusBoxClose.Keys.Count - 1].Value)
                        / (bonusBoxClose.Evaluate(0) - bonusBoxClose.Keys[bonusBoxClose.Keys.Count - 1].Value));
                }
                Vector2 relativePos = new Vector2(
                    (curvePos.X + shift.X) * scale.X,
                    (curvePos.Y + shift.Y) * scale.Y);
                //adjustment.X + (1 - adjustment.X) * curvePos.X,
                //adjustment.Y + (1 - adjustment.Y) * curvePos.Y);
                Vector2 newBonusPos = new Vector2(-bonusBackgroundTexture.Width * relativePos.X,
                        bonusPos[bonusPos.Count - 1].Y + bonusBackgroundTexture.Height * relativePos.Y);

                // React to changes in player's bonuses.
                if ((player.Bonuses & bonus) != 0 &&
                    !bonusEntryDirections[bonus])
                {
                    bonusEntryDirections[bonus] = true;
                    bonusEntryPosAdjustments[bonus] = relativePos;
                    bonusEntryTimeins[bonus] = AssaultWing.Instance.GameTime.TotalGameTime;
                }
                if ((player.Bonuses & bonus) == 0 &&
                    bonusEntryDirections[bonus])
                {
                    bonusEntryDirections[bonus] = false;
                    bonusEntryPosAdjustments[bonus] = relativePos;
                    bonusEntryTimeins[bonus] = AssaultWing.Instance.GameTime.TotalGameTime;
                }

                // Draw the box only if it's visible.
                if (newBonusPos.X < 0)
                {
                    bonusBonus.Add(bonus);
                    bonusPos.Add(newBonusPos);
                }
            }

            // Draw the bonus boxes in their places.
            Point dimensions = Dimensions;
            Vector2 bonusBoxAreaTopRight = new Vector2(dimensions.X * 2,
                dimensions.Y - bonusPos[bonusPos.Count - 1].Y) / 2;
            for (int i = 1; i < bonusBonus.Count; ++i)
            {
                Vector2 leftMiddlePoint = new Vector2(bonusPos[i].X + bonusBoxAreaTopRight.X,
                    bonusBoxAreaTopRight.Y + (bonusPos[i].Y + bonusPos[i - 1].Y) / 2);
                DrawBonusBox(spriteBatch, leftMiddlePoint, bonusBonus[i]);
            }
        }

        /// <summary>
        /// Draws a player bonus box.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use. <c>Begin</c> is assumed
        /// to have been called and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        /// <param name="bonusPos">The position at which to draw 
        /// the background's left middle point.</param>
        /// <param name="playerBonus">Which player bonus it is.</param>
        private void DrawBonusBox(SpriteBatch spriteBatch, Vector2 bonusPos, PlayerBonus playerBonus)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

            // Figure out what to draw for this bonus.
            string bonusText;
            Texture2D bonusIcon;
            switch (playerBonus)
            {
                case PlayerBonus.Weapon1LoadTime:
                    bonusText = player.Weapon1Name + "\nspeedloader";
                    bonusIcon = bonusIconWeapon1LoadTimeTexture;
                    break;
                case PlayerBonus.Weapon2LoadTime:
                    bonusText = player.Weapon2Name + "\nspeedloader";
                    bonusIcon = bonusIconWeapon2LoadTimeTexture;
                    break;
                case PlayerBonus.Weapon1Upgrade:
                    {
                        Weapon weapon1 = player.Ship != null
                            ? player.Ship.Weapon1
                            : (Weapon)data.GetTypeTemplate(typeof(Weapon), player.Weapon1Name);
                        bonusText = player.Weapon1Name;
                        bonusIcon = data.GetTexture(weapon1.IconName);
                    }
                    break;
                case PlayerBonus.Weapon2Upgrade:
                    {
                        Weapon weapon2 = player.Ship != null
                            ? player.Ship.Weapon2
                            : (Weapon)data.GetTypeTemplate(typeof(Weapon), player.Weapon2Name);
                        bonusText = player.Weapon2Name;
                        bonusIcon = data.GetTexture(weapon2.IconName);
                    }
                    break;
                default:
                    bonusText = "<unknown>";
                    bonusIcon = data.GetTexture(TextureName.IconWeaponLoad);
                    Log.Write("Warning: Don't know how to draw player bonus box " + playerBonus);
                    throw new ArgumentException("Don't know how to draw player bonus box " + playerBonus);
            }

            // Draw bonus box background.
            Vector2 backgroundOrigin = new Vector2(0, bonusBackgroundTexture.Height) / 2;
            spriteBatch.Draw(bonusBackgroundTexture,
                bonusPos, null, Color.White, 0, backgroundOrigin, 1, SpriteEffects.None, 0);

            // Draw bonus icon.
            Vector2 iconPos = bonusPos - backgroundOrigin + new Vector2(112, 9);
            spriteBatch.Draw(bonusIcon,
                iconPos, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Draw bonus duration meter.
            float startSeconds = (float)player.BonusTimeins[playerBonus].TotalSeconds;
            float endSeconds = (float)player.BonusTimeouts[playerBonus].TotalSeconds;
            float nowSeconds = (float)AssaultWing.Instance.GameTime.TotalGameTime.TotalSeconds;
            float duration = (endSeconds - nowSeconds) / (endSeconds - startSeconds);
            int durationHeight = (int)Math.Round(duration * bonusDurationTexture.Height);
            int durationY = bonusDurationTexture.Height - durationHeight;
            Rectangle durationClip = new Rectangle(0, durationY, bonusDurationTexture.Width, durationHeight);
            Vector2 durationPos = bonusPos - backgroundOrigin + new Vector2(14, 8 + durationY);
            spriteBatch.Draw(bonusDurationTexture,
                durationPos, durationClip, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Draw bonus text.
            // Round coordinates for beautiful text.
            Vector2 textSize = bonusFont.MeasureString(bonusText);
            Vector2 textPos = bonusPos - backgroundOrigin + new Vector2(32, 23.5f - textSize.Y / 2);
            textPos.X = (float)Math.Round(textPos.X);
            textPos.Y = (float)Math.Round(textPos.Y);
            spriteBatch.DrawString(bonusFont, bonusText, textPos, Color.White);
        }
    }
}
