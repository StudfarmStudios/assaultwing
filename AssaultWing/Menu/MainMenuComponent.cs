using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Net;
using AW2.Net.Messages;
using AW2.UI;

namespace AW2.Menu
{
    /// <summary>
    /// The main menu component where the user can choose to go play, go setup, or go away.
    /// </summary>
    class MainMenuComponent : MenuComponent
    {
        #region Fields

        /// <summary>
        /// The very first menu when the game starts.
        /// </summary>
        MainMenuContents startContents;

        /// <summary>
        /// Menu for establishing a network game.
        /// </summary>
        MainMenuContents networkContents;

        /// <summary>
        /// Currently active main menu contents.
        /// </summary>
        MainMenuContents currentContents;

        /// <summary>
        /// Index of the currently active menu item.
        /// </summary>
        int currentItem = 0;

        /// <summary>
        /// IP address of server to connect.
        /// </summary>
        EditableText connectAddress;

        string connectItemPrefix = "Connect to ";
        MultiControl controlUp, controlDown, controlSelect;
        Control controlBack;
        TriggeredCallbackCollection commonCallbacks;
        Vector2 pos; // position of the component's background texture in menu system coordinates
        SpriteFont menuBigFont;
        Texture2D backgroundTexture, cursorTexture, highlightTexture;

        /// <summary>
        /// Cursor fade curve as a function of time in seconds.
        /// Values range from 0 (transparent) to 255 (opaque).
        /// </summary>
        Curve cursorFade;

        /// <summary>
        /// Time at which the cursor started fading.
        /// </summary>
        TimeSpan cursorFadeStartTime;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Does the menu component react to input.
        /// </summary>
        public override bool Active
        {
            set
            {
                base.Active = value;
                if (value)
                {
                    // Update our controls to players' possibly changed controls.
                    InitializeControls();
                    InitializeControlCallbacks();

                    // Fall back to start menu.
                    currentContents = startContents;
                    currentItem = 0;

                    // Cut network connections.
                    if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
                        AssaultWing.Instance.StopClient();
                    if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                        AssaultWing.Instance.StopServer();
                }
            }
        }

        /// <summary>
        /// The center of the menu component in menu system coordinates.
        /// </summary>
        /// This is a good place to center the menu view to when the menu component
        /// is to be seen well on the screen.
        public override Vector2 Center { get { return pos + new Vector2(700, 495); } }

        #endregion Properties

        #region Constructor

        /// <summary>
        /// Creates a main menu component for a menu system.
        /// </summary>
        /// <param name="menuEngine">The menu system.</param>
        public MainMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            pos = new Vector2(0, 698);

            cursorFade = new Curve();
            cursorFade.Keys.Add(new CurveKey(0, 255, 0, 0, CurveContinuity.Step));
            cursorFade.Keys.Add(new CurveKey(0.5f, 0, 0, 0, CurveContinuity.Step));
            cursorFade.Keys.Add(new CurveKey(1, 255, 0, 0, CurveContinuity.Step));
            cursorFade.PreLoop = CurveLoopType.Cycle;
            cursorFade.PostLoop = CurveLoopType.Cycle;

            InitializeMenuContents();

            // Set initial menu contents
            currentContents = startContents;
        }

        #endregion Constructor

        #region Public methods

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            menuBigFont = data.GetFont(FontName.MenuFontBig);
            backgroundTexture = data.GetTexture(TextureName.MainMenuBackground);
            cursorTexture = data.GetTexture(TextureName.MainMenuCursor);
            highlightTexture = data.GetTexture(TextureName.MainMenuHighlight);
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public override void UnloadContent()
        {
            // The textures and fonts we reference will be disposed by GraphicsEngine.
        }

        /// <summary>
        /// Updates the menu component.
        /// </summary>
        public override void Update()
        {
            if (!Active) return;
            commonCallbacks.Update();

            // Text field editing
            if (currentContents == networkContents && currentItem == 1)
            {
                connectAddress.Update(() =>
                {
                    cursorFadeStartTime = AssaultWing.Instance.GameTime.TotalRealTime;
                    networkContents[1].Name = connectItemPrefix + connectAddress.Content;
                });
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
            Vector2 textPos = pos - view + new Vector2(585, 355);
            Vector2 cursorPos = pos - view + new Vector2(551, 358 + (int)currentItem * menuBigFont.LineSpacing);
            Vector2 highlightPos = cursorPos + new Vector2(cursorTexture.Width, 0);
            float cursorTime = (float)(AssaultWing.Instance.GameTime.TotalRealTime - cursorFadeStartTime).TotalSeconds;

            // HACK: Draw cursor as text field editing caret in a special case
            if (currentContents == networkContents && currentItem == 1)
            {
                Vector2 partialTextSize = menuBigFont.MeasureString(connectItemPrefix + 
                    connectAddress.Content.Substring(0, connectAddress.CaretPosition));
                cursorPos.X = textPos.X + partialTextSize.X;
            }
            spriteBatch.Draw(cursorTexture, cursorPos, new Color(255, 255, 255, (byte)cursorFade.Evaluate(cursorTime)));

            spriteBatch.Draw(highlightTexture, highlightPos, Color.White);
            for (int i = 0; i < currentContents.Count; ++i)
            {
                spriteBatch.DrawString(menuBigFont, currentContents[i].Name, textPos, Color.White);
                textPos.Y += menuBigFont.LineSpacing;
            }
        }

        #endregion Public methods

        #region Private methods

        private void InitializeMenuContents()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            startContents = new MainMenuContents("Start Menu", 4);
            startContents[0].Name = "Play Local";
            startContents[0].Action = () => menuEngine.ActivateComponent(MenuComponentType.Equip);
            startContents[1].Name = "Play at the Battlefront";
            startContents[1].Action = () => currentContents = networkContents;
            startContents[2].Name = "Setup";
            //startContents[2].Action = () => stuff;
            startContents[3].Name = "Quit";
            startContents[3].Action = AssaultWing.Instance.Exit;

            networkContents = new MainMenuContents("Battlefront Menu", 2);
            networkContents[0].Name = "Play as Server";
            networkContents[0].Action = () =>
            {
                if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone) return;
                AssaultWing.Instance.StartServer(result =>
                {
                    if (!result.Successful)
                        Log.Write("Some client failed to connect: " + result.Error);
                    else
                        Log.Write("Server obtained connection from " + result.Value.RemoteEndPoint);
                });
                menuEngine.ActivateComponent(MenuComponentType.Equip);

                // HACK: Force one local player and Amazonas as the only arena.
                int count = 0;
                data.RemovePlayers(player => count++ > 0);
                data.ArenaPlaylist = new List<string> { "Amazonas" };
            };

            connectAddress = new EditableText("192.168.11.2");
            networkContents[1].Name = connectItemPrefix + connectAddress.Content;
            networkContents[1].Action = () =>
            {
                if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone) return;
                AssaultWing.Instance.StartClient(connectAddress.Content, result =>
                {
                    if (!result.Successful)
                    {
                        Log.Write("Failed to connect to server: " + result.Error);
                        AssaultWing.Instance.StopClient();
                        return;
                    }
                    Log.Write("Client connected to " + result.Value.RemoteEndPoint);
                    menuEngine.ActivateComponent(MenuComponentType.Equip);

                    // HACK: Force one local player.
                    int count = 0;
                    data.RemovePlayers(player => count++ > 0);

                    // Send a game join request to the game server.
                    NetworkEngine net = (NetworkEngine)AssaultWing.Instance.Services.GetService(typeof(NetworkEngine));
                    JoinGameRequest joinGameRequest = new JoinGameRequest();
                    joinGameRequest.PlayerInfos = new List<PlayerInfo>();
                    data.ForEachPlayer(player => joinGameRequest.PlayerInfos.Add(new PlayerInfo(player)));
                    net.SendToServer(joinGameRequest);
                });
            };
        }

        private void InitializeControlCallbacks()
        {
            commonCallbacks = new TriggeredCallbackCollection();
            commonCallbacks.TriggeredCallback = () =>
            {
                cursorFadeStartTime = AssaultWing.Instance.GameTime.TotalRealTime;
            };
            commonCallbacks.Callbacks.Add(new TriggeredCallback(controlUp, () =>
            {
                if (currentItem > 0)
                    --currentItem;
            }));
            commonCallbacks.Callbacks.Add(new TriggeredCallback(controlDown, () =>
            {
                if (currentItem < currentContents.Count - 1)
                    ++currentItem;
            }));
            commonCallbacks.Callbacks.Add(new TriggeredCallback(controlSelect, () =>
            {
                currentContents[currentItem].Action();
            }));
            commonCallbacks.Callbacks.Add(new TriggeredCallback(controlBack, () =>
            {
                currentContents = startContents;
            }));
        }

        /// <summary>
        /// Sets up the menu component's controls based on players' current control setup.
        /// </summary>
        private void InitializeControls()
        {
            if (controlUp != null) controlUp.Release();
            if (controlDown != null) controlDown.Release();
            if (controlSelect != null) controlSelect.Release();
            if (controlBack != null) controlBack.Release();

            controlBack = new KeyboardKey(Keys.Escape);
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

        #endregion Private methods
    }

    /// <summary>
    /// Pluggable contents of the main menu, consisting of a list of menu items.
    /// </summary>
    public class MainMenuContents
    {
        List<MainMenuItem> menuItems;

        /// <summary>
        /// Name of the menu contents.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Number of menu items.
        /// </summary>
        public int Count { get { return menuItems.Count; } }

        /// <summary>
        /// The menu items.
        /// </summary>
        /// <param name="i">Zero-based index of the menu item.</param>
        /// <returns>The <c>i</c>'th menu item.</returns>
        public MainMenuItem this[int i] { get { return menuItems[i]; } }

        /// <summary>
        /// Creates new menu contents with a number of dummy menu items.
        /// </summary>
        /// <param name="name">Name of the menu contents.</param>
        /// <param name="menuItemCount">Number of menu items.</param>
        public MainMenuContents(string name, int menuItemCount)
        {
            if (name == null || name == "") throw new ArgumentNullException("Null or empty menu mode name");
            if (menuItemCount < 1) throw new ArgumentException("Must have at least one menu item");
            Name = name;
            menuItems = new List<MainMenuItem>(menuItemCount);
            for (int i = 0; i < menuItemCount; ++i)
                menuItems.Add(new MainMenuItem
                {
                    Name = "???",
                    Action = () => Log.Write("WARNING: Triggered an uninitialised menu item")
                });
        }
    }

    /// <summary>
    /// An item in the main menu, consisting of a visible name and an action
    /// to trigger when the item is selected.
    /// </summary>
    public class MainMenuItem
    {
        /// <summary>
        /// Visible name of the menu item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Action to perform on triggering the menu item.
        /// </summary>
        public Action Action { get; set; }
    }
}
