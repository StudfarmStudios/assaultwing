using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component displaying the player's status.
    /// </summary>
    class PlayerStatusOverlay : OverlayComponent
    {
        Player player;
        Texture2D statusDisplayTexture;
        Texture2D barShipTexture;
        Texture2D iconShipTexture;
        Texture2D barMainTexture;
        Texture2D iconWeaponLoadTexture;
        Texture2D barSpecialTexture;

        /// <summary>
        /// The dimensions of the component in pixels.
        /// </summary>
        /// The return value field <c>Point.X</c> is the width of the component,
        /// and the field <c>Point.Y</c> is the height of the component,
        public override Point Dimensions
        {
            get { return new Point(statusDisplayTexture.Width, statusDisplayTexture.Height); }
        }

        /// <summary>
        /// Creates a player status display.
        /// </summary>
        /// <param name="player">The player whose status to display.</param>
        public PlayerStatusOverlay(Player player)
            : base(HorizontalAlignment.Center, VerticalAlignment.Top)
        {
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
            // Status display background
            spriteBatch.Draw(statusDisplayTexture, Vector2.Zero, Color.White);

            // Damage meter
            if (player.Ship != null)
            {
                Rectangle damageBarRect = new Rectangle(0, 0,
                    (int)Math.Ceiling((1 - player.Ship.DamageLevel / player.Ship.MaxDamageLevel)
                    * barShipTexture.Width),
                    barShipTexture.Height);
                Color damageBarColor = Color.White;
                if (player.Ship.DamageLevel / player.Ship.MaxDamageLevel >= 0.8f)
                {
                    float seconds = (float)AssaultWing.Instance.GameTime.TotalRealTime.TotalSeconds;
                    if (seconds % 0.5f < 0.25f)
                        damageBarColor = Color.Red;
                }
                spriteBatch.Draw(barShipTexture,
                    new Vector2(statusDisplayTexture.Width, 8 * 2) / 2,
                    damageBarRect, damageBarColor, 0,
                    new Vector2(barShipTexture.Width, 0) / 2,
                    1, SpriteEffects.None, 0);
            }

            // Player lives left
            for (int i = 0; i < player.Lives; ++i)
                spriteBatch.Draw(iconShipTexture,
                    new Vector2(statusDisplayTexture.Width +
                                barShipTexture.Width + (8 + i * 10) * 2,
                                barShipTexture.Height + 8 * 2) / 2,
                    null,
                    Color.White,
                    0,
                    new Vector2(0, iconShipTexture.Height) / 2,
                    1, SpriteEffects.None, 0);

            // Primary weapon charge
            if (player.Ship != null)
            {
                Rectangle charge1BarRect = new Rectangle(0, 0,
                    (int)Math.Ceiling(player.Ship.Weapon1Charge / player.Ship.Weapon1ChargeMax
                    * barMainTexture.Width),
                    barMainTexture.Height);
                spriteBatch.Draw(barMainTexture,
                    new Vector2(statusDisplayTexture.Width, 24 * 2) / 2,
                    charge1BarRect, Color.White, 0,
                    new Vector2(barMainTexture.Width, 0) / 2,
                    1, SpriteEffects.None, 0);
            }

            // Primary weapon loadedness
            if (player.Ship != null)
            {
                if (player.Ship.Weapon1Loaded)
                {
                    float seconds = (float)(AssaultWing.Instance.GameTime.TotalGameTime - player.Ship.Weapon1.LoadedTime).TotalSeconds;
                    float scale = 1;
                    Color color = Color.White;
                    if (seconds < 0.2f)
                    {
                        //scale = MathHelper.Lerp(3, 1, seconds / 0.2f);
                        color = new Color(Vector4.Lerp(new Vector4(0, 1, 0, 0.1f), Vector4.One, seconds / 0.2f));
                    }
                    spriteBatch.Draw(iconWeaponLoadTexture,
                        new Vector2(statusDisplayTexture.Width + iconWeaponLoadTexture.Width +
                                    barMainTexture.Width + 8 * 2,
                                    barMainTexture.Height + 24 * 2) / 2,
                        null, color, 0,
                        new Vector2(iconWeaponLoadTexture.Width, iconWeaponLoadTexture.Height) / 2,
                        scale, SpriteEffects.None, 0);
                }
            }

            // Secondary weapon charge
            if (player.Ship != null)
            {
                Rectangle charge2BarRect = new Rectangle(0, 0,
                    (int)Math.Ceiling(player.Ship.Weapon2Charge / player.Ship.Weapon2ChargeMax
                    * barSpecialTexture.Width),
                    barSpecialTexture.Height);
                spriteBatch.Draw(barSpecialTexture,
                    new Vector2(statusDisplayTexture.Width, 40 * 2) / 2,
                    charge2BarRect, Color.White, 0,
                    new Vector2(barSpecialTexture.Width, 0) / 2,
                    1, SpriteEffects.None, 0);
            }

            // Secondary weapon loadedness
            if (player.Ship != null)
            {
                if (player.Ship.Weapon2Loaded)
                {
                    float seconds = (float)(AssaultWing.Instance.GameTime.TotalGameTime - player.Ship.Weapon2.LoadedTime).TotalSeconds;
                    float scale = 1;
                    Color color = Color.White;
                    if (seconds < 0.2f)
                    {
                        //scale = MathHelper.Lerp(3, 1, seconds / 0.2f);
                        color = new Color(Vector4.Lerp(new Vector4(0, 1, 0, 0.2f), Vector4.One, seconds / 0.2f));
                    }
                    spriteBatch.Draw(iconWeaponLoadTexture,
                        new Vector2(statusDisplayTexture.Width + iconWeaponLoadTexture.Width +
                                    barSpecialTexture.Width + 8 * 2,
                                    barSpecialTexture.Height + 40 * 2) / 2,
                        null, color, 0,
                        new Vector2(iconWeaponLoadTexture.Width, iconWeaponLoadTexture.Height) / 2,
                        scale, SpriteEffects.None, 0);
                }
            }
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            statusDisplayTexture = content.Load<Texture2D>("gui_playerinfo_bg");
            barShipTexture = content.Load<Texture2D>("gui_playerinfo_bar_ship");
            iconShipTexture = content.Load<Texture2D>("gui_playerinfo_ship");
            barMainTexture = content.Load<Texture2D>("gui_playerinfo_bar_main");
            iconWeaponLoadTexture = content.Load<Texture2D>("gui_playerinfo_loaded");
            barSpecialTexture = content.Load<Texture2D>("gui_playerinfo_bar_special");
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public override void UnloadContent()
        {
            // Our textures are disposed by the graphics engine.
        }
    }
}
