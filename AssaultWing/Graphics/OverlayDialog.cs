using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;
using AW2.UI;
using AW2.Events;
using AW2.Game;

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
        string dialogText;
        Action<object> yesAction;
        Action<object> noAction;
        MultiControl dialogYesControls, dialogNoControls;

        /// <summary>
        /// The text to display in the dialog.
        /// </summary>
        public string DialogText { get { return dialogText; } set { dialogText = value; } }

        /// <summary>
        /// The action to perform when the user gives positive input.
        /// </summary>
        public Action<object> YesAction { set { yesAction = value; } }

        /// <summary>
        /// The action to perform when the user gives negative input.
        /// </summary>
        public Action<object> NoAction { set { noAction = value; } }

        /// <summary>
        /// Creates an overlay dialog.
        /// </summary>
        /// <param name="game">The game instance to attach the dialog to.</param>
        public OverlayDialog(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            dialogText = "Huh?";
            yesAction = delegate(object obj) { };
            noAction = delegate(object obj) { };
            dialogYesControls = new MultiControl(); 
            dialogYesControls.Add(new KeyboardKey(Keys.Y));
            dialogNoControls = new MultiControl();
            dialogNoControls.Add(new KeyboardKey(Keys.N));
            dialogNoControls.Add(new KeyboardKey(Keys.Escape));
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
            // Check our controls and react to them.
            if (dialogYesControls.Pulse)
                yesAction(null);
            if (dialogNoControls.Pulse)
                noAction(null);

#if DEBUG
            // Check for cheat codes.
            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.K) && keys.IsKeyDown(Keys.P))
            {
                // K + P = kill players
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                data.ForEachPlayer(delegate(Player player)
                {
                    if (player.Ship != null)
                        player.Ship.Die(new DeathCause());
                });
            }

            if (keys.IsKeyDown(Keys.L) && keys.IsKeyDown(Keys.E))
            {
                string dialogText = "Arena Ended!\nNext arena?";
                AssaultWing.Instance.ShowDialog(dialogText,
                    delegate(object obj)
                    {
                        AssaultWing.Instance.PlayNextArena();
                    },
                    delegate(object obj)
                    {
                        AssaultWing.Instance.ShowMenu();
                    });
            }
#endif

            base.Update(gameTime);
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        protected override void LoadContent()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            textWriter = data.GetFont(FontName.MenuFontSmall);
            dialogTexture = data.GetTexture(TextureName.OverlayDialogBackground);
            spriteBatch = new SpriteBatch(this.GraphicsDevice);
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        protected override void UnloadContent()
        {
            spriteBatch.Dispose();
            // Our textures and fonts are disposed by GraphicsEngine.
        }

        /// <summary>
        /// Called when the DrawableGameComponent needs to be drawn. Override this method
        /// with component-specific drawing code.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Microsoft.Xna.Framework.DrawableGameComponent.Draw(Microsoft.Xna.Framework.GameTime).</param>
        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            Vector2 dialogTopLeft = new Vector2(
                gfx.Viewport.Width - dialogTexture.Width,
                gfx.Viewport.Height - dialogTexture.Height) / 2;
            Vector2 textCenter = dialogTopLeft + new Vector2(dialogTexture.Width, dialogTexture.Height) / 2;
            Vector2 textSize = textWriter.MeasureString(dialogText);
            spriteBatch.Begin();
            spriteBatch.Draw(dialogTexture, dialogTopLeft, Color.White);
            spriteBatch.DrawString(textWriter, dialogText, textCenter - textSize / 2, Color.White);
            spriteBatch.End();
        }
   }
}