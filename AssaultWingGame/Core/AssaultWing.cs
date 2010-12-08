using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Core.GameComponents;
using AW2.Core.OverlayDialogs;
using AW2.Game;
using AW2.Graphics;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
using AW2.Menu;
using AW2.Net;
using AW2.Net.Connections;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;
using AW2.UI;

namespace AW2.Core
{
    public class AssaultWing : AssaultWingCore
    {
        private static readonly TimeSpan FRAME_NUMBER_SYNCHRONIZATION_INTERVAL = TimeSpan.FromSeconds(1);

        private GameState _gameState;
        private Control _escapeControl;
        private TimeSpan _nextFrameNumberSynchronize;
        private List<Gob> _addedGobs;

        // HACK: Debug keys
        private Control _frameStepControl;
        private Control _frameRunControl;
        private bool _frameStep;

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
        public bool ClientAllowedToStartArena { get; set; }

        public event Action<GameState> GameStateChanged;
        public string SelectedArenaName { get; set; }
        public MenuEngineImpl MenuEngine { get; private set; }
        private StartupScreen StartupScreen { get; set; }
        private IntroEngine IntroEngine { get; set; }
        private LogicEngine LogicEngine { get { return (LogicEngine)Components.First(c => c is LogicEngine); } }
        private OverlayDialog OverlayDialog { get; set; }
        private PlayerChat PlayerChat { get; set; }
        private UIEngineImpl UIEngine { get { return (UIEngineImpl)Components.First(c => c is UIEngineImpl); } }

        public AssaultWing(GraphicsDeviceService graphicsDeviceService)
            : base(graphicsDeviceService)
        {
            StartupScreen = new StartupScreen(this, -1);
            OverlayDialog = new OverlayDialog(this, 5);
            MenuEngine = new MenuEngineImpl(this, 6);
            IntroEngine = new IntroEngine(this, 7);
            PlayerChat = new PlayerChat(this, 8);
            Components.Add(StartupScreen);
            Components.Add(OverlayDialog);
            Components.Add(MenuEngine);
            Components.Add(IntroEngine);
            Components.Add(PlayerChat);
            GameState = GameState.Initializing;
            _escapeControl = new KeyboardKey(Keys.Escape);
            _frameStepControl = new KeyboardKey(Keys.F8);
            _frameRunControl = new KeyboardKey(Keys.F7);
            _frameStep = false;
            _addedGobs = new List<Gob>();
            DataEngine.NewArena += arena =>
            {
                arena.GobAdded += gob => { if (NetworkMode == NetworkMode.Server && gob.IsRelevant) _addedGobs.Add(gob); };
                arena.GobRemoved += gob => GobRemovedFromArena(arena, gob);
            };
        }

        public override void Update(AWGameTime gameTime)
        {
            base.Update(gameTime);
            UpdateSpecialKeys();
            UpdateDebugKeys();
            SynchronizeFrameNumber();
            SendGobCreationMessage();
        }

        /// <summary>
        /// Displays the dialog on top of the game and stops updating the game logic.
        /// </summary>
        /// <param name="dialogData">The contents and actions for the dialog.</param>
        public void ShowDialog(OverlayDialogData dialogData)
        {
            if (!AllowDialogs) return;
            OverlayDialog.Data = dialogData;
            GameState = GameState.OverlayDialog;
            SoundEngine.PlaySound("EscPause");
        }

        /// <summary>
        /// Displays the main menu and stops any ongoing gameplay.
        /// </summary>
        public void ShowMenu()
        {
            Log.Write("Entering menus");
            DeactivateAllMessageHandlers();
            if (NetworkMode == NetworkMode.Server) MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers());
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
            DataEngine.GameplayMode.ShipTypes = new string[] { "Windlord", "Bugger", "Plissken" };
            DataEngine.GameplayMode.ExtraDeviceTypes = new string[] { "reverse thruster", "blink" };
            DataEngine.GameplayMode.Weapon2Types = new string[] { "bazooka", "rockets", "mines" };

            GameState = GameState.Intro;
            base.BeginRun();
        }

        public void PrepareArena()
        {
            if (NetworkMode == NetworkMode.Server)
            {
                // Arena loading is heavy and would show up in ping measurements.
                // Ping measurement is unfreezed by StartArenaOnServer().
                // TODO: Is this helpful at all? Note that also client's arena loading affects ping.
                foreach (var conn in NetworkEngine.GameClientConnections) conn.PingInfo.IsMeasuringFreezed = true;
                var message = new StartGameMessage { ArenaToPlay = SelectedArenaName };
                NetworkEngine.SendToGameClients(message);
            }
            // base.PrepareFirstArena adds gobs to the arena which triggers AssaultWing.GobAddedToArena
            base.PrepareArena(SelectedArenaName);
        }

        public override void StartArena()
        {
            if (NetworkMode == NetworkMode.Server)
            {
                foreach (var conn in NetworkEngine.GameClientConnections) conn.PingInfo.IsMeasuringFreezed = false;
                MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerMenuHandlers());
                MessageHandlers.ActivateHandlers(MessageHandlers.GetServerGameplayHandlers());
            }
            base.StartArena();
            GameState = GameState.Gameplay;
        }

        public override void FinishArena()
        {
            base.FinishArena();
            DeactivateAllMessageHandlers();
            ShowDialog(new GameOverOverlayDialogData(this));
            if (NetworkMode == NetworkMode.Server)
            {
                var message = new ArenaFinishMessage();
                NetworkEngine.SendToGameClients(message);
            }
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
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers());
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
            ClientAllowedToStartArena = false;
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

        public override void StopClient(string errorOrNull)
        {
            base.StopClient(errorOrNull);
            if (NetworkMode != NetworkMode.Client)
                throw new InvalidOperationException("Cannot stop client while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Standalone;
            NetworkEngine.StopClient();
            DataEngine.RemoveRemoteSpectators();
            if (errorOrNull != null)
            {
                var dialogData = new CustomOverlayDialogData(
                    errorOrNull + "\nPress Enter to return to Main Menu",
                    new TriggeredCallback(TriggeredCallback.GetProceedControl(), ShowMenu));
                ShowDialog(dialogData);
            }
        }

        public override void CutNetworkConnections()
        {
            switch (NetworkMode)
            {
                case NetworkMode.Client: StopClient(null); break;
                case NetworkMode.Server: StopServer(); break;
                case NetworkMode.Standalone: break;
                default: throw new ApplicationException("Unexpected NetworkMode: " + NetworkMode);
            }
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
                    Log.Write("Saving settings to file");
                    Settings.ToFile();
                    LogicEngine.Enabled = DataEngine.Arena.IsForPlaying;
                    GraphicsEngine.Visible = true;
                    if (NetworkMode != NetworkMode.Standalone) PlayerChat.Enabled = PlayerChat.Visible = true;
                    break;
                case GameState.Menu:
                    MenuEngine.Enabled = true;
                    MenuEngine.Visible = true;
                    break;
                case GameState.OverlayDialog:
                    OverlayDialog.Enabled = true;
                    OverlayDialog.Visible = true;
                    GraphicsEngine.Visible = true;
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
                    GraphicsEngine.Visible = false;
                    PlayerChat.Enabled = PlayerChat.Visible = false;
                    break;
                case GameState.Menu:
                    MenuEngine.Enabled = false;
                    MenuEngine.Visible = false;
                    break;
                case GameState.OverlayDialog:
                    OverlayDialog.Enabled = false;
                    OverlayDialog.Visible = false;
                    GraphicsEngine.Visible = false;
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
                    ? new CustomOverlayDialogData(
                        "Finish Arena? (Yes/No)",
                        new TriggeredCallback(TriggeredCallback.GetYesControl(), FinishArena),
                        new TriggeredCallback(TriggeredCallback.GetNoControl(), ResumePlay))
                    : new CustomOverlayDialogData(
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
                LogicEngine.Enabled = true;
                _frameStep = false;
            }
            if (_frameStep)
            {
                if (_frameStepControl.Pulse)
                    LogicEngine.Enabled = true;
                else
                    LogicEngine.Enabled = false;
            }
            else if (_frameStepControl.Pulse)
            {
                LogicEngine.Enabled = false;
                _frameStep = true;
            }

            // Cheat codes during dialog.
            if (GameState == GameState.OverlayDialog)
            {
                var keys = Keyboard.GetState();
                if (keys.IsKeyDown(Keys.K) && keys.IsKeyDown(Keys.P))
                {
                    // K + P = kill players
                    var ships = DataEngine.Players.Select(p => p.Ship).Where(s => s != null);
                    foreach (var ship in ships) ship.Die(new DeathCause());
                }

                if (keys.IsKeyDown(Keys.E) && keys.IsKeyDown(Keys.A))
                {
                    // E + A = end arena
                    if (!DataEngine.ProgressBar.TaskRunning)
                        FinishArena();
                }
            }
        }

        private void SynchronizeFrameNumber()
        {
            if (NetworkMode != NetworkMode.Client || GameState != GameState.Gameplay) return;
            if (GameTime.TotalRealTime < _nextFrameNumberSynchronize) return;
            _nextFrameNumberSynchronize = GameTime.TotalRealTime + FRAME_NUMBER_SYNCHRONIZATION_INTERVAL;
            var MINIMUM_ACCEPTABLE_FRAME_NUMBER_OFFSET = 1;
            if (Math.Abs(NetworkEngine.GameServerConnection.PingInfo.RemoteFrameNumberOffset) > MINIMUM_ACCEPTABLE_FRAME_NUMBER_OFFSET)
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
            if (NetworkMode == NetworkMode.Client)
            {
                MessageHandlers.DeactivateHandlers(MessageHandlers.GetClientGameplayHandlers(null, null));
            }
            if (NetworkMode == NetworkMode.Server)
            {
                MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers());
            }
        }

        private void GobRemovedFromArena(Arena arena, Gob gob)
        {
            if (NetworkMode != NetworkMode.Server || !gob.IsRelevant) return;
            if (!arena.IsActive) throw new ApplicationException("Removing a gob from an inactive arena during network game");
            var message = new GobDeletionMessage();
            message.GobId = gob.ID;
            NetworkEngine.SendToGameClients(message);
        }
    }
}
