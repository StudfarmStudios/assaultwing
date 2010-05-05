using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Menu;
using AW2.Net;
using AW2.Net.Messages;
using AW2.Settings;
using AW2.Sound;
using AW2.UI;

namespace AW2
{
    /// <summary>
    /// The state of the game.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// The game is initialising.
        /// </summary>
        Initializing,

        /// <summary>
        /// The game is active.
        /// </summary>
        Gameplay,

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
        /// <summary>
        /// Wraps <see cref="CounterCreationDataCollection"/>, adding to it an implementation
        /// of <see cref="IEnumerable&lt;CounterCreationData&gt;"/>.
        /// </summary>
        private class AWCounterCreationDataCollection : CounterCreationDataCollection, IEnumerable<CounterCreationData>
        {
            /// <summary>
            /// Returns an enumerator for the collection.
            /// </summary>
            public new IEnumerator<CounterCreationData> GetEnumerator()
            {
                foreach (var x in (System.Collections.IEnumerable)this) yield return (CounterCreationData)x;
            }
        }

        #region AssaultWing fields

        private UIEngineImpl uiEngine;
        private GraphicsEngineImpl graphicsEngine;
        private OverlayDialog overlayDialog;
        private NetworkEngine networkEngine;
        private LogicEngine logicEngine;
        private DataEngine dataEngine;
        private PhysicsEngine physicsEngine;
        private SoundEngine soundEngine;
        private IMenuEngine menuEngine;
        private int preferredWindowWidth, preferredWindowHeight;
        private SurfaceFormat preferredWindowFormat;
        private int preferredFullscreenWidth, preferredFullscreenHeight;
        private SurfaceFormat preferredFullscreenFormat;
        private TimeSpan lastFramerateCheck;
        private int framesSinceLastCheck;
        private GameState gameState;
        private IWindow window; // use this and not Game.Window

        // Fields for game server starting an arena
        private bool startingArenaOnServer;
        private List<int> startedArenaOnClients = new List<int>();

        // HACK: Debug keys
        private Control musicSwitch;
        private Control arenaReload;
        private Control frameStepControl;
        private Control frameRunControl;
        private bool frameStep;

#if DEBUG_PROFILE
        /// <summary>
        /// Gob count for the current frame.
        /// </summary>
        public int gobCount;
        /// <summary>
        /// Collision count for the current frame.
        /// </summary>
        public int collisionCount;
        private List<int> frameCounts = new List<int>();
        private List<int> gobCounts = new List<int>();
        private List<int> collisionCounts = new List<int>();
#endif

        /// <summary>
        /// Time of previously finished call to Draw(), in game time.
        /// </summary>
        private TimeSpan lastDrawTime;

        /// <summary>
        /// The only existing instance of this class.
        /// </summary>
        private static AssaultWing instance;

        #endregion AssaultWing fields

        #region Callbacks

        /// <summary>
        /// Called during initialisation of the game instance.
        /// The event handler should return the menu engine of the game instance.
        /// If no handlers have been added, a dummy menu engine is used.
        /// </summary>
        public static event Func<AssaultWing, IMenuEngine> MenuEngineInitializing;

        /// <summary>
        /// Called during initialisation of the game instance.
        /// The event handler should return a window where AssaultWing can draw itself.
        /// </summary>
        public static event Func<AssaultWing, IWindow> WindowInitializing;

        /// <summary>
        /// Called when <see cref="GameState"/> has changed.
        /// </summary>
        public event Action<GameState> GameStateChanged;

        /// <summary>
        /// Called when <see cref="BeginRun"/> is complete.
        /// </summary>
        public event Action RunBegan;

        #endregion Callbacks

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

        public AWSettings Settings { get; private set; }
        public string[] CommandLineArgs { get; set; }
        public PhysicsEngine PhysicsEngine { get { return physicsEngine; } }
        public DataEngine DataEngine { get { return dataEngine; } }
        public NetworkEngine NetworkEngine { get { return networkEngine; } }
        public SoundEngine SoundEngine { get { return soundEngine; } }

        /// <summary>
        /// The current state of the game.
        /// </summary>
        public GameState GameState
        {
            get { return gameState; }
            private set
            {
                DisableCurrentGameState();
                EnableGameState(value);
                var oldState = gameState;
                gameState = value;
                if (GameStateChanged != null && gameState != oldState)
                    GameStateChanged(gameState);
            }
        }

        /// <summary>
        /// The current mode of network operation of the game.
        /// </summary>
        public NetworkMode NetworkMode { get; private set; }

        /// <summary>
        /// The game time on this frame.
        /// </summary>
        public AWGameTime GameTime { get; private set; }

        /// <summary>
        /// Time of previously finished call to Draw(), in game time.
        /// </summary>
        public TimeSpan LastDrawTime { get { return lastDrawTime; } }

        /// <summary>
        /// The screen dimensions of the game window's client rectangle.
        /// </summary>
        public Rectangle ClientBounds { get { return window.ClientBounds; } }

        /// <summary>
        /// The minimum allowed screen dimensions of the game window's client rectangle.
        /// </summary>
        public Rectangle ClientBoundsMin
        {
            get { return window.ClientBoundsMin; }
            set { window.ClientBoundsMin = value; }
        }

        /// <summary>
        /// The <see cref="Microsoft.Xna.Framework.Game.Window"/> property inherited from
        /// <see cref="Microsoft.Xna.Framework.Game"/> is dangerously confusable with
        /// the private <see cref="window"/> field. Thus, access to
        /// <see cref="Microsoft.Xna.Framework.Game.Window"/> is limited only to
        /// references of type <see cref="Microsoft.Xna.Framework.Game"/>.
        /// </summary>
        public new GameWindow Window { get { throw new Exception("Use either ((Microsoft.Xna.Framework.Game)AssaultWing).Window or AssaultWing.window"); } }

        public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

        /// <summary>
        /// Are overlay dialogs allowed.
        /// </summary>
        public bool AllowDialogs { get; set; }

        #endregion AssaultWing properties

        #region AssaultWing performance counters

        /// <summary>
        /// Number of gobs created per frame, averaged over one second.
        /// </summary>
        public AWPerformanceCounter GobsCreatedPerFrameAvgPerSecondCounter { get; private set; }

        /// <summary>
        /// Number of elapsed frames.
        /// </summary>
        public AWPerformanceCounter GobsCreatedPerFrameAvgPerSecondBaseCounter { get; private set; }

        /// <summary>
        /// Number of gobs drawn per frame, averaged over one second.
        /// </summary>
        public AWPerformanceCounter GobsDrawnPerFrameAvgPerSecondCounter { get; private set; }

        /// <summary>
        /// Number of elapsed frames.
        /// </summary>
        public AWPerformanceCounter GobsDrawnPerFrameAvgPerSecondBaseCounter { get; private set; }

        /// <summary>
        /// Number of drawn frames per second.
        /// </summary>
        public AWPerformanceCounter FramesDrawnPerSecondCounter { get; private set; }

        /// <summary>
        /// Number of gobs currently alive.
        /// </summary>
        public AWPerformanceCounter GobsCounter { get; private set; }

        #endregion

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
            if (WindowInitializing == null)
                throw new ApplicationException("AssaultWing.WindowInitializing must be set before first reference to AssaultWing.Instance");

            Log.Write("Loading settings from file");
            Settings = AWSettings.FromFile();
            InitializeGraphics();

            musicSwitch = new KeyboardKey(Keys.F5);
            arenaReload = new KeyboardKey(Keys.F6);
            frameStepControl = new KeyboardKey(Keys.F8);
            frameRunControl = new KeyboardKey(Keys.F7);
            frameStep = false;

            Content = new AWContentManager(Services);
            lastFramerateCheck = new TimeSpan(0);
            framesSinceLastCheck = 0;
            GameState = GameState.Initializing;
            NetworkMode = NetworkMode.Standalone;
            GameTime = new AWGameTime();
            lastDrawTime = new TimeSpan(0);

            InitializeComponents();
        }

        /// <summary>
        /// If there is an NVIDIA PerfHUD adapter, sets the GraphicsDeviceManager 
        /// to use that adapter and a reference device.
        /// </summary>
        private void Graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs args)
        {
#if DEBUG
            var adapter = GraphicsAdapter.Adapters.FirstOrDefault(ada => ada.Description == "NVIDIA PerfHUD");
            if (adapter != null)
            {
                args.GraphicsDeviceInformation.DeviceType = DeviceType.Reference;
                args.GraphicsDeviceInformation.Adapter = adapter;
                Log.Write("Found NVIDIA PerfHUD device, PerfHUD now enabled.");
            }
            else
#endif
                args.GraphicsDeviceInformation.PresentationParameters.DeviceWindowHandle = window.Handle;

        }

        /// <summary>
        /// Reacts to a client window resize event.
        /// </summary>
        private void ClientSizeChanged(object sender, EventArgs e)
        {
            if (ClientBounds.Width == 0 || ClientBounds.Height == 0) return;
            GraphicsDeviceManager.PreferredBackBufferWidth = ClientBounds.Width;
            GraphicsDeviceManager.PreferredBackBufferHeight = ClientBounds.Height;
            GraphicsDeviceManager.ApplyChanges();
            if (graphicsEngine != null) graphicsEngine.WindowResize();
            if (menuEngine != null) menuEngine.WindowResize();
        }

        private void StartArenaImpl()
        {
            GameTime = GameTime.ResetArenaTime();
            dataEngine.StartArena();
            dataEngine.RearrangeViewports();
            GameState = GameState.Gameplay;
            soundEngine.PlayMusic(dataEngine.Arena.BackgroundMusic);
        }

        private void InitializeGraphics()
        {
            // Decide on preferred windowed and fullscreen sizes and formats.
            DisplayMode displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            preferredFullscreenWidth = displayMode.Width;
            preferredFullscreenHeight = displayMode.Height;
            preferredFullscreenFormat = displayMode.Format;
            preferredWindowWidth = 1000;
            preferredWindowHeight = 800;
            preferredWindowFormat = displayMode.Format;

            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            GraphicsDeviceManager.IsFullScreen = false;
            GraphicsDeviceManager.PreferredBackBufferWidth = preferredWindowWidth;
            GraphicsDeviceManager.PreferredBackBufferHeight = preferredWindowHeight;
            GraphicsDeviceManager.PreparingDeviceSettings += Graphics_PreparingDeviceSettings;

            window = WindowInitializing(this);
            window.AllowUserResizing = true;
            window.ClientSizeChanged += ClientSizeChanged;
            ClientBoundsMin = new Rectangle(0, 0, 1000, 800);
            AllowDialogs = true;
        }

        private void InitializeComponents()
        {
            uiEngine = new UIEngineImpl(this);
            logicEngine = new LogicEngine(this);
            soundEngine = new SoundEngine(this);
            graphicsEngine = new GraphicsEngineImpl(this);
            menuEngine = MenuEngineInitializing != null ? MenuEngineInitializing(this) : new DummyMenuEngine();
            networkEngine = new NetworkEngine(this);
            overlayDialog = new OverlayDialog(this);
            dataEngine = new DataEngine();
            physicsEngine = new PhysicsEngine();

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
            Services.AddService(typeof(PhysicsEngine), physicsEngine);

            // Disable all optional components.
            logicEngine.Enabled = false;
            graphicsEngine.Visible = false;
            menuEngine.Enabled = false;
            menuEngine.Visible = false;
            overlayDialog.Enabled = false;
            overlayDialog.Visible = false;
        }

        [Conditional("DEBUG")]
        private void InitializePerformanceCounters()
        {
            var categoryName = "Assault Wing";
            var instanceName = "AW Instance " + Process.GetCurrentProcess().Id;
            
            var counters = new AWCounterCreationDataCollection();
            counters.Add(new CounterCreationData("Gobs Created/f Avg/s", "Number of gobs created per frame as an average over the last second", PerformanceCounterType.AverageCount64));
            counters.Add(new CounterCreationData("Gobs Created/f Avg/s Base", "Number of frames elapsed during the latest arena", PerformanceCounterType.AverageBase));
            counters.Add(new CounterCreationData("Gobs Drawn/f Avg/s", "Number of gobs drawn per frame as an average over the last second", PerformanceCounterType.AverageCount64));
            counters.Add(new CounterCreationData("Gobs Drawn/f Avg/s Base", "Number of frames elapsed during the latest arena", PerformanceCounterType.AverageBase));
            counters.Add(new CounterCreationData("Frames Drawn/s", "Number of frames drawn per second", PerformanceCounterType.RateOfCountsPerSecond32));
            counters.Add(new CounterCreationData("Gobs", "Number of gobs in current arena", PerformanceCounterType.NumberOfItems32));

            // Delete registered category if it seems outdated.
            if (PerformanceCounterCategory.Exists(categoryName))
            {
                var category = new PerformanceCounterCategory(categoryName).ReadCategory();
                if (counters.Any(counter => !category.Contains(counter.CounterName)))
                    try
                    {
                        PerformanceCounterCategory.Delete(categoryName);
                    }
                    catch (System.ComponentModel.Win32Exception e)
                    {
                        if (e.NativeErrorCode == 5)
                            Log.Write("Note: Performance monitoring not available due to lack of user rights. Try 'Run as administrator'");
                        else
                            Log.Write("Note: Performance monitoring not available, native error " + e);
                    }
            }

            // Create the category if it's missing
            if (!PerformanceCounterCategory.Exists(categoryName))
                PerformanceCounterCategory.Create(categoryName, "Assault Wing internal performance and activity counters", PerformanceCounterCategoryType.MultiInstance, counters);

            // Initialise our counter instances dynamically with reflection
            int propertyCount = 0;
            foreach (var prop in GetType().GetProperties())
                if (prop.PropertyType == typeof(AWPerformanceCounter))
                {
                    ++propertyCount;
                    var counterData = counters.FirstOrDefault(data => CounterNameToPropertyName(data.CounterName) == prop.Name);
                    if (counterData == null) throw new Exception("Superfluous performance counter property: AssaultWing." + prop.Name);
                    var counter = new AWPerformanceCounter
                    {
                        Impl = new PerformanceCounter
                        {
                            CategoryName = categoryName,
                            CounterName = counterData.CounterName,
                            InstanceName = instanceName,
                            ReadOnly = false,
                            InstanceLifetime = PerformanceCounterInstanceLifetime.Process,
                            RawValue = 0
                        }
                    };
                    prop.SetValue(this, counter, null);
                }
            if (propertyCount < counters.Count(counter => !counter.CounterName.EndsWith("Base")))
                throw new Exception("Some performance counters don't have corresponding public properties in class AssaultWing and thus won't have meaningful values");
        }

        private string CounterNameToPropertyName(string counterName)
        {
            return counterName
                .Replace(" ", "")
                .Replace("/s", "PerSecond")
                .Replace("/f", "PerFrame")
                + "Counter";
        }

        private void EnableGameState(GameState value)
        {
            switch (value)
            {
                case GameState.Initializing:
                    break;
                case GameState.Gameplay:
                    Log.Write("Saving settings to file");
                    Settings.ToFile();
                    logicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    graphicsEngine.Visible = true;
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
                    throw new ApplicationException("Cannot change to unexpected game state " + value);
            }
        }

        private void DisableCurrentGameState()
        {
            switch (gameState)
            {
                case GameState.Initializing:
                    break;
                case GameState.Gameplay:
                    logicEngine.Enabled = false;
                    graphicsEngine.Visible = false;
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
                    throw new ApplicationException("Cannot change away from unexpected game state " + GameState);
            }
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
            foreach (var player in dataEngine.Spectators)
                player.InitializeForGameSession();

            // Notify game clients if we are the game server.
            if (NetworkMode == NetworkMode.Server)
            {
                var message = new StartGameMessage();
                foreach (var player in dataEngine.Spectators)
                    message.SerializePlayer((Player)player);
                message.ArenaPlaylist = dataEngine.ArenaPlaylist;
                networkEngine.GameClientConnections.Send(message);
            }

            dataEngine.ArenaPlaylist.Reset();
            PrepareNextArena();
        }

        /// <summary>
        /// Starts playing a previously prepared arena.
        /// </summary>
        public void StartArena()
        {
            if (NetworkMode == NetworkMode.Server)
            {
                startingArenaOnServer = true;
                startedArenaOnClients.Clear();
                networkEngine.GameClientConnections.Send(new ArenaStartRequest());
                networkEngine.MessageHandlers.Add(new MessageHandler<ArenaStartReply>(false, networkEngine.GameClientConnections, mess =>
                {
                    startedArenaOnClients.Add(mess.ConnectionId);
                }));
            }
            else
                StartArenaImpl();
        }

        /// <summary>
        /// Finishes playing the current arena.
        /// </summary>
        public void FinishArena()
        {
            if (DataEngine.ArenaPlaylist.HasNext)
                ShowDialog(new ArenaOverOverlayDialogData(DataEngine.ArenaPlaylist.Next));
            else
                ShowDialog(new GameOverOverlayDialogData());
            if (NetworkMode == NetworkMode.Server)
            {
                var message = new ArenaFinishMessage();
                networkEngine.GameClientConnections.Send(message);
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
            // Disallow window resizing during arena loading.
            // A window resize event may reset the graphics card, fatally
            // screwing up initialisation of walls' index maps.
            bool oldAllowUserResizing = window.AllowUserResizing;
            if (oldAllowUserResizing)
                window.AllowUserResizing = false;

            try
            {
                if (!DataEngine.NextArena())
                    throw new InvalidOperationException("There is no next arena to play");
            }
            finally
            {
                if (oldAllowUserResizing)
                    window.AllowUserResizing = true;
            }
        }

        /// <summary>
        /// Resumes playing the current arena, closing the dialog if it's visible.
        /// </summary>
        public void ResumePlay()
        {
            GameState = GameState.Gameplay;
        }

        /// <summary>
        /// Displays the dialog on top of the game and stops updating the game logic.
        /// </summary>
        /// <param name="dialogData">The contents and actions for the dialog.</param>
        public void ShowDialog(OverlayDialogData dialogData)
        {
            if (!AllowDialogs) return;
            overlayDialog.Data = dialogData;
            GameState = GameState.OverlayDialog;
        }

        /// <summary>
        /// Displays the main menu and stops any ongoing gameplay.
        /// </summary>
        public void ShowMenu()
        {
            dataEngine.ClearGameState();
            menuEngine.Activate();
            GameState = GameState.Menu;
        }

        /// <summary>
        /// Toggles between fullscreen and windowed mode.
        /// </summary>
        public void ToggleFullscreen()
        {
            window.ToggleFullscreen();
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
                dataEngine.RearrangeViewports();
            else
                dataEngine.RearrangeViewports(player);
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
            networkEngine.StopServer();
            NetworkMode = NetworkMode.Standalone;
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
            try
            {
                InitializePerformanceCounters();
            }
            catch (System.Security.SecurityException)
            {
                // User lacks privileges to initialize performance counters.
                // This may happen on Windows 7 unless you right click on the EXE
                // and choose "Run as Administrator".
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Also this seems to happen on Windows 7 due to lack of user privileges.
            }
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
            plr1Controls.extra = new KeyboardKey(Keys.Down);

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
            plr2Controls.extra = new KeyboardKey(Keys.X);
            uiEngine.MouseControlsEnabled = false;
#endif

            Player player1 = new Player("Kaiser Lohengramm", (CanonicalString)"Windlord", (CanonicalString)"rockets", (CanonicalString)"reverse thruster", plr1Controls);
            Player player2 = new Player("John Crichton", (CanonicalString)"Bugger", (CanonicalString)"bouncegun", (CanonicalString)"reverse thruster", plr2Controls);
            dataEngine.Spectators.Add(player1);
            dataEngine.Spectators.Add(player2);

            dataEngine.GameplayMode = new GameplayMode();
            dataEngine.GameplayMode.ShipTypes = new string[] { "Windlord", "Bugger", "Plissken" };
            dataEngine.GameplayMode.ExtraDeviceTypes = new string[] { "reverse thruster", "blink" };
            dataEngine.GameplayMode.Weapon2Types = new string[] { "bouncegun", "rockets" };

            GameState = GameState.Menu;
            base.BeginRun();
            if (RunBegan != null) RunBegan();
        }

        /// <summary>
        /// Called after the game loop has stopped running before exiting. 
        /// </summary>
        protected override void EndRun()
        {
            Log.Write("Assault Wing ends the run");

            Log.Write("Saving settings to file");
            Settings.ToFile();

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

        protected override void Update(GameTime gameTime)
        {
            if (startingArenaOnServer)
            {
                if (networkEngine.GameClientConnections.Connections.All(
                    conn => startedArenaOnClients.Contains(conn.Id)))
                {
                    startingArenaOnServer = false;
                    StartArenaImpl();
                }
            }

            // Switch music off
            if (musicSwitch.Pulse && GameState == GameState.Gameplay)
            {
                soundEngine.StopMusic();
            }

            // Instant arena reload (simple aid for hand-editing an arena)
            if (arenaReload.Pulse && GameState == GameState.Gameplay && NetworkMode == NetworkMode.Standalone)
            {
                dataEngine.InitializeFromArena(dataEngine.ArenaPlaylist.Current, true);
                StartArena();
            }

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

            GameTime = GameTime.Update(gameTime, logicEngine.Enabled);

            base.Update(GameTime);
            if (logicEngine.Enabled)
            {
                GobsCreatedPerFrameAvgPerSecondBaseCounter.Increment();
                dataEngine.CommitPending();
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if ((gameTime.TotalRealTime - lastFramerateCheck).TotalSeconds < 1)
            {
                FramesDrawnPerSecondCounter.Increment();
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
                window.Title = "Assault Wing [~" + framesSinceLastCheck + " fps]";
                framesSinceLastCheck = 1;
                lastFramerateCheck = gameTime.TotalRealTime;

                window.Title += " [arenatime: " + GameTime.TotalArenaTime + "]"; // !!!

                if (NetworkMode != NetworkMode.Standalone)
                    window.Title += " [" + networkEngine.GetSendQueueSize() + " B send queue]";

                if (NetworkMode == NetworkMode.Client && networkEngine.IsConnectedToGameServer)
                    window.Title += string.Format(" [{0} ms lag, {1} ms offset]",
                        (int)networkEngine.ServerPingTime.TotalMilliseconds,
                        (int)networkEngine.ServerGameTimeOffset.TotalMilliseconds);

                if (NetworkMode == NetworkMode.Server)
                    foreach (PingedConnection conn in networkEngine.GameClientConnections.Connections)
                        window.Title += string.Format(" [#{0}: {1} ms lag, {2} ms offset]",
                            conn.Id,
                            (int)conn.PingTime.TotalMilliseconds,
                            (int)conn.RemoteGameTimeOffset.TotalMilliseconds);
            }
            lock (GraphicsDevice) base.Draw(GameTime);
            lastDrawTime = GameTime.TotalArenaTime;
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
