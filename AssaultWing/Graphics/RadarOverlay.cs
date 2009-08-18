using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component displaying a radar view of the arena.
    /// </summary>
    class RadarOverlay : OverlayComponent
    {
        Player player;
        Texture2D radarDisplayTexture;
        Texture2D shipOnRadarTexture;

        /// <summary>
        /// The dimensions of the component in pixels.
        /// </summary>
        /// The return value field <c>Point.X</c> is the width of the component,
        /// and the field <c>Point.Y</c> is the height of the component,
        public override Point Dimensions
        {
            get { return new Point(radarDisplayTexture.Width, radarDisplayTexture.Height); }
        }

        /// <summary>
        /// Creates a player status display.
        /// </summary>
        /// <param name="player">The player whose status to display.</param>
        public RadarOverlay(Player player)
            : base(HorizontalAlignment.Left, VerticalAlignment.Top)
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
            // Radar background
            spriteBatch.Draw(radarDisplayTexture, Vector2.Zero, Color.White);

            // Arena walls on radar
            Vector2 radarDisplayTopLeft = new Vector2(0, 1); // TODO: Make this constant configurable
            spriteBatch.Draw(AssaultWing.Instance.DataEngine.ArenaRadarSilhouette, radarDisplayTopLeft, Color.White);

            // Ships on radar
            Matrix arenaToRadarTransform = AssaultWing.Instance.DataEngine.ArenaToRadarTransform;
            Vector2 shipOnRadarTextureCenter = new Vector2(shipOnRadarTexture.Width, shipOnRadarTexture.Height) / 2;
            foreach (var player in AssaultWing.Instance.DataEngine.Players)
            {
                if (player.Ship == null) return;
                Vector2 posInArena = player.Ship.Pos;
                Vector2 posOnRadar = radarDisplayTopLeft + Vector2.Transform(posInArena, arenaToRadarTransform);
                spriteBatch.Draw(shipOnRadarTexture, posOnRadar, null, player.PlayerColor, 0,
                    shipOnRadarTextureCenter, 0.4f, SpriteEffects.None, 0);
            }
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            radarDisplayTexture = AssaultWing.Instance.Content.Load<Texture2D>("gui_radar_bg");
            shipOnRadarTexture = AssaultWing.Instance.Content.Load<Texture2D>("gui_playerinfo_white_ball");
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
