using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Graphics;
using AW2.UI;

namespace AW2.Menu
{
    /// <summary>
    /// The main menu component where the user can choose to go play, go setup, or go away.
    /// </summary>
    class MainMenuComponent : MenuComponent
    {
        /// <summary>
        /// An item on the main menu.
        /// </summary>
        enum MainMenuItem
        {
            /// <summary>
            /// Start a local play session.
            /// </summary>
            PlayLocal,

            /// <summary>
            /// Start a network play session.
            /// </summary>
            PlayNetwork,

            /// <summary>
            /// Set up Assault Wing's technical thingies.
            /// </summary>
            Setup,

            /// <summary>
            /// Shut down the game program.
            /// </summary>
            Quit,

            /// <summary>
            /// The first item in the main menu.
            /// </summary>
            _FirstItem = PlayLocal,

            /// <summary>
            /// The last item in the main menu.
            /// </summary>
            _LastItem = Quit,
        }

        MainMenuItem currentMenu = MainMenuItem._FirstItem;
        MultiControl controlUp, controlDown, controlSelect;
        Vector2 pos; // position of the component's background texture in menu system coordinates
        SpriteFont menuBigFont;
        Texture2D backgroundTexture;

        /// <summary>
        /// Does the menu component react to input.
        /// </summary>
        public override bool Active
        {
            set
            {
                base.Active = value;
                InitializeControls();
            }
        }

        /// <summary>
        /// The center of the menu component in menu system coordinates.
        /// </summary>
        /// This is a good place to center the menu view to when the menu component
        /// is to be seen well on the screen.
        public override Vector2 Center { get { return pos + new Vector2(700, 495); } }

        /// <summary>
        /// Creates a main menu component for a menu system.
        /// </summary>
        /// <param name="menuEngine">The menu system.</param>
        public MainMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            menuBigFont = data.GetFont(FontName.MenuFontBig);
            backgroundTexture = data.GetTexture(TextureName.MainMenuBackground);
            pos = new Vector2(0, 698);
        }

        /// <summary>
        /// Updates the menu component.
        /// </summary>
        public override void Update()
        {
            // Check our controls and react to them.
            if (Active)
            {
                if (controlUp.Pulse)
                {
                }
                if (controlDown.Pulse)
                {
                }
                if (controlSelect.Pulse)
                {
                    switch (currentMenu)
                    {
                        case MainMenuItem.PlayLocal:
                            menuEngine.ActivateComponent(MenuComponentType.Equip);
                            break;
                        case MainMenuItem.Quit:
                            AssaultWing.Instance.Exit();
                            break;
                        default: throw new Exception("Menu item " + currentMenu + " not implemented");
                    }
                }
            }
        }

        /// <summary>
        /// Draws the menu component.
        /// </summary>
        /// <param name="view">Top left corner of the menu view in menu system coordinates.</param>
        /// <param name="spriteBatch">The sprite batch to use. <c>Begin</c> is assumed
        /// to have been called and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(backgroundTexture, pos - view, Color.White);
            spriteBatch.DrawString(menuBigFont, "First Playable Demo 2008-04-27", pos - view + new Vector2(420, 50), Color.LightGray);
            switch (currentMenu)
            {
                case MainMenuItem.PlayLocal:
                    {
                        DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                        string arenaNames = "Play arenas:\n" +
                            String.Join("\n", data.ArenaPlaylist.ToArray());
                        string keysText1 = @"Player 1 controls:
W - Thrust
A - Turn left
D - Turn right
Left Ctrl - Fire primary
Left Shift - Fire secondary";
                        string keysText2 = @"Player 2 controls:
Numpad 8 - Thrust
Numpad 4 - Turn left
Numpad 6 - Turn right
Right Ctrl - Fire primary
Right Shift - Fire secondary";
                        string keysOther = "Esc closes the whole game.";
                        spriteBatch.DrawString(menuBigFont, arenaNames, pos - view + new Vector2(550, 150), Color.White);
                        spriteBatch.DrawString(menuBigFont, keysText1, pos - view + new Vector2(450, 300), Color.White);
                        spriteBatch.DrawString(menuBigFont, keysText2, pos - view + new Vector2(450, 460), Color.White);
                        spriteBatch.DrawString(menuBigFont, keysOther, pos - view + new Vector2(450, 620), Color.White);
                    } break;
                case MainMenuItem.Quit:
                    {
                        string text = "Exit the game";
                        spriteBatch.DrawString(menuBigFont, text, pos - view + new Vector2(550, 150), Color.White);
                    } break;
            }
        }


        /// <summary>
        /// Sets up the menu component's controls based on players' current control setup.
        /// </summary>
        void InitializeControls()
        {
            if (controlUp != null) controlUp.Release();
            if (controlDown != null) controlDown.Release();
            if (controlSelect != null) controlSelect.Release();

            controlUp = new MultiControl();
            controlUp.Add(new KeyboardKey(Keys.Up));
            controlDown = new MultiControl();
            controlDown.Add(new KeyboardKey(Keys.Down));
            controlSelect = new MultiControl();
            controlSelect.Add(new KeyboardKey(Keys.Enter));

            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            data.ForEachPlayer(delegate(Player player)
            {
                controlUp.Add(player.Controls.thrust);
                controlDown.Add(player.Controls.down);
                controlSelect.Add(player.Controls.fire1);
            });
        }
    }
}
