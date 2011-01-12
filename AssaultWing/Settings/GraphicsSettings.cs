using System;

namespace AW2.Settings
{
    public class GraphicsSettings
    {
        private int _fullscreenWidth;
        private int _fullscreenHeight;

        public int FullscreenWidth { get { return _fullscreenWidth; } set { _fullscreenWidth = value; } }
        public int FullscreenHeight { get { return _fullscreenHeight; } set { _fullscreenHeight = value; } }

        public GraphicsSettings()
        {
            FullscreenWidth = 1024;
            FullscreenHeight = 768;
        }
    }
}
