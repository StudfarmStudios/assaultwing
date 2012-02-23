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
        private IntroEngine IntroEngine { get; set; }
        protected MenuEngineImpl MenuEngine { get; set; }
        private OverlayDialog OverlayDialog { get; set; }
        private PlayerChat PlayerChat { get; set; }

        public UserControlledLogic(AssaultWing game)
            : base(game)
        {
            MenuEngine = new MenuEngineImpl(Game, 10);
            IntroEngine = new IntroEngine(Game, 11);
            PlayerChat = new PlayerChat(Game, 12);
            OverlayDialog = new OverlayDialog(Game, 20);
            Game.Components.Add(MenuEngine);
            Game.Components.Add(IntroEngine);
            Game.Components.Add(PlayerChat);
            Game.Components.Add(OverlayDialog);
            Game.MenuEngine = MenuEngine;
            Game.PlayerChat = PlayerChat;
            CreateCustomControls(Game);
        }

        public override void Initialize()
        {
            Game.GameState = GameState.Intro;
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
                case GameState.Intro:
                    IntroEngine.Enabled = true;
                    IntroEngine.Visible = true;
                    return true;
                default:
                    return false;
            }
        }

        public override bool TryDisableGameState(GameState value)
        {
            switch (value)
            {
                case GameState.Intro:
                    IntroEngine.Enabled = false;
                    IntroEngine.Visible = false;
                    return true;
                default:
                    return false;
            }
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
    }
}
