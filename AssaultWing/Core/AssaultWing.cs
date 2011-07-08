using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Core.GameComponents;
using AW2.Core.OverlayComponents;
using AW2.Game;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Menu;
using AW2.Net;
using AW2.Net.Connections;
using AW2.Net.ManagementMessages;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;
using AW2.UI;

namespace AW2.Core
{
    [System.Diagnostics.DebuggerDisplay("AssaultWing {NetworkMode} {GameState}")]
    public class AssaultWing : AssaultWingCore
    {
        private GameState _gameState;
        private Control _escapeControl;
        private List<Gob> _addedGobs;
        private TimeSpan _lastGameSettingsSent;
        private TimeSpan _lastFrameNumberSynchronization;
        private byte _nextArenaID;


        // Debug keys, used only #if DEBUG
        private Control _frameStepControl;
        private Control _frameRunControl;
        private bool _frameStep;

        /// <summary>
        /// The AssaultWing instance. Avoid using this remnant of the old times.
        /// </summary>
        public static new AssaultWing Instance { get { return (AssaultWing)AssaultWingCore.Instance; } }

        public override Version Version
        {
            get
            {
                return ApplicationDeployment.IsNetworkDeployed
                    ? ApplicationDeployment.CurrentDeployment.CurrentVersion
                    : base.Version;
            }
        }
        public override string SettingsDirectory
        {
            get
            {
                return ApplicationDeployment.IsNetworkDeployed
                    ? ApplicationDeployment.CurrentDeployment.DataDirectory
                    : base.SettingsDirectory;
            }
        }

        public GameState GameState
        {
            get { return _gameState; }
            private set
            {
                DisableCurrentGameState();
                EnableGameState(value);
                var oldState = _gameState;
                _gameState = value;
                if (value == GameState.Gameplay || value == GameState.GameplayStopped)
                    ApplyInGameGraphicsSettings();
                if (GameStateChanged != null && _gameState != oldState)
                    GameStateChanged(_gameState);
            }
        }
        public bool IsClientAllowedToStartArena { get; set; }
        public bool IsLoadingArena { get { return MenuEngine.ArenaLoadTask.TaskRunning; } }

        public event Action<GameState> GameStateChanged;
        public string SelectedArenaName { get; set; }
        public MenuEngineImpl MenuEngine { get; private set; }
        private StartupScreen StartupScreen { get; set; }
        private IntroEngine IntroEngine { get; set; }
        private OverlayDialog OverlayDialog { get; set; }
        private PlayerChat PlayerChat { get; set; }
        public UIEngineImpl UIEngine { get { return (UIEngineImpl)Components.First(c => c is UIEngineImpl); } }
        public NetworkEngine NetworkEngine { get; private set; }

        public AssaultWing(GraphicsDeviceService graphicsDeviceService, CommandLineOptions args)
            : base(graphicsDeviceService, args)
        {
            StartupScreen = new StartupScreen(this, -1);
            NetworkEngine = new NetworkEngine(this, 0);
            MenuEngine = new MenuEngineImpl(this, 10);
            IntroEngine = new IntroEngine(this, 11);
            PlayerChat = new PlayerChat(this, 12);
            OverlayDialog = new OverlayDialog(this, 20);
            Components.Add(NetworkEngine);
            Components.Add(StartupScreen);
            Components.Add(MenuEngine);
            if (!CommandLineOptions.DedicatedServer) Components.Add(IntroEngine);
            Components.Add(PlayerChat);
            Components.Add(OverlayDialog);
            GameState = GameState.Initializing;
            _escapeControl = new KeyboardKey(Keys.Escape);
            _frameStepControl = new KeyboardKey(Keys.F8);
            _frameRunControl = new KeyboardKey(Keys.F7);
            _frameStep = false;
            _addedGobs = new List<Gob>();
            DataEngine.SpectatorAdded += SpectatorAddedHandler;
            DataEngine.SpectatorRemoved += SpectatorRemovedHandler;
            NetworkEngine.Enabled = true;
            if (CommandLineOptions.DedicatedServer)
            {
                var dedicatedServer = new DedicatedServer(this, 13);
                Components.Add(dedicatedServer);
                dedicatedServer.Enabled = true;
            }
        }

        public override void Update(AWGameTime gameTime)
        {
            base.Update(gameTime);
            UpdateSpecialKeys();
            UpdateDebugKeys();
            SynchronizeFrameNumber();
            SendGobCreationMessage();
            SendGameSettings();
        }

        public void ShowDialog(OverlayDialogData dialogData)
        {
            if (!AllowDialogs) return;
            OverlayDialog.Show(dialogData);
        }

        public void HideDialog()
        {
            OverlayDialog.Dismiss();
        }

        public void ShowMainMenuAndResetGameplay()
        {
            CutNetworkConnections();
            EnsureArenaLoadingStopped();
            DataEngine.ClearGameState();
            MenuEngine.Activate();
            GameState = GameState.Menu;
        }

        /// <summary>
        /// Called after all components are initialized but before the first update in the game loop. 
        /// </summary>
        public override void BeginRun()
        {
            Log.Write("Assault Wing begins to run");
            SelectedArenaName = DataEngine.GetTypeTemplates<Arena>().First().Info.Name;
            DataEngine.GameplayMode = new GameplayMode();
            DataEngine.GameplayMode.ShipTypes = new[] { "Windlord", "Bugger", "Plissken" };
            DataEngine.GameplayMode.ExtraDeviceTypes = new[] { "blink", "repulsor", "catmoflage" };
            DataEngine.GameplayMode.Weapon2Types = new[] { "bazooka", "rockets", "hovermine" };
            if (!CommandLineOptions.DedicatedServer) GameState = GameState.Intro;
            base.BeginRun();
        }

        public override void EndRun()
        {
            GameState = GameState.Initializing;
            base.EndRun();
        }

        /// <summary>
        /// Prepares a new play session to start from the arena called <see cref="SelectedArenaName"/>.
        /// Call <see cref="StartArena"/> after this method returns to start playing the arena.
        /// This method usually takes a long time to run. It's therefore a good
        /// idea to make it run in a background thread.
        /// </summary>
        public void PrepareSelectedArena(byte? arenaIDOnClient = null)
        {
            foreach (var player in DataEngine.Spectators)
                player.InitializeForGameSession();
            var arenaTemplate = (Arena)DataEngine.GetTypeTemplate((CanonicalString)SelectedArenaName);
            // Note: Must create a new Arena instance and not use the existing template
            // because playing an arena will modify it.
            InitializeFromArena(arenaTemplate.Info.FileName, arenaIDOnClient.HasValue ? arenaIDOnClient.Value : _nextArenaID++);
        }

        public void StartArenaButStayInMenu()
        {
            if (NetworkMode != NetworkMode.Client) throw new InvalidOperationException("Only client can start arena on background");
            base.StartArena();
            PostFrameLogicEngine.DoEveryFrame += AfterEveryFrame;
            GameState = GameState.GameAndMenu;
        }

        public override void ProgressBarSubtaskCompleted()
        {
            MenuEngine.ProgressBar.SubtaskCompleted();
        }

        public override void StartArena()
        {
            Log.Write("Saving settings to file");
            Settings.ToFile();
            if (NetworkMode == NetworkMode.Server)
            {
                MessageHandlers.ActivateHandlers(MessageHandlers.GetServerGameplayHandlers());
                DataEngine.Arena.GobAdded += gob => { if (gob.IsRelevant) _addedGobs.Add(gob); };
                DataEngine.Arena.GobRemoved += GobRemovedFromArenaHandler;
            }
            if (GameState != GameState.GameAndMenu)
            {
                base.StartArena();
                PostFrameLogicEngine.DoEveryFrame += AfterEveryFrame;
            }
            GameState = GameState.Gameplay;
            SoundEngine.PlayMusic(DataEngine.Arena.BackgroundMusic);
        }

        public void InitializePlayers(int count)
        {
            var players = new[]
            {
                new Player(this, "Newbie",
                    (CanonicalString)"Plissken", (CanonicalString)"bazooka", (CanonicalString)"repulsor",
                    PlayerControls.FromSettings(Settings.Controls.Player1)),
                new Player(this, "Lamer",
                    (CanonicalString)"Bugger", (CanonicalString)"hovermine", (CanonicalString)"catmoflage",
                    PlayerControls.FromSettings(Settings.Controls.Player2))
            };
            DataEngine.Spectators.Clear();
            foreach (var plr in players.Take(count)) DataEngine.Spectators.Add(plr);
        }

        /// <summary>
        /// Turns this game instance into a game server to whom other game instances
        /// can connect as game clients. Returns true on success, false on failure.
        /// </summary>
        public bool StartServer()
        {
            if (NetworkMode != NetworkMode.Standalone)
                throw new InvalidOperationException("Cannot start server while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Server;
            try
            {
                NetworkEngine.StartServer(result => MessageHandlers.IncomingConnectionHandlerOnServer(result, () => true));
                MessageHandlers.ActivateHandlers(MessageHandlers.GetServerMenuHandlers());
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
        public void StopServer()
        {
            if (NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot stop server while in mode " + NetworkMode);
            DeactivateAllMessageHandlers();
            NetworkEngine.StopServer();
            NetworkMode = NetworkMode.Standalone;
            DataEngine.RemoveRemoteSpectators();
        }

        /// <summary>
        /// Turns this game instance into a game client by connecting to a game server.
        /// </summary>
        public void StartClient(AWEndPoint[] serverEndPoints, Action<Result<Connection>> connectionHandler)
        {
            if (NetworkMode != NetworkMode.Standalone)
                throw new InvalidOperationException("Cannot start client while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Client;
            IsClientAllowedToStartArena = false;
            try
            {
                NetworkEngine.StartClient(this, serverEndPoints, connectionHandler);
                foreach (var spectator in DataEngine.Spectators) spectator.ResetForClient();
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Log.Write("Could not start client: " + e.Message);
                StopClient(null);
            }
        }

        public void StopClient(string errorOrNull)
        {
            if (NetworkMode != NetworkMode.Client)
                throw new InvalidOperationException("Cannot stop client while in mode " + NetworkMode);
            DeactivateAllMessageHandlers();
            NetworkEngine.StopClient();
            DataEngine.RemoveRemoteSpectators();
            StopGameplay(); // gameplay cannot continue because it's initialized only for a client
            NetworkMode = NetworkMode.Standalone;
            if (errorOrNull != null)
            {
                var dialogData = new CustomOverlayDialogData(this,
                    errorOrNull + "\nPress Enter to return to Main Menu",
                    new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, ShowMainMenuAndResetGameplay));
                ShowDialog(dialogData);
            }
        }

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

        public void SendMessageToAllPlayers(string text, Player from)
        {
            var messageContent = text.Trim();
            if (messageContent == "") return;
            var preText = from.Name + ">";
            var textColor = from.PlayerColor;
            var message = new PlayerMessage(preText, messageContent, textColor);
            switch (NetworkMode)
            {
                case NetworkMode.Server:
                    foreach (var plr in DataEngine.Players) plr.Messages.Add(message);
                    break;
                case NetworkMode.Client:
                    foreach (var plr in DataEngine.Players.Where(plr => !plr.IsRemote)) plr.Messages.Add(message);
                    NetworkEngine.GameServerConnection.Send(new PlayerMessageMessage
                    {
                        PlayerID = -1,
                        Message = message,
                    });
                    break;
                default: throw new InvalidOperationException("Text messages not supported in mode " + NetworkMode);
            }
        }

        public void HandleGobCreationMessage(GobCreationMessage message, int framesAgo)
        {
            if (message.ArenaID != DataEngine.Arena.ID) return;
            message.ReadGobs(framesAgo,
                (typeName, layerIndex) =>
                {
                    if (layerIndex < 0 || layerIndex >= DataEngine.Arena.Layers.Count) return null;
                    var gob = (Gob)Clonable.Instantiate(typeName);
                    gob.Game = this;
                    gob.Layer = DataEngine.Arena.Layers[layerIndex];
                    return gob;
                },
                DataEngine.Arena.Gobs.Add);
        }

        protected override string GetStatusText()
        {
            string myStatusText = "";
            if (NetworkMode == NetworkMode.Client && NetworkEngine.IsConnectedToGameServer)
                myStatusText = string.Format(" [{0} ms lag]",
                    (int)NetworkEngine.ServerPingTime.TotalMilliseconds);
            if (NetworkMode == NetworkMode.Server)
                myStatusText = string.Join(" ", NetworkEngine.GameClientConnections
                    .Select(conn => string.Format(" [#{0}: {1} ms lag]", conn.ID, (int)conn.PingInfo.PingTime.TotalMilliseconds)
                    ).ToArray());
            return base.GetStatusText() + myStatusText;
        }

        protected override void FinishArenaImpl()
        {
            IsClientAllowedToStartArena = false;
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetClientGameplayHandlers(null));
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers());
            EnsureArenaLoadingStopped();
            DataEngine.ClearGameState();
            if (CommandLineOptions.DedicatedServer)
                GameState = GameState.Initializing;
            else
            {
                ShowEquipMenu();
                ShowDialog(new GameOverOverlayDialogData(this));
            }
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            EnsureArenaLoadingStopped();
            base.OnExiting(sender, args);
        }

        private void EnsureArenaLoadingStopped()
        {
            if (MenuEngine.ArenaLoadTask.TaskRunning) MenuEngine.ArenaLoadTask.AbortTask();
            MenuEngine.ProgressBar.SkipRemainingSubtasks();
        }

        private void ApplyInGameGraphicsSettings()
        {
            if (Window == null) return;
            if (Settings.Graphics.IsVerticalSynced)
                Window.EnableVerticalSync();
            else
                Window.DisableVerticalSync();
            if (Settings.Graphics.InGameFullscreen)
                Window.SetFullScreen(Settings.Graphics.FullscreenWidth, Settings.Graphics.FullscreenHeight);
            else
                Window.SetWindowed();
        }

        private void EnableGameState(GameState value)
        {
            switch (value)
            {
                case GameState.Initializing:
                    StartupScreen.Enabled = true;
                    StartupScreen.Visible = true;
                    break;
                case GameState.Intro:
                    IntroEngine.Enabled = true;
                    IntroEngine.Visible = true;
                    break;
                case GameState.Gameplay:
                    LogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    PreFrameLogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    PostFrameLogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    if (!CommandLineOptions.DedicatedServer)
                    {
                        GraphicsEngine.Visible = true;
                        if (NetworkMode != NetworkMode.Standalone) PlayerChat.Enabled = PlayerChat.Visible = true;
                    }
                    break;
                case GameState.GameplayStopped:
                    GraphicsEngine.Visible = true;
                    if (NetworkMode != NetworkMode.Standalone) PlayerChat.Visible = true;
                    break;
                case GameState.GameAndMenu:
                    LogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    PreFrameLogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    PostFrameLogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    MenuEngine.Enabled = true;
                    MenuEngine.Visible = true;
                    break;
                case GameState.Menu:
                    MenuEngine.Enabled = true;
                    MenuEngine.Visible = true;
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
                    StartupScreen.Enabled = false;
                    StartupScreen.Visible = false;
                    break;
                case GameState.Intro:
                    IntroEngine.Enabled = false;
                    IntroEngine.Visible = false;
                    break;
                case GameState.Gameplay:
                    LogicEngine.Enabled = false;
                    PreFrameLogicEngine.Enabled = false;
                    PostFrameLogicEngine.Enabled = false;
                    GraphicsEngine.Visible = false;
                    PlayerChat.Enabled = PlayerChat.Visible = false;
                    break;
                case GameState.GameplayStopped:
                    GraphicsEngine.Visible = false;
                    PlayerChat.Visible = false;
                    break;
                case GameState.GameAndMenu:
                    LogicEngine.Enabled = false;
                    PreFrameLogicEngine.Enabled = false;
                    PostFrameLogicEngine.Enabled = false;
                    MenuEngine.Enabled = false;
                    MenuEngine.Visible = false;
                    break;
                case GameState.Menu:
                    MenuEngine.Enabled = false;
                    MenuEngine.Visible = false;
                    break;
                default:
                    throw new ApplicationException("Cannot change away from unexpected game state " + GameState);
            }
        }

        /// <summary>
        /// Prepares the game data for playing an arena.
        /// When the playing really should start, call <see cref="StartArena"/>.
        /// </summary>
        private void InitializeFromArena(string arenaFilename, byte arenaID)
        {
            var arena = Arena.FromFile(this, arenaFilename);
            arena.ID = arenaID;
            arena.Bin.Load(System.IO.Path.Combine(Paths.ARENAS, arena.BinFilename));
            arena.IsForPlaying = true;
            // Note: Client starts progressbar when receiving StartGameMessage.
            if (NetworkMode != NetworkMode.Client) MenuEngine.ProgressBar.Start(arena.Gobs.OfType<AW2.Game.Gobs.Wall>().Count());
            foreach (var conn in NetworkEngine.GameClientConnections) conn.PingInfo.AllowLatePingsForAWhile();
            arena.Reset(); // this usually takes several seconds
            DataEngine.Arena = arena;
        }

        private void StopGameplay()
        {
            switch (GameState)
            {
                case GameState.Gameplay: GameState = GameState.GameplayStopped; break;
                case GameState.GameAndMenu: GameState = GameState.Menu; break;
            }
            MenuEngine.DeactivateComponentsExceptMainMenu();
        }

        private void ShowEquipMenu()
        {
            MenuEngine.Activate();
            MenuEngine.ActivateComponent(MenuComponentType.Equip);
            GameState = GameState.Menu;
        }

        private void ShowEquipMenuWhileKeepingGameRunning()
        {
            if (GameState == GameState.Menu) return;
            MenuEngine.Activate();
            MenuEngine.ActivateComponent(MenuComponentType.Equip);
            GameState = GameState.GameAndMenu;
        }

        private void UpdateSpecialKeys()
        {
            if (GameState == GameState.Gameplay && _escapeControl.Pulse && !OverlayDialog.Enabled)
            {
                OverlayDialogData dialogData;
                switch (NetworkMode)
                {
                    case NetworkMode.Server:
                        dialogData = new CustomOverlayDialogData(this,
                            "Finish Arena? (Yes/No)",
                            new TriggeredCallback(TriggeredCallback.YES_CONTROL, FinishArena),
                            new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }));
                        break;
                    case NetworkMode.Client:
                        dialogData = new CustomOverlayDialogData(this,
                            "Pop by to equip your ship? (Yes/No)",
                            new TriggeredCallback(TriggeredCallback.YES_CONTROL, ShowEquipMenuWhileKeepingGameRunning),
                            new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }));
                        break;
                    case NetworkMode.Standalone:
                        dialogData = new CustomOverlayDialogData(this,
                            "Quit to Main Menu? (Yes/No)",
                            new TriggeredCallback(TriggeredCallback.YES_CONTROL, ShowMainMenuAndResetGameplay),
                            new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }));
                        break;
                    default: throw new ApplicationException();
                }
                ShowDialog(dialogData);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void UpdateDebugKeys()
        {
            // Frame stepping (for debugging)
            if (_frameRunControl.Pulse)
            {
                LogicEngine.Enabled = PreFrameLogicEngine.Enabled = PostFrameLogicEngine.Enabled = true;
                _frameStep = false;
            }
            if (_frameStep)
            {
                if (_frameStepControl.Pulse)
                    LogicEngine.Enabled = PreFrameLogicEngine.Enabled = PostFrameLogicEngine.Enabled = true;
                else
                    LogicEngine.Enabled = PreFrameLogicEngine.Enabled = PostFrameLogicEngine.Enabled = false;
            }
            else if (_frameStepControl.Pulse)
            {
                LogicEngine.Enabled = PreFrameLogicEngine.Enabled = PostFrameLogicEngine.Enabled = false;
                _frameStep = true;
            }

            // Cheat codes during dialog.
            if (OverlayDialog.Enabled && (GameState == GameState.Gameplay || GameState == GameState.GameplayStopped))
            {
                var keys = Keyboard.GetState();
                if (keys.IsKeyDown(Keys.K) && keys.IsKeyDown(Keys.P))
                {
                    // K + P = kill players
                    var ships = DataEngine.Players.Select(p => p.Ship).Where(s => s != null);
                    foreach (var ship in ships) ship.Die();
                }

                if (keys.IsKeyDown(Keys.E) && keys.IsKeyDown(Keys.A))
                {
                    // E + A = end arena
                    if (!MenuEngine.ArenaLoadTask.TaskRunning) FinishArena();
                }
            }
        }

        private void SynchronizeFrameNumber()
        {
            if (NetworkMode != NetworkMode.Client) return;
            if (!NetworkEngine.IsConnectedToGameServer) return;
            if (_lastFrameNumberSynchronization + TimeSpan.FromSeconds(1) > GameTime.TotalRealTime) return;
            _lastFrameNumberSynchronization = GameTime.TotalRealTime;
            if (GameState != GameState.Gameplay && GameState != GameState.GameAndMenu) return;
            var remoteFrameNumberOffset = NetworkEngine.GameServerConnection.PingInfo.RemoteFrameNumberOffset;
            DataEngine.Arena.FrameNumber -= remoteFrameNumberOffset;
            NetworkEngine.GameServerConnection.PingInfo.AdjustRemoteFrameNumberOffset(remoteFrameNumberOffset);
        }

        private void SendGobCreationMessage()
        {
            if (MenuEngine.ArenaLoadTask.TaskRunning) return; // wait for arena load completion
            if (DataEngine.Arena == null) return; // happens if gobs are created on the frame the arena ends
            if (NetworkMode == NetworkMode.Server && _addedGobs.Any())
            {
                var message = new GobCreationMessage { ArenaID = DataEngine.Arena.ID };
                foreach (var gob in _addedGobs) message.AddGob(gob);
                _addedGobs.Clear();
                NetworkEngine.SendToGameClients(message);
            }
        }

        private void DeactivateAllMessageHandlers()
        {
            NetworkEngine.MessageHandlers.Clear();
        }

        private void GobRemovedFromArenaHandler(Gob gob)
        {
            if (!gob.IsRelevant) return;
            var message = new GobDeletionMessage();
            message.GobID = gob.ID;
            NetworkEngine.SendToGameClients(message);
        }

        private void SpectatorAddedHandler(Spectator spectator)
        {
            if (NetworkMode == NetworkMode.Server) UpdateGameServerInfoToManagementServer();
            var player = spectator as Player;
            if (player == null) return;
            player.ResetForArena();
            if (NetworkMode != NetworkMode.Server || !player.IsRemote) return;
            player.IsAllowedToCreateShip = () => NetworkEngine.GetGameClientConnection(player.ConnectionID).ConnectionStatus.IsPlayingArena;
            player.Messages.NewMessage += message =>
            {
                try
                {
                    var messageMessage = new PlayerMessageMessage { PlayerID = player.ID, Message = message };
                    NetworkEngine.GetGameClientConnection(player.ConnectionID).Send(messageMessage);
                }
                catch (InvalidOperationException)
                {
                    // The connection of the player doesn't exist any more. Just don't send the message then.
                }
            };
        }

        private void SpectatorRemovedHandler(Spectator spectator)
        {
            if (NetworkMode == NetworkMode.Server)
            {
                UpdateGameServerInfoToManagementServer();
                var clientMessage = new PlayerDeletionMessage { PlayerID = spectator.ID };
                NetworkEngine.SendToGameClients(clientMessage);
            }
        }

        public void UpdateGameServerInfoToManagementServer()
        {
            var managementMessage = new UpdateGameServerMessage { CurrentClients = DataEngine.Players.Count() };
            NetworkEngine.ManagementServerConnection.Send(managementMessage);
        }

        private void AfterEveryFrame()
        {
#if NETWORK_PROFILING
            if (DataEngine.Arena.FrameNumber == 1)
            {
                ProfilingNetworkBinaryWriter.Reset();
            }
            using(new NetworkProfilingScope(string.Format("Frame {0:0000}", DataEngine.Arena.FrameNumber)))
#endif
            {
                switch (NetworkMode)
                {
                    case NetworkMode.Server:
                        SendGobUpdates();
                        SendPlayerUpdatesOnServer();
                        break;
                    case NetworkMode.Client:
                        SendPlayerUpdatesOnClient();
                        break;
                }
            }
        }

        private void SendGobUpdates()
        {
            var now = DataEngine.ArenaTotalTime;
            var gobMessage = new GobUpdateMessage();
            foreach (var gob in DataEngine.Arena.Gobs.GameplayLayer.Gobs)
            {
                if (!gob.ForcedNetworkUpdate)
                {
                    if (!gob.IsRelevant) continue;
                    if (!gob.Movable) continue;
                    if (gob.NetworkUpdatePeriod == TimeSpan.Zero) continue;
                    if (gob.LastNetworkUpdate + gob.NetworkUpdatePeriod > now) continue;
                }
                gob.ForcedNetworkUpdate = false;
                gob.LastNetworkUpdate = now;
                gobMessage.AddGob(gob.ID, gob);
            }
            gobMessage.CollisionEvents = DataEngine.Arena.GetCollisionEvents();
            NetworkEngine.SendToGameClients(gobMessage);
        }

        private void SendPlayerUpdatesOnServer()
        {
            foreach (var player in DataEngine.Players.Where(p => p.MustUpdateToClients))
            {
                player.MustUpdateToClients = false;
                var plrMessage = new PlayerUpdateMessage();
                plrMessage.PlayerID = player.ID;
                plrMessage.Write(player, SerializationModeFlags.VaryingData);
                NetworkEngine.SendToGameClients(plrMessage);
            }
        }

        private void SendPlayerUpdatesOnClient()
        {
            foreach (var player in DataEngine.Players.Where(plr => !plr.IsRemote && plr.ID != Spectator.UNINITIALIZED_ID))
            {
                var message = new PlayerControlsMessage();
                message.PlayerID = player.ID;
                foreach (PlayerControlType controlType in Enum.GetValues(typeof(PlayerControlType)))
                    message.SetControlState(controlType, player.Controls[controlType].State);
                NetworkEngine.GameServerConnection.Send(message);
            }
        }

        private void SendGameSettings()
        {
            if (_lastGameSettingsSent.SecondsAgoRealTime() < 1) return;
            _lastGameSettingsSent = GameTime.TotalRealTime;
            switch (NetworkMode)
            {
                case NetworkMode.Client:
                    if (NetworkEngine.IsConnectedToGameServer)
                        SendPlayerSettingsToGameServer(p => !p.IsRemote && p.ServerRegistration != Spectator.ServerRegistrationType.Requested);
                    break;
                case NetworkMode.Server:
                    SendPlayerSettingsToGameClients(p => p.ID != Player.UNINITIALIZED_ID);
                    SendGameSettingsToRemote(NetworkEngine.GameClientConnections);
                    break;
            }
        }

        private void SendGameSettingsToRemote(IEnumerable<Connection> connections)
        {
            var mess = new GameSettingsRequest { ArenaToPlay = MenuEngine.Game.SelectedArenaName };
            foreach (var conn in connections) conn.Send(mess);
        }

        private void SendPlayerSettingsToGameServer(Func<Player, bool> sendCriteria)
        {
            Func<Player, PlayerSettingsRequest> newPlayerSettingsRequest = plr => new PlayerSettingsRequest
            {
                IsRegisteredToServer = plr.ServerRegistration == Spectator.ServerRegistrationType.Yes,
                IsGameClientPlayingArena = GameState == Core.GameState.Gameplay,
                IsGameClientReadyToStartArena = MenuEngine.IsReadyToStartArena,
                PlayerID = plr.ServerRegistration == Spectator.ServerRegistrationType.Yes ? plr.ID : plr.LocalID,
            };
            SendPlayerSettingsToRemote(sendCriteria, newPlayerSettingsRequest, new[] { NetworkEngine.GameServerConnection });
        }

        private void SendPlayerSettingsToGameClients(Func<Player, bool> sendCriteria)
        {
            Func<Player, PlayerSettingsRequest> newPlayerSettingsRequest = plr => new PlayerSettingsRequest
            {
                IsRegisteredToServer = true,
                PlayerID = plr.ID,
            };
            SendPlayerSettingsToRemote(sendCriteria, newPlayerSettingsRequest, NetworkEngine.GameClientConnections);
            foreach (var conn in NetworkEngine.GameClientConnections) conn.ConnectionStatus.HasPlayerSettings = true;
        }

        private void SendPlayerSettingsToRemote(Func<Player, bool> sendCriteria, Func<Player, PlayerSettingsRequest> newPlayerSettingsRequest, IEnumerable<Connection> connections)
        {
            foreach (var player in MenuEngine.Game.DataEngine.Players.Where(sendCriteria))
            {
                var mess = newPlayerSettingsRequest(player);
                mess.Write(player, SerializationModeFlags.ConstantData);
                if (player.ServerRegistration == Spectator.ServerRegistrationType.No)
                    player.ServerRegistration = Spectator.ServerRegistrationType.Requested;
                foreach (var conn in connections) conn.Send(mess);
            }
        }
    }
}
