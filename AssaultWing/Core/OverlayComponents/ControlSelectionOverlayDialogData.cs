using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Menu;
using AW2.Settings;
using AW2.UI;

namespace AW2.Core.OverlayComponents
{
    /// <summary>
    /// Content and action data for an overlay dialog asking for input from the keyboard
    /// or a game pad.
    /// </summary>
    public class ControlSelectionOverlayDialogData : CustomOverlayDialogData
    {
        private static Keys[] g_ignoredKeys = new[]
        {
            Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6,
            Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12,
            Keys.Escape,
        };
        private static GamePadButtonType[] g_ignoredGamePadButtons = new[]
        {
            GamePadButtonType.Start, GamePadButtonType.Back, GamePadButtonType.BigButton,
        };

        private List<Keys> _keysDownSinceEntry;
        private List<GamePadButtonControlType> _gamePadButtonsDownsinceEntry;
        private Action<IControlType> _returnControl;
        private bool _returned;

        public ControlSelectionOverlayDialogData(MenuEngineImpl menu, string text, Action<IControlType> returnControl)
            : base(menu, text)
        {
            _returnControl = returnControl;
            _keysDownSinceEntry = new List<Keys>(Keyboard.GetState().GetPressedKeys());
            _gamePadButtonsDownsinceEntry =
                (from gamePad in Enumerable.Range(0, 4)
                 from button in Enum.GetValues(typeof(GamePadButtonType)).Cast<GamePadButtonType>()
                 let buttonControlType = new GamePadButtonControlType(gamePad, button)
                 where buttonControlType.GetControl().Pulse
                 select buttonControlType).ToList();
        }

        public override void Update()
        {
            var state = Game.UIEngine.InputState;
            if (state.Keyboard.IsKeyDown(Keys.Escape)) Hide();
            _keysDownSinceEntry.RemoveAll(key => state.Keyboard.IsKeyUp(key));
            foreach (var key in state.Keyboard.GetPressedKeys()) TryReturnKey(key);
            for (int gamePad = 0; gamePad < 4; gamePad++)
            {
                foreach (GamePadButtonType button in Enum.GetValues(typeof(GamePadButtonType)))
                    TryReturnGamePadButton(new GamePadButtonControlType(gamePad, button));
                foreach (GamePadStickType stick in Enum.GetValues(typeof(GamePadStickType)))
                    foreach (GamePadStickDirectionType direction in Enum.GetValues(typeof(GamePadStickDirectionType)))
                        TryReturnControl(new GamePadStickDirectionControlType(gamePad, stick, direction));
            }
        }

        private void TryReturnKey(Keys key)
        {
            if (!g_ignoredKeys.Contains(key) && !_keysDownSinceEntry.Contains(key))
                TryReturnControl(new KeyControlType(key));
        }

        private void TryReturnGamePadButton(GamePadButtonControlType button)
        {
            if (!g_ignoredGamePadButtons.Contains(button.Button) && !_gamePadButtonsDownsinceEntry.Contains(button))
                TryReturnControl(button);
        }

        private void TryReturnControl(IControlType controlType)
        {
            var control = controlType.GetControl();
            control.SetLocalState(Game.UIEngine.PreviousInputState, Game.UIEngine.InputState);
            if (control.Pulse) ReturnControl(controlType);
        }

        private void ReturnControl(IControlType controlType)
        {
            if (_returned) return;
            _returned = true;
            _returnControl(controlType);
            Hide();
        }
    }
}
