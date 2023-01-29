using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Game.Players;
using AW2.Helpers;

namespace AW2.Graphics.OverlayComponents
{
    /// <summary>
    /// Overlay graphics component displaying a player's current bonuses.
    /// </summary>
    public class BonusListOverlay : OverlayComponent
    {
        private enum DisplayDirection { Enter, Exit };

        [System.Diagnostics.DebuggerDisplay("BonusOverlay {bonusActionData.TypeName} {bonusEntryDirection} {bonusEntryTimeIn} {bonusEntryPosAdjustment}")]
        private class BonusOverlay
        {
            public BonusOverlay(BonusAction action)
            {
                bonusEntryDirection = DisplayDirection.Enter;
                bonusEntryPosAdjustment = Vector2.Zero;
                bonusEntryTimeIn = AssaultWingCore.Instance.DataEngine.ArenaTotalTime;
                bonusActionData = action;
            }

            /// <summary>
            /// Game time at which the bonus box started
            /// sliding in to the player's viewport overlay or out of it.
            /// </summary>
            public TimeSpan bonusEntryTimeIn;

            /// <summary>
            /// Start position relative X and Y adjustments for the sliding bonus box, 
            /// usually between 0 and 1. The adjustment is the relative coordinate
            /// at which the box was when it started its current movement.
            /// Normally this is 0 for entering boxes and 1 for exiting boxes.
            /// </summary>
            public Vector2 bonusEntryPosAdjustment;

            public DisplayDirection bonusEntryDirection;
            public BonusAction bonusActionData;
            public Vector2 displayPosition;
        }

        /// <summary>
        /// The X-movement curve of a bonus box that enters a player's
        /// viewport overlay.
        /// </summary>
        /// The curve defines the relative X-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// not visible; 1 means the box is fully visible.
        private static Curve g_bonusBoxEntry;

        /// <summary>
        /// The X-movement curve of a bonus box that leaves a player's
        /// viewport overlay.
        /// </summary>
        /// The curve defines the relative X-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// not visible; 1 means the box is fully visible.
        private static Curve g_bonusBoxExit;

        /// <summary>
        /// The Y-movement curve of a bonus box that is giving space for another
        /// bonus box that is entering a player's viewport overlay.
        /// </summary>
        /// The curve defines the relative Y-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// still blocking the other box; 1 means the box has moved totally aside.
        private static Curve g_bonusBoxAvoid;

        /// <summary>
        /// The Y-movement curve of a bonus box that is closing in space from another
        /// bonus box that is leaving a player's viewport overlay.
        /// </summary>
        /// The curve defines the relative Y-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// still blocking the other box; 1 means the box has moved totally aside.
        private static Curve g_bonusBoxClose;

        private Player _player;
        private Texture2D _bonusBackgroundTexture;
        private Texture2D _bonusDurationTexture;
        private SpriteFont _bonusFont;

        /// <summary>
        /// All objects we need to display
        /// </summary>
        private List<BonusOverlay> _displayQueue = new List<BonusOverlay>();

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
                var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
                return new Point(gfx.Viewport.Width, gfx.Viewport.Height);
            }
        }

        static BonusListOverlay()
        {
            g_bonusBoxEntry = new Curve();
            g_bonusBoxEntry.Keys.Add(new CurveKey(0, 0));
            g_bonusBoxEntry.Keys.Add(new CurveKey(0.3f, 1));
            g_bonusBoxEntry.Keys.Add(new CurveKey(0.9f, 1));
            g_bonusBoxEntry.Keys.Add(new CurveKey(2.0f, 1));
            g_bonusBoxEntry.ComputeTangents(CurveTangent.Smooth);
            g_bonusBoxEntry.PostLoop = CurveLoopType.Constant;
            g_bonusBoxExit = new Curve();
            g_bonusBoxExit.Keys.Add(new CurveKey(0, 1));
            g_bonusBoxExit.Keys.Add(new CurveKey(0.4f, 0.2f));
            g_bonusBoxExit.Keys.Add(new CurveKey(1.0f, 0));
            g_bonusBoxExit.ComputeTangents(CurveTangent.Smooth);
            g_bonusBoxExit.PostLoop = CurveLoopType.Constant;
            g_bonusBoxAvoid = new Curve();
            g_bonusBoxAvoid.Keys.Add(new CurveKey(0, 0));
            g_bonusBoxAvoid.Keys.Add(new CurveKey(0.2f, 0.8f));
            g_bonusBoxAvoid.Keys.Add(new CurveKey(0.5f, 1));
            g_bonusBoxAvoid.Keys.Add(new CurveKey(1.0f, 1));
            g_bonusBoxAvoid.ComputeTangents(CurveTangent.Smooth);
            g_bonusBoxAvoid.PostLoop = CurveLoopType.Constant;
            g_bonusBoxClose = new Curve();
            g_bonusBoxClose.Keys.Add(new CurveKey(0, 1));
            g_bonusBoxClose.Keys.Add(new CurveKey(0.4f, 0.8f));
            g_bonusBoxClose.Keys.Add(new CurveKey(1.0f, 0));
            g_bonusBoxClose.ComputeTangents(CurveTangent.Smooth);
            g_bonusBoxClose.PostLoop = CurveLoopType.Constant;
        }

        public BonusListOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Right, VerticalAlignment.Center)
        {
            _player = viewport.Owner;
            _displayQueue.Add(new BonusOverlay(null));
        }

        /// <summary>
        /// Draws the overlay graphics component using the guarantee that the
        /// graphics device's viewport is set to the exact area needed by the component.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use. <c>Begin</c> is assumed
        /// to have been called and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        private void DrawBonusBox(SpriteBatch spriteBatch, Vector2 bonusPos, BonusAction bonusAction)
        {
            // Draw bonus box background.
            Vector2 backgroundOrigin = new Vector2(0, _bonusBackgroundTexture.Height) / 2;
            spriteBatch.Draw(_bonusBackgroundTexture,
                bonusPos, null, Color.White, 0, backgroundOrigin, 1, SpriteEffects.None, 0);

            // Draw bonus icon.
            var iconPos = bonusPos - backgroundOrigin + new Vector2(133, 9);
            var icon = _player.Game.Content.Load<Texture2D>(bonusAction.BonusIconName);
            spriteBatch.Draw(icon, iconPos, null, _player.Color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Draw bonus duration meter.
            var duration = (bonusAction.EndTime - _player.Game.DataEngine.ArenaTotalTime).Divide(bonusAction.Duration);
            int durationHeight = (int)Math.Round(duration * _bonusDurationTexture.Height);
            int durationY = _bonusDurationTexture.Height - durationHeight;
            var durationClip = new Rectangle(0, durationY, _bonusDurationTexture.Width, durationHeight);
            var durationPos = bonusPos - backgroundOrigin + new Vector2(14, 8 + durationY);
            spriteBatch.Draw(_bonusDurationTexture,
                durationPos, durationClip, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Draw bonus text.
            // Round coordinates for beautiful text.
            var bonusText = bonusAction.BonusText;
            var textSize = _bonusFont.MeasureString(bonusText);
            var textPos = bonusPos - backgroundOrigin + new Vector2(32, 25.5f - textSize.Y / 2);
            spriteBatch.DrawString(_bonusFont, bonusText, Vector2.Round(textPos), Color.White);
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
            // Remove expired bonusOverlays from the queue.
            _displayQueue.RemoveAll(overlay => overlay.displayPosition.X >= 0 && overlay.bonusEntryDirection == DisplayDirection.Exit);

            // This dictionary is only used for reduce load from the loop that adds new objects to queue.
            var bonusActionsInQueue = _displayQueue
                .Skip(1)
                .Where(overlay => overlay.bonusEntryDirection == DisplayDirection.Enter)
                .ToDictionary(overlay => overlay.bonusActionData.GetType());

            // Loop bonusActions and add them to displayQueue if they don't exist yet.
            if (_player.Ship != null)
            {
                foreach (var action in _player.Ship.BonusActions)
                {
                    if (!bonusActionsInQueue.Keys.Contains(action.GetType()))
                    {
                        _displayQueue.Add(new BonusOverlay(action));
                    }
                    else
                    {
                        bonusActionsInQueue[action.GetType()].bonusActionData = action;
                    }
                }
            }

            for (int i = 1; i < _displayQueue.Count; i++)
            {
                var bonusOverlay = _displayQueue[i];
                float slideTime = (float)(AssaultWingCore.Instance.DataEngine.ArenaTotalTime.TotalSeconds
                    - bonusOverlay.bonusEntryTimeIn.TotalSeconds);

                var adjustment = bonusOverlay.bonusEntryPosAdjustment;
                Vector2 curvePos, shift, scale;

                if (bonusOverlay.bonusEntryDirection == DisplayDirection.Enter)
                {
                    // Do entry for bonusOverlay
                    curvePos = GetCurvePos(g_bonusBoxEntry, g_bonusBoxAvoid, slideTime);
                    shift = GetEntryShift(g_bonusBoxEntry, g_bonusBoxAvoid, adjustment);
                    scale = GetScale(g_bonusBoxEntry, g_bonusBoxAvoid, adjustment);
                }
                else
                {
                    // Do exit for bonusOverlay
                    curvePos = GetCurvePos(g_bonusBoxExit, g_bonusBoxClose, slideTime);
                    shift = GetExitShift(g_bonusBoxExit, g_bonusBoxClose, adjustment);
                    scale = GetScale(g_bonusBoxExit, g_bonusBoxClose, adjustment);
                }

                var relativePos = new Vector2(
                    (curvePos.X + shift.X) * scale.X,
                    (curvePos.Y + shift.Y) * scale.Y);

                // Update bonusOverlay when the bonus in player ceases to be.
                if (bonusOverlay.bonusEntryDirection == DisplayDirection.Enter &&
                    (_player.Ship == null || !_player.Ship.BonusActions.Contains(bonusOverlay.bonusActionData)))
                {
                    bonusOverlay.bonusEntryPosAdjustment = relativePos;
                    bonusOverlay.bonusEntryTimeIn = AssaultWingCore.Instance.DataEngine.ArenaTotalTime;
                    bonusOverlay.bonusEntryDirection = DisplayDirection.Exit;
                }

                bonusOverlay.displayPosition = new Vector2(-_bonusBackgroundTexture.Width * relativePos.X,
                    _displayQueue[i - 1].displayPosition.Y + _bonusBackgroundTexture.Height * relativePos.Y);
            }

            var bonusBoxAreaTopRight = new Vector2(Dimensions.X * 2,
                Dimensions.Y - _displayQueue[_displayQueue.Count - 1].displayPosition.Y) / 2;

            for (int i = 1; i < _displayQueue.Count; ++i)
            {
                var leftMiddlePoint = new Vector2(_displayQueue[i].displayPosition.X + bonusBoxAreaTopRight.X,
                    bonusBoxAreaTopRight.Y + (_displayQueue[i].displayPosition.Y + _displayQueue[i - 1].displayPosition.Y) / 2);
                DrawBonusBox(spriteBatch, leftMiddlePoint, _displayQueue[i].bonusActionData);
            }
        }

        /// <param name="bonusBoxDirection"> Entry or Exit Direction</param>
        /// <param name="bonusBoxEffect"> Avoid or Close</param>
        /// <param name="slideTime"> Time passed since entry or exit</param>
        private Vector2 GetCurvePos(Curve bonusBoxDirection, Curve bonusBoxEffect, float slideTime)
        {
            return new Vector2(bonusBoxDirection.Evaluate(slideTime), bonusBoxEffect.Evaluate(slideTime));
        }

        /// <param name="bonusBoxDirection"> Entry or Exit Direction</param>
        /// <param name="bonusBoxEffect"> Avoid or Close</param>
        /// <param name="adjustment"> Place where the adjustment starts</param>
        private Vector2 GetEntryShift(Curve bonusBoxDirection, Curve bonusBoxEffect, Vector2 adjustment)
        {
            return new Vector2(adjustment.X - bonusBoxDirection.Evaluate(0), adjustment.Y - bonusBoxEffect.Evaluate(0));
        }

        /// <param name="bonusBoxDirection"> Entry or Exit Direction</param>
        /// <param name="bonusBoxEffect"> Avoid or Close</param>
        /// <param name="adjustment"> Place where the adjustment starts</param>
        private Vector2 GetExitShift(Curve bonusBoxDirection, Curve bonusBoxEffect, Vector2 adjustment)
        {
            return new Vector2(adjustment.Y - bonusBoxDirection.Evaluate(0), adjustment.Y - bonusBoxEffect.Evaluate(0));
        }

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

        public override void LoadContent()
        {
            var content = AssaultWingCore.Instance.Content;
            _bonusBackgroundTexture = content.Load<Texture2D>("gui_bonus_bg");
            _bonusDurationTexture = content.Load<Texture2D>("gui_bonus_duration");
            _bonusFont = content.Load<SpriteFont>("ConsoleFont");
        }
    }
}
