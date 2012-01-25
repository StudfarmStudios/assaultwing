using System;
using Microsoft.Xna.Framework;

namespace AW2.Core
{
    /// <summary>
    /// Represents an operating system window.
    /// </summary>
    public class Window
    {
        public struct WindowImpl
        {
            public Func<string> GetTitle;
            public Action<string> SetTitle;
            public Func<Rectangle> GetClientBounds;
            public Func<bool> GetFullScreen;
            public Action SetWindowed;
            public Action<int, int> SetFullScreen;
            public Func<bool> IsVerticalSynced;
            public Action EnableVerticalSync;
            public Action DisableVerticalSync;
            public Action EnsureCursorHidden;
            public Action EnsureCursorShown;
        }

        public WindowImpl Impl { get; private set; }
        public bool ForceDisableKeypresses { get; set; }

        public event Action<object, char> KeyPress;

        public Window(WindowImpl impl)
        {
            Impl = impl;
        }

        public void OnKeyPress(char keyChar)
        {
            if (KeyPress != null && !ForceDisableKeypresses) KeyPress(this, keyChar);
        }
    }
}
