using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Core.GameComponents;
using AW2.Core.OverlayComponents;
using AW2.Menu;
using AW2.Helpers;

namespace AW2.UI
{
    public class UserControlledLogic : ProgramLogic
    {
        private bool _clearGameDataWhenEnteringMenus;

        private StartupScreen StartupScreen { get; set; }
        private IntroEngine IntroEngine { get; set; }
        protected MenuEngineImpl MenuEngine { get; set; }
        private OverlayDialog OverlayDialog { get; set; }
        private PlayerChat PlayerChat { get; set; }

        private bool EquipMenuActive
        {
            get
            {
                return (Game.GameState == GameState.Menu || Game.GameState == GameState.GameAndMenu)
                    && MenuEngine.EquipMenu.Active;
            }
        }


        public UserControlledLogic(AssaultWing game)
            : base(game)
        {
            StartupScreen = new StartupScreen(Game, -1);
            MenuEngine = new MenuEngineImpl(Game, 10);
            IntroEngine = new IntroEngine(Game, 11);
            PlayerChat = new PlayerChat(Game, 12);
            OverlayDialog = new OverlayDialog(Game, 20);
            Game.Components.Add(StartupScreen);
            Game.Components.Add(MenuEngine);
            Game.Components.Add(IntroEngine);
            Game.Components.Add(PlayerChat);
            Game.Components.Add(OverlayDialog);
            Game.MenuEngine = MenuEngine;
            CreateCustomControls(Game);
        }

        public override void Initialize()
        {
            Game.GameState = GameState.Intro;
        }

        public override void EndRun()
        {
            EnsureArenaLoadingStopped();
            Game.GameState = GameState.Initializing;
        }

        public override void FinishArena()
        {
            EnsureArenaLoadingStopped();
            Game.StopGameplay();
            _clearGameDataWhenEnteringMenus = true;
            var standings = Game.DataEngine.GameplayMode.GetStandings(Game.DataEngine.Spectators).ToArray(); // ToArray takes a copy
            var callback = new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL,
                () => { if (Game.GameState == GameState.GameplayStopped) ShowEquipMenu(); });
            ShowDialog(new GameOverOverlayDialogData(MenuEngine, standings, callback) { GroupName = "Game over" });
        }

        public override void Update()
        {
            if (Game.GameState == GameState.Intro && IntroEngine.Mode == IntroEngine.ModeType.Finished) ShowMainMenuAndResetGameplay();
            if (EquipMenuActive) CheckArenaStart();
            if (Game.ArenaLoadTask.TaskCompleted)
            {
                Game.ArenaLoadTask.FinishTask();
                if (Game.NetworkMode == NetworkMode.Client)
                {
                    Game.MessageHandlers.ActivateHandlers(Game.MessageHandlers.GetClientGameplayHandlers());
                    Game.IsClientAllowedToStartArena = true;
                    Game.StartArenaButStayInMenu();
                }
                else
                    Game.StartArena();
            }
        }

        public override bool TryEnableGameState(GameState value)
        {
            switch (value)
            {
                case GameState.Initializing:
                    StartupScreen.Enabled = true;
                    StartupScreen.Visible = true;
                    return true;
                case GameState.Intro:
                    IntroEngine.Enabled = true;
                    IntroEngine.Visible = true;
                    return true;
                case GameState.Gameplay:
                    Game.LogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PreFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PostFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.GraphicsEngine.Enabled = true;
                    Game.GraphicsEngine.Visible = true;
                    if (Game.NetworkMode != NetworkMode.Standalone) PlayerChat.Enabled = PlayerChat.Visible = true;
                    Game.SoundEngine.PlayMusic(Game.DataEngine.Arena.BackgroundMusic.FileName, Game.DataEngine.Arena.BackgroundMusic.Volume);
                    return true;
                case GameState.GameplayStopped:
                    Game.GraphicsEngine.Enabled = true;
                    Game.GraphicsEngine.Visible = true;
                    if (Game.NetworkMode != NetworkMode.Standalone) PlayerChat.Visible = true;
                    return true;
                default:
                    return false;
            }
        }

        public override bool TryDisableGameState(GameState value)
        {
            switch (value)
            {
                case GameState.Initializing:
                    StartupScreen.Enabled = false;
                    StartupScreen.Visible = false;
                    return true;
                case GameState.Intro:
                    IntroEngine.Enabled = false;
                    IntroEngine.Visible = false;
                    return true;
                case GameState.Gameplay:
                    Game.LogicEngine.Enabled = false;
                    Game.PreFrameLogicEngine.Enabled = false;
                    Game.PostFrameLogicEngine.Enabled = false;
                    Game.GraphicsEngine.Enabled = false;
                    Game.GraphicsEngine.Visible = false;
                    PlayerChat.Enabled = PlayerChat.Visible = false;
                    return true;
                case GameState.GameplayStopped:
                    Game.GraphicsEngine.Enabled = false;
                    Game.GraphicsEngine.Visible = false;
                    PlayerChat.Visible = false;
                    return true;
                default:
                    return false;
            }
        }

        public override void StopServer()
        {
            if (Game.NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot stop server while in mode " + Game.NetworkMode);
            Game.NetworkEngine.MessageHandlers.Clear();
            Game.NetworkEngine.StopServer();
            Game.NetworkMode = NetworkMode.Standalone;
            Game.DataEngine.RemoveAllButLocalSpectators();
        }

        public override void StopClient(string errorOrNull)
        {
            if (Game.NetworkMode != NetworkMode.Client)
                throw new InvalidOperationException("Cannot stop client while in mode " + Game.NetworkMode);
            EnsureArenaLoadingStopped();
            Game.NetworkEngine.MessageHandlers.Clear();
            Game.NetworkEngine.StopClient();
            Game.DataEngine.RemoveAllButLocalSpectators();
            Game.StopGameplay(); // gameplay cannot continue because it's initialized only for a client
            Game.NetworkMode = NetworkMode.Standalone;
            if (errorOrNull != null)
            {
                var dialogData = new CustomOverlayDialogData(MenuEngine,
                    errorOrNull + "\nPress Enter to return to Main Menu.",
                    new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, ShowMainMenuAndResetGameplay));
                ShowDialog(dialogData);
            }
        }

        public override void PrepareArena()
        {
            AW2.Game.Gobs.Wall.WallActivatedCounter = 0;
            foreach (var conn in Game.NetworkEngine.GameClientConnections) conn.PingInfo.AllowLatePingsForAWhile();
            MenuEngine.ProgressBar.Start(Game.DataEngine.Arena.Gobs.OfType<AW2.Game.Gobs.Wall>().Count(), () => AW2.Game.Gobs.Wall.WallActivatedCounter);
            Game.ArenaLoadTask.StartTask(Game.DataEngine.Arena.Reset);
        }

        public override void ShowMainMenuAndResetGameplay()
        {
            Log.Write("Entering menus");
            Game.CutNetworkConnections();
            EnsureArenaLoadingStopped();
            Game.DataEngine.ClearGameState();
            MenuEngine.Activate(MenuComponentType.Main);
            Game.GameState = GameState.Menu;
        }

        public override void ShowEquipMenu()
        {
            if (_clearGameDataWhenEnteringMenus) Game.DataEngine.ClearGameState();
            _clearGameDataWhenEnteringMenus = false;
            MenuEngine.Activate(MenuComponentType.Equip);
            Game.GameState = GameState.Menu;
        }

        private void ShowEquipMenuWhileKeepingGameRunning()
        {
            if (Game.GameState == GameState.Menu) return;
            MenuEngine.Activate(MenuComponentType.Equip);
            Game.GameState = GameState.GameAndMenu;
        }

        public override void ShowDialog(OverlayDialogData dialogData)
        {
            OverlayDialog.Show(dialogData);
        }

        public override void ShowInfoDialog(string text, string groupName = null)
        {
            ShowDialog(new CustomOverlayDialogData(MenuEngine, text,
                new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, () => { })) { GroupName = groupName });
        }

        public override void HideDialog(string groupName = null)
        {
            OverlayDialog.Dismiss(groupName);
        }

        private void CreateCustomControls(AssaultWing game)
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

        private void CheckArenaStart()
        {
            bool okToStart = Game.NetworkMode == NetworkMode.Client
                ? Game.IsClientAllowedToStartArena && MenuEngine.IsReadyToStartArena && MenuEngine.ProgressBar.IsFinished
                : MenuEngine.IsReadyToStartArena;
            if (!okToStart) return;
            MenuEngine.IsReadyToStartArena = false;
            MenuEngine.Deactivate();
            if (Game.NetworkMode == NetworkMode.Client)
                Game.StartArena(); // arena prepared in MessageHandlers.HandleStartGameMessage
            else
            {
                Game.LoadSelectedArena();
                PrepareArena();
            }
        }

        private void EnsureArenaLoadingStopped()
        {
            if (Game.ArenaLoadTask.TaskRunning) Game.ArenaLoadTask.AbortTask();
            MenuEngine.ProgressBar.SkipRemainingSubtasks();
        }

        private void Click_EscapeControl()
        {
            if (Game.GameState != GameState.Gameplay || OverlayDialog.Enabled) return;
            OverlayDialogData dialogData;
            switch (Game.NetworkMode)
            {
                case NetworkMode.Server:
                    dialogData = new CustomOverlayDialogData(MenuEngine,
                        "Finish Arena? (Yes/No)",
                        new TriggeredCallback(TriggeredCallback.YES_CONTROL, Game.FinishArena),
                        new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }));
                    break;
                case NetworkMode.Client:
                    dialogData = new CustomOverlayDialogData(MenuEngine,
                        "Pop by to equip your ship? (Yes/No)",
                        new TriggeredCallback(TriggeredCallback.YES_CONTROL, ShowEquipMenuWhileKeepingGameRunning),
                        new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }));
                    break;
                case NetworkMode.Standalone:
                    dialogData = new CustomOverlayDialogData(MenuEngine,
                        "Quit to Main Menu? (Yes/No)",
                        new TriggeredCallback(TriggeredCallback.YES_CONTROL, ShowMainMenuAndResetGameplay),
                        new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }));
                    break;
                default: throw new ApplicationException();
            }
            ShowDialog(dialogData);
        }

        private void Click_MenuBackControl()
        {
            if (!EquipMenuActive) return;
            Action backToMainMenuImpl = () =>
            {
                MenuEngine.IsReadyToStartArena = false;
                if (Game.ArenaLoadTask.TaskRunning) Game.ArenaLoadTask.AbortTask();
                ShowMainMenuAndResetGameplay();
            };
            if (Game.NetworkMode == NetworkMode.Standalone)
                backToMainMenuImpl();
            else
                MenuEngine.Game.ShowDialog(new CustomOverlayDialogData(MenuEngine,
                    "Quit network game? (Yes/No)",
                    new TriggeredCallback(TriggeredCallback.YES_CONTROL, backToMainMenuImpl),
                    new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { })));
        }
    }
}
