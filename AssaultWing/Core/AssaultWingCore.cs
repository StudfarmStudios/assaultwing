using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Net;
using AW2.Settings;
using AW2.Sound;
using AW2.UI;

namespace AW2.Core
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
        private TimeSpan _lastFramerateCheck;
        private int _framesSinceLastCheck;

        #endregion AssaultWing fields

        #region Callbacks

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
        public CommandLineOptions CommandLineOptions { get; set; }
        public PhysicsEngine PhysicsEngine { get; private set; }
        public DataEngine DataEngine { get; private set; }
        public LogicEngine LogicEngine { get; private set; }
        public NetworkEngine NetworkEngine { get; private set; }
        public GraphicsEngineImpl GraphicsEngine { get; private set; }
        public SoundEngine SoundEngine { get; private set; }

        /// <summary>
        /// The current mode of network operation of the game.
        /// </summary>
        public NetworkMode NetworkMode { get; protected set; }

        /// <summary>
        /// The game time on this frame.
        /// </summary>
        public AWGameTime GameTime { get; private set; }

        /// <summary>
        /// Are overlay dialogs allowed.
        /// </summary>
        public bool AllowDialogs { get; set; }

        public Window Window { get; set; }

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
            GameTime = new AWGameTime();

            InitializeComponents();
        }

        #region AssaultWing private methods

        private void InitializeGraphics()
        {
            AllowDialogs = true;
        }

        private void InitializeComponents()
        {
            NetworkEngine = new NetworkEngine(this, 0);
            DataEngine = new DataEngine(this, 0);
            PhysicsEngine = new PhysicsEngine(this, 0);
            _uiEngine = new UIEngineImpl(this, 1);
            LogicEngine = new LogicEngine(this, 2);
            SoundEngine = new SoundEngineXACT(this, 3);
            GraphicsEngine = new GraphicsEngineImpl(this, 4);

            Components.Add(LogicEngine);
            Components.Add(GraphicsEngine);
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
                    catch (UnauthorizedAccessException)
                    {
                        Log.Write("Note: Performance monitoring not available due to lack of user rights. Try 'Run as administrator'");
                    }
                    catch (System.ComponentModel.Win32Exception e)
                    {
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
        /// Prepares a new play session to start from an arena.
        /// Call <see cref="StartArena"/> after this method returns to start
        /// playing the arena.
        /// This method usually takes a long time to run. It's therefore a good
        /// idea to make it run in a background thread.
        /// </summary>
        protected void PrepareArena(string arenaName)
        {
            foreach (var player in DataEngine.Spectators)
                player.InitializeForGameSession();
            var arenaFilename = DataEngine.ArenaInfos.Single(info => info.Name == arenaName).FileName;
            DataEngine.InitializeFromArena(arenaFilename, true);
        }

        /// <summary>
        /// Starts playing a previously prepared arena.
        /// </summary>
        public virtual void StartArena()
        {
            Log.Write("Starting arena");
            LogicEngine.Reset();
            DataEngine.StartArena();
            DataEngine.RearrangeViewports();
            SoundEngine.PlayMusic(DataEngine.Arena.BackgroundMusic);
            Log.Write("...started arena " + DataEngine.Arena.Info.Name);
        }

        /// <summary>
        /// Finishes playing the current arena.
        /// </summary>
        public virtual void FinishArena()
        {
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
        /// Turns this game client into a standalone game instance by disconnecting
        /// from the game server.
        /// </summary>
        [Obsolete("Move to AW2.Core.AssaultWing")]
        public virtual void StopClient(string errorOrNull)
        {
        }

        /// <summary>
        /// Turns this game instance into a standalone instance, irrespective of the current
        /// network mode.
        /// </summary>
        [Obsolete("Move to AW2.Core.AssaultWing")]
        public virtual void CutNetworkConnections()
        {
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
                if (CommandLineOptions.PerformanceCounters)
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
            TargetFPS = 60;
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

        public override void Update(AWGameTime gameTime)
        {
            GameTime = gameTime;
            if (LogicEngine.Enabled)
            {
                if (gameTime.ElapsedGameTime != TargetElapsedTime) throw new ApplicationException("Timestep expected " + TargetElapsedTime.TotalSeconds + " s but was " + gameTime.ElapsedGameTime + " s");
                DataEngine.Arena.TotalTime += gameTime.ElapsedGameTime;
                DataEngine.Arena.FrameNumber++;
            }

            base.Update(GameTime);
            if (LogicEngine.Enabled)
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
                var newStatusText = "Assault Wing [~" + _framesSinceLastCheck + " fps]";
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
                Window.Title = newStatusText;
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
