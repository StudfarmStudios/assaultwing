using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace AW2.Core.OverlayComponents
{
    /// <summary>
    /// Content and action data for an overlay dialog asking for a keypress.
    /// </summary>
    public class KeypressOverlayDialogData : CustomOverlayDialogData
    {
        private static Keys[] g_ignoredKeys = new[]
        {
            Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6,
            Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12,
            Keys.Escape, Keys.PageUp, Keys.PageDown,
        };

        private List<Keys> _keysDownSinceEntry;
        private Action<Keys> _returnKey;

        public KeypressOverlayDialogData(AssaultWing game, string text, Action<Keys> returnKey)
            : base(game, text)
        {
            _returnKey = returnKey;
            _keysDownSinceEntry = new List<Keys>(Keyboard.GetState().GetPressedKeys());
        }

        public override void Update()
        {
            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Escape)) Hide();
            _keysDownSinceEntry.RemoveAll(key => keyState.IsKeyUp(key));
            foreach (var key in keyState.GetPressedKeys())
            {
                if (g_ignoredKeys.Contains(key) || _keysDownSinceEntry.Contains(key)) continue;
                _returnKey(key);
                Hide();
                break;
            }
        }
    }
}
