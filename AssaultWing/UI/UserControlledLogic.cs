using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Core.GameComponents;
using AW2.Core.OverlayComponents;
using AW2.Menu;
using Microsoft.Xna.Framework.Input;

namespace AW2.UI
{
    public class UserControlledLogic : ProgramLogic
    {
        private IntroEngine IntroEngine { get; set; }
        private MenuEngineImpl MenuEngine { get; set; }
        private OverlayDialog OverlayDialog { get; set; }
        private PlayerChat PlayerChat { get; set; }

        public UserControlledLogic(AssaultWing game)
            : base(game)
        {
            MenuEngine = new MenuEngineImpl(game, 10);
            IntroEngine = new IntroEngine(game, 11);
            PlayerChat = new PlayerChat(game, 12);
            OverlayDialog = new OverlayDialog(game, 20);
            game.Components.Add(MenuEngine);
            game.Components.Add(IntroEngine);
            game.Components.Add(PlayerChat);
            game.Components.Add(OverlayDialog);
            game.MenuEngine = MenuEngine;
            game.PlayerChat = PlayerChat;
            game.IntroEngine = IntroEngine;
            CreateCustomControls(game);
        }

        public override void ShowDialog(OverlayDialogData dialogData)
        {
            OverlayDialog.Show(dialogData);
        }

        /// <summary>
        /// Like calling <see cref="ShowDialog"/> with <see cref="TriggeredCallback.PROCEED_CONTROL"/> that
        /// doesn't do anything.
        /// </summary>
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
