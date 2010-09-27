using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Graphics;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
using AW2.Net;
using AW2.Net.Connections;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;
using AW2.Settings;
using AW2.Sound;
using AW2.UI;

namespace AW2
{
    public class AssaultWingCore : AWGame
    {
        /// <summary>
        /// Wraps <see cref="CounterCreationDataCollection"/>, adding to it an implementation
        /// of <see cref="IEnumerable&lt;CounterCreationData&gt;"/>.
        /// </summary>
        private class AWCounterCreationDataCollection : CounterCreationDataCollection, IEnumerable<CounterCreationData>
        {
            public new IEnumerator<CounterCreationData> GetEnumerator()
            {
                foreach (var x in (System.Collections.IEnumerable)this) yield return (CounterCreationData)x;
            }
        }

        #region AssaultWing fields

        private UIEngineImpl _uiEngine;
        private IntroEngine _introEngine;
        private OverlayDialog _overlayDialog;
        private LogicEngine _logicEngine;
        private int _preferredWindowWidth, _preferredWindowHeight;
        private SurfaceFormat _preferredWindowFormat;
        private int _preferredFullscreenWidth, _preferredFullscreenHeight;
        private SurfaceFormat _preferredFullscreenFormat;
        private TimeSpan _lastFramerateCheck;
        private int _framesSinceLastCheck;

        #endregion AssaultWing fields

        #region Callbacks

        /// <summary>
        /// A hack to pass the true client area size from Arena Editor to Assault Wing window.
        /// </summary>
        public static Func<System.Drawing.Size> GetRealClientAreaSize;

        /// <summary>
        /// Called when <see cref="BeginRun"/> is complete.
        /// </summary>
        public event Action RunBegan;

        #endregion Callbacks

        #region AssaultWing properties

        /// <summary>
        /// The AssaultWingCore instance. Avoid using this remnant of the old times.
        /// </summary>
        public static AssaultWingCore Instance { get; set; }

        public bool DoNotFreezeCanonicalStrings { get; set; }
        public int ManagedThreadID { get; private set; }
        public AWSettings Settings { get; private set; }
        public string[] CommandLineArgs { get; set; }
        public PhysicsEngine PhysicsEngine { get; private set; }
        public DataEngine DataEngine { get; private set; }
        public NetworkEngine NetworkEngine { get; private set; }
        public GraphicsEngineImpl GraphicsEngine { get; private set; }
        public SoundEngine SoundEngine { get; private set; }

        /// <summary>
        /// The current mode of network operation of the game.
        /// </summary>
        public NetworkMode NetworkMode { get; private set; }

        /// <summary>
        /// The game time on this frame.
        /// </summary>
        public GameTime GameTime { get; private set; }

        /// <summary>
        /// Are overlay dialogs allowed.
        /// </summary>
        public bool AllowDialogs { get; set; }

        public event Action<string> StatusTextChanged;

        #endregion AssaultWing properties

        #region AssaultWing performance counters

        /// <summary>
        /// Number of gobs created per frame, averaged over one second.
        /// </summary>
        public AWPerformanceCounter GobsCreatedPerFrameAvgPerSecondCounter { get; protected set; }

        /// <summary>
        /// Number of elapsed frames.
        /// </summary>
        public AWPerformanceCounter GobsCreatedPerFrameAvgPerSecondBaseCounter { get; protected set; }

        /// <summary>
        /// Number of gobs drawn per frame, averaged over one second.
        /// </summary>
        public AWPerformanceCounter GobsDrawnPerFrameAvgPerSecondCounter { get; protected set; }

        /// <summary>
        /// Number of elapsed frames.
        /// </summary>
        public AWPerformanceCounter GobsDrawnPerFrameAvgPerSecondBaseCounter { get; protected set; }

        /// <summary>
        /// Number of drawn frames per second.
        /// </summary>
        public AWPerformanceCounter FramesDrawnPerSecondCounter { get; protected set; }

        /// <summary>
        /// Number of gobs currently alive.
        /// </summary>
        public AWPerformanceCounter GobsCounter { get; protected set; }

        #endregion

        public AssaultWingCore(GraphicsDeviceService graphicsDeviceService)
            : base(graphicsDeviceService)
        {
            Log.Write("Creating an Assault Wing instance");
            ManagedThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;

            Log.Write("Loading settings from file");
            Settings = AWSettings.FromFile();
            InitializeGraphics();

            NetworkMode = NetworkMode.Standalone;
            GameTime = new GameTime();

            InitializeComponents();
        }

        #region AssaultWing private methods

        private void InitializeGraphics()
        {
            // Decide on preferred windowed and fullscreen sizes and formats.
            DisplayMode displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _preferredFullscreenWidth = displayMode.Width;
            _preferredFullscreenHeight = displayMode.Height;
            _preferredFullscreenFormat = displayMode.Format;
            if (GetRealClientAreaSize != null)
            {
                var size = GetRealClientAreaSize();
                _preferredWindowWidth = size.Width;
                _preferredWindowHeight = size.Height;
            }
            else
            {
                _preferredWindowWidth = Math.Min(1000, displayMode.Width);
                _preferredWindowHeight = Math.Min(800, displayMode.Height);
            }
            _preferredWindowFormat = displayMode.Format;
            AllowDialogs = true;
        }

        private void InitializeComponents()
        {
            _uiEngine = new UIEngineImpl(this);
            _logicEngine = new LogicEngine(this);
            SoundEngine = new SoundEngineXACT(this);
            GraphicsEngine = new GraphicsEngineImpl(this);
            _introEngine = new IntroEngine(this);
            NetworkEngine = new NetworkEngine(this);
            _overlayDialog = new OverlayDialog(this);
            DataEngine = new DataEngine(this);
            PhysicsEngine = new PhysicsEngine(this);

            NetworkEngine.UpdateOrder = 0;
            _uiEngine.UpdateOrder = 1;
            _logicEngine.UpdateOrder = 2;
            SoundEngine.UpdateOrder = 3;
            GraphicsEngine.UpdateOrder = 4;
            _introEngine.UpdateOrder = 4;
            _overlayDialog.UpdateOrder = 5;

            Components.Add(_logicEngine);
            Components.Add(GraphicsEngine);
            Components.Add(_introEngine);
            Components.Add(_overlayDialog);
            Components.Add(_uiEngine);
            Components.Add(SoundEngine);
            Components.Add(NetworkEngine);

            // Disable all optional components
            foreach (var component in Components)
            {
                component.Visible = false;
                component.Enabled = false;
            }
            NetworkEngine.Enabled = true;
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

        /// <summary>
        /// Freezes <see cref="CanonicalString"/> instances to enable sharing them over a network.
        /// </summary>
        private void FreezeCanonicalStrings()
        {
            // Type names of gobs, ship devices and particle engines are registered implicitly
            // above while loading the types. Graphics and ShipDeviceCollection need separate handling.
            // TODO: Loop through all textures and all 3D models available in the ContentManager.
            foreach (var assetName in ((AWContentManager)Content).GetAssetNames()) CanonicalString.Register(assetName);
            CanonicalString.DisableRegistering();
        }

        #endregion AssaultWing private methods

        #region Methods for game components

        /// <summary>
        /// Prepares a new play session to start from the first chosen arena.
        /// Call <c>StartArena</c> after this method returns to start
        /// playing the arena.
        /// </summary>
        public virtual void PrepareFirstArena()
        {
            foreach (var player in DataEngine.Spectators)
                player.InitializeForGameSession();
            DataEngine.ArenaPlaylist.Reset();
            PrepareNextArena();
        }

        /// <summary>
        /// Starts playing a previously prepared arena.
        /// </summary>
        public virtual void StartArena()
        {
            Log.Write("Starting arena");
            DataEngine.StartArena();
            DataEngine.RearrangeViewports();
            SoundEngine.PlayMusic(DataEngine.Arena.BackgroundMusic);
            Log.Write("...started arena " + DataEngine.Arena.Name);
        }

        /// <summary>
        /// Finishes playing the current arena.
        /// </summary>
        public void FinishArena()
        {
            if (NetworkMode == NetworkMode.Client) MessageHandlers.DeactivateHandlers(MessageHandlers.GetClientGameplayHandlers());
            if (NetworkMode == NetworkMode.Server) MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers());
            if (DataEngine.ArenaPlaylist.HasNext)
                ShowDialog(new ArenaOverOverlayDialogData(DataEngine.ArenaPlaylist.Next));
            else
                ShowDialog(new GameOverOverlayDialogData());
            if (NetworkMode == NetworkMode.Server)
            {
                var message = new ArenaFinishMessage();
                NetworkEngine.SendToGameClients(message);
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
            if (!DataEngine.NextArena())
                throw new InvalidOperationException("There is no next arena to play");
        }

        [Obsolete("Move to AW2.Core.AssaultWing")]
        public virtual void ShowDialog(AW2.Graphics.OverlayComponents.OverlayDialogData dialogData)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Move to AW2.Core.AssaultWing")]
        public virtual void ShowMenu()
        {
            throw new NotImplementedException();
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
        /// <returns>True on success, false on failure</returns>
        [Obsolete("Move to AW2.Core.AssaultWing")]
        public bool StartServer(Action<Result<Connection>> connectionHandler)
        {
            if (NetworkMode != NetworkMode.Standalone)
                throw new InvalidOperationException("Cannot start server while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Server;
            try
            {
                NetworkEngine.StartServer(connectionHandler);
                var handlers = MessageHandlers.GetServerMenuHandlers();
                NetworkEngine.MessageHandlers.AddRange(handlers);
                return true;
            }
            catch (Exception e)
            {
                Log.Write("Could not start server: " + e);
                NetworkMode = NetworkMode.Standalone;
            }
            return false;
        }

        /// <summary>
        /// Turns this game server into a standalone game instance and disposes of
        /// any connections to game clients.
        /// </summary>
        [Obsolete("Move to AW2.Core.AssaultWing")]
        public void StopServer()
        {
            if (NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot stop server while in mode " + NetworkMode);
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers());
            NetworkEngine.StopServer();
            NetworkMode = NetworkMode.Standalone;
            DataEngine.RemoveRemoteSpectators();
        }

        /// <summary>
        /// Turns this game instance into a game client by connecting to a game server.
        /// </summary>
        [Obsolete("Move to AW2.Core.AssaultWing")]
        public void StartClient(AWEndPoint[] serverEndPoints, Action<Result<Connection>> connectionHandler)
        {
            if (NetworkMode != NetworkMode.Standalone)
                throw new InvalidOperationException("Cannot start client while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Client;
            try
            {
                NetworkEngine.StartClient(serverEndPoints, connectionHandler);
                foreach (var spectator in DataEngine.Spectators) spectator.ResetForClient();
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Log.Write("Could not start client: " + e.Message);
                StopClient(null);
            }
        }

        /// <summary>
        /// Turns this game client into a standalone game instance by disconnecting
        /// from the game server.
        /// </summary>
        [Obsolete("Move to AW2.Core.AssaultWing")]
        public void StopClient(string errorOrNull)
        {
            if (NetworkMode != NetworkMode.Client)
                throw new InvalidOperationException("Cannot stop client while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Standalone;
            NetworkEngine.StopClient();
            DataEngine.RemoveRemoteSpectators();
            if (errorOrNull != null)
            {
                var dialogData = new AW2.Graphics.OverlayComponents.CustomOverlayDialogData(
                    errorOrNull + "\nPress Enter to return to Main Menu",
                    new TriggeredCallback(TriggeredCallback.GetProceedControl(), ShowMenu));
                ShowDialog(dialogData);
            }
        }

        /// <summary>
        /// Turns this game instance into a standalone instance, irrespective of the current
        /// network mode.
        /// </summary>
        [Obsolete("Move to AW2.Core.AssaultWing")]
        public void CutNetworkConnections()
        {
            switch (NetworkMode)
            {
                case NetworkMode.Client: StopClient(null); break;
                case NetworkMode.Server: StopServer(); break;
                case NetworkMode.Standalone: break;
                default: throw new ApplicationException("Unexpected NetworkMode: " + NetworkMode);
            }
        }

        /// <summary>
        /// Loads graphical content required by an arena to DataEngine.
        /// </summary>
        public void LoadArenaContent(Arena arena)
        {
            GraphicsEngine.LoadArenaContent(arena);
        }

        #endregion Methods for game components

        #region Overridden Game methods

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        public override void Initialize()
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
            if (!DoNotFreezeCanonicalStrings) FreezeCanonicalStrings();
        }

        public override void BeginRun()
        {
            base.BeginRun();
            if (RunBegan != null) RunBegan();
        }

        /// <summary>
        /// Called after the game loop has stopped running before exiting. 
        /// </summary>
        public override void EndRun()
        {
            Log.Write("Assault Wing ends the run");
            Log.Write("Saving settings to file");
            Settings.ToFile();
            base.EndRun();
        }

        public override void Update(GameTime gameTime)
        {
            GameTime = gameTime;
            if (_logicEngine.Enabled)
            {
                if (gameTime.ElapsedGameTime != TargetElapsedTime) throw new ApplicationException("Timestep expected " + TargetElapsedTime.TotalSeconds + " s but was " + gameTime.ElapsedGameTime + " s");
                DataEngine.Arena.TotalTime += gameTime.ElapsedGameTime;
                DataEngine.Arena.FrameNumber++;
            }

            base.Update(GameTime);
            if (_logicEngine.Enabled)
            {
                GobsCreatedPerFrameAvgPerSecondBaseCounter.Increment();
                DataEngine.CommitPending();
            }
        }

        public override void Draw()
        {
            var secondsSinceLastFramerateCheck = (GameTime.TotalRealTime - _lastFramerateCheck).TotalSeconds;
            if (secondsSinceLastFramerateCheck < 1)
            {
                FramesDrawnPerSecondCounter.Increment();
                ++_framesSinceLastCheck;
            }
            else
            {
                var newStatusText = "[~" + _framesSinceLastCheck + " fps]";
                _framesSinceLastCheck = 1;
                if (secondsSinceLastFramerateCheck < 2)
                    _lastFramerateCheck += TimeSpan.FromSeconds(1);
                else
                    _lastFramerateCheck = GameTime.TotalRealTime;

                if (NetworkMode == NetworkMode.Client && NetworkEngine.IsConnectedToGameServer)
                    newStatusText += string.Format(" [{0} ms lag]",
                        (int)NetworkEngine.ServerPingTime.TotalMilliseconds);

                if (NetworkMode == NetworkMode.Server)
                    foreach (var conn in NetworkEngine.GameClientConnections)
                        newStatusText += string.Format(" [#{0}: {1} ms lag]",
                            conn.ID,
                            (int)conn.PingInfo.PingTime.TotalMilliseconds);

#if DEBUG
                if (DataEngine.ArenaFrameCount > 0)
                    newStatusText += string.Format(" [frame {0}]", DataEngine.ArenaFrameCount);
#endif
                if (StatusTextChanged != null) StatusTextChanged(newStatusText);
            }
            base.Draw();
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
