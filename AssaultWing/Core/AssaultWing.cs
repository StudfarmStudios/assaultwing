using System;
using System.Collections.Generic;
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
using AW2.Graphics;

namespace AW2.Core
{
    [System.Diagnostics.DebuggerDisplay("AssaultWing {NetworkMode} {GameState}")]
    public class AssaultWing : AssaultWingCore
    {
        private GameState _gameState;
        private Control _escapeControl, _screenShotControl;
        private TimeSpan _lastGameSettingsSent;
        private TimeSpan _lastFrameNumberSynchronization;
        private byte _nextArenaID;
        private bool _clearGameDataWhenEnteringMenus;

        // Debug keys, used only #if DEBUG
        private Control _frameStepControl;
        private Control _frameRunControl;
        private bool _frameStep;

        /// <summary>
        /// The AssaultWing instance. Avoid using this remnant of the old times.
        /// </summary>
        public static new AssaultWing Instance { get { return (AssaultWing)AssaultWingCore.Instance; } }

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
        public bool IsLoadingArena { get { return !CommandLineOptions.DedicatedServer && MenuEngine.ArenaLoadTask.TaskRunning; } }
        public Control ChatStartControl { get; set; }

        public event Action<GameState> GameStateChanged;
        public string SelectedArenaName { get; set; }
        public MenuEngineImpl MenuEngine { get; private set; }
        private StartupScreen StartupScreen { get; set; }
        private IntroEngine IntroEngine { get; set; }
        private OverlayDialog OverlayDialog { get; set; }
        private PlayerChat PlayerChat { get; set; }
        public UIEngineImpl UIEngine { get { return (UIEngineImpl)Components.First(c => c is UIEngineImpl); } }
        public NetworkEngine NetworkEngine { get; private set; }
        public MessageHandlers MessageHandlers { get; private set; }
        public WebData WebData { get; private set; }

        public AssaultWing(GraphicsDeviceService graphicsDeviceService, CommandLineOptions args)
            : base(graphicsDeviceService, args)
        {
            MessageHandlers = new Net.MessageHandling.MessageHandlers(this);
            StartupScreen = new StartupScreen(this, -1);
            NetworkEngine = new NetworkEngine(this, 0);
            WebData = new WebData(this, 21);
            Components.Add(StartupScreen);
            Components.Add(NetworkEngine);
            Components.Add(WebData);
            GameState = GameState.Initializing;
            ChatStartControl = Settings.Controls.Chat.GetControl();
            _escapeControl = new MultiControl
            {
                new KeyboardKey(Keys.Escape),
                new GamePadButton(0, GamePadButtonType.Start),
                new GamePadButton(0, GamePadButtonType.Back),
            };
            _screenShotControl = new KeyboardKey(Keys.PrintScreen);
            _frameStepControl = new KeyboardKey(Keys.F8);
            _frameRunControl = new KeyboardKey(Keys.F7);
            _frameStep = false;
            DataEngine.SpectatorAdded += SpectatorAddedHandler;
            DataEngine.SpectatorRemoved += SpectatorRemovedHandler;
            NetworkEngine.Enabled = true;
            if (CommandLineOptions.DedicatedServer)
            {
                var dedicatedServer = new DedicatedServer(this, 13);
                Components.Add(dedicatedServer);
                dedicatedServer.Enabled = true;
            }
            else
            {
                MenuEngine = new MenuEngineImpl(this, 10);
                IntroEngine = new IntroEngine(this, 11);
                PlayerChat = new PlayerChat(this, 12);
                OverlayDialog = new OverlayDialog(this, 20);
                Components.Add(MenuEngine);
                Components.Add(IntroEngine);
                Components.Add(PlayerChat);
                Components.Add(OverlayDialog);
            }
            AW2.Graphics.PlayerViewport.CustomOverlayCreators.Add(viewport => new SystemStatusOverlay(viewport));

            // Replace the dummy StatsBase by a proper StatsSender.
            Components.Remove(comp => comp is StatsBase);
            Stats = new StatsSender(this, 7);
            Components.Add(Stats);
            Stats.Enabled = true;
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

        /// <summary>
        /// Opens a URL (usually in the default web browser). Exceptions are caught and the user is notified.
        /// </summary>
        public void OpenURL(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception)
            {
                ShowInfoDialog("Couldn't open browser.\nPlease open this URL manually:\n" + url);
            }
        }

        public void ShowDialog(OverlayDialogData dialogData)
        {
            if (!AllowDialogs) return;
            OverlayDialog.Show(dialogData);
        }

        /// <summary>
        /// Like calling <see cref="ShowDialog"/> with <see cref="TriggeredCallback.PROCEED_CONTROL"/> that
        /// doesn't do anything.
        /// </summary>
        public void ShowInfoDialog(string text, string groupName = null)
        {
            ShowDialog(new CustomOverlayDialogData(this, text,
                new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, () => { })) { GroupName = groupName });
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
            MenuEngine.Activate(MenuComponentType.Main);
            GameState = GameState.Menu;
        }

        public void ShowEquipMenu()
        {
            if (_clearGameDataWhenEnteringMenus)
            {
                _clearGameDataWhenEnteringMenus = false;
                DataEngine.ClearGameState();
            }
            MenuEngine.Activate(MenuComponentType.Equip);
            GameState = GameState.Menu;
        }

        /// <summary>
        /// Called after all components are initialized but before the first update in the game loop. 
        /// </summary>
        public override void BeginRun()
        {
            Log.Write("Assault Wing begins to run");
            var arenas = DataEngine.GetTypeTemplates<Arena>();
            if (!arenas.Any()) throw new ApplicationException("No arenas found");
            SelectedArenaName = arenas.First().Info.Name;
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
            if (MenuEngine != null) MenuEngine.ProgressBar.SubtaskCompleted();
        }

        public override void StartArena()
        {
            Stats.BasicInfoSent = false;
            if (NetworkMode == NetworkMode.Server)
            {
                MessageHandlers.ActivateHandlers(MessageHandlers.GetServerGameplayHandlers());
                DataEngine.Arena.GobRemoved += GobRemovedFromArenaHandler;
            }
            if (GameState != GameState.GameAndMenu)
            {
                base.StartArena();
                PostFrameLogicEngine.DoEveryFrame += AfterEveryFrame;
            }
            GameState = GameState.Gameplay;
        }

        public void InitializePlayers(int count)
        {
            Settings.Players.Validate(this);
            var players = new[]
            {
                new Player(this, Settings.Players.Player1.Name,
                    (CanonicalString)Settings.Players.Player1.ShipName,
                    (CanonicalString)Settings.Players.Player1.Weapon2Name,
                    (CanonicalString)Settings.Players.Player1.ExtraDeviceName,
                    PlayerControls.FromSettings(Settings.Controls.Player1)),
                new Player(this, Settings.Players.Player2.Name,
                    (CanonicalString)Settings.Players.Player2.ShipName,
                    (CanonicalString)Settings.Players.Player2.Weapon2Name,
                    (CanonicalString)Settings.Players.Player2.ExtraDeviceName,
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
            if (Settings.Players.BotsEnabled) DataEngine.Spectators.Add(new BotPlayer(this));
            WebData.LoginPilots();
            try
            {
                // TODO: Allow rejoin even if there are no free slots.
                NetworkEngine.StartServer(result => MessageHandlers.IncomingConnectionHandlerOnServer(result,
                    allowNewConnection: () => DataEngine.Players.Count() < Settings.Net.GameServerMaxPlayers));
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
            DataEngine.RemoveAllButLocalSpectators();
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
            DataEngine.RemoveAllButLocalSpectators();
            StopGameplay(); // gameplay cannot continue because it's initialized only for a client
            NetworkMode = NetworkMode.Standalone;
            if (errorOrNull != null)
            {
                var dialogData = new CustomOverlayDialogData(this,
                    errorOrNull + "\nPress Enter to return to Main Menu.",
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
            var textColor = from.Color;
            var message = new PlayerMessage(preText, messageContent, textColor);
            switch (NetworkMode)
            {
                case NetworkMode.Server:
                    foreach (var plr in DataEngine.Players) plr.Messages.Add(message);
                    break;
                case NetworkMode.Client:
                    foreach (var plr in DataEngine.Players.Where(plr => plr.IsLocal)) plr.Messages.Add(message);
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
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetClientGameplayHandlers());
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers());
            EnsureArenaLoadingStopped();
            var standings = DataEngine.GameplayMode.GetStandings(DataEngine.Spectators).ToArray(); // ToArray takes a copy
            Stats.Send(new { ArenaFinished = standings.Select(st => new { st.Name, st.LoginToken, st.Score, st.Kills, st.Deaths }).ToArray() });
            if (CommandLineOptions.DedicatedServer)
            {
                DataEngine.ClearGameState();
                GameState = GameState.Initializing;
            }
            else
            {
                StopGameplay();
                _clearGameDataWhenEnteringMenus = true;
                ShowDialog(new GameOverOverlayDialogData(this, standings) { GroupName = "Game over" });
            }
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            EnsureArenaLoadingStopped();
            base.OnExiting(sender, args);
        }

        private void EnsureArenaLoadingStopped()
        {
            if (CommandLineOptions.DedicatedServer) return;
            if (IsLoadingArena) MenuEngine.ArenaLoadTask.AbortTask();
            MenuEngine.ProgressBar.SkipRemainingSubtasks();
        }

        private void ApplyInGameGraphicsSettings()
        {
            if (Window == null || CommandLineOptions.DedicatedServer) return;
            if (Settings.Graphics.IsVerticalSynced)
                Window.Impl.EnableVerticalSync();
            else
                Window.Impl.DisableVerticalSync();
            if (Settings.Graphics.InGameFullscreen)
                Window.Impl.SetFullScreen(Settings.Graphics.FullscreenWidth, Settings.Graphics.FullscreenHeight);
            else
                Window.Impl.SetWindowed();
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
                        GraphicsEngine.Enabled = true;
                        GraphicsEngine.Visible = true;
                        if (NetworkMode != NetworkMode.Standalone) PlayerChat.Enabled = PlayerChat.Visible = true;
                        SoundEngine.PlayMusic(DataEngine.Arena.BackgroundMusic.FileName, DataEngine.Arena.BackgroundMusic.Volume);
                    }
                    break;
                case GameState.GameplayStopped:
                    GraphicsEngine.Enabled = true;
                    GraphicsEngine.Visible = true;
                    if (NetworkMode != NetworkMode.Standalone) PlayerChat.Visible = true;
                    break;
                case GameState.GameAndMenu:
                    LogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    PreFrameLogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    PostFrameLogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    MenuEngine.Enabled = true;
                    MenuEngine.Visible = true;
                    SoundEngine.PlayMusic(DataEngine.Arena.BackgroundMusic.FileName, DataEngine.Arena.BackgroundMusic.Volume);
                    break;
                case GameState.Menu:
                    MenuEngine.Enabled = true;
                    MenuEngine.Visible = true;
                    SoundEngine.PlayMusic("menu music", 1);
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
                    if (!CommandLineOptions.DedicatedServer)
                    {
                        GraphicsEngine.Enabled = false;
                        GraphicsEngine.Visible = false;
                        PlayerChat.Enabled = PlayerChat.Visible = false;
                    }
                    break;
                case GameState.GameplayStopped:
                    if (!CommandLineOptions.DedicatedServer)
                    {
                        GraphicsEngine.Enabled = false;
                        GraphicsEngine.Visible = false;
                        PlayerChat.Visible = false;
                    }
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
            if (NetworkMode != NetworkMode.Client && !CommandLineOptions.DedicatedServer)
                MenuEngine.ProgressBar.Start(arena.Gobs.OfType<AW2.Game.Gobs.Wall>().Count());
            foreach (var conn in NetworkEngine.GameClientConnections) conn.PingInfo.AllowLatePingsForAWhile();
            DataEngine.Arena = arena;
            arena.Reset(); // this usually takes several seconds
        }

        private void StopGameplay()
        {
            switch (GameState)
            {
                case GameState.Gameplay: GameState = GameState.GameplayStopped; break;
                case GameState.GameAndMenu: GameState = GameState.Menu; break;
            }
        }

        private void ShowEquipMenuWhileKeepingGameRunning()
        {
            if (GameState == GameState.Menu) return;
            MenuEngine.Activate(MenuComponentType.Equip);
            GameState = GameState.GameAndMenu;
        }

        private void UpdateSpecialKeys()
        {
            if (OverlayDialog != null && GameState == GameState.Gameplay && _escapeControl.Pulse && !OverlayDialog.Enabled)
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
            if (_screenShotControl.Pulse) TakeScreenShot();
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
            if (OverlayDialog != null && OverlayDialog.Enabled && (GameState == GameState.Gameplay || GameState == GameState.GameplayStopped))
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
                    if (!IsLoadingArena) FinishArena();
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
            if (NetworkMode != NetworkMode.Server) return;
            if (IsLoadingArena) return; // wait for arena load completion
            if (DataEngine.Arena == null) return; // happens if gobs are created on the frame the arena ends
            foreach (var conn in NetworkEngine.GameClientConnections)
            {
                var gobsToSend = DataEngine.Arena.Gobs.GameplayLayer.Gobs.Where(gob => gob.IsRelevant && !gob.ClientStatus[1 << conn.ID]);
                if (!gobsToSend.Any()) continue;
                var message = new GobCreationMessage { ArenaID = DataEngine.Arena.ID };
                foreach (var gob in gobsToSend.Take(10)) // Avoid sending lots of gobs in one frame.
                {
                    message.AddGob(gob);
                    gob.ClientStatus[1 << conn.ID] = true;
                }
                conn.Send(message);
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
            spectator.ResetForArena();
            if (NetworkMode != NetworkMode.Server || spectator.IsLocal) return;
            var player = spectator as Player;
            if (player == null) return;
            player.IsAllowedToCreateShip = () => player.IsRemote && NetworkEngine.GetGameClientConnection(player.ConnectionID).ConnectionStatus.IsPlayingArena;
            player.Messages.NewChatMessage += mess => SendPlayerMessageToRemoteSpectator(mess, player);
            player.Messages.NewCombatLogMessage += mess => SendPlayerMessageToRemoteSpectator(mess, player);
        }

        private void SendPlayerMessageToRemoteSpectator(PlayerMessage message, Player player)
        {
            if (!player.IsRemote) return;
            try
            {
                var messageMessage = new PlayerMessageMessage { PlayerID = player.ID, Message = message };
                NetworkEngine.GetGameClientConnection(player.ConnectionID).Send(messageMessage);
            }
            catch (InvalidOperationException)
            {
                // The connection of the player doesn't exist any more. Just don't send the message then.
            }
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
                        SendGobUpdatesToRemote(DataEngine.Arena.Gobs.GameplayLayer.Gobs,
                            SerializationModeFlags.VaryingDataFromServer, NetworkEngine.GameClientConnections);
                        SendPlayerUpdatesOnServer();
                        break;
                    case NetworkMode.Client:
                        SendGobUpdatesToRemote(DataEngine.Minions.Where(gob => gob.Owner != null && gob.Owner.IsLocal),
                            SerializationModeFlags.VaryingDataFromClient, new[] { NetworkEngine.GameServerConnection });
                        SendPlayerUpdatesOnClient();
                        break;
                }
            }
        }

        private void SendPlayerUpdatesOnServer()
        {
            foreach (var player in DataEngine.Spectators.Where(p => p.MustUpdateToClients))
            {
                player.MustUpdateToClients = false;
                var plrMessage = new PlayerUpdateMessage();
                plrMessage.PlayerID = player.ID;
                plrMessage.Write(player, SerializationModeFlags.VaryingDataFromServer);
                NetworkEngine.SendToGameClients(plrMessage);
            }
        }

        private void SendPlayerUpdatesOnClient()
        {
            if (GameState != GameState.Gameplay) return;
            foreach (var player in DataEngine.Players.Where(plr => plr.IsLocal && plr.ID != Spectator.UNINITIALIZED_ID))
            {
                var message = new PlayerControlsMessage();
                message.PlayerID = player.ID;
                foreach (PlayerControlType controlType in Enum.GetValues(typeof(PlayerControlType)))
                    message.SetControlState(controlType, player.Controls[controlType].State);
                NetworkEngine.GameServerConnection.Send(message);
            }
        }

        private void SendGobUpdatesToRemote(IEnumerable<Gob> gobs, SerializationModeFlags serializationMode, IEnumerable<Connection> connections)
        {
            var now = DataEngine.ArenaTotalTime;
            var gobMessage = new GobUpdateMessage();
            foreach (var gob in gobs)
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
                gobMessage.AddGob(gob.ID, gob, serializationMode);
            }
            gobMessage.CollisionEvents = DataEngine.Arena.GetCollisionEvents();
            foreach (var conn in connections) conn.Send(gobMessage);
        }

        private void SendGameSettings()
        {
            if (_lastGameSettingsSent.SecondsAgoRealTime() < 1) return;
            _lastGameSettingsSent = GameTime.TotalRealTime;
            switch (NetworkMode)
            {
                case NetworkMode.Client:
                    if (NetworkEngine.IsConnectedToGameServer)
                        SendSpectatorSettingsToGameServer(p => p.IsLocal && p.ServerRegistration != Spectator.ServerRegistrationType.Requested);
                    break;
                case NetworkMode.Server:
                    SendSpectatorSettingsToGameClients(p => !p.IsDisconnected && p.ID != Spectator.UNINITIALIZED_ID);
                    SendGameSettingsToRemote(NetworkEngine.GameClientConnections);
                    break;
            }
        }

        private void SendGameSettingsToRemote(IEnumerable<Connection> connections)
        {
            var mess = new GameSettingsRequest { ArenaToPlay = SelectedArenaName };
            foreach (var conn in connections) conn.Send(mess);
        }

        private void SendSpectatorSettingsToGameServer(Func<Spectator, bool> sendCriteria)
        {
            Func<Spectator, SpectatorSettingsRequest> newPlayerSettingsRequest = spec => new SpectatorSettingsRequest
            {
                IsRegisteredToServer = spec.ServerRegistration == Spectator.ServerRegistrationType.Yes,
                IsGameClientPlayingArena = GameState == Core.GameState.Gameplay,
                IsGameClientReadyToStartArena = MenuEngine.IsReadyToStartArena,
                SpectatorID = spec.ServerRegistration == Spectator.ServerRegistrationType.Yes ? spec.ID : spec.LocalID,
                Subclass = SpectatorSettingsRequest.GetSubclassType(spec),
            };
            SendSpectatorSettingsToRemote(SerializationModeFlags.ConstantDataFromClient, sendCriteria, newPlayerSettingsRequest, new[] { NetworkEngine.GameServerConnection });
        }

        private void SendSpectatorSettingsToGameClients(Func<Spectator, bool> sendCriteria)
        {
            Func<Spectator, SpectatorSettingsRequest> newPlayerSettingsRequest = spec => new SpectatorSettingsRequest
            {
                IsRegisteredToServer = true,
                SpectatorID = spec.ID,
                Subclass = SpectatorSettingsRequest.GetSubclassType(spec),
            };
            SendSpectatorSettingsToRemote(SerializationModeFlags.ConstantDataFromServer, sendCriteria, newPlayerSettingsRequest, NetworkEngine.GameClientConnections);
            foreach (var conn in NetworkEngine.GameClientConnections) conn.ConnectionStatus.HasPlayerSettings = true;
        }

        private void SendSpectatorSettingsToRemote(SerializationModeFlags mode, Func<Spectator, bool> sendCriteria, Func<Spectator, SpectatorSettingsRequest> newSpectatorSettingsRequest, IEnumerable<Connection> connections)
        {
            foreach (var spectator in DataEngine.Spectators.Where(sendCriteria))
            {
                var mess = newSpectatorSettingsRequest(spectator);
                mess.Write(spectator, mode);
                if (spectator.ServerRegistration == Spectator.ServerRegistrationType.No)
                    spectator.ServerRegistration = Spectator.ServerRegistrationType.Requested;
                foreach (var conn in connections) conn.Send(mess);
            }
        }
    }
}
