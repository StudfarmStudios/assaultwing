#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Graphics;
using AW2.Menu;
using AW2.UI;
using AW2.Sound;
using AW2.Events;
using AW2.Helpers;
using AW2.Net;
using AW2.Net.Messages;
using Form = System.Windows.Forms.Form;

#endregion

namespace AW2
{
    /// <summary>
    /// The state of the game.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// The game is active.
        /// </summary>
        Gameplay,

        /// <summary>
        /// An arena is being loaded in the middle of a gameplay session.
        /// </summary>
        ArenaLoading,

        /// <summary>
        /// The game overlay dialog is visible, game is active but paused.
        /// </summary>
        OverlayDialog,

        /// <summary>
        /// The menu is active.
        /// </summary>
        Menu,
    }

    /// <summary>
    /// Mode of network operation.
    /// </summary>
    public enum NetworkMode
    {
        /// <summary>
        /// Acting as a standalone game session, no networking involved.
        /// </summary>
        Standalone,

        /// <summary>
        /// Acting as a client in a game session.
        /// </summary>
        Client,

        /// <summary>
        /// Acting as a server in a game session.
        /// </summary>
        Server,
    }

    /// <summary>
    /// The main class of the Assault Wing game. A singleton class.
    /// </summary>
    /// Game components can be requested from the AssaultWing.Services property.
    public class AssaultWing : Microsoft.Xna.Framework.Game
    {
        #region AssaultWing fields

        UIEngineImpl uiEngine;
        GraphicsEngineImpl graphicsEngine;
        OverlayDialog overlayDialog;
        MenuEngineImpl menuEngine;
        NetworkEngine networkEngine;
        LogicEngine logicEngine;
        DataEngine dataEngine;
        PhysicsEngine physicsEngine;
        SoundEngineImpl soundEngine;
        EventEngineImpl eventEngine;
        ContentManager content;
        GraphicsDeviceManager graphics;
        int preferredWindowWidth, preferredWindowHeight;
        SurfaceFormat preferredWindowFormat;
        int preferredFullscreenWidth, preferredFullscreenHeight;
        SurfaceFormat preferredFullscreenFormat;
        TimeSpan gameTimeDelay;
        TimeSpan lastFramerateCheck;
        int framesSinceLastCheck;
        GameState gameState;
        GameTime gameTime;
        Rectangle clientBoundsMin;
        Form windowForm;

        // HACK: Fields for frame stepping (for debugging)
        Control frameStepControl;
        Control frameRunControl;
        bool frameStep;

#if DEBUG_PROFILE
        /// <summary>
        /// Gob count for the current frame.
        /// </summary>
        public int gobCount;
        /// <summary>
        /// Collision count for the current frame.
        /// </summary>
        public int collisionCount;
        List<int> frameCounts = new List<int>();
        List<int> gobCounts = new List<int>();
        List<int> collisionCounts = new List<int>();
#endif

        /// <summary>
        /// Time of previously finished call to Draw(), in game time.
        /// </summary>
        TimeSpan lastDrawTime;

        /// <summary>
        /// The only existing instance of this class.
        /// </summary>
        static AssaultWing instance;

        #endregion AssaultWing fields

        #region AssaultWing properties

        /// <summary>
        /// Returns (after creating) the only instance of class AssaultWing.
        /// </summary>
        public static AssaultWing Instance
        {
            get
            {
                if (instance == null)
                    instance = new AssaultWing();
                return instance;
            }
        }

        /// <summary>
        /// The physics engine of the game instance.
        /// </summary>
        public PhysicsEngine PhysicsEngine { get { return physicsEngine; } }

        /// <summary>
        /// The data engine of the game instance.
        /// </summary>
        public DataEngine DataEngine { get { return dataEngine; } }

        /// <summary>
        /// The network engine of the game instance.
        /// </summary>
        public NetworkEngine NetworkEngine { get { return networkEngine; } }

        /// <summary>
        /// The current state of the game.
        /// </summary>
        public GameState GameState { get { return gameState; } }

        /// <summary>
        /// The current mode of network operation of the game.
        /// </summary>
        public NetworkMode NetworkMode { get; private set; }

        /// <summary>
        /// The game time on this frame.
        /// </summary>
        public GameTime GameTime { get { return gameTime; } }

        /// <summary>
        /// Time of previously finished call to Draw(), in game time.
        /// </summary>
        public TimeSpan LastDrawTime { get { return lastDrawTime; } }

        /// <summary>
        /// The screen dimensions of the game window's client rectangle.
        /// </summary>
        public Rectangle ClientBounds { get { return Window.ClientBounds; } }

        /// <summary>
        /// The minimum allowed screen dimensions of the game window's client rectangle.
        /// </summary>
        public Rectangle ClientBoundsMin
        {
            get { return clientBoundsMin; }
            set
            {
                clientBoundsMin = value;
                // Enforce new minimum bounds.
                if (Window.ClientBounds.Width < clientBoundsMin.Width ||
                    Window.ClientBounds.Height < clientBoundsMin.Height)
                    Window_ClientSizeChanged(null, null);
            }
        }

        #endregion AssaultWing properties

        #region AssaultWing private methods

        /// <summary>
        /// Creates a new Assault Wing - Galactic Battlefront game instance.
        /// </summary>
        /// This constructor is not meant to be called from outside this class.
        /// To obtain an AssaultWing instance, use <b>AssaultWing.Instance</b>.
        private AssaultWing()
            : base()
        {
            Log.Write("Creating an Assault Wing instance");

            // Decide on preferred windowed and fullscreen sizes and formats.
            DisplayMode displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            preferredFullscreenWidth = displayMode.Width;
            preferredFullscreenHeight = displayMode.Height;
            preferredFullscreenFormat = displayMode.Format;
            preferredWindowWidth = 1000;
            preferredWindowHeight = 800;
            preferredWindowFormat = displayMode.Format;
            clientBoundsMin.Width = 1000;
            clientBoundsMin.Height = 800;

            content = new ContentManager(Services);
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = preferredWindowWidth;
            graphics.PreferredBackBufferHeight = preferredWindowHeight;
            // loading the NVIDIA perfHUD device if available
            graphics.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(graphics_PreparingDeviceSettings);
            graphics.IsFullScreen = false;
            Window.ClientSizeChanged += new EventHandler(Window_ClientSizeChanged);
            Window.AllowUserResizing = true;

            frameStepControl = new KeyboardKey(Keys.F8);
            frameRunControl = new KeyboardKey(Keys.F7);
            frameStep = false;
            gameTimeDelay = new TimeSpan(0);

            lastFramerateCheck = new TimeSpan(0);
            framesSinceLastCheck = 0;
            gameState = GameState.Gameplay;
            NetworkMode = NetworkMode.Standalone;
            gameTime = new GameTime();
            lastDrawTime = new TimeSpan(0);
        }

        /// <summary>
        /// If there is an NVIDIA PerfHUD adapter, set the GraphicsDeviceManager to use that adapter, and a Reference Device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
#if false
            e.GraphicsDeviceInformation.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
            if (e.GraphicsDeviceInformation.PresentationParameters.IsFullScreen)
                e.GraphicsDeviceInformation.PresentationParameters.FullScreenRefreshRateInHz = 60;
#endif
#if DEBUG
            foreach (GraphicsAdapter adapter in GraphicsAdapter.Adapters)
            {
                if (adapter.Description.Equals("NVIDIA PerfHUD"))
                {
                    e.GraphicsDeviceInformation.DeviceType = DeviceType.Reference;
                    e.GraphicsDeviceInformation.Adapter = adapter;
                    Log.Write("Found NVIDIA PerfHUD device, PerfHUD now enabled.");
                    break;
                }
            }
#endif
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            // If screen dimensions are below acceptable bounds, make the window bigger.
            if (ClientBounds.Width < clientBoundsMin.Width ||
                ClientBounds.Height < clientBoundsMin.Height)
            {
                int newClientWidth = Math.Max(ClientBounds.Width, clientBoundsMin.Width);
                int newClientHeight = Math.Max(ClientBounds.Height, clientBoundsMin.Height);
                IntPtr ptr = Window.Handle;
                System.Windows.Forms.Form form = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(ptr);
                form.Size = new System.Drawing.Size(newClientWidth, newClientHeight);
                graphics.PreferredBackBufferWidth = newClientWidth;
                graphics.PreferredBackBufferHeight = newClientHeight;
                graphics.ApplyChanges();
            }
            graphicsEngine.WindowResize();
            menuEngine.WindowResize();
        }

        /// <summary>
        /// Changes game state.
        /// </summary>
        /// <param name="newState">The state to change to.</param>
        void ChangeState(GameState newState)
        {
            // Disable current state.
            switch (gameState)
            {
                case GameState.Gameplay:
                    logicEngine.Enabled = false;
                    graphicsEngine.Visible = false;
                    break;
                case GameState.ArenaLoading:
                    overlayDialog.Enabled = false;
                    overlayDialog.Visible = false;
                    break;
                case GameState.Menu:
                    menuEngine.Enabled = false;
                    menuEngine.Visible = false;
                    break;
                case GameState.OverlayDialog:
                    overlayDialog.Enabled = false;
                    overlayDialog.Visible = false;
                    graphicsEngine.Visible = false;
                    break;
                default:
                    throw new Exception("Unhandled game state " + gameState + " in AssaultWing.ChangeState()");
            }

            // Enable new state.
            switch (newState)
            {
                case GameState.Gameplay:
                    logicEngine.Enabled = true;
                    graphicsEngine.Visible = true;
                    break;
                case GameState.ArenaLoading:
                    overlayDialog.Enabled = true;
                    overlayDialog.Visible = true;
                    break;
                case GameState.Menu:
                    menuEngine.Enabled = true;
                    menuEngine.Visible = true;
                    break;
                case GameState.OverlayDialog:
                    overlayDialog.Enabled = true;
                    overlayDialog.Visible = true;
                    graphicsEngine.Visible = true;
                    break;
                default:
                    throw new Exception("Unhandled game state " + newState + " in AssaultWing.ChangeState()");
            }

            gameState = newState;
        }

        #endregion AssaultWing private methods

        #region Methods for game components

        /// <summary>
        /// Prepares a new play session to start from the first chosen arena.
        /// Call <c>StartArena</c> after this method returns to start
        /// playing the arena.
        /// </summary>
        public void PrepareFirstArena()
        {
            foreach (var player in dataEngine.Players)
                player.Kills = player.Suicides = 0;

            // Notify game clients if we are the game server.
            if (NetworkMode == NetworkMode.Server)
            {
                var message = new StartGameMessage();
                int playerCount = 0;
                foreach (var player in dataEngine.Players)
                {
                    ++playerCount;
                    message.Write(player, SerializationModeFlags.All);
                }
                message.PlayerCount = playerCount;
                message.ArenaPlaylist = dataEngine.ArenaPlaylist;
                networkEngine.SendToClients(message);
            }

            dataEngine.ArenaPlaylist.Reset();
            PrepareNextArena();
        }

        /// <summary>
        /// Starts playing a previously prepared arena.
        /// </summary>
        public void StartArena()
        {
            dataEngine.StartArena();
            if (NetworkMode == NetworkMode.Server)
                networkEngine.SendToClients(new ArenaStartRequest());
            graphicsEngine.RearrangeViewports();
            ChangeState(GameState.Gameplay);
            soundEngine.PlayMusic(dataEngine.Arena);
        }

        /// <summary>
        /// Finishes playing the current arena.
        /// </summary>
        public void FinishArena()
        {
            if (gameState == GameState.OverlayDialog)
                ChangeState(GameState.ArenaLoading); // HACK !!!
            if (DataEngine.ArenaPlaylist.HasNext)
                ShowDialog(new ArenaOverOverlayDialogData(DataEngine.ArenaPlaylist.Next));
            else
                ShowDialog(new GameOverOverlayDialogData());
            if (NetworkMode == NetworkMode.Server)
            {
                var message = new ArenaFinishMessage();
                networkEngine.SendToClients(message);
            }
        }

        /// <summary>
        /// Prepares an ongoing play session to move to the next chosen arena.
        /// Call <c>StartArena</c> after this method returns to start
        /// playing the arena.
        /// </summary>
        /// This method usually takes a long time to run. It's therefore a good
        /// idea to make it run in a background thread.
        public void PrepareNextArena()
        {
            if (gameState == GameState.OverlayDialog)
                ChangeState(GameState.ArenaLoading); // HACK !!!

            // Disallow window resizing during arena loading.
            // A window resize event may reset the graphics card, fatally
            // screwing up initialisation of walls' index maps.
            bool oldAllowUserResizing = Window.AllowUserResizing;
            if (oldAllowUserResizing)
            {
                // We cannot set Window.AllowUserResizing = false; if we are
                // not in the main thread. Therefore, use Invoke() of the underlying Form.
                windowForm.Invoke(new Action(() => { Window.AllowUserResizing = false; }));
            }

            try
            {
                if (!DataEngine.NextArena())
                    throw new InvalidOperationException("There is no next arena to play");
            }
            finally
            {
                if (oldAllowUserResizing)
                    windowForm.Invoke(new Action(() => { Window.AllowUserResizing = true; }));
            }
        }

        /// <summary>
        /// Resumes playing the current arena, closing the dialog if it's visible.
        /// </summary>
        public void ResumePlay()
        {
            ChangeState(GameState.Gameplay);
        }

        /// <summary>
        /// Displays the dialog on top of the game and stops updating the game logic.
        /// </summary>
        /// <param name="dialogData">The contents and actions for the dialog.</param>
        public void ShowDialog(OverlayDialogData dialogData)
        {
            overlayDialog.Data = dialogData;
            if (DataEngine.ProgressBar.TaskRunning)
                ChangeState(GameState.ArenaLoading); // HACK !!!
            else
                ChangeState(GameState.OverlayDialog);
        }

        /// <summary>
        /// Displays the main menu and stops any ongoing gameplay.
        /// </summary>
        public void ShowMenu()
        {
            soundEngine.StopMusic();
            dataEngine.ClearGameState();
            menuEngine.ActivateComponent(MenuComponentType.Main);
            ChangeState(GameState.Menu);
        }

        /// <summary>
        /// Toggles between fullscreen and windowed mode.
        /// </summary>
        public void ToggleFullscreen()
        {
            lock (GraphicsDevice)
            {
                // Set our window size and format preferences before switching.
                if (graphics.IsFullScreen)
                {
                    graphics.PreferredBackBufferFormat = preferredWindowFormat;
                    graphics.PreferredBackBufferHeight = preferredWindowHeight;
                    graphics.PreferredBackBufferWidth = preferredWindowWidth;
                }
                else
                {
                    graphics.PreferredBackBufferFormat = preferredFullscreenFormat;
                    graphics.PreferredBackBufferHeight = preferredFullscreenHeight;
                    graphics.PreferredBackBufferWidth = preferredFullscreenWidth;
                }
                graphics.ToggleFullScreen();
            }
        }

        /// <summary>
        /// Rearranges player viewports, optionally so that 
        /// the whole screen area is given to only one player.
        /// </summary>
        /// <param name="player">The player to give all the screen space to,
        /// or <b>-1</b> to share the screen equally.</param>
        public void ShowOnlyPlayer(int player)
        {
            if (player < 0)
                graphicsEngine.RearrangeViewports();
            else
                graphicsEngine.RearrangeViewports(player);
        }

        /// <summary>
        /// Turns this game instance into a game server to whom other game instances
        /// can connect as game clients.
        /// </summary>
        /// <param name="connectionHandler">Handler of connection result.</param>
        public void StartServer(Action<Result<Connection>> connectionHandler)
        {
            if (NetworkMode != NetworkMode.Standalone)
                throw new InvalidOperationException("Cannot start server while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Server;
            try
            {
                networkEngine.StartServer(connectionHandler);
            }
            catch (Exception e)
            {
                Log.Write("Could not start server: " + e);
                NetworkMode = NetworkMode.Standalone;
            }
        }

        /// <summary>
        /// Turns this game server into a standalone game instance and disposes of
        /// any connections to game clients.
        /// </summary>
        public void StopServer()
        {
            if (NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot stop server while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Standalone;
            networkEngine.StopServer();
        }

        /// <summary>
        /// Turns this game instance into a game client by connecting to a game server.
        /// </summary>
        /// <param name="serverAddress">Network address of the server.</param>
        /// <param name="connectionHandler">Handler of connection result.</param>
        public void StartClient(string serverAddress, Action<Result<Connection>> connectionHandler)
        {
            if (NetworkMode != NetworkMode.Standalone)
                throw new InvalidOperationException("Cannot start client while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Client;
            try
            {
                networkEngine.StartClient(serverAddress, connectionHandler);
            }
            catch (Exception e)
            {
                Log.Write("Could not start client: " + e.Message);
                NetworkMode = NetworkMode.Standalone;
            }
        }

        /// <summary>
        /// Turns this game client into a standalone game instance by disconnecting
        /// from the game server.
        /// </summary>
        public void StopClient()
        {
            if (NetworkMode != NetworkMode.Client)
                throw new InvalidOperationException("Cannot stop client while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Standalone;
            networkEngine.StopClient();
        }

        /// <summary>
        /// Loads graphical content required by an arena to DataEngine.
        /// </summary>
        /// <param name="arena"></param>
        public void LoadArenaContent(Arena arena)
        {
            graphicsEngine.LoadArenaContent(arena);
        }

        #endregion Methods for game components

        #region Overridden Game methods

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Log.Write("Assault Wing initializing");
            windowForm = (Form)Form.FromHandle(Window.Handle);

            uiEngine = new UIEngineImpl(this);
            logicEngine = new LogicEngine(this);
            soundEngine = new SoundEngineImpl(this);
            graphicsEngine = new GraphicsEngineImpl(this);
            menuEngine = new MenuEngineImpl(this);
            networkEngine = new NetworkEngine(this);
            overlayDialog = new OverlayDialog(this);
            dataEngine = new DataEngine();
            physicsEngine = new PhysicsEngine();
            eventEngine = new EventEngineImpl();

            networkEngine.UpdateOrder = 0;
            uiEngine.UpdateOrder = 1;
            logicEngine.UpdateOrder = 2;
            soundEngine.UpdateOrder = 3;
            graphicsEngine.UpdateOrder = 4;
            overlayDialog.UpdateOrder = 5;
            menuEngine.UpdateOrder = 6;

            Components.Add(logicEngine);
            Components.Add(graphicsEngine);
            Components.Add(overlayDialog);
            Components.Add(uiEngine);
            Components.Add(soundEngine);
            Components.Add(menuEngine);
            Components.Add(networkEngine);
            Services.AddService(typeof(NetworkEngine), networkEngine);
            Services.AddService(typeof(DataEngine), dataEngine);
            Services.AddService(typeof(EventEngine), eventEngine);
            Services.AddService(typeof(PhysicsEngine), physicsEngine);

            // Disable all optional components.
            logicEngine.Enabled = false;
            graphicsEngine.Visible = false;
            menuEngine.Enabled = false;
            menuEngine.Visible = false;
            overlayDialog.Enabled = false;
            overlayDialog.Visible = false;

#if DEBUG
            SoundEffectEvent eventti = new SoundEffectEvent();
            eventti.setAction(SoundOptions.Action.Artillery);
            eventti.setEffect(SoundOptions.Effect.None);
            eventEngine.SendEvent(eventti);
#endif

            TargetElapsedTime = TimeSpan.FromSeconds(1 / 60.0); // 60 frames per second
            base.Initialize();
        }

        /// <summary>
        /// Called after all components are initialized but before the first update in the game loop. 
        /// </summary>
        protected override void BeginRun()
        {
            Log.Write("Assault Wing begins to run");

            // Hardcoded for now!!!

            PlayerControls plr1Controls;
            plr1Controls.thrust = new KeyboardKey(Keys.Up);
            plr1Controls.left = new KeyboardKey(Keys.Left);
            plr1Controls.right = new KeyboardKey(Keys.Right);
            plr1Controls.down = new KeyboardKey(Keys.Down);
            plr1Controls.fire1 = new KeyboardKey(Keys.RightControl);
            plr1Controls.fire2 = new KeyboardKey(Keys.RightShift);
            plr1Controls.extra = new KeyboardKey(Keys.Enter);

            PlayerControls plr2Controls;
#if false // mouse control
            //plr2Controls.thrust = new MouseDirection(MouseDirections.Up, 2, 7, 5, 1);
            plr2Controls.thrust = new MouseButton(MouseButtons.Left);
            plr2Controls.left = new MouseDirection(MouseDirections.Left, 2, 9, 5, 1);
            plr2Controls.right = new MouseDirection(MouseDirections.Right, 2, 9, 5, 1);
            plr2Controls.down = new MouseDirection(MouseDirections.Down, 2, 12, 5, 1);
            //plr2Controls.fire1 = new MouseDirection(MouseDirections.Down, 0, 12, 20, 1);
            //plr2Controls.fire2 = new MouseButton(MouseButtons.Right);
            plr2Controls.fire1 = new MouseWheelDirection(MouseWheelDirections.Forward, 0, 1, 1, 1);
            plr2Controls.fire2 = new MouseWheelDirection(MouseWheelDirections.Backward, 0, 1, 1, 1);
            plr2Controls.extra = new KeyboardKey(Keys.CapsLock);
            uiEngine.MouseControlsEnabled = true;
#else
            plr2Controls.thrust = new KeyboardKey(Keys.W);
            plr2Controls.left = new KeyboardKey(Keys.A);
            plr2Controls.right = new KeyboardKey(Keys.D);
            plr2Controls.down = new KeyboardKey(Keys.X);
            plr2Controls.fire1 = new KeyboardKey(Keys.LeftControl);
            plr2Controls.fire2 = new KeyboardKey(Keys.LeftShift);
            plr2Controls.extra = new KeyboardKey(Keys.CapsLock);
            uiEngine.MouseControlsEnabled = false;
#endif

            Player player1 = new Player("Kaiser Lohengramm", "Hyperion", "peashooter", "rockets", plr1Controls);
            Player player2 = new Player("John Crichton", "Prowler", "shotgun", "bazooka", plr2Controls);
            dataEngine.Players.Add(player1);
            dataEngine.Players.Add(player2);

            dataEngine.GameplayMode = new GameplayMode();
            dataEngine.GameplayMode.ShipTypes = new string[] { "Hyperion", "Prowler" };
            dataEngine.GameplayMode.Weapon1Types = new string[] { "peashooter", "shotgun" };
            dataEngine.GameplayMode.Weapon2Types = new string[] { "bazooka", "rockets" };

            ChangeState(GameState.Menu);

            base.BeginRun();
        }

        /// <summary>
        /// Called after the game loop has stopped running before exiting. 
        /// </summary>
        protected override void EndRun()
        {
            Log.Write("Assault Wing ends the run");

#if DEBUG_PROFILE
            // HACK: profiling printout for gnuplot
            using (System.IO.StreamWriter sw = System.IO.File.CreateText("framecounts.txt"))
            {
                foreach (int x in frameCounts)
                    sw.WriteLine(x);
                sw.Close();
            }
            using (System.IO.StreamWriter sw = System.IO.File.CreateText("gobcounts.txt"))
            {
                foreach (int x in gobCounts)
                    sw.WriteLine(x);
                sw.Close();
            }
            using (System.IO.StreamWriter sw = System.IO.File.CreateText("collisioncounts.txt"))
            {
                foreach (int x in collisionCounts)
                    sw.WriteLine(x);
                sw.Close();
            }
#endif

            base.EndRun();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Frame stepping (for debugging)
            if (frameRunControl.Pulse)
            {
                logicEngine.Enabled = true;
                frameStep = false;
            }
            if (frameStep)
            {
                if (frameStepControl.Pulse)
                    logicEngine.Enabled = true;
                else
                    logicEngine.Enabled = false;
            }
            else if (frameStepControl.Pulse)
            {
                logicEngine.Enabled = false;
                frameStep = true;
            }

            // Take care of game time freezing if game logic is disabled.
            if (!logicEngine.Enabled)
                gameTimeDelay = gameTimeDelay.Add(gameTime.ElapsedGameTime);
            this.gameTime = new GameTime(gameTime.TotalRealTime, gameTime.ElapsedRealTime,
                gameTime.TotalGameTime.Subtract(gameTimeDelay),
                gameTime.ElapsedGameTime);

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            base.Update(this.gameTime);
            if (logicEngine.Enabled)
                dataEngine.CommitPending();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if ((gameTime.TotalRealTime - lastFramerateCheck).TotalSeconds < 1)
            {
                ++framesSinceLastCheck;
            }
            else
            {
#if DEBUG_PROFILE
                frameCounts.Add(framesSinceLastCheck);
                gobCounts.Add(gobCount);
                collisionCounts.Add(collisionCount);
                gobCount = collisionCount = 0;
#endif
                Window.Title = "Assault Wing [~" + framesSinceLastCheck + " fps]";
                framesSinceLastCheck = 1;
                lastFramerateCheck = gameTime.TotalRealTime;

                if (NetworkMode != NetworkMode.Standalone)
                    Window.Title += " [" + networkEngine.GetSendQueueSize() + " B send queue]";

                if (NetworkMode == NetworkMode.Client && networkEngine.IsConnectedToGameServer)
                    Window.Title += " [" + (int)networkEngine.ServerPingTime.TotalMilliseconds + " ms lag]";
            }
            lock (GraphicsDevice)
                base.Draw(this.gameTime);
            lastDrawTime = this.gameTime.TotalGameTime;
        }

        /// <summary>
        /// Raises an <see cref="Microsoft.Xna.Framework.Game.Exiting"/> event.
        /// </summary>
        /// <param name="sender">The Game.</param>
        /// <param name="args">Arguments for the 
        /// <see cref="Microsoft.Xna.Framework.Game.Exiting"/> event.</param>
        protected override void OnExiting(object sender, EventArgs args)
        {
            // If progress bar is running, kill its thread.
            if (dataEngine.ProgressBar.TaskRunning)
                dataEngine.ProgressBar.AbortTask();

            base.OnExiting(sender, args);
        }

        #endregion Overridden Game methods
    }
}