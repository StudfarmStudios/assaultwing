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
        Texture2D backgroundTexture, cursorTexture, highlightTexture;

        /// <summary>
        /// Cursor fade curve as a function of time in seconds.
        /// Values range from 0 (transparent) to 255 (opaque).
        /// </summary>
        Curve cursorFade;

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
            cursorTexture = data.GetTexture(TextureName.MainMenuCursor);
            highlightTexture = data.GetTexture(TextureName.MainMenuHighlight);
            pos = new Vector2(0, 698);

            cursorFade = new Curve();
            cursorFade.Keys.Add(new CurveKey(0, 255, 0, 0, CurveContinuity.Step));
            cursorFade.Keys.Add(new CurveKey(0.5f, 0, 0, 0, CurveContinuity.Step));
            cursorFade.Keys.Add(new CurveKey(1, 255, 0, 0, CurveContinuity.Step));
            cursorFade.PreLoop = CurveLoopType.Cycle;
            cursorFade.PostLoop = CurveLoopType.Cycle;
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
                    if (currentMenu != MainMenuItem._FirstItem) currentMenu -= 3;
                }
                if (controlDown.Pulse)
                {
                    if (currentMenu != MainMenuItem._LastItem) currentMenu += 3;
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
            float time = (float)AssaultWing.Instance.GameTime.TotalRealTime.TotalSeconds;
            spriteBatch.Draw(backgroundTexture, pos - view, Color.White);
            Vector2 cursorPos = pos - view + new Vector2(551, 358 + (int)currentMenu * menuBigFont.LineSpacing);
            Vector2 highlightPos = cursorPos + new Vector2(cursorTexture.Width, 0);
            Vector2 textPos = pos - view + new Vector2(585, 355);
            spriteBatch.Draw(cursorTexture, cursorPos, new Color(255, 255, 255, (byte)cursorFade.Evaluate(time)));
            spriteBatch.Draw(highlightTexture, highlightPos, Color.White);
            spriteBatch.DrawString(menuBigFont, "Play Local\nPlay at the Battlefront\nSetup\nQuit",
                textPos, Color.White);
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
