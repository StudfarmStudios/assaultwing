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

        public string Title { get { return _getTitle(); } set { _setTitle(value); } }
        public Rectangle ClientBounds { get { return _getClientBounds(); } }

        public Window(Func<string> getTitle, Action<string> setTitle, Func<Rectangle> getClientBounds)
        {
            _getTitle = getTitle;
            _setTitle = setTitle;
            _getClientBounds = getClientBounds;
        }
    }
}
