using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using Viewport = AW2.Graphics.AWViewport;

namespace AW2.Graphics
{
    public class AWViewportCollection : IEnumerable<Viewport>, IDisposable
    {
        public delegate Viewport ViewportConstructor(int viewportIndex, Rectangle viewportArea);

        private GraphicsDeviceService _graphicsDeviceService;
        private List<Viewport> _items;
        private List<ViewportSeparator> _separators;
        private Point _lastGraphicsDeviceViewportSize;
        private int _viewportCount;
        private ViewportConstructor _viewportConstructor;

        public IEnumerable<ViewportSeparator> Separators { get { return _separators; } }

        private int WindowWidth { get { return _graphicsDeviceService.GraphicsDevice.Viewport.Width; } }
        private int WindowHeight { get { return _graphicsDeviceService.GraphicsDevice.Viewport.Height; } }
        private Point CurrentGraphicsDeviceViewportSize
        {
            get { return new Point(_graphicsDeviceService.GraphicsDevice.Viewport.Width, _graphicsDeviceService.GraphicsDevice.Viewport.Height); }
        }

        #region Public methods

        public AWViewportCollection(GraphicsDeviceService graphicsDeviceService, int viewports, ViewportConstructor viewportConstructor)
        {
            if (viewports < 0) throw new ArgumentException("Nonnegative number of viewports required");
            _graphicsDeviceService = graphicsDeviceService;
            _viewportCount = viewports;
            _viewportConstructor = viewportConstructor;
            _items = new List<AWViewport>();
            _separators = new List<ViewportSeparator>();
            CreateAll();
        }

        public void Dispose()
        {
            foreach (var viewport in _items) viewport.Dispose();
        }

        public IEnumerator<AWViewport> GetEnumerator()
        {
            if (_lastGraphicsDeviceViewportSize != CurrentGraphicsDeviceViewportSize) CreateAll();
            return _items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion Public methods

        #region Private methods

        private void CreateAll()
        {
            if (_viewportCount == 0) return;
            _lastGraphicsDeviceViewportSize = CurrentGraphicsDeviceViewportSize;
            RemoveAllViewports();
            int rows, columns;
            FindOptimalArrangement(_viewportCount, WindowWidth, WindowHeight, out rows, out columns);
            CreateViewports(_viewportCount, _viewportConstructor, rows, columns);
            CreateViewportSeparators(rows, columns);
        }

        private void RemoveAllViewports()
        {
            foreach (var item in _items) item.Dispose();
            _items.Clear();
        }

        /// <summary>
        /// Finds an optimal arrangement by going through viewport arrangements in
        /// different NxM grids.
        /// An optimal arrangement of viewports must meet these conditions:
        /// - they are all equal in size (give or take a pixel),
        /// - they fill up the whole system window.
        /// An optimal arrangement of viewports preferably has this condition:
        /// - each viewport is as wide as tall.
        /// </summary>
        private static void FindOptimalArrangement(int viewports, int windowWidth, int windowHeight, out int bestRows, out int bestColumns)
        {
            float bestAspectRatio = Single.MaxValue;
            bestRows = 1;
            for (int rows = 1; rows <= viewports; ++rows)
            {
                // Only check out grids with cells as many as viewports.
                if (viewports % rows != 0) continue;
                int columns = viewports / rows;
                int viewportWidth = windowWidth / columns;
                int viewportHeight = windowHeight / rows;
                float aspectRatio = (float)viewportHeight / (float)viewportWidth;
                if (CompareAspectRatios(aspectRatio, bestAspectRatio) < 0)
                {
                    bestAspectRatio = aspectRatio;
                    bestRows = rows;
                }
            }
            bestColumns = viewports / bestRows;
        }

        /// <summary>
        /// Compares aspect ratios based on visual appropriateness.
        /// </summary>
        /// In C sense, this method defines an order on aspect ratios, 
        /// where more preferable aspect ratios come before less 
        /// preferable aspect ratios.
        /// <param name="aspectRatio1">One aspect ratio.</param>
        /// <param name="aspectRatio2">Another aspect ratio.</param>
        /// <returns><b>-1</b> if <b>aspectRatio1</b> is more preferable;
        /// <b>0</b> if <b>aspectRatio1</b> is as preferable as <b>aspectRatio2</b>;
        /// <b>1</b> if <b>aspectRatio2</b> is more preferable.</returns>
        public static int CompareAspectRatios(float aspectRatio1, float aspectRatio2)
        {
            float badness1 = aspectRatio1 >= 1.0f
                ? aspectRatio1 - 1.0f
                : 1.0f / aspectRatio1 - 1.0f;
            float badness2 = aspectRatio2 >= 1.0f
                ? aspectRatio2 - 1.0f
                : 1.0f / aspectRatio2 - 1.0f;
            if (badness1 < badness2) return -1;
            if (badness1 > badness2) return 1;
            return 0;
        }

        private void CreateViewports(int viewports, ViewportConstructor viewportConstructor, int bestRows, int bestColumns)
        {
            for (int viewportI = 0; viewportI < viewports; ++viewportI)
            {
                int viewportX = viewportI % bestColumns;
                int viewportY = viewportI / bestColumns;
                int onScreenX1 = WindowWidth * viewportX / bestColumns;
                int onScreenY1 = WindowHeight * viewportY / bestRows;
                int onScreenX2 = WindowWidth * (viewportX + 1) / bestColumns;
                int onScreenY2 = WindowHeight * (viewportY + 1) / bestRows;
                var onScreen = new Rectangle(onScreenX1, onScreenY1, onScreenX2 - onScreenX1, onScreenY2 - onScreenY1);
                var viewport = viewportConstructor(viewportI, onScreen);
                viewport.LoadContent();
                _items.Add(viewport);
            }
        }

        private void CreateViewportSeparators(int bestRows, int bestColumns)
        {
            _separators.Clear();
            for (int i = 1; i < bestColumns; ++i)
                _separators.Add(new ViewportSeparator(true, WindowWidth * i / bestColumns));
            for (int i = 1; i < bestRows; ++i)
                _separators.Add(new ViewportSeparator(false, WindowHeight * i / bestRows));
        }

        #endregion Private methods
    }
}
