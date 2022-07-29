using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core.GameComponents;
using AW2.Game;
using AW2.Game.Logic;
using AW2.Game.Players;
using AW2.Graphics;
using AW2.Graphics.Content;
using AW2.Helpers;
using AW2.Settings;
using AW2.Sound;
using AW2.UI;
using Microsoft.Xna.Framework.Graphics;
using AW2.Net;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;

namespace AW2.Core
{
    [DebuggerDisplay("AssaultWingCore {NetworkMode}")]
    public class AssaultWingCore : AWGame
    {
        #region AssaultWing fields

        private UIEngineImpl _uiEngine;
        private bool _arenaFinished;
        private AWTimer _standingsTimer;

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

        public AWSettings Settings { get; set; }
        public CommandLineOptions CommandLineOptions { get; private set; }
        public DataEngine DataEngine { get; private set; }
        public PreFrameLogicEngine PreFrameLogicEngine { get; private set; }
        public LogicEngine LogicEngine { get; private set; }
        public PostFrameLogicEngine PostFrameLogicEngine { get; private set; }
        public GraphicsEngineImpl GraphicsEngine { get; private set; }
        public SoundEngineXNA SoundEngine { get; private set; }
        public StatsBase Stats { get; set; }
        public SteamComponent SteamComponent { get; set; }

        public bool IsSteam { get; set; }

        /// <summary>
        /// The current mode of network operation of the game.
        /// </summary>
        public NetworkMode NetworkMode { get; set; }

        public virtual bool IsShipControlsEnabled { get { return true; } }
        public Window Window { get; set; }

        private static readonly TimeSpan ARGUMENT_FILE_AGE_MAX = TimeSpan.FromHours(1);
        public static string ArgumentPath { get { return Path.Combine(MiscHelper.DataDirectory, "arguments.txt"); } }
        public static string GetArgumentText()
        {
            if (!File.Exists(ArgumentPath)) return "";
            var argumentAge = DateTime.Now - File.GetLastWriteTime(ArgumentPath);
            if (argumentAge < ARGUMENT_FILE_AGE_MAX)
            {
                Log.Write("Reading argument file {0} because it's less than {1} old", ArgumentPath, MiscHelper.ToDurationString(ARGUMENT_FILE_AGE_MAX, "day", "hour", "minute", "second", usePlurals: true));
                var text = File.ReadAllText(ArgumentPath);
                try { File.Delete(ArgumentPath); }
                catch { }
                return text;
            }
            return "";
        }

        #endregion AssaultWing properties

        public AssaultWingCore(GameServiceContainer serviceContainer, CommandLineOptions args)
            : base(serviceContainer, ignoreGraphicsContent: args.DedicatedServer)
        {
            Log.Write("Assault Wing version " + MiscHelper.Version);
            CommandLineOptions = args;
            Log.Write("Loading settings from " + MiscHelper.DataDirectory);
            Settings = AWSettings.FromFile(this, MiscHelper.DataDirectory);            
            NetworkMode = NetworkMode.Standalone;
            NetworkingErrors = new Queue<string>();

            InitializeComponents();
            _standingsTimer = new AWTimer(() => GameTime.TotalRealTime, TimeSpan.FromSeconds(1));
        }

        #region AssaultWing private methods

        private void InitializeComponents()
        {
            DataEngine = new DataEngine(this, 0);
            PreFrameLogicEngine = new PreFrameLogicEngine(this, 2);
            LogicEngine = new LogicEngine(this, 3);
            PostFrameLogicEngine = new PostFrameLogicEngine(this, 4);
            _uiEngine = new UIEngineImpl(this, 1);
            Stats = new StatsBase(this, 7);
            SoundEngine = new SoundEngineXNA(this, 5);

            SteamComponent = new SteamComponent(this, 0);

            Components.Add(PreFrameLogicEngine);
            Components.Add(LogicEngine);
            Components.Add(PostFrameLogicEngine);
            Components.Add(SoundEngine);
            Components.Add(_uiEngine);
            Components.Add(Stats);
            Components.Add(SteamComponent);
            SoundEngine.Enabled = !CommandLineOptions.DedicatedServer;
            _uiEngine.Enabled = true;
            SteamComponent.Enabled = true;

            if (!CommandLineOptions.DedicatedServer)
            {
                GraphicsEngine = new GraphicsEngineImpl(this, 6);
                Components.Add(GraphicsEngine);
            }
        }

        /// <summary>
        /// Freezes <see cref="CanonicalString"/> instances to enable sharing them over a network.
        /// </summary>
        private void FreezeCanonicalStrings()
        {
            // Type names of gobs, ship devices and particle engines are registered implicitly
            // above while loading the types. Graphics and ShipDeviceCollection need separate handling.
            foreach (var assetName in ((AWContentManager)Content).GetAssetNames()) CanonicalString.Register(assetName);
            CanonicalString.DisableRegistering();
        }

        private string[] GetHelpMessages(Player player)
        {
            return new[]
            {
                string.Format("Moving: {0}, {1}, {2}", player.Controls.Thrust, player.Controls.Left, player.Controls.Right),
                string.Format("Firing: {0}, {1}, {2}", player.Controls.Fire1, player.Controls.Fire2, player.Controls.Extra),
                string.Format("Chat: {0}; Equip ship: Esc; Quick help: F1", Settings.Controls.Chat),
            };
        }

        private void BeforeEveryFrame()
        {
            DataEngine.Arena.TotalTime += GameTime.ElapsedGameTime;
            DataEngine.Arena.FrameNumber++;
        }

        private void AfterEveryFrame()
        {
            if (_standingsTimer.IsElapsed) DataEngine.UpdateStandings();
        }

        #endregion AssaultWing private methods

        #region Methods for game components

        /// <summary>
        /// Starts playing a previously prepared arena.
        /// </summary>
        public virtual void StartArena()
        {
            Log.Write("Starting arena");
            _arenaFinished = false;
            LogicEngine.Reset();
            PreFrameLogicEngine.Reset();
            PostFrameLogicEngine.Reset();
            PreFrameLogicEngine.DoEveryFrame += BeforeEveryFrame;
            PostFrameLogicEngine.DoEveryFrame += AfterEveryFrame;
            DataEngine.StartArena();
            DataEngine.RearrangeViewports();
            ShowPlayerHelp();
            Log.Write("...started arena " + DataEngine.Arena.Info.Name);
        }

        public virtual string StartServer() { return ""; }
        public string SelectedArenaName { get; set; }

        public NetworkEngine NetworkEngine { get; protected set; }
        
        /// <summary>
        /// Errors that occurred when establishing or maintaining a network game instance.
        /// These errors are eventually reported to the user and game state is reset out of networking.
        /// </summary>
        public Queue<string> NetworkingErrors { get; init; }

        public MessageHandlers MessageHandlers { get; protected set; }
        
        public virtual void GobCreationMessageReceived(GobCreationMessage message, int framesAgo) {}

        public virtual void StartClient(AWEndPoint[] serverEndPoints) {}

        public virtual void PrepareArenaOnClient(CanonicalString gameplayMode, string arenaName, byte arenaIDOnClient, int wallCount) {}

        public virtual void AddRemoteSpectator(Spectator newSpectator) {}

        public virtual void UpdateGameServerInfoToManagementServer() {}

        public virtual void LoadSelectedArena(byte? arenaIDOnClient = null) {}


        /// <summary>
        /// Finishes playing the current arena.
        /// </summary>
        public void FinishArena()
        {
            if (_arenaFinished) return;
            _arenaFinished = true;
            FinishArenaImpl();
        }

        public void ShowPlayerHelp()
        {
            foreach (var player in DataEngine.Players.Where(plr => plr.IsLocal))
                foreach (var mess in GetHelpMessages(player))
                    player.Messages.Add(new PlayerMessage("Help>", mess, PlayerMessage.DEFAULT_COLOR));
        }

        public virtual void RefreshGameSettings()
        {
            Settings = AWSettings.FromFile(this, MiscHelper.DataDirectory);
            if (NetworkMode != NetworkMode.Client)
                foreach (var op in DataEngine.GameplayMode.UpdateBotPlayerConfiguration(DataEngine.Teams, Settings).ToArray())
                    DataEngine.Apply(op);
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
            base.Initialize();
            if (!CanonicalString.IsForLocalUseOnly) FreezeCanonicalStrings();
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
#if NETWORK_PROFILING
            AW2.Helpers.Serialization.ProfilingNetworkBinaryWriter.DumpStats();
#endif
            base.EndRun();
        }

        public override void Draw()
        {
            Window.Impl.SetTitle(GetStatusText());
            base.Draw();
        }

        protected virtual string GetStatusText()
        {
            return "Assault Wing [~" + FramesDrawnLastSecond + " fps]";
        }

        protected virtual void FinishArenaImpl() { }

        #endregion Overridden Game methods
    }
}
