using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Settings
{
    public class GraphicsSettings
    {
        private int _fullscreenWidth;
        private int _fullscreenHeight;
        private bool _isVerticalSynced;
        private bool _inGameFullscreen;

        public int FullscreenWidth { get { return _fullscreenWidth; } set { _fullscreenWidth = value; } }
        public int FullscreenHeight { get { return _fullscreenHeight; } set { _fullscreenHeight = value; } }
        public bool IsVerticalSynced { get { return _isVerticalSynced; } set { _isVerticalSynced = value; } }
        public bool InGameFullscreen { get { return _inGameFullscreen; } set { _inGameFullscreen = value; } }

        public static IEnumerable<Tuple<int, int>> GetDisplayModes()
        {
            var goodAspectRatio = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.AspectRatio;
            // Note: XNA Reach profile limits texture sizes to 2048x2048. This limits maximum screen size
            // because some display effects require creating a texture that covers the screen.
            return GraphicsAdapter.DefaultAdapter.SupportedDisplayModes[SurfaceFormat.Color]
                .Where(mode => mode.Height >= 600 && mode.Height <= 2048
                    && mode.Width >= 1024 && mode.Width <= 2048
                    && Math.Abs(goodAspectRatio - mode.AspectRatio) < 0.1)
                .Select(mode => Tuple.Create(mode.Width, mode.Height));
        }

        public GraphicsSettings()
        {
            Reset();
        }

        public void Reset()
        {
            var resolution = GetDefaultFullscreenResolution();
            FullscreenWidth = resolution.Item1;
            FullscreenHeight = resolution.Item2;
            IsVerticalSynced = false;
            InGameFullscreen = true;
        }

        private static Tuple<int, int> GetDefaultFullscreenResolution()
        {
            return GetDisplayModes()
                .OrderByDescending(mode => mode.Item1)
                .First();
        }
    }
}
