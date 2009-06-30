using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component displaying a small damage bar and health
    /// percentage below a player's ship.
    /// </summary>
    class MiniStatusOverlay : OverlayComponent
    {
        float lastRelativeHealth;
        TimeSpan fadeoutFinishTime; // in game time
        Player player;
        Texture2D barFillTexture, barBackgroundTexture;
        SpriteFont healthFont;

        /// <summary>
        /// The dimensions of the component in pixels.
        /// </summary>
        /// The return value field <c>Point.X</c> is the width of the component,
        /// and the field <c>Point.Y</c> is the height of the component,
        public override Point Dimensions
        {
            get { return new Point(barBackgroundTexture.Width, barBackgroundTexture.Height + healthFont.LineSpacing); }
        }
        
        /// <summary>
        /// Creates a mini status display.
        /// </summary>
        /// <param name="player">The player whose status to display.</param>
        public MiniStatusOverlay(Player player)
            : base(HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            CustomAlignment = new Vector2(0, 40);
            this.player = player;
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
            if (player.Ship == null) return;

            // Calculate alpha level based on changes in player's ship damage.
            float opaqueDuration = 2; // component's opaque state duration in seconds
            float fadeoutDuration = 1; // component's transparent fadeout duration in seconds
            float relativeHealth = 1 - player.Ship.DamageLevel / player.Ship.MaxDamageLevel;
            if (lastRelativeHealth != relativeHealth)
            {
                fadeoutFinishTime = AssaultWing.Instance.GameTime.TotalGameTime + 
                    TimeSpan.FromSeconds(opaqueDuration + fadeoutDuration);
                lastRelativeHealth = relativeHealth;
            }
            float alpha = MathHelper.Clamp((float)(fadeoutFinishTime - AssaultWing.Instance.GameTime.TotalGameTime).TotalSeconds / fadeoutDuration, 0, 1);
            Color color = new Color(new Vector4(1, 1, 1, alpha));
            Color halfColor = new Color(new Vector4(1, 1, 1, alpha * 0.5f));

            // Health bar
            Rectangle healthBarRect = new Rectangle(0, 0,
                (int)Math.Ceiling(relativeHealth * barFillTexture.Width),
                barFillTexture.Height);
            spriteBatch.Draw(barBackgroundTexture, Vector2.Zero, color);
            spriteBatch.Draw(barFillTexture, new Vector2(1, 1), healthBarRect, color);

            // Health percentage
            string healthText = ((int)Math.Ceiling(relativeHealth * 100)).ToString() + "%";
            Vector2 textSize = healthFont.MeasureString(healthText);
            Vector2 textPos = new Vector2((int)((Dimensions.X - textSize.X) / 2), barBackgroundTexture.Height);
            spriteBatch.DrawString(healthFont, healthText, textPos, halfColor);
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            barFillTexture = content.Load<Texture2D>("mini_hpbar_fill");
            barBackgroundTexture = content.Load<Texture2D>("mini_hpbar_bg");
            healthFont = content.Load<SpriteFont>("ConsoleFont");
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
