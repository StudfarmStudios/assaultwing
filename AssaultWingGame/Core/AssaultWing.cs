using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Core.GameComponents;
using AW2.Core.OverlayDialogs;
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
    public class AssaultWing : AssaultWingCore
    {
        private GameState _gameState;
        private Control _escapeControl;
        private List<Gob> _addedGobs;

        // HACK: Debug keys
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
                if (GameStateChanged != null && _gameState != oldState)
                    GameStateChanged(_gameState);
            }
        }
        public bool IsClientAllowedToStartArena { get; set; }

        public event Action<GameState> GameStateChanged;
        public string SelectedArenaName { get; set; }
        public MenuEngineImpl MenuEngine { get; private set; }
        private StartupScreen StartupScreen { get; set; }
        private IntroEngine IntroEngine { get; set; }
        private OverlayDialog OverlayDialog { get; set; }
        private PlayerChat PlayerChat { get; set; }
        private UIEngineImpl UIEngine { get { return (UIEngineImpl)Components.First(c => c is UIEngineImpl); } }
        public NetworkEngine NetworkEngine { get; private set; }

        public AssaultWing(GraphicsDeviceService graphicsDeviceService)
            : base(graphicsDeviceService)
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
            Components.Add(IntroEngine);
            Components.Add(PlayerChat);
            Components.Add(OverlayDialog);
            GameState = GameState.Initializing;
            _escapeControl = new KeyboardKey(Keys.Escape);
            _frameStepControl = new KeyboardKey(Keys.F8);
            _frameRunControl = new KeyboardKey(Keys.F7);
            _frameStep = false;
            _addedGobs = new List<Gob>();
            DataEngine.NewArena += NewArenaHandler;
            DataEngine.SpectatorAdded += SpectatorAddedHandler;
            DataEngine.SpectatorRemoved += SpectatorRemovedHandler;
            NetworkEngine.Enabled = true;
        }

        public override void Update(AWGameTime gameTime)
        {
            base.Update(gameTime);
            UpdateSpecialKeys();
            UpdateDebugKeys();
            SynchronizeFrameNumber();
            SendGobCreationMessage();
        }

        public void ShowDialog(OverlayDialogData dialogData)
        {
            if (!AllowDialogs) return;
            OverlayDialog.Data.Enqueue(dialogData);
            if (!OverlayDialog.Enabled) SoundEngine.PlaySound("EscPause");
            OverlayDialog.Enabled = OverlayDialog.Visible = true;
        }

        public void HideDialog()
        {
            OverlayDialog.Data.Dequeue();
            if (OverlayDialog.Data.Any())
                SoundEngine.PlaySound("EscPause");
            else
                OverlayDialog.Enabled = OverlayDialog.Visible = false;
        }

        /// <summary>
        /// Displays the main menu and stops any ongoing gameplay.
        /// </summary>
        public void ShowMenu()
        {
            CutNetworkConnections();
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

            SelectedArenaName = DataEngine.ArenaInfos.First().Name;

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
            UIEngine.MouseControlsEnabled = false;
#endif

            var player1 = new Player(this, "Newbie", (CanonicalString)"Windlord", (CanonicalString)"rockets", (CanonicalString)"reverse thruster", plr1Controls);
            var player2 = new Player(this, "Lamer", (CanonicalString)"Bugger", (CanonicalString)"bazooka", (CanonicalString)"reverse thruster", plr2Controls);
            DataEngine.Spectators.Add(player1);
            DataEngine.Spectators.Add(player2);

            DataEngine.GameplayMode = new GameplayMode();
            DataEngine.GameplayMode.ShipTypes = new[] { "Windlord", "Bugger", "Plissken" };
            DataEngine.GameplayMode.ExtraDeviceTypes = new[] { "reverse thruster", "blink" };
            DataEngine.GameplayMode.Weapon2Types = new[] { "bazooka", "rockets", "mines" };

            GameState = GameState.Intro;
            base.BeginRun();
        }

        public void StartArenaButStayInMenu()
        {
            if (NetworkMode != Core.NetworkMode.Client) throw new InvalidOperationException("Only client can start arena on background");
            base.StartArena();
            GameState = GameState.GameAndMenu;
        }

        public override void StartArena()
        {
            Log.Write("Saving settings to file");
            Settings.ToFile();
            if (NetworkMode == NetworkMode.Server)
                MessageHandlers.ActivateHandlers(MessageHandlers.GetServerGameplayHandlers());
            if (GameState != Core.GameState.GameAndMenu) base.StartArena();
            PostFrameLogicEngine.DoEveryFrame += AfterEveryFrame;
            GameState = GameState.Gameplay;
        }

        public override void FinishArena()
        {
            base.FinishArena();
            ShowDialog(new GameOverOverlayDialogData(this));
        }

        /// <summary>
        /// Turns this game instance into a game server to whom other game instances
        /// can connect as game clients.
        /// </summary>
        /// <param name="connectionHandler">Handler of connection result.</param>
        /// <returns>True on success, false on failure</returns>
        public bool StartServer(Action<Result<Connection>> connectionHandler)
        {
            if (NetworkMode != NetworkMode.Standalone)
                throw new InvalidOperationException("Cannot start server while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Server;
            try
            {
                NetworkEngine.StartServer(connectionHandler);
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
            NetworkMode = NetworkMode.Standalone;
            NetworkEngine.StopClient();
            DataEngine.RemoveRemoteSpectators();
            GameState = GameState.GameplayStopped; // gameplay cannot continue because it's initialized only for a client
            if (errorOrNull != null)
            {
                var dialogData = new CustomOverlayDialogData(this,
                    errorOrNull + "\nPress Enter to return to Main Menu",
                    new TriggeredCallback(TriggeredCallback.GetProceedControl(), ShowMenu));
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
                    IntroEngine.BeginIntro();
                    break;
                case GameState.Gameplay:
                    LogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    PreFrameLogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    PostFrameLogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    GraphicsEngine.Visible = true;
                    if (NetworkMode != NetworkMode.Standalone) PlayerChat.Enabled = PlayerChat.Visible = true;
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
        /// Resumes playing the current arena, closing the dialog if it's visible.
        /// </summary>
        private void ResumePlay()
        {
            GameState = GameState.Gameplay;
        }

        private void UpdateSpecialKeys()
        {
            if (GameState == GameState.Gameplay && _escapeControl.Pulse)
            {
                var dialogData = NetworkMode == NetworkMode.Server
                    ? new CustomOverlayDialogData(this,
                        "Finish Arena? (Yes/No)",
                        new TriggeredCallback(TriggeredCallback.GetYesControl(), FinishArena),
                        new TriggeredCallback(TriggeredCallback.GetNoControl(), ResumePlay))
                    : new CustomOverlayDialogData(this,
                        "Quit to Main Menu? (Yes/No)",
                        new TriggeredCallback(TriggeredCallback.GetYesControl(), ShowMenu),
                        new TriggeredCallback(TriggeredCallback.GetNoControl(), ResumePlay));

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
                    foreach (var ship in ships) ship.Die(new DeathCause(ship, DeathCauseType.Unspecified));
                }

                if (keys.IsKeyDown(Keys.E) && keys.IsKeyDown(Keys.A))
                {
                    // E + A = end arena
                    if (!DataEngine.ProgressBar.TaskRunning)
                    {
                        FinishArena();
                        HideDialog();
                    }
                }
            }
        }

        private void SynchronizeFrameNumber()
        {
            if (NetworkMode != NetworkMode.Client) return;
            if (GameState != GameState.Gameplay && GameState != GameState.GameAndMenu) return;
            DataEngine.Arena.FrameNumber -= NetworkEngine.GameServerConnection.PingInfo.RemoteFrameNumberOffset;
        }

        private void SendGobCreationMessage()
        {
            if (DataEngine.ProgressBar.TaskRunning) return; // wait for arena load completion
            if (NetworkMode == NetworkMode.Server && _addedGobs.Any())
            {
                var message = new GobCreationMessage();
                foreach (var gob in _addedGobs) message.AddGob(gob);
                _addedGobs.Clear();
                NetworkEngine.SendToGameClients(message);
            }
        }

        public void HandleGobCreationMessage(GobCreationMessage message, int framesAgo)
        {
            message.ReadGobs(framesAgo,
                (typeName, layerIndex) =>
                {
                    var gob = (Gob)Clonable.Instantiate(typeName);
                    gob.Game = this;
                    gob.Layer = DataEngine.Arena.Layers[layerIndex];
                    return gob;
                },
                DataEngine.Arena.Gobs.Add);
        }

        private void DeactivateAllMessageHandlers()
        {
            NetworkEngine.MessageHandlers.Clear();
        }

        private void NewArenaHandler(Arena arena)
        {
            arena.GobAdded += gob =>
            {
                if (NetworkMode == NetworkMode.Server && gob.IsRelevant) _addedGobs.Add(gob);
            };
            arena.GobRemoved += gob => GobRemovedFromArenaHandler(arena, gob);
        }

        private void GobRemovedFromArenaHandler(Arena arena, Gob gob)
        {
            if (NetworkMode != NetworkMode.Server || !gob.IsRelevant) return;
            if (!arena.IsActive) throw new ApplicationException("Removing a gob from an inactive arena during network game");
            var message = new GobDeletionMessage();
            message.GobId = gob.ID;
            NetworkEngine.SendToGameClients(message);
        }

        private void SpectatorAddedHandler(Spectator spectator)
        {
            var player = spectator as Player;
            if (NetworkMode == NetworkMode.Server)
            {
                UpdateGameServerInfoToManagementServer();
                if (player != null) player.NewMessage += message =>
                {
                    if (player.IsRemote)
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

        private void UpdateGameServerInfoToManagementServer()
        {
            var managementMessage = new UpdateGameServerMessage { CurrentClients = DataEngine.Players.Count() };
            NetworkEngine.ManagementServerConnection.Send(managementMessage);
        }

        private void AfterEveryFrame()
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
                gob.LastNetworkUpdate = now;
                gobMessage.AddGob(gob.ID, gob, AW2.Helpers.Serialization.SerializationModeFlags.VaryingData);
            }
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
            foreach (var player in DataEngine.Players.Where(plr => !plr.IsRemote))
            {
                var message = new PlayerControlsMessage();
                message.PlayerID = player.ID;
                foreach (PlayerControlType controlType in Enum.GetValues(typeof(PlayerControlType)))
                    message.SetControlState(controlType, player.Controls[controlType].State);
                NetworkEngine.GameServerConnection.Send(message);
            }
        }

        public void SendPlayerSettingsToGameServer(Func<Player, bool> sendCriteria)
        {
            Func<Player, PlayerSettingsRequest> newPlayerSettingsRequest = plr => new PlayerSettingsRequest
            {
                IsRegisteredToServer = plr.ServerRegistration == Spectator.ServerRegistrationType.Yes,
                PlayerID = plr.ServerRegistration == Spectator.ServerRegistrationType.Yes ? plr.ID : plr.LocalID,
            };
            SendPlayerSettingsToRemote(sendCriteria, newPlayerSettingsRequest, new[] { NetworkEngine.GameServerConnection });
        }

        public void SendPlayerSettingsToGameClients(Func<Player, bool> sendCriteria)
        {
            Func<Player, PlayerSettingsRequest> newPlayerSettingsRequest = plr => new PlayerSettingsRequest
            {
                IsRegisteredToServer = true,
                PlayerID = plr.ID,
            };
            SendPlayerSettingsToRemote(sendCriteria, newPlayerSettingsRequest, NetworkEngine.GameClientConnections);
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
