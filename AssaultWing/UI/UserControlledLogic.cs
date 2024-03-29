using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Core.GameComponents;
using AW2.Core.OverlayComponents;
using AW2.Helpers;
using AW2.Menu;
using AW2.Stats;
using AW2.Game;

namespace AW2.UI
{
    public class UserControlledLogic : ProgramLogic<ClientEvent>
    {
        private const int GAMESTATE_INITIALIZING = 0;
        private const int GAMESTATE_INTRO = 1;
        private const int GAMESTATE_GAMEPLAY = 2;
        private const int GAMESTATE_GAMEPLAY_STOPPED = 3;
        protected const int GAMESTATE_MENU = 4;
        private const int GAMESTATE_GAME_AND_MENU = 5;

        private bool _clearGameDataWhenEnteringMenus;

        public override bool IsGameplay { get { return GameState == GAMESTATE_GAMEPLAY; } }

        private StartupScreen StartupScreen { get; init; }
        private IntroEngine IntroEngine { get; init; }
        protected MenuEngineImpl MenuEngine { get; init; }
        private OverlayDialog OverlayDialog { get; init; }
        private PlayerChat PlayerChat { get; init; }
        private SteamServerComponent SteamServerComponent { get; init; }

        private LocalPilotRankingHandler LocalPilotRankingHandler { get; init; }

        protected bool MainMenuActive { get { return GameState == GAMESTATE_MENU && MenuEngine.MainMenu.Active; } }
        protected bool MainMenuNetworkItemsActive { get { return MainMenuActive && MenuEngine.MainMenu.IsActive(MenuEngine.MainMenu.ItemCollections.NetworkItems); } }


        protected bool EquipMenuActive
        {
            get
            {
                return (GameState == GAMESTATE_MENU || GameState == GAMESTATE_GAME_AND_MENU)
                    && MenuEngine.EquipMenu.Active;
            }
        }

        public UserControlledLogic(AssaultWing<ClientEvent> game)
            : base(game)
        {
            StartupScreen = new StartupScreen(Game, -1);
            MenuEngine = new MenuEngineImpl(Game, 10);
            IntroEngine = new IntroEngine(Game, 11);
            PlayerChat = new PlayerChat(Game, 12);
            OverlayDialog = new OverlayDialog(Game, 20);
            SteamServerComponent = new SteamServerComponent(Game, 0, consoleServer: false);
            Game.Components.Add(StartupScreen);
            Game.Components.Add(MenuEngine);
            Game.Components.Add(IntroEngine);
            Game.Components.Add(PlayerChat);
            Game.Components.Add(OverlayDialog);
            Game.Components.Add(SteamServerComponent);

            if (game.IsSteam)
            {
                LocalPilotRankingHandler = new LocalPilotRankingHandler(game);
                Game.Components.Add(LocalPilotRankingHandler);
                LocalPilotRankingHandler.Enabled = true;
            }

            CreateCustomControls(Game);
            Game.MessageHandlers.GameServerConnectionClosing += Handle_GameServerConnectionClosing;
        }

        public override void Initialize()
        {
            GameState = GAMESTATE_INTRO;
        }

        public override void EndRun()
        {
            EnsureArenaLoadingStopped();
            GameState = GAMESTATE_INITIALIZING;
        }

        public override void FinishArena()
        {
            EnsureArenaLoadingStopped();
            StopGameplay();
            _clearGameDataWhenEnteringMenus = true;
            Game.DataEngine.Standings = Game.DataEngine.GameplayMode.GetStandings(Game.DataEngine.Teams);
            var callback = new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL,
                () => { if (GameState == GAMESTATE_GAMEPLAY_STOPPED) ShowEquipMenu(); });
            ShowDialog(new GameOverOverlayDialogData(MenuEngine, Game.DataEngine.Standings, callback) { GroupName = "Game over" });
            // The game server will soon load the next arena which may delay ping replies for a while.
            if (Game.NetworkEngine.IsConnectedToGameServer) Game.NetworkEngine.GameServerConnection.PingInfo.AllowLatePingsForAWhile();
        }

        public override void Update()
        {
            HandleNetworkingErrors();
            if (GameState == GAMESTATE_INTRO && IntroEngine.Mode == IntroEngine.ModeType.Finished)
                ShowMainMenuAndResetGameplay();
            if (EquipMenuActive) CheckArenaStart();
            if (Game.ArenaLoadTask.TaskCompleted) Handle_ArenaLoadingFinished();
            if (MainMenuActive && Game.NetworkEngine.IsConnectedToGameServer) MenuEngine.Activate(AW2.Menu.MenuComponentType.Equip);
        }

        private void HandleNetworkingErrors()
        {
            if (!Game.NetworkingErrors.Any()) return;
            ShowMainMenuAndResetGameplay();
            while (Game.NetworkingErrors.Any())
            {
                var info = Game.NetworkingErrors.Dequeue();
                Log.Write(info);
                ShowInfoDialog(info, "Networking error");
            }
        }

        public override void StartArena()
        {
            if (GameState != GAMESTATE_GAME_AND_MENU) Game.StartArenaBase();
            GameState = GAMESTATE_GAMEPLAY;
        }

        public override void StartServer()
        {
            SteamServerComponent.Enabled = true;
        }

        public override void StopServer()
        {
            if (Game.NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot stop server while in mode " + Game.NetworkMode);
            Game.NetworkEngine.MessageHandlers.Clear();
            Game.NetworkEngine.StopServer();
            Game.NetworkMode = NetworkMode.Standalone;
            Game.DataEngine.RemoveAllButLocalSpectators();
            SteamServerComponent.Enabled = false;
        }

        public override void StopClient(string errorOrNull)
        {
            if (Game.NetworkMode != NetworkMode.Client)
                throw new InvalidOperationException("Cannot stop client while in mode " + Game.NetworkMode);
            EnsureArenaLoadingStopped();
            Game.NetworkEngine.MessageHandlers.Clear();
            Game.NetworkEngine.StopClient();
            Game.DataEngine.RemoveAllButLocalSpectators();
            StopGameplay(); // gameplay cannot continue because it's initialized only for a client
            Game.NetworkMode = NetworkMode.Standalone;
            if (errorOrNull != null)
                ShowCustomDialog(errorOrNull + "\nPress Enter to return to Main Menu.", null,
                    new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, ShowMainMenuAndResetGameplay));
        }

        public override void PrepareArena(int wallCount)
        {
            AW2.Game.Gobs.Wall.WallActivatedCounter = 0;
            foreach (var conn in Game.NetworkEngine.GameClientConnections) conn.PingInfo.AllowLatePingsForAWhile();
            MenuEngine.ProgressBar.Start(wallCount, () => AW2.Game.Gobs.Wall.WallActivatedCounter);
            Game.ArenaLoadTask.StartTask(Game.DataEngine.Arena.Initialize);
        }

        public override void ShowMainMenuAndResetGameplay()
        {
            Log.Write("Entering menus");
            Game.CutNetworkConnections();
            EnsureArenaLoadingStopped();
            Game.DataEngine.ClearGameState();
            MenuEngine.Activate(MenuComponentType.Main);
            GameState = GAMESTATE_MENU;
        }

        public override void ShowEquipMenu()
        {
            if (_clearGameDataWhenEnteringMenus) Game.DataEngine.ClearGameState();
            _clearGameDataWhenEnteringMenus = false;
            MenuEngine.Activate(MenuComponentType.Equip);
            GameState = GAMESTATE_MENU;
        }

        private void ShowEquipMenuWhileKeepingGameRunning()
        {
            if (GameState == GAMESTATE_MENU) return;
            MenuEngine.Activate(MenuComponentType.Equip);
            GameState = GAMESTATE_GAME_AND_MENU;
        }

        public override void ExternalEvent(ClientEvent e)
        {
            ShowDialog(e.dialogData);
        }

        public void ShowDialog(OverlayDialogData dialogData)
        {
            OverlayDialog.Show(dialogData);
        }

        public override void ShowCustomDialog(string text, string groupName, params TriggeredCallback[] actions)
        {
            ShowDialog(new CustomOverlayDialogData(MenuEngine, text, actions) { GroupName = groupName });
        }

        public override void ShowInfoDialog(string text, string groupName = null)
        {
            ShowCustomDialog(text, groupName, new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, () => { }));
        }

        public override void HideDialog(string groupName = null)
        {
            OverlayDialog.Dismiss(groupName);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Game.NetworkMode, GameState);
        }

        private void CreateCustomControls(AssaultWing<ClientEvent> game)
        {
            var escapeControl = new MultiControl
            {
                new KeyboardKey(Keys.Escape),
                new GamePadButton(0, GamePadButtonType.Start),
                new GamePadButton(0, GamePadButtonType.Back),
            };
            var screenShotControl = new KeyboardKey(Keys.PrintScreen);
            game.CustomControls.Add(Tuple.Create<Control, Action>(escapeControl, Click_EscapeControl));
            game.CustomControls.Add(Tuple.Create<Control, Action>(screenShotControl, Game.TakeScreenShot));
            game.CustomControls.Add(Tuple.Create<Control, Action>(MenuEngine.Controls.Back, Click_MenuBackControl));
        }

        protected override void EnableGameState(int value)
        {
            switch (value)
            {
                case GAMESTATE_INITIALIZING:
                    StartupScreen.Enabled = true;
                    StartupScreen.Visible = true;
                    break;
                case GAMESTATE_INTRO:
                    IntroEngine.Enabled = true;
                    IntroEngine.Visible = true;
                    break;
                case GAMESTATE_GAMEPLAY:
                    Game.LogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PreFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PostFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.GraphicsEngine.Enabled = true;
                    Game.GraphicsEngine.Visible = true;
                    if (Game.NetworkMode != NetworkMode.Standalone) PlayerChat.Enabled = PlayerChat.Visible = true;
                    Game.SoundEngine.PlayMusic(Game.DataEngine.Arena.BackgroundMusic.FileName, Game.DataEngine.Arena.BackgroundMusic.Volume);
                    Game.ApplyInGameGraphicsSettings();
                    break;
                case GAMESTATE_GAMEPLAY_STOPPED:
                    Game.GraphicsEngine.Enabled = true;
                    Game.GraphicsEngine.Visible = true;
                    if (Game.NetworkMode != NetworkMode.Standalone) PlayerChat.Visible = true;
                    Game.ApplyInGameGraphicsSettings();
                    break;
                case GAMESTATE_GAME_AND_MENU:
                    Game.LogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PreFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PostFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    MenuEngine.Enabled = true;
                    MenuEngine.Visible = true;
                    Game.SoundEngine.PlayMusic(Game.DataEngine.Arena.BackgroundMusic.FileName, Game.DataEngine.Arena.BackgroundMusic.Volume);
                    break;
                case GAMESTATE_MENU:
                    MenuEngine.Enabled = true;
                    MenuEngine.Visible = true;
                    Game.SoundEngine.PlayMusic("menu music", 1);

                    if (Game.IsSteam && Game.DataEngine.Spectators.Count == 0)
                    {
                        // Add a stand in for the current player to show rankings.
                        // When entering local game the Spectators list is cleared by AssaultWing.InitializePlayers.
                        Game.InitializePlayers(1);

                    }

                    break;
                default:
                    throw new ApplicationException("Unexpected game state " + value);
            }
        }

        protected override void DisableGameState(int value)
        {
            switch (value)
            {
                case GAMESTATE_INITIALIZING:
                    StartupScreen.Enabled = false;
                    StartupScreen.Visible = false;
                    break;
                case GAMESTATE_INTRO:
                    IntroEngine.Enabled = false;
                    IntroEngine.Visible = false;
                    break;
                case GAMESTATE_GAMEPLAY:
                    Game.LogicEngine.Enabled = false;
                    Game.PreFrameLogicEngine.Enabled = false;
                    Game.PostFrameLogicEngine.Enabled = false;
                    Game.GraphicsEngine.Enabled = false;
                    Game.GraphicsEngine.Visible = false;
                    PlayerChat.Enabled = PlayerChat.Visible = false;
                    break;
                case GAMESTATE_GAMEPLAY_STOPPED:
                    Game.GraphicsEngine.Enabled = false;
                    Game.GraphicsEngine.Visible = false;
                    PlayerChat.Visible = false;
                    break;
                case GAMESTATE_GAME_AND_MENU:
                    Game.LogicEngine.Enabled = false;
                    Game.PreFrameLogicEngine.Enabled = false;
                    Game.PostFrameLogicEngine.Enabled = false;
                    MenuEngine.Enabled = false;
                    MenuEngine.Visible = false;
                    break;
                case GAMESTATE_MENU:
                    MenuEngine.Enabled = false;
                    MenuEngine.Visible = false;
                    break;
                default:
                    throw new ApplicationException("Unexpected game state " + value);
            }
        }

        private void CheckArenaStart()
        {
            bool okToStart = Game.NetworkMode == NetworkMode.Client
                ? Game.IsClientAllowedToStartArena && Game.IsReadyToStartArena && MenuEngine.ProgressBar.IsFinished
                : Game.IsReadyToStartArena;
            if (!okToStart) return;
            Game.IsReadyToStartArena = false;
            MenuEngine.Deactivate();
            if (Game.NetworkMode == NetworkMode.Client)
                Game.StartArena(); // arena prepared in MessageHandlers.HandleStartGameMessage
            else
            {
                Game.LoadSelectedArena();
                PrepareArena(Game.DataEngine.Arena.Gobs.All<AW2.Game.Gobs.Wall>().Count());
            }
        }

        private void StopGameplay()
        {
            switch (GameState)
            {
                case GAMESTATE_GAMEPLAY: GameState = GAMESTATE_GAMEPLAY_STOPPED; break;
                case GAMESTATE_GAME_AND_MENU: GameState = GAMESTATE_MENU; break;
            }
        }

        private void EnsureArenaLoadingStopped()
        {
            if (Game.ArenaLoadTask.TaskRunning) Game.ArenaLoadTask.AbortTask();
            MenuEngine.ProgressBar.SkipRemainingSubtasks();
        }

        private void Click_EscapeControl()
        {
            if (GameState != GAMESTATE_GAMEPLAY || OverlayDialog.Enabled) return;
            string dialogText;
            Action yesCallback;
            switch (Game.NetworkMode)
            {
                case NetworkMode.Server:
                    dialogText = "Finish Arena? (Yes/No)";
                    yesCallback = Game.FinishArena;
                    break;
                case NetworkMode.Client:
                    dialogText = "Pop by to equip your ship? (Yes/No)";
                    yesCallback = ShowEquipMenuWhileKeepingGameRunning;
                    break;
                case NetworkMode.Standalone:
                    dialogText = "Quit to Main Menu? (Yes/No)";
                    yesCallback = ShowMainMenuAndResetGameplay;
                    break;
                default: throw new ApplicationException();
            }
            ShowCustomDialog(dialogText, null,
                new TriggeredCallback(TriggeredCallback.YES_CONTROL, yesCallback),
                new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }));
        }

        private void Click_MenuBackControl()
        {
            if (!EquipMenuActive) return;
            Action backToMainMenuImpl = () =>
            {
                Game.IsReadyToStartArena = false;
                if (Game.ArenaLoadTask.TaskRunning) Game.ArenaLoadTask.AbortTask();
                ShowMainMenuAndResetGameplay();
            };
            if (Game.NetworkMode == NetworkMode.Standalone)
                backToMainMenuImpl();
            else
                ShowCustomDialog("Quit network game? (Yes/No)", null,
                    new TriggeredCallback(TriggeredCallback.YES_CONTROL, backToMainMenuImpl),
                    new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }));
        }

        private void Handle_ArenaLoadingFinished()
        {
            Game.ArenaLoadTask.FinishTask();
            if (Game.NetworkMode == NetworkMode.Client)
            {
                Game.MessageHandlers.ActivateHandlers(Game.MessageHandlers.GetClientGameplayHandlers());
                Game.IsClientAllowedToStartArena = true;
                Game.StartArenaBase();
                GameState = GAMESTATE_GAME_AND_MENU;
            }
            else
                Game.StartArena();
        }

        private void Handle_GameServerConnectionClosing(string info)
        {
            Log.Write("Server is going to close the connection because {0}.", info);
            ShowCustomDialog("Server closed connection because\n" + info + ".", null,
                new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, ShowMainMenuAndResetGameplay));
        }
    }
}
