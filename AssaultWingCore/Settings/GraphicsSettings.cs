using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Settings
{
    public class GraphicsSettings
    {
        public int FullscreenWidth { get; set; }
        public int FullscreenHeight { get; set; }
        public bool IsVerticalSynced { get; set; }
        public bool InGameFullscreen { get; set; }

        public static IEnumerable<Tuple<int, int>> GetDisplayModes()
        {
            var currentMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            var goodAspectRatio = currentMode.AspectRatio;
            // Note: XNA Reach profile limits texture sizes to 2048x2048. This limits maximum screen size
            // because some display effects require creating a texture that covers the screen.
            var HiDefProfile = true;
            var modes = GraphicsAdapter.DefaultAdapter.SupportedDisplayModes[SurfaceFormat.Color]
                .Where(mode => (HiDefProfile || (mode.Height <= 2048 && mode.Width <= 2048))
                    && mode.Width >= 1024 && mode.Height >= 600
                    && Math.Abs(goodAspectRatio - mode.AspectRatio) < 0.1)
                .Select(mode => Tuple.Create(mode.Width, mode.Height));
            if (modes.Any()) return modes;
            return new[] { Tuple.Create(currentMode.Width, currentMode.Height) };
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

        public void Validate()
        {
            if (GetDisplayModes().Contains(Tuple.Create(FullscreenWidth, FullscreenHeight))) return;

            // Find a close match to the requested display mode.
            var mode = GetDisplayModes().OrderBy(size => Math.Abs(size.Item1 * size.Item2 - FullscreenWidth * FullscreenHeight)).First();
            FullscreenWidth = mode.Item1;
            FullscreenHeight = mode.Item2;
        }

        private static Tuple<int, int> GetDefaultFullscreenResolution()
        {
            return GetDisplayModes()
                .OrderByDescending(mode => mode.Item1)
                .First();
        }
    }
}
