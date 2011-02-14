using System;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Settings
{
    public class GraphicsSettings
    {
        private int _fullscreenWidth;
        private int _fullscreenHeight;
        private bool _isVerticalSynced;

        public int FullscreenWidth { get { return _fullscreenWidth; } set { _fullscreenWidth = value; } }
        public int FullscreenHeight { get { return _fullscreenHeight; } set { _fullscreenHeight = value; } }
        public bool IsVerticalSynced { get { return _isVerticalSynced; } set { _isVerticalSynced = value; } }

        public GraphicsSettings()
        {
            Reset();
        }

        public void Reset()
        {
            FullscreenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            FullscreenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            IsVerticalSynced = false;
        }
    }
}
