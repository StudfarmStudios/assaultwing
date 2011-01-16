using System;
using Microsoft.Xna.Framework;

namespace AW2.Core
{
    /// <summary>
    /// Represents an operating system window.
    /// </summary>
    public class Window
    {
        private Func<string> _getTitle;
        private Action<string> _setTitle;
        private Func<Rectangle> _getClientBounds;
        private Func<bool> _getFullScreen;

        public string Title { get { return _getTitle(); } set { _setTitle(value); } }
        public Rectangle ClientBounds { get { return _getClientBounds(); } }
        public bool IsFullScreen { get { return _getFullScreen(); } }
        public Action SetWindowed { get; private set; }
        public Action<int, int> SetFullScreen { get; private set; }

        public Window(Func<string> getTitle, Action<string> setTitle, Func<Rectangle> getClientBounds,
            Func<bool> getFullScreen, Action setWindowed, Action<int, int> setFullScreen)
        {
            _getTitle = getTitle;
            _setTitle = setTitle;
            _getClientBounds = getClientBounds;
            _getFullScreen = getFullScreen;
            SetWindowed = setWindowed;
            SetFullScreen = setFullScreen;
        }
    }
}
