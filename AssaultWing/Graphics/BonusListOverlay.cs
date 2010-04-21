using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics
{
    public enum DisplayDirection { ENTER, EXIT };

    public class BonusOverlay
    {
        public BonusOverlay(GameAction action)
        {
            bonusEntryDirection = DisplayDirection.ENTER;
            bonusEntryPosAdjustments = Vector2.Zero;
            bonusEntryTimeins = AssaultWing.Instance.GameTime.TotalArenaTime;
            gameActionData = action;
        }

        public BonusOverlay()
        {
        }

        /// <summary>
        /// Times, in game time, at which the player's bonus boxes started
        /// sliding in to the player's viewport overlay or out of it.
        /// </summary>
        public TimeSpan bonusEntryTimeins;

        /// <summary>
        /// Start position relative X and Y adjustments for sliding bonus boxes, 
        /// usually between 0 and 1. The adjustment is the relative coordinate
        /// at which the box was when it started its current movement.
        /// Normally this is 0 for entering boxes and 1 for exiting boxes.
        /// </summary>
        public Vector2 bonusEntryPosAdjustments;

        /// <summary>
        /// Direction of movement of the overlay
        /// </summary>
        public DisplayDirection bonusEntryDirection;

        /// <summary>
        /// GameAction which we are displaying.
        /// </summary>
        public GameAction gameActionData;

        public Vector2 displayPosition;
    }

    /// <summary>
    /// Overlay graphics component displaying a player's current bonuses.
    /// </summary>
    class BonusListOverlay : OverlayComponent
    {
        Player player;
        Texture2D bonusBackgroundTexture;
        Texture2D bonusDurationTexture;
        SpriteFont bonusFont;

        /// <summary>
        /// All objects we need to display
        /// </summary>
        List<BonusOverlay> displayQueue = new List<BonusOverlay>();

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
            displayQueue.Add(new BonusOverlay
            {
                bonusEntryDirection = DisplayDirection.ENTER,
                bonusEntryPosAdjustments = Vector2.Zero,
            }
            );

        }

        /// <summary>
        /// Draws the overlay graphics component using the guarantee that the
        /// graphics device's viewport is set to the exact area needed by the component.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use. <c>Begin</c> is assumed
        /// to have been called and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        private void DrawBonusBox(SpriteBatch spriteBatch, Vector2 bonusPos, GameAction bonusAction)
        {
            // Figure out what to draw for this bonus.
            string bonusText = bonusAction.BonusText;
            Texture2D bonusIcon = bonusAction.BonusIcon;

            // Draw bonus box background.
            Vector2 backgroundOrigin = new Vector2(0, bonusBackgroundTexture.Height) / 2;
            spriteBatch.Draw(bonusBackgroundTexture,
                bonusPos, null, Color.White, 0, backgroundOrigin, 1, SpriteEffects.None, 0);

            // Draw bonus icon.
            Vector2 iconPos = bonusPos - backgroundOrigin + new Vector2(112, 9);
            spriteBatch.Draw(bonusIcon,
                iconPos, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Draw bonus duration meter.
            float startSeconds = (float)bonusAction.actionTimeins.TotalSeconds;
            float endSeconds = (float)bonusAction.actionTimeouts.TotalSeconds;
            float nowSeconds = (float)AssaultWing.Instance.GameTime.TotalArenaTime.TotalSeconds;
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
            spriteBatch.DrawString(bonusFont, bonusText, textPos.Round(), Color.White);
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
            var data = AssaultWing.Instance.DataEngine;
            //Remove expired bonusOverlays from the queue
            for (int i = displayQueue.Count - 1; i >= 1; i--)
            {
                BonusOverlay bonusOverlay = displayQueue[i];
                if (bonusOverlay.displayPosition.X >= 0 && bonusOverlay.bonusEntryDirection == DisplayDirection.EXIT)
                    displayQueue.RemoveAt(i);
            }

            //this dictionary is only used for reduce load from the loop that adds new objects to queue
            var gameActionsInQueue = new Dictionary<Type, BonusOverlay>();
            for (int i = 1; i < displayQueue.Count; i++)
            {
                BonusOverlay bonusOverlay = displayQueue[i];
                //if bonus is exitting it doesn't exist. when the same bonus is activated
                //when the bonusOverlay is exitting, the will be a new bonusOverlay as a last item it the list
                if (bonusOverlay.bonusEntryDirection == DisplayDirection.ENTER)
                    gameActionsInQueue.Add(bonusOverlay.gameActionData.GetType(), bonusOverlay);
            }

            //Loop bonusActions and add them to displayQueue if they don't exist yet
            foreach (GameAction action in player.BonusActions)
            {
                if (!gameActionsInQueue.Keys.Contains(action.GetType()))
                {
                    displayQueue.Add(new BonusOverlay(action));
                }
                else
                {
                    BonusOverlay bonusOverlay = gameActionsInQueue[action.GetType()];
                    bonusOverlay.gameActionData = action;
                }
            }

            //Handle Displayables
            for (int i = 1; i < displayQueue.Count; i++)
            {
                BonusOverlay bonusOverlay = displayQueue[i];
                float slideTime = (float)(AssaultWing.Instance.GameTime.TotalArenaTime.TotalSeconds
                - bonusOverlay.bonusEntryTimeins.TotalSeconds);

                Vector2 adjustment = bonusOverlay.bonusEntryPosAdjustments;
                Vector2 curvePos, shift, scale;

                //Do entry for bonusOverlay
                if (bonusOverlay.bonusEntryDirection == DisplayDirection.ENTER)
                {
                    curvePos = GetCurvePos(bonusBoxEntry, bonusBoxAvoid, slideTime);
                    shift = GetEntryShift(bonusBoxEntry, bonusBoxAvoid, adjustment);
                    scale = GetScale(bonusBoxEntry, bonusBoxAvoid, adjustment);
                } //do exit for bonusOverlay
                else
                {
                    curvePos = GetCurvePos(bonusBoxExit, bonusBoxClose, slideTime);
                    shift = GetExitShift(bonusBoxExit, bonusBoxClose, adjustment);
                    scale = GetScale(bonusBoxExit, bonusBoxClose, adjustment);
                }

                //get relative position
                Vector2 relativePos = new Vector2(
                    (curvePos.X + shift.X) * scale.X,
                    (curvePos.Y + shift.Y) * scale.Y);

                /*update bonusOverlay when the bonus in player ceases to be*/
                if (!player.BonusActions.Contains(bonusOverlay.gameActionData) && bonusOverlay.bonusEntryDirection == DisplayDirection.ENTER)
                {
                    bonusOverlay.bonusEntryPosAdjustments = relativePos;
                    bonusOverlay.bonusEntryTimeins = AssaultWing.Instance.GameTime.TotalArenaTime;
                    bonusOverlay.bonusEntryDirection = DisplayDirection.EXIT;
                }

                //calculate position for each displayable
                bonusOverlay.displayPosition = new Vector2(-bonusBackgroundTexture.Width * relativePos.X,
                        displayQueue[i - 1].displayPosition.Y + bonusBackgroundTexture.Height * relativePos.Y);
            }

            // Draw the bonus boxes in their places.
            Point dimensions = Dimensions;
            Vector2 bonusBoxAreaTopRight = new Vector2(dimensions.X * 2,
                dimensions.Y - displayQueue[displayQueue.Count - 1].displayPosition.Y) / 2;

            for (int i = 1; i < displayQueue.Count; ++i)
            {
                Vector2 leftMiddlePoint = new Vector2(displayQueue[i].displayPosition.X + bonusBoxAreaTopRight.X,
                    bonusBoxAreaTopRight.Y + (displayQueue[i].displayPosition.Y + displayQueue[i - 1].displayPosition.Y) / 2);
                DrawBonusBox(spriteBatch, leftMiddlePoint, displayQueue[i].gameActionData);
            }
        }

        /// <summary>
        /// called to get the Curve for position
        /// </summary>
        /// <param name="bonusBoxDirection"> Entry or Exit Direction</param>
        /// <param name="bonusBoxEffect"> Avoid or Close</param>
        /// <param name="slideTime"> Time passed since entry or exit</param>
        private Vector2 GetCurvePos(Curve bonusBoxDirection, Curve bonusBoxEffect, float slideTime)
        {
            return new Vector2(bonusBoxDirection.Evaluate(slideTime), bonusBoxEffect.Evaluate(slideTime));
        }
        /// <summary>
        /// called to get the shift for entry
        /// </summary>
        /// <param name="bonusBoxDirection"> Entry or Exit Direction</param>
        /// <param name="bonusBoxEffect"> Avoid or Close</param>
        /// <param name="adjustment"> Place where the adjustment starts</param>
        private Vector2 GetEntryShift(Curve bonusBoxDirection, Curve bonusBoxEffect, Vector2 adjustment)
        {
            return new Vector2(adjustment.X - bonusBoxDirection.Evaluate(0), adjustment.Y - bonusBoxEffect.Evaluate(0));
        }
        /// <summary>
        /// called to get the shift for exit
        /// </summary>
        /// <param name="bonusBoxDirection"> Entry or Exit Direction</param>
        /// <param name="bonusBoxEffect"> Avoid or Close</param>
        /// <param name="adjustment"> Place where the adjustment starts</param>
        private Vector2 GetExitShift(Curve bonusBoxDirection, Curve bonusBoxEffect, Vector2 adjustment)
        {
            return new Vector2(adjustment.Y - bonusBoxDirection.Evaluate(0), adjustment.Y - bonusBoxEffect.Evaluate(0));
        }
        /// <summary>
        /// called to get the scale
        /// </summary>
        /// <param name="bonusBoxDirection"> Entry or Exit Direction</param>
        /// <param name="bonusBoxEffect"> Avoid or Close</param>
        /// <param name="adjustment"> Place where the adjustment starts</param>
        private Vector2 GetScale(Curve bonusBoxDirection, Curve bonusBoxEffect, Vector2 adjustment)
        {
            return new Vector2((adjustment.X - bonusBoxDirection.Keys[bonusBoxDirection.Keys.Count - 1].Value)
                        / (bonusBoxDirection.Evaluate(0) - bonusBoxDirection.Keys[bonusBoxDirection.Keys.Count - 1].Value),
                        (adjustment.Y - bonusBoxEffect.Keys[bonusBoxEffect.Keys.Count - 1].Value)
                        / (bonusBoxEffect.Evaluate(0) - bonusBoxEffect.Keys[bonusBoxEffect.Keys.Count - 1].Value));
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            bonusBackgroundTexture = content.Load<Texture2D>("gui_bonus_bg");
            bonusDurationTexture = content.Load<Texture2D>("gui_bonus_duration");
            bonusFont = content.Load<SpriteFont>("ConsoleFont");
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public override void UnloadContent()
        {
            // Our textures and fonts are disposed by the graphics engine.
        }
    }
}
