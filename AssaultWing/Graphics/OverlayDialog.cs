using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

namespace AW2.Graphics
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class OverlayDialog : Microsoft.Xna.Framework.DrawableGameComponent
    {
        SpriteFont textWriter;
        SpriteBatch spriteBatch;
        Texture2D dialogTexture;

        public OverlayDialog(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            // TODO: Construct any child components here
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here

            base.Update(gameTime);
        }

        //
        // Summary:
        //     Called when graphics resources need to be loaded. Override this method to
        //     load any component-specific graphics resources.
        protected override void LoadContent()
        {           
            textWriter = this.Game.Content.Load<SpriteFont>(System.IO.Path.Combine("fonts", "DotMatrix"));
            dialogTexture = this.Game.Content.Load<Texture2D>(System.IO.Path.Combine("textures", "dialog")); 
            spriteBatch = new SpriteBatch(this.GraphicsDevice);
        }

        //
        // Summary:
        //     Called when the DrawableGameComponent needs to be drawn. Override this method
        //     with component-specific drawing code.
        //
        // Parameters:
        //   gameTime:
        //     Time passed since the last call to Microsoft.Xna.Framework.DrawableGameComponent.Draw(Microsoft.Xna.Framework.GameTime).
        public override void Draw(GameTime gameTime)
        {
            #region Overlay menu
            spriteBatch.Begin();
            spriteBatch.Draw(dialogTexture, new Vector2(0, this.Game.GraphicsDevice.Viewport.Height / 2 - dialogTexture.Height / 2), Color.White);
            spriteBatch.DrawString(textWriter, "YOU WANT TO QUIT?", new Vector2(dialogTexture.Width/2, this.Game.GraphicsDevice.Viewport.Height / 2 - textWriter.LineSpacing / 2), Color.White);
            spriteBatch.End();
            #endregion

        }


    }
}