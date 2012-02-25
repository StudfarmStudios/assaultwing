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
            Game.GameState = GameState.Initializing;
        }

        public override void FinishArena()
        {
            Game.StopGameplay();
            _clearGameDataWhenEnteringMenus = true;
            var standings = Game.DataEngine.GameplayMode.GetStandings(Game.DataEngine.Spectators).ToArray(); // ToArray takes a copy
            ShowDialog(new GameOverOverlayDialogData(MenuEngine, standings) { GroupName = "Game over" });
        }

        public override void Update()
        {
            if (Game.GameState == GameState.Intro && IntroEngine.Mode == Core.GameComponents.IntroEngine.ModeType.Finished)
            {
                Log.Write("Entering menus");
                Game.ShowMainMenuAndResetGameplay();
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

        public override void ShowEquipMenu()
        {
            if (_clearGameDataWhenEnteringMenus) Game.DataEngine.ClearGameState();
            _clearGameDataWhenEnteringMenus = false;
            MenuEngine.Activate(MenuComponentType.Equip);
            Game.GameState = GameState.Menu;
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
            game.CustomControls.Add(Tuple.Create<Control,Action>(MenuEngine.Controls.Back, Click_MenuBackControl));
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
                        new TriggeredCallback(TriggeredCallback.YES_CONTROL, Game.ShowEquipMenuWhileKeepingGameRunning),
                        new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }));
                    break;
                case NetworkMode.Standalone:
                    dialogData = new CustomOverlayDialogData(MenuEngine,
                        "Quit to Main Menu? (Yes/No)",
                        new TriggeredCallback(TriggeredCallback.YES_CONTROL, Game.ShowMainMenuAndResetGameplay),
                        new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }));
                    break;
                default: throw new ApplicationException();
            }
            ShowDialog(dialogData);
        }

        private void Click_MenuBackControl()
        {
            if (Game.GameState != GameState.Menu && Game.GameState != GameState.GameAndMenu) return;
            if (!MenuEngine.EquipMenu.Active) return;
            Action backToMainMenuImpl = () =>
            {
                MenuEngine.IsReadyToStartArena = false;
                if (MenuEngine.ArenaLoadTask.TaskRunning) MenuEngine.ArenaLoadTask.AbortTask();
                MenuEngine.Game.ShowMainMenuAndResetGameplay();
            };
            if (MenuEngine.Game.NetworkMode == NetworkMode.Standalone)
                backToMainMenuImpl();
            else
                MenuEngine.Game.ShowDialog(new CustomOverlayDialogData(MenuEngine,
                    "Quit network game? (Yes/No)",
                    new TriggeredCallback(TriggeredCallback.YES_CONTROL, backToMainMenuImpl),
                    new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { })));
        }

    }
}
