using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Menu;
using AW2.Net;
using AW2.Net.Messages;
using AW2.Settings;
using AW2.Sound;
using AW2.UI;
using AW2.UI.Mouse;

namespace AW2
{
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

        private UIEngineImpl _uiEngine;
        private GraphicsEngineImpl _graphicsEngine;
        private IntroEngine _introEngine;
        private OverlayDialog _overlayDialog;
        private LogicEngine _logicEngine;
        private int _preferredWindowWidth, _preferredWindowHeight;
        private SurfaceFormat _preferredWindowFormat;
        private int _preferredFullscreenWidth, _preferredFullscreenHeight;
        private SurfaceFormat _preferredFullscreenFormat;
        private TimeSpan _lastFramerateCheck;
        private int _framesSinceLastCheck;
        private GameState _gameState;
        private IWindow _window; // use this and not Game.Window
        private ArenaStartWaiter _arenaStartWaiter;

        // HACK: Debug keys
        private Control _musicSwitch;
        private Control _arenaReload;
        private Control _frameStepControl;
        private Control _frameRunControl;
        private bool _frameStep;

#if DEBUG_PROFILE
        /// <summary>
        /// Gob count for the current frame.
        /// </summary>
        public int GobCount { get; set; }
        /// <summary>
        /// Collision count for the current frame.
        /// </summary>
        public int CollisionCount { get; set; }
        private List<int> _frameCounts = new List<int>();
        private List<int> _gobCounts = new List<int>();
        private List<int> _collisionCounts = new List<int>();
#endif

        /// <summary>
        /// The only existing instance of this class.
        /// </summary>
        private static AssaultWing g_instance;

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
                if (g_instance == null)
                    g_instance = new AssaultWing();
                return g_instance;
            }
        }

        public int ManagedThreadID { get; private set; }
        public AWSettings Settings { get; private set; }
        public string[] CommandLineArgs { get; set; }
        public PhysicsEngine PhysicsEngine { get; private set; }
        public DataEngine DataEngine { get; private set; }
        public NetworkEngine NetworkEngine { get; private set; }
        public SoundEngine SoundEngine { get; private set; }
        public IMenuEngine MenuEngine { get; private set; }

        /// <summary>
        /// The current state of the game.
        /// </summary>
        public GameState GameState
        {
            get { return _gameState; }
            private set
            {
                DisableCurrentGameState();
                EnableGameState(value);
                var oldState = _gameState;
                _gameState = value;
                if (GameStateChanged != null && _gameState != oldState)
                    GameStateChanged(_gameState);
            }
        }

        /// <summary>
        /// The current mode of network operation of the game.
        /// </summary>
        public NetworkMode NetworkMode { get; private set; }

        /// <summary>
        /// The game time on this frame.
        /// </summary>
        public GameTime GameTime { get; private set; }

        /// <summary>
        /// The screen dimensions of the game window's client rectangle.
        /// </summary>
        public Rectangle ClientBounds { get { return _window.ClientBounds; } }

        /// <summary>
        /// The minimum allowed screen dimensions of the game window's client rectangle.
        /// </summary>
        public Rectangle ClientBoundsMin
        {
            get { return _window.ClientBoundsMin; }
            set { _window.ClientBoundsMin = value; }
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
        {
            Log.Write("Creating an Assault Wing instance");
            ManagedThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (WindowInitializing == null)
                throw new ApplicationException("AssaultWing.WindowInitializing must be set before first reference to AssaultWing.Instance");

            Log.Write("Loading settings from file");
            Settings = AWSettings.FromFile();
            InitializeGraphics();

            _musicSwitch = new KeyboardKey(Keys.F5);
            _arenaReload = new KeyboardKey(Keys.F6);
            _frameStepControl = new KeyboardKey(Keys.F8);
            _frameRunControl = new KeyboardKey(Keys.F7);
            _frameStep = false;

            Content = new AWContentManager(Services);
            GameState = GameState.Initializing;
            NetworkMode = NetworkMode.Standalone;
            GameTime = new GameTime();

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
                args.GraphicsDeviceInformation.PresentationParameters.DeviceWindowHandle = _window.Handle;

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
            if (_graphicsEngine != null) _graphicsEngine.WindowResize();
            if (MenuEngine != null) MenuEngine.WindowResize();
        }

        private void StartArenaImpl()
        {
            Log.Write("Starting arena");
            DataEngine.StartArena();
            DataEngine.RearrangeViewports();
            GameState = GameState.Gameplay;
            SoundEngine.PlayMusic(DataEngine.Arena.BackgroundMusic);
            Log.Write("...started arena " + DataEngine.Arena.Name);
        }

        private void InitializeGraphics()
        {
            // Decide on preferred windowed and fullscreen sizes and formats.
            DisplayMode displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _preferredFullscreenWidth = displayMode.Width;
            _preferredFullscreenHeight = displayMode.Height;
            _preferredFullscreenFormat = displayMode.Format;
            _preferredWindowWidth = Math.Min(1000, displayMode.Width);
            _preferredWindowHeight = Math.Min(800, displayMode.Height);
            _preferredWindowFormat = displayMode.Format;

            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            GraphicsDeviceManager.IsFullScreen = false;
            GraphicsDeviceManager.PreferredBackBufferWidth = _preferredWindowWidth;
            GraphicsDeviceManager.PreferredBackBufferHeight = _preferredWindowHeight;
            GraphicsDeviceManager.PreparingDeviceSettings += Graphics_PreparingDeviceSettings;

            _window = WindowInitializing(this);
            _window.AllowUserResizing = true;
            _window.ClientSizeChanged += ClientSizeChanged;
            ClientBoundsMin = new Rectangle(0, 0, _preferredWindowWidth, _preferredWindowHeight);
            AllowDialogs = true;
        }

        private void InitializeComponents()
        {
            _uiEngine = new UIEngineImpl(this);
            _logicEngine = new LogicEngine(this);
            SoundEngine = new SoundEngine(this);
            _graphicsEngine = new GraphicsEngineImpl(this);
            _introEngine = new IntroEngine(this);
            MenuEngine = MenuEngineInitializing != null ? MenuEngineInitializing(this) : new DummyMenuEngine();
            NetworkEngine = new NetworkEngine(this);
            _overlayDialog = new OverlayDialog(this);
            DataEngine = new DataEngine();
            PhysicsEngine = new PhysicsEngine();

            NetworkEngine.UpdateOrder = 0;
            _uiEngine.UpdateOrder = 1;
            _logicEngine.UpdateOrder = 2;
            SoundEngine.UpdateOrder = 3;
            _graphicsEngine.UpdateOrder = 4;
            _introEngine.UpdateOrder = 4;
            _overlayDialog.UpdateOrder = 5;
            MenuEngine.UpdateOrder = 6;

            Components.Add(_logicEngine);
            Components.Add(_graphicsEngine);
            Components.Add(_introEngine);
            Components.Add(_overlayDialog);
            Components.Add(_uiEngine);
            Components.Add(SoundEngine);
            Components.Add(MenuEngine);
            Components.Add(NetworkEngine);
            Services.AddService(typeof(NetworkEngine), NetworkEngine);
            Services.AddService(typeof(DataEngine), DataEngine);
            Services.AddService(typeof(PhysicsEngine), PhysicsEngine);

            // Disable all optional components
            foreach (var component in Components)
            {
                if (component is DrawableGameComponent) ((DrawableGameComponent)component).Visible = false;
                if (component is GameComponent) ((GameComponent)component).Enabled = false;
            }
            _uiEngine.Enabled = true;
            SoundEngine.Enabled = true;
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
                case GameState.Intro:
                    _introEngine.Enabled = true;
                    _introEngine.Visible = true;
                    break;
                case GameState.Gameplay:
                    Log.Write("Saving settings to file");
                    Settings.ToFile();
                    _logicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    _graphicsEngine.Visible = true;
                    break;
                case GameState.Menu:
                    MenuEngine.Enabled = true;
                    MenuEngine.Visible = true;
                    break;
                case GameState.OverlayDialog:
                    _overlayDialog.Enabled = true;
                    _overlayDialog.Visible = true;
                    _graphicsEngine.Visible = true;
                    break;
                default:
                    throw new ApplicationException("Cannot change to unexpected game state " + value);
            }
        }

        private void DisableCurrentGameState()
        {
            switch (_gameState)
            {
                case GameState.Initializing:
                    break;
                case GameState.Intro:
                    _introEngine.Enabled = false;
                    _introEngine.Visible = false;
                    break;
                case GameState.Gameplay:
                    _logicEngine.Enabled = false;
                    _graphicsEngine.Visible = false;
                    break;
                case GameState.Menu:
                    MenuEngine.Enabled = false;
                    MenuEngine.Visible = false;
                    break;
                case GameState.OverlayDialog:
                    _overlayDialog.Enabled = false;
                    _overlayDialog.Visible = false;
                    _graphicsEngine.Visible = false;
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
            foreach (var player in DataEngine.Spectators)
                player.InitializeForGameSession();

            // Notify game clients if we are the game server.
            if (NetworkMode == NetworkMode.Server)
            {
                var message = new StartGameMessage();
                foreach (var player in DataEngine.Spectators)
                    message.SerializePlayer((Player)player);
                message.ArenaPlaylist = DataEngine.ArenaPlaylist;
                NetworkEngine.GameClientConnections.Send(message);
            }

            DataEngine.ArenaPlaylist.Reset();
            PrepareNextArena();
        }

        /// <summary>
        /// Starts playing a previously prepared arena.
        /// </summary>
        public void StartArena()
        {
            if (NetworkMode == NetworkMode.Server)
            {
                _arenaStartWaiter = new ArenaStartWaiter(NetworkEngine.GameClientConnections);
                _arenaStartWaiter.BeginWait();
            }
            else
                StartArenaImpl();
        }

        /// <summary>
        /// Finishes playing the current arena.
        /// </summary>
        public void FinishArena()
        {
            if (NetworkMode == NetworkMode.Client) MessageHandlers.DeactivateHandlers(MessageHandlers.GetClientGameplayHandlers(null));
            if (NetworkMode == NetworkMode.Server) MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers(null));
            if (DataEngine.ArenaPlaylist.HasNext)
                ShowDialog(new ArenaOverOverlayDialogData(DataEngine.ArenaPlaylist.Next));
            else
                ShowDialog(new GameOverOverlayDialogData());
            if (NetworkMode == NetworkMode.Server)
            {
                var message = new ArenaFinishMessage();
                NetworkEngine.GameClientConnections.Send(message);
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
            bool oldAllowUserResizing = _window.AllowUserResizing;
            if (oldAllowUserResizing)
                _window.AllowUserResizing = false;

            try
            {
                if (!DataEngine.NextArena())
                    throw new InvalidOperationException("There is no next arena to play");
            }
            finally
            {
                if (oldAllowUserResizing)
                    _window.AllowUserResizing = true;
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
            _overlayDialog.Data = dialogData;
            GameState = GameState.OverlayDialog;
        }

        /// <summary>
        /// Displays the main menu and stops any ongoing gameplay.
        /// </summary>
        public void ShowMenu()
        {
            Log.Write("Entering menus");
            if (NetworkMode == NetworkMode.Client) MessageHandlers.DeactivateHandlers(MessageHandlers.GetClientGameplayHandlers(null));
            if (NetworkMode == NetworkMode.Server) MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers(null));
            DataEngine.ClearGameState();
            MenuEngine.Activate();
            GameState = GameState.Menu;
        }

        /// <summary>
        /// Toggles between fullscreen and windowed mode.
        /// </summary>
        public void ToggleFullscreen()
        {
            _window.ToggleFullscreen();
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
                DataEngine.RearrangeViewports();
            else
                DataEngine.RearrangeViewports(player);
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
                NetworkEngine.StartServer(connectionHandler);
                var handlers = MessageHandlers.GetServerMenuHandlers(NetworkEngine.GameClientConnections);
                NetworkEngine.MessageHandlers.AddRange(handlers);
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
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers(null));
            NetworkEngine.StopServer();
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
                NetworkEngine.StartClient(serverAddress, connectionHandler);
            }
            catch (System.Net.Sockets.SocketException e)
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
            NetworkEngine.StopClient();
        }

        /// <summary>
        /// Loads graphical content required by an arena to DataEngine.
        /// </summary>
        public void LoadArenaContent(Arena arena)
        {
            _graphicsEngine.LoadArenaContent(arena);
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
            plr1Controls.Thrust = new KeyboardKey(Keys.Up);
            plr1Controls.Left = new KeyboardKey(Keys.Left);
            plr1Controls.Right = new KeyboardKey(Keys.Right);
            plr1Controls.Down = new KeyboardKey(Keys.Down);
            plr1Controls.Fire1 = new KeyboardKey(Keys.RightControl);
            plr1Controls.Fire2 = new KeyboardKey(Keys.RightShift);
            plr1Controls.Extra = new KeyboardKey(Keys.Down);

            PlayerControls plr2Controls;
#if false // mouse control
            //plr2Controls.Thrust = new MouseDirection(MouseDirections.Up, 2, 7, 5);
            plr2Controls.Thrust = new MouseButton(MouseButtons.Left);
            plr2Controls.Left = new MouseDirection(MouseDirections.Left, 2, 9, 5);
            plr2Controls.Right = new MouseDirection(MouseDirections.Right, 2, 9, 5);
            plr2Controls.Down = new MouseDirection(MouseDirections.Down, 2, 12, 5);
            //plr2Controls.Fire1 = new MouseDirection(MouseDirections.Down, 0, 12, 20);
            //plr2Controls.Fire2 = new MouseButton(MouseButtons.Right);
            plr2Controls.Fire1 = new MouseWheelDirection(MouseWheelDirections.Forward, 0, 1, 1);
            plr2Controls.Fire2 = new MouseWheelDirection(MouseWheelDirections.Backward, 0, 1, 1);
            plr2Controls.Extra = new KeyboardKey(Keys.CapsLock);
            _uiEngine.MouseControlsEnabled = true;
#else
            plr2Controls.Thrust = new KeyboardKey(Keys.W);
            plr2Controls.Left = new KeyboardKey(Keys.A);
            plr2Controls.Right = new KeyboardKey(Keys.D);
            plr2Controls.Down = new KeyboardKey(Keys.X);
            plr2Controls.Fire1 = new KeyboardKey(Keys.LeftControl);
            plr2Controls.Fire2 = new KeyboardKey(Keys.LeftShift);
            plr2Controls.Extra = new KeyboardKey(Keys.X);
            _uiEngine.MouseControlsEnabled = false;
#endif

            Player player1 = new Player("Kaiser Lohengramm", (CanonicalString)"Windlord", (CanonicalString)"rockets", (CanonicalString)"reverse thruster", plr1Controls);
            Player player2 = new Player("John Crichton", (CanonicalString)"Bugger", (CanonicalString)"bazooka", (CanonicalString)"reverse thruster", plr2Controls);
            DataEngine.Spectators.Add(player1);
            DataEngine.Spectators.Add(player2);

            DataEngine.GameplayMode = new GameplayMode();
            DataEngine.GameplayMode.ShipTypes = new string[] { "Windlord", "Bugger", "Plissken" };
            DataEngine.GameplayMode.ExtraDeviceTypes = new string[] { "reverse thruster", "blink" };
            DataEngine.GameplayMode.Weapon2Types = new string[] { "bazooka", "rockets", "mines" };

            GameState = GameState.Intro;
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
                foreach (int x in _frameCounts)
                    sw.WriteLine(x);
                sw.Close();
            }
            using (System.IO.StreamWriter sw = System.IO.File.CreateText("gobcounts.txt"))
            {
                foreach (int x in _gobCounts)
                    sw.WriteLine(x);
                sw.Close();
            }
            using (System.IO.StreamWriter sw = System.IO.File.CreateText("collisioncounts.txt"))
            {
                foreach (int x in _collisionCounts)
                    sw.WriteLine(x);
                sw.Close();
            }
#endif

            base.EndRun();
        }

        protected override void Update(GameTime gameTime)
        {
            if (_arenaStartWaiter != null && _arenaStartWaiter.IsEverybodyReady)
            {
                _arenaStartWaiter.EndWait();
                _arenaStartWaiter = null;
                MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerMenuHandlers(null));
                MessageHandlers.ActivateHandlers(MessageHandlers.GetServerGameplayHandlers(NetworkEngine.GameClientConnections));
                StartArenaImpl();
            }

            // Switch music off
            if (_musicSwitch.Pulse && GameState == GameState.Gameplay)
            {
                SoundEngine.StopMusic();
            }

            // Instant arena reload (simple aid for hand-editing an arena)
            if (_arenaReload.Pulse && GameState == GameState.Gameplay && NetworkMode == NetworkMode.Standalone)
            {
                var arenaFilename = DataEngine.ArenaInfos.Single(info => info.Name == DataEngine.ArenaPlaylist.Current).FileName;
                var arena = Arena.FromFile(arenaFilename);
                DataEngine.InitializeFromArena(arena, true);
                StartArena();
            }

            // Frame stepping (for debugging)
            if (_frameRunControl.Pulse)
            {
                _logicEngine.Enabled = true;
                _frameStep = false;
            }
            if (_frameStep)
            {
                if (_frameStepControl.Pulse)
                    _logicEngine.Enabled = true;
                else
                    _logicEngine.Enabled = false;
            }
            else if (_frameStepControl.Pulse)
            {
                _logicEngine.Enabled = false;
                _frameStep = true;
            }

            GameTime = gameTime;
            if (_logicEngine.Enabled) DataEngine.Arena.TotalTime += gameTime.ElapsedGameTime;

            base.Update(GameTime);
            if (_logicEngine.Enabled)
            {
                GobsCreatedPerFrameAvgPerSecondBaseCounter.Increment();
                DataEngine.CommitPending();
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if ((gameTime.TotalRealTime - _lastFramerateCheck).TotalSeconds < 1)
            {
                FramesDrawnPerSecondCounter.Increment();
                ++_framesSinceLastCheck;
            }
            else
            {
#if DEBUG_PROFILE
                _frameCounts.Add(_framesSinceLastCheck);
                _gobCounts.Add(GobCount);
                _collisionCounts.Add(CollisionCount);
                GobCount = CollisionCount = 0;
#endif
                _window.Title = "Assault Wing [~" + _framesSinceLastCheck + " fps]";
                _framesSinceLastCheck = 1;
                _lastFramerateCheck = gameTime.TotalRealTime;

                if (NetworkMode != NetworkMode.Standalone)
                    _window.Title += " [" + NetworkEngine.GetSendQueueSize() + " B send queue]";

                if (NetworkMode == NetworkMode.Client && NetworkEngine.IsConnectedToGameServer)
                    _window.Title += string.Format(" [{0} ms lag]",
                        (int)NetworkEngine.ServerPingTime.TotalMilliseconds);

                if (NetworkMode == NetworkMode.Server)
                    foreach (PingedConnection conn in NetworkEngine.GameClientConnections.Connections)
                        _window.Title += string.Format(" [#{0}: {1} ms lag]",
                            conn.Id,
                            (int)conn.PingTime.TotalMilliseconds);
            }
            lock (GraphicsDevice) base.Draw(GameTime);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            // If progress bar is running, kill its thread.
            if (DataEngine.ProgressBar.TaskRunning)
                DataEngine.ProgressBar.AbortTask();

            base.OnExiting(sender, args);
        }

        #endregion Overridden Game methods
    }
}
