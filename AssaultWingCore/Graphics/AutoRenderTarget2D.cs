using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Graphics
{
    /// <summary>
    /// A <see cref="RenderTarget2D"/> that automatically reallocates the target
    /// when its parameters change, such as the required width and height.
    /// Doesn't support the use of stencil.
    /// </summary>
    public class AutoRenderTarget2D : IDisposable
    {
        public struct CreationData
        {
            public int Width;
            public int Height;
            public DepthStencilState DepthStencilState;
        }

        private GraphicsDevice _graphicsDevice;
        private RenderTarget2D _target;
        private Func<CreationData> _getCreationData;
        private CreationData _oldCreationData;

        public AutoRenderTarget2D(GraphicsDevice graphicsDevice, Func<CreationData> getCreationData)
        {
            _graphicsDevice = graphicsDevice;
            _getCreationData = getCreationData;
        }

        public Texture2D GetTexture()
        {
            return _target;
        }

        public void SetAsRenderTarget()
        {
            var data = _getCreationData();
            if (_target == null || !data.Equals(_oldCreationData))
            {
                _oldCreationData = data;
                if (_target != null) _target.Dispose();
                _target = new RenderTarget2D(_graphicsDevice, data.Width, data.Height, false, SurfaceFormat.Color, DepthFormat.Depth24);
            }
            _graphicsDevice.SetRenderTarget(_target);
            _graphicsDevice.DepthStencilState = data.DepthStencilState;
            _graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1, 0);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_target != null && !_target.IsDisposed)
            {
                _target.Dispose();
                _target = null;
            }
        }

        #endregion
    }
}
